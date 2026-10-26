using System.Diagnostics;

namespace Htmxor.Samples.Tests;

internal sealed class SampleKestrelHost : IAsyncDisposable
{
	private const string ListeningPrefix = "Now listening on: ";
	private readonly Process process;
	private readonly Thread[] outputReaders;

	private SampleKestrelHost(Process process, Thread[] outputReaders, Uri address)
	{
		this.process = process;
		this.outputReaders = outputReaders;
		Address = address;
		Client = new HttpClient { BaseAddress = address };
	}

	public Uri Address { get; }

	public HttpClient Client { get; }

	public static Task<SampleKestrelHost> StartAsync(string projectPath, IReadOnlyDictionary<string, string>? environment = null)
	{
		var completion = new TaskCompletionSource<SampleKestrelHost>(TaskCreationOptions.RunContinuationsAsynchronously);
		// Pipe reads, the deadline and the returned task must progress even when the test pool is saturated.
		var coordinator = new Thread(() =>
		{
			try
			{
				completion.SetResult(Start(projectPath, environment));
			}
			catch (Exception exception)
			{
				completion.SetException(exception);
			}
		}) { IsBackground = true, Name = "Sample startup" };
		coordinator.Start();
		return completion.Task;
	}

	private static SampleKestrelHost Start(string projectPath, IReadOnlyDictionary<string, string>? environment)
	{
		var address = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
		var output = new List<string>();
		var elapsed = Stopwatch.StartNew();
		var process = Process.Start(CreateStartInfo(projectPath, environment)) ?? throw new InvalidOperationException($"Could not start {projectPath}.");
		var readers = new List<Thread>();
		var remainingReaders = 2;
		try
		{
			foreach (var stream in new[] { process.StandardOutput, process.StandardError })
			{
				var reader = new Thread(() =>
				{
					ReadOutput(stream, address, output, elapsed);
					if (Interlocked.Decrement(ref remainingReaders) == 0)
					{
						address.TrySetException(new InvalidOperationException($"{projectPath} closed its output before Kestrel started: {CapturedOutput(output)}"));
					}
				}) { IsBackground = true, Name = "Sample output" };
				reader.Start();
				readers.Add(reader);
			}

			return new SampleKestrelHost(process, readers.ToArray(), WaitForAddress(address.Task, process, projectPath, output, elapsed));
		}
		catch
		{
			Stop(process, readers);
			throw;
		}
	}

	private static Uri WaitForAddress(Task<Uri> address, Process process, string projectPath, List<string> output, Stopwatch elapsed)
	{
		if (Task.WaitAny([address], TimeSpan.FromSeconds(30)) >= 0)
		{
			return address.GetAwaiter().GetResult();
		}

		var state = process.HasExited ? $"exited (code {process.ExitCode})" : "running";
		throw new TimeoutException(
			$"Timed out starting {projectPath} after {elapsed.Elapsed.TotalSeconds:F3}s (readiness limit: 30s). " +
			$"Process {process.Id}: {state}. " +
			$"ThreadPool.ThreadCount={ThreadPool.ThreadCount}, PendingWorkItemCount={ThreadPool.PendingWorkItemCount}, CompletedWorkItemCount={ThreadPool.CompletedWorkItemCount}." +
			$"{Environment.NewLine}Captured sample output (timestamps are observation times):{Environment.NewLine}{CapturedOutput(output)}");
	}

	private static string CapturedOutput(List<string> output)
	{
		lock (output)
		{
			return string.Join(Environment.NewLine, output);
		}
	}

	private static void ReadOutput(StreamReader stream, TaskCompletionSource<Uri> address, List<string> output, Stopwatch elapsed)
	{
		try
		{
			while (stream.ReadLine() is { } line)
			{
				ObserveOutput(line, address, output, elapsed);
			}
		}
		catch (Exception exception)
		{
			address.TrySetException(exception);
		}
	}

	public ValueTask DisposeAsync()
	{
		Client.Dispose();
		Stop(process, outputReaders);
		return ValueTask.CompletedTask;
	}

	private static void Stop(Process process, IEnumerable<Thread> readers)
	{
		if (!process.HasExited)
		{
			process.Kill(entireProcessTree: true);
		}

		process.WaitForExit();
		foreach (var reader in readers)
		{
			reader.Join();
		}

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

	private static void ObserveOutput(string line, TaskCompletionSource<Uri> address, List<string> output, Stopwatch elapsed)
	{
		lock (output)
		{
			output.Add($"[{DateTimeOffset.UtcNow:O} +{elapsed.Elapsed.TotalSeconds:F3}s] {line}");
		}

		var prefix = line.IndexOf(ListeningPrefix, StringComparison.Ordinal);
		if (prefix >= 0)
		{
			address.TrySetResult(new Uri(line[(prefix + ListeningPrefix.Length)..]));
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
