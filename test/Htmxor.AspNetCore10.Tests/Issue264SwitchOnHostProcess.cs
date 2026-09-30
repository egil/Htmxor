using System.Collections.Concurrent;
using System.Diagnostics;

namespace Htmxor.AspNetCore10;

// Spawns test/Htmxor.AspNetCore10.SwitchOnHost as a separate OS process, one of three modes: "stock",
// "candidate" (both switch-on), or "candidate-switch-off" -- the paired oracle the owner's htmx decision on
// #264 requires (https://github.com/egil/Htmxor/issues/264#issuecomment-5901861030). That host's own csproj
// bakes Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException into its
// runtimeconfig.json, applied by the .NET host before Main runs; "candidate-switch-off" then flips it back off
// itself, before any request. On net11.0, HttpNavigationManager reads the switch into a `static readonly`
// field the first time it initializes in the process, so an in-process toggle after any earlier navigation
// cannot take effect; a process that starts with the switch already settled is the only way to prove either
// value there. This test project uses the same mechanism on net10.0 too, so all targets and modes share one
// isolation story and the switch never leaks into any other test's process.
public sealed class Issue264SwitchOnHostProcess : IAsyncDisposable
{
	private const string ListeningPrefix = "LISTENING ";
	private const string SwitchPrefix = "SWITCH ";

	private readonly Process process;
	private readonly ConcurrentQueue<string> outputLines;

