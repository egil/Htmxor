using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class QualityCommandTests
{
	[Theory]
	[InlineData("fix")]
	[InlineData("mutation")]
	public async Task ExecuteAsync_rejects_an_invalid_project_map_before_running_any_process(
		string requestedAction)
	{
		using var repository = RepositoryPolicyFixture.CreateCurrent(
			("test/NewProduct.Tests/NewProduct.Tests.csproj", "production", true));
		WriteValidPolicyFiles(repository.Path);
		var runner = new RecordingProcessRunner();
		var command = new QualityCommand(repository.Path, runner);
		var options = CreateOptions(requestedAction);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => command.ExecuteAsync(options));

		Assert.Contains("must use profile 'tests'", exception.Message, StringComparison.Ordinal);
		Assert.Empty(runner.Commands);
	}

	[Theory]
	[InlineData("fix", "manifest", "version")]
	[InlineData("fix", "config", "baseline")]
	[InlineData("mutation", "manifest", "version")]
	[InlineData("mutation", "config", "baseline")]
	[InlineData("mutation", "unexpected", "unexpected properties")]
	public async Task ExecuteAsync_rejects_invalid_repository_policy_before_dispatch(
		string requestedAction,
		string invalidFile,
		string expected)
	{
		using var repository = RepositoryPolicyFixture.CreateCurrent();
		WriteValidPolicyFiles(repository.Path);
		WriteInvalidPolicy(repository.Path, invalidFile);
		var runner = new RecordingProcessRunner();
		var command = new QualityCommand(repository.Path, runner);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => command.ExecuteAsync(CreateOptions(requestedAction)));

		Assert.Contains(expected, exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Empty(runner.Commands);
	}

	[Fact]
	public async Task ExecuteAsync_retains_json_characterization_when_auxiliary_reports_are_missing()
	{
		using var repository = RepositoryPolicyFixture.CreateCurrent();
		WriteValidPolicyFiles(repository.Path);
		var command = new QualityCommand(repository.Path, new MutationArtifactRunner());

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => command.ExecuteAsync(new(QualityAction.Check, QualityProfile.Mutation)));

		Assert.Contains("mutation-report.html", exception.Message, StringComparison.Ordinal);
		Assert.Contains("mutation-report.md", exception.Message, StringComparison.Ordinal);
		var output = Path.Combine(repository.Path, "artifacts", "results", "mutation");
		using var characterization = JsonDocument.Parse(
			File.ReadAllText(Path.Combine(output, "characterization.json")));
		Assert.Equal(2, characterization.RootElement.GetProperty("generated").GetInt32());
		Assert.Equal(1, characterization.RootElement.GetProperty("killed").GetInt32());
		Assert.Equal(1, characterization.RootElement.GetProperty("survived").GetInt32());
		using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "summary.json")));
		Assert.True(summary.RootElement.GetProperty("jsonReportGenerated").GetBoolean());
		Assert.False(summary.RootElement.GetProperty("valid").GetBoolean());
		var failures = summary.RootElement.GetProperty("failures")
			.EnumerateArray()
			.Select(failure => failure.GetString()!)
			.ToArray();
		Assert.Contains(failures, failure => failure.Contains("mutation-report.html", StringComparison.Ordinal));
		Assert.Contains(failures, failure => failure.Contains("mutation-report.md", StringComparison.Ordinal));
	}

	[Fact]
	public async Task ExecuteAsync_full_retains_trx_counts_when_coverage_is_missing()
	{
		using var repository = RepositoryPolicyFixture.CreateCurrent(
			("test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj", "tests", true));
		WriteValidPolicyFiles(repository.Path);
		var command = new QualityCommand(repository.Path, new TestArtifactRunner());

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => command.ExecuteAsync(new(QualityAction.Check, QualityProfile.Full)));

		Assert.Contains("coverage.cobertura.xml", exception.Message, StringComparison.Ordinal);
		var summaryPath = Path.Combine(
			repository.Path,
			"artifacts",
			"results",
			"full",
			"summary.json");
		using var summary = JsonDocument.Parse(File.ReadAllText(summaryPath));
		var testRuns = summary.RootElement.GetProperty("testRuns").EnumerateArray().ToArray();
		Assert.Equal(2, testRuns.Length);
		Assert.All(testRuns, run => Assert.Equal(1, run.GetProperty("total").GetInt32()));
		var htmxor = Assert.Single(testRuns, run => run.GetProperty("project").GetString() == "test/Htmxor.Tests/Htmxor.Tests.csproj");
		Assert.True(htmxor.GetProperty("coverageRequired").GetBoolean());
		Assert.Equal(JsonValueKind.Null, htmxor.GetProperty("coverageReport").ValueKind);
		Assert.Equal(0, htmxor.GetProperty("coverageReportCopies").GetInt32());
		Assert.Contains(
			summary.RootElement.GetProperty("failures").EnumerateArray(),
			failure => failure.GetString()!.Contains("coverage.cobertura.xml", StringComparison.Ordinal));
	}

	private static QualityOptions CreateOptions(string requestedAction) =>
		requestedAction == "fix"
			? new(QualityAction.Fix, QualityProfile.Fast)
			: new(QualityAction.Check, QualityProfile.Mutation);

	private static void WriteValidPolicyFiles(string repositoryRoot)
	{
		var directory = Path.Combine(repositoryRoot, ".config");
		Directory.CreateDirectory(directory);
		File.WriteAllText(
			Path.Combine(directory, "dotnet-tools.json"),
			ValidManifest);
		File.WriteAllText(
			Path.Combine(repositoryRoot, "stryker-config.json"),
			ValidMutationConfig);
	}

	private static void WriteInvalidPolicy(string repositoryRoot, string invalidFile)
	{
		if (invalidFile == "manifest")
		{
			File.WriteAllText(
				Path.Combine(repositoryRoot, ".config", "dotnet-tools.json"),
				ValidManifest.Replace("4.16.0", "4.15.0", StringComparison.Ordinal));
			return;
		}

		var property = invalidFile == "unexpected"
			? "\"test-case-filter\": \"Category!=Browser\""
			: "\"baseline\": { \"enabled\": true }";
		File.WriteAllText(
			Path.Combine(repositoryRoot, "stryker-config.json"),
			ValidMutationConfig.Replace(
				"\"concurrency\":1",
				$"\"concurrency\":1,{property}",
				StringComparison.Ordinal));
	}

	private const string ValidManifest =
		"""
		{"version":1,"isRoot":true,"tools":{"dotnet-stryker":{"version":"4.16.0","commands":["dotnet-stryker"],"rollForward":false}}}
		""";

	private const string ValidMutationConfig =
		"""
		{"stryker-config":{"project":"src/Htmxor/Htmxor.csproj","configuration":"Release","reporters":["progress","json","html","markdown"],"report-file-name":"mutation-report","test-runner":"vstest","coverage-analysis":"perTest","additional-timeout":30000,"concurrency":1}}
		""";

	private sealed class RecordingProcessRunner : IProcessRunner
	{
		public List<ProcessCommand> Commands { get; } = [];

		public Task<ProcessResult> RunAsync(
			ProcessCommand command,
			CancellationToken cancellationToken = default)
		{
			Commands.Add(command);
			return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
		}
	}

	private sealed class MutationArtifactRunner : IProcessRunner
	{
		public Task<ProcessResult> RunAsync(
			ProcessCommand command,
			CancellationToken cancellationToken = default)
		{
			if (command.FileName == "git")
			{
				var output = command.Arguments.Contains("rev-parse", StringComparer.Ordinal)
					? "0123456789abcdef0123456789abcdef01234567" + Environment.NewLine
					: string.Empty;
				return Task.FromResult(new ProcessResult(0, output, string.Empty));
			}

			if (command.Arguments.Contains("dotnet-stryker", StringComparer.Ordinal))
			{
				var arguments = command.Arguments.ToArray();
				var outputIndex = Array.IndexOf(arguments, "--output");
				var output = arguments[outputIndex + 1];
				Directory.CreateDirectory(output);
				File.WriteAllText(
					Path.Combine(output, "mutation-report.json"),
					"{\"files\":{\"source\":{\"mutants\":[{\"status\":\"Killed\"},{\"status\":\"Survived\"}]}}}");
			}

			return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
		}
	}

	private sealed class TestArtifactRunner : IProcessRunner
	{
		public Task<ProcessResult> RunAsync(
			ProcessCommand command,
			CancellationToken cancellationToken = default)
		{
			if (command.FileName == "git")
			{
				var output = command.Arguments.Contains("rev-parse", StringComparer.Ordinal)
					? "0123456789abcdef0123456789abcdef01234567" + Environment.NewLine
					: string.Empty;
				return Task.FromResult(new ProcessResult(0, output, string.Empty));
			}

			if (command.Arguments.Contains("--logger", StringComparer.Ordinal))
			{
				var arguments = command.Arguments.ToArray();
				var resultsDirectory = arguments[Array.IndexOf(arguments, "--results-directory") + 1];
				var logger = arguments[Array.IndexOf(arguments, "--logger") + 1];
				var fileName = logger[(logger.IndexOf('=') + 1)..];
				Directory.CreateDirectory(resultsDirectory);
				File.WriteAllText(
					Path.Combine(resultsDirectory, fileName),
					"<TestRun><ResultSummary><Counters total=\"1\" executed=\"1\" passed=\"1\" failed=\"0\" notExecuted=\"0\" error=\"0\" timeout=\"0\" /></ResultSummary></TestRun>");
			}

			return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
		}
	}
}
