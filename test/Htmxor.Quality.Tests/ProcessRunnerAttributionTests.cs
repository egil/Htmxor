using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(QualityConsoleCaptureCollection.Name)]
public sealed class ProcessRunnerAttributionTests
{
	[Fact]
	public async Task Concurrent_test_process_diagnostics_identify_their_project_and_retain_raw_output()
	{
		using var directory = new TemporaryDirectory();
		using var console = new StringWriter();
		var originalOut = Console.Out;
		var originalError = Console.Error;
		var capture = TextWriter.Synchronized(console);
		var commands = new[] { "First.Tests.csproj", "Second.Tests.csproj" }
			.Select(project => new ProcessCommand("dotnet", directory.Path,
				["test", Path.Combine(directory.Path, project), "--no-build", "--no-restore",
					"--blame-hang", "--blame-hang-timeout", "5min"], EnsureSuccess: false)).ToArray();
		ProcessResult[] results;
		Console.SetOut(capture);
		Console.SetError(capture);
		try
		{
			results = await Task.WhenAll(commands.Select(command => new ProcessRunner().RunAsync(command)));
		}
		finally
		{
			Console.SetOut(originalOut);
			Console.SetError(originalError);
		}

		var lines = console.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (var index = 0; index < commands.Length; index++)
		{
			// Missing projects deliberately make MSBuild emit the same generic error for both children.
			Assert.NotEqual(0, results[index].ExitCode);
			Assert.Contains("MSB1009", results[index].StandardOutput, StringComparison.Ordinal);
			var diagnostic = Assert.Single(results[index].StandardOutput.Split('\n'), line => line.Contains("MSB1009", StringComparison.Ordinal)).Trim();
			Assert.StartsWith("MSBUILD : error", diagnostic);
			var project = Path.GetFileName(commands[index].Arguments[1]);
			Assert.Contains(lines, line => line.Contains(diagnostic, StringComparison.Ordinal) && line.Contains(project, StringComparison.Ordinal));
		}
	}
}
