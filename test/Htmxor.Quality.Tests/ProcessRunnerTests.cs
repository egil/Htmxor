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
		var command = new ProcessCommand("dotnet", Directory.GetCurrentDirectory(), ["--version"]) { OutputLog = log };

		var result = await new ProcessRunner().RunAsync(command);

		Assert.Equal(result.StandardOutput, File.ReadAllText(log));
	}
}
