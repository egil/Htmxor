using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class ProcessRunnerTests
{
	[Fact]
	public async Task RunAsync_executes_a_real_tokenized_process()
	{
		var runner = new ProcessRunner();
		var command = new ProcessCommand("dotnet", Directory.GetCurrentDirectory(), ["--version"]);

		var result = await runner.RunAsync(command);

		Assert.Equal(0, result.ExitCode);
		Assert.False(string.IsNullOrWhiteSpace(result.StandardOutput));
	}

	[Fact]
	public async Task RunAsync_can_retain_a_nonzero_result()
	{
		var runner = new ProcessRunner();
		var command = new ProcessCommand(
			"dotnet",
			Directory.GetCurrentDirectory(),
			["--not-a-real-dotnet-option"],
			EnsureSuccess: false);

		var result = await runner.RunAsync(command);

		Assert.NotEqual(0, result.ExitCode);
	}

	[Fact]
	public async Task RunAsync_writes_output_to_its_log_while_the_process_runs()
	{
		using var directory = new TemporaryDirectory();
		var log = Path.Combine(directory.Path, "nested", "console.log");
		var command = (OperatingSystem.IsWindows()
			? new ProcessCommand("cmd", directory.Path, ["/c", "echo marker& ping -n 6 127.0.0.1 >NUL"])
			: new ProcessCommand("sh", directory.Path, ["-c", "echo marker; sleep 5"])) with { OutputLog = log };

		var run = new ProcessRunner().RunAsync(command);
		var seenWhileRunning = await WaitForMarkerAsync(log, run);

		Assert.True(seenWhileRunning, "The output log did not contain the marker before the process exited.");
		Assert.Equal((await run).StandardOutput, File.ReadAllText(log));
	}

	private static async Task<bool> WaitForMarkerAsync(string log, Task run)
	{
		while (!run.IsCompleted)
		{
			if (File.Exists(log) && ReadShared(log).Contains("marker", StringComparison.Ordinal))
			{
				return !run.IsCompleted;
			}

			await Task.Delay(50);
		}

		return false;
	}

	private static string ReadShared(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