	private Issue264SwitchOnHostProcess(
		Process process, ConcurrentQueue<string> outputLines, Uri baseAddress, bool observedSwitch)
	{
		this.process = process;
		this.outputLines = outputLines;
		BaseAddress = baseAddress;
		ObservedSwitch = observedSwitch;
		Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = baseAddress };
	}

	public Uri BaseAddress { get; }

	public HttpClient Client { get; }

	// The value this host itself observed through AppContext.TryGetSwitch, the same API
	// HttpNavigationManager reads. The host refuses to start (see Program.cs) unless this matches what its
	// mode requires, so a caller that gets this far already has that proof; exposing it lets tests assert it
	// too, rather than trusting a silent precondition.
	public bool ObservedSwitch { get; }

	public static async Task<Issue264SwitchOnHostProcess> StartAsync(string mode)
	{
		var assemblyPath = ResolveSwitchOnHostAssemblyPath();
		var process = new Process { StartInfo = CreateStartInfo(assemblyPath, mode), EnableRaisingEvents = true };
		var outputLines = new ConcurrentQueue<string>();
		var errorLines = new ConcurrentQueue<string>();
		var signals = new StartupSignals();
		WireHandlers(process, outputLines, errorLines, signals);

		if (!process.Start())
		{
			throw new InvalidOperationException($"Failed to start the Issue #264 '{mode}' switch-on host process.");
		}

		// Holding this open, and only ever closing or killing it from this side, is what lets the child use
		// stdin EOF as its own "the parent is gone" signal (see Program.cs), instead of leaking a process when
		// this side crashes or is killed without a clean DisposeAsync.
		_ = process.StandardInput;
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		var (baseAddress, observedSwitch) = await WaitForStartupAsync(process, mode, signals, errorLines);
		return new Issue264SwitchOnHostProcess(process, outputLines, baseAddress, observedSwitch);
	}

	public Task<HttpResponseMessage> ReleaseStreamingNavigateAsync() =>
		Client.PostAsync("/issue-264/streaming/release-navigate", content: null);

	public Task<HttpResponseMessage> ReleaseStreamingResumeAsync() =>
		Client.PostAsync("/issue-264/streaming/release-resume", content: null);

	// Drains every line the host has written to its own stdout since the last drain. Each switch-on test
	// method calls this right after issuing its request, so a line reporting
	// TaskScheduler.UnobservedTaskException (see Program.cs) is attributed to the request that caused it,
	// never to an unrelated later test.
	public async Task<IReadOnlyList<string>> DrainRecentOutputAsync()
	{
		// The GC-forcing middleware in Program.cs runs inside the same request pipeline the client is waiting
		// on, so its stdout line is normally already queued before the HTTP response finishes. This bounded
		// poll only guards against pipe-buffering latency between the child process and this reader.
		var deadline = DateTime.UtcNow.AddMilliseconds(500);
		while (outputLines.IsEmpty && DateTime.UtcNow < deadline)
		{
			await Task.Delay(25);
		}

		var lines = new List<string>();
		while (outputLines.TryDequeue(out var line))
		{
			lines.Add(line);
		}

		return lines;
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		if (!process.HasExited)
		{
			process.Kill(entireProcessTree: true);
		}

		await process.WaitForExitAsync();
		process.Dispose();
	}

	private static ProcessStartInfo CreateStartInfo(string assemblyPath, string mode)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = Path.GetDirectoryName(assemblyPath),
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			UseShellExecute = false,
		};
		startInfo.ArgumentList.Add("exec");
		startInfo.ArgumentList.Add(assemblyPath);
		startInfo.ArgumentList.Add(mode);
		return startInfo;
	}

	private sealed class StartupSignals
	{
		public TaskCompletionSource<Uri> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource<bool> Switch { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	private static void WireHandlers(
		Process process, ConcurrentQueue<string> outputLines, ConcurrentQueue<string> errorLines, StartupSignals signals)
	{
		process.OutputDataReceived += (_, dataArgs) => OnOutputLine(dataArgs.Data, outputLines, signals);
		process.ErrorDataReceived += (_, dataArgs) => OnErrorLine(dataArgs.Data, errorLines);
	}

	private static void OnOutputLine(string? line, ConcurrentQueue<string> outputLines, StartupSignals signals)
	{
		if (line is null)
		{
			return;
		}

		outputLines.Enqueue(line);
		if (line.StartsWith(ListeningPrefix, StringComparison.Ordinal))
		{
			signals.Ready.TrySetResult(new Uri(line[ListeningPrefix.Length..], UriKind.Absolute));
		}
		else if (line.StartsWith(SwitchPrefix, StringComparison.Ordinal))
		{
			signals.Switch.TrySetResult(bool.Parse(line[SwitchPrefix.Length..]));
		}
	}

	private static void OnErrorLine(string? line, ConcurrentQueue<string> errorLines)
	{
		if (line is not null)
		{
			errorLines.Enqueue(line);
		}
	}

	private static async Task<(Uri BaseAddress, bool ObservedSwitch)> WaitForStartupAsync(
		Process process, string mode, StartupSignals signals, ConcurrentQueue<string> errorLines)
	{
		var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		process.Exited += (_, _) => exited.TrySetResult();
		var ready = signals.Ready.Task;
		var timeout = Task.Delay(TimeSpan.FromSeconds(30));
		var completed = await Task.WhenAny(ready, exited.Task, timeout);
		if (completed == ready)
		{
			// The switch line is always written before the listening line (see Program.cs), so it is already
			// available the moment the listening line arrives.
			return (await ready, await signals.Switch.Task);
		}

		if (!process.HasExited)
		{
			process.Kill(entireProcessTree: true);
		}

		var reason = completed == exited.Task
			? $"exited early with code {process.ExitCode}"
			: "did not report a listening address within 30s";
		throw new InvalidOperationException(
			$"The Issue #264 '{mode}' switch-on host {reason}. " +
			$"Stderr:{Environment.NewLine}{string.Join(Environment.NewLine, errorLines)}");
	}

	private static string ResolveSwitchOnHostAssemblyPath()
	{
		// Htmxor.AspNetCore10.SwitchOnHost.csproj is referenced with ReferenceOutputAssembly="false": it must
		// build alongside this test project (so its output exists), but it is invoked as a separate process,
		// never linked in, so its own AppContext switch never applies to this test process.
		var testBinDirectory = AppContext.BaseDirectory.TrimEnd(
			Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var targetFramework = Path.GetFileName(testBinDirectory);
		var configurationDirectory = RequireParentDirectory(testBinDirectory);
		var configuration = Path.GetFileName(configurationDirectory);
		var testProjectBinDirectory = RequireParentDirectory(configurationDirectory);
		var testProjectDirectory = RequireParentDirectory(testProjectBinDirectory);
		var testDirectory = RequireParentDirectory(testProjectDirectory);
		var hostAssemblyPath = Path.Combine(
			testDirectory,
			"Htmxor.AspNetCore10.SwitchOnHost",
			"bin",
			configuration,
			targetFramework,
			"Htmxor.AspNetCore10.SwitchOnHost.dll");
		return File.Exists(hostAssemblyPath)
			? hostAssemblyPath
			: throw new InvalidOperationException(
				$"Expected the Issue #264 switch-on host at '{hostAssemblyPath}'. Build Htmxor.sln so " +
				"Htmxor.AspNetCore10.SwitchOnHost.csproj produces a matching-configuration, matching-framework output.");
	}

	private static string RequireParentDirectory(string path) =>
		Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Cannot resolve a parent directory above '{path}'.");
}
