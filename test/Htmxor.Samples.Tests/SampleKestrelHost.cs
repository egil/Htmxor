using System.Diagnostics;

namespace Htmxor.Samples.Tests;

internal sealed class SampleKestrelHost : IAsyncDisposable
{
	private const string ListeningPrefix = "Now listening on: ";
	private readonly Process process;

	private SampleKestrelHost(Process process, Uri address)
	{
		this.process = process;
		Address = address;
		Client = new HttpClient { BaseAddress = address };
	}

	public Uri Address { get; }

	public HttpClient Client { get; }

	public static async Task<SampleKestrelHost> StartAsync(string projectPath, IReadOnlyDictionary<string, string>? environment = null)
	{
		var address = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
		var output = new List<string>();
		var elapsed = Stopwatch.StartNew();
		var process = Process.Start(CreateStartInfo(projectPath, environment)) ?? throw new InvalidOperationException($"Could not start {projectPath}.");
		process.OutputDataReceived += (_, eventArgs) => ObserveOutput(eventArgs.Data, address, output, elapsed);
		process.ErrorDataReceived += (_, eventArgs) => ObserveOutput(eventArgs.Data, address, output, elapsed);
		process.EnableRaisingEvents = true;
		process.Exited += (_, _) => address.TrySetException(new InvalidOperationException($"{projectPath} stopped before Kestrel started: {string.Join(Environment.NewLine, output)}"));
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		try
		{
			return new SampleKestrelHost(process, await WaitForAddressAsync(address.Task, process, projectPath, output, elapsed));
		}
		catch
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}

			await process.WaitForExitAsync();
			process.Dispose();
			throw;
		}
	}

	private static async Task<Uri> WaitForAddressAsync(Task<Uri> address, Process process, string projectPath, List<string> output, Stopwatch elapsed)
	{
		try
		{
			return await address.WaitAsync(TimeSpan.FromSeconds(30));
		}
		catch (TimeoutException exception)
		{
			var state = process.HasExited ? $"exited (code {process.ExitCode})" : "running";
			string capturedOutput;
			lock (output)
			{
				capturedOutput = string.Join(Environment.NewLine, output);
			}

			throw new TimeoutException(
				$"Timed out starting {projectPath} after {elapsed.Elapsed.TotalSeconds:F3}s (readiness limit: 30s). " +
				$"Process {process.Id}: {state}. " +
				$"ThreadPool.ThreadCount={ThreadPool.ThreadCount}, PendingWorkItemCount={ThreadPool.PendingWorkItemCount}, CompletedWorkItemCount={ThreadPool.CompletedWorkItemCount}." +
				$"{Environment.NewLine}Captured sample output (timestamps are observation times):{Environment.NewLine}{capturedOutput}", exception);
		}
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

	private static ProcessStartInfo CreateStartInfo(string projectPath, IReadOnlyDictionary<string, string>? environment)
	{
		var projectDirectory = Path.GetDirectoryName(Path.Combine(FindRepositoryRoot(), projectPath))!;
		var assemblyPath = Path.Combine(projectDirectory, "bin", "Release", "net10.0", Path.GetFileNameWithoutExtension(projectPath) + ".dll");
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = projectDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		startInfo.ArgumentList.Add(assemblyPath);
		startInfo.ArgumentList.Add("--urls");
		startInfo.ArgumentList.Add("http://127.0.0.1:0");
		foreach (var (key, value) in environment ?? new Dictionary<string, string>())
		{
			startInfo.Environment[key] = value;
		}

		return startInfo;
	}

	private static void ObserveOutput(string? line, TaskCompletionSource<Uri> address, List<string> output, Stopwatch elapsed)
	{
		if (line is { } value && value.IndexOf(ListeningPrefix, StringComparison.Ordinal) is var prefix && prefix >= 0)
		{
			address.TrySetResult(new Uri(value[(prefix + ListeningPrefix.Length)..]));
		}

		if (line is not null)
		{
			lock (output)
			{
				output.Add($"[{DateTimeOffset.UtcNow:O} +{elapsed.Elapsed.TotalSeconds:F3}s] {line}");
			}
		}
	}

	private static string FindRepositoryRoot()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "Htmxor.sln")))
			{
				return directory.FullName;
			}
		}

		throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
	}
}
