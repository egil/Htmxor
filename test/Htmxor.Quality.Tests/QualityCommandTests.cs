using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class QualityCommandTests
{
	[Theory]
	[InlineData("fast", false)]
	[InlineData("full", false)]
	[InlineData("fast", true)]
	[InlineData("full", true)]
	public async Task ExecuteAsync_overlaps_all_six_boundaries_and_retains_each_result(
		string profile,
		bool failing)
	{
		using var repository = RepositoryPolicyFixture.CreateCurrent();
		QualityPolicyFiles.WriteValid(repository.Path);
		var runner = new ConcurrentTestArtifactRunner(failing);
		var command = new QualityCommand(repository.Path, runner);
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var execution = command.ExecuteAsync(QualityOptions.Parse(["check", "--profile", profile]), deadline.Token);

		var startedBeforeRelease = await runner.ObserveOverlapAsync();
		var exception = await Record.ExceptionAsync(() => execution);

		AssertConcurrentRunOutcome(exception, failing);
		AssertConcurrentRunReceipt(repository.Path, profile, runner, failing);
		Assert.Equal(6, runner.Commands.Count);
		Assert.Equal(6, startedBeforeRelease);
	}

	private static void AssertConcurrentRunOutcome(Exception? exception, bool failing)
	{
		if (!failing)
		{
			Assert.Null(exception);
			return;
		}

		var error = Assert.IsType<InvalidOperationException>(exception);
		Assert.Contains("test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj' discovered 0 and executed 0", error.Message, StringComparison.Ordinal);
		Assert.Contains("test/Htmxor.Samples.Tests/Htmxor.Samples.Tests.csproj' exited with code 7", error.Message, StringComparison.Ordinal);
	}

	private static void AssertConcurrentRunReceipt(
		string repository,
		string profile,
		ConcurrentTestArtifactRunner runner,
		bool failing)
	{
		var output = Path.Combine(repository, "artifacts", "results", profile);
		using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "summary.json")));
		var summary = document.RootElement;
		Assert.Equal(!failing, summary.GetProperty("valid").GetBoolean());
		Assert.Equal(ConcurrentTestArtifactRunner.Head, summary.GetProperty("openingHead").GetString());
		Assert.Equal(ConcurrentTestArtifactRunner.Head, summary.GetProperty("closingHead").GetString());
		Assert.Equal("cleanThroughout", summary.GetProperty("worktreeState").GetString());
		Assert.Equal(failing ? 2 : 0, summary.GetProperty("failures").GetArrayLength());
		var runs = summary.GetProperty("testRuns").EnumerateArray().ToArray();
		Assert.Equal(6, runs.Length);
		Assert.All(runner.Boundaries, boundary => AssertBoundaryReceipt(output, runs, boundary));
		var coverage = Assert.Single(runs, run => run.GetProperty("coverageRequired").GetBoolean() == (profile == "full") &&
			run.GetProperty("project").GetString() == "test/Htmxor.Tests/Htmxor.Tests.csproj");
		Assert.Equal(profile == "full" ? 2 : 0, coverage.GetProperty("coverageReportCopies").GetInt32());
	}

	private static void AssertBoundaryReceipt(
		string output,
		JsonElement[] runs,
		ConcurrentTestArtifactRunner.Boundary boundary)
	{
		var counts = boundary.Counts;
		Assert.Equal(counts, TrxTestRun.Read(Path.Combine(output, boundary.Name, $"{boundary.Name}.trx")));
		var run = Assert.Single(runs, run => run.GetProperty("project").GetString() == boundary.Project &&
			run.GetProperty("total").GetInt32() == counts.Total);
		Assert.Equal(boundary.ExitCode, run.GetProperty("processExitCode").GetInt32());
		Assert.Equal(counts.Executed, run.GetProperty("executed").GetInt32());
		Assert.Equal(counts.Passed, run.GetProperty("passed").GetInt32());
		Assert.Equal(counts.Failed, run.GetProperty("failed").GetInt32());
		Assert.Equal(counts.Skipped, run.GetProperty("skipped").GetInt32());
		Assert.Equal(counts.Errors, run.GetProperty("errors").GetInt32());
		Assert.Equal(counts.TimedOut, run.GetProperty("timedOut").GetInt32());
	}

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
		Assert.Equal(6, testRuns.Length);
		Assert.All(testRuns, run => Assert.Equal(1, run.GetProperty("total").GetInt32()));
		var upstreamMonitor = Assert.Single(testRuns, run => run.GetProperty("project").GetString() == "test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj");
		Assert.False(upstreamMonitor.GetProperty("coverageRequired").GetBoolean());
		Assert.Equal(JsonValueKind.Null, upstreamMonitor.GetProperty("coverageReport").ValueKind);
		Assert.Equal(0, upstreamMonitor.GetProperty("coverageReportCopies").GetInt32());
		var aspNetCore = testRuns.Where(run => run.GetProperty("project").GetString() == "test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj").ToArray();
		Assert.Equal(2, aspNetCore.Length);
		Assert.All(aspNetCore, run =>
		{
			Assert.False(run.GetProperty("coverageRequired").GetBoolean());
			Assert.Equal(JsonValueKind.Null, run.GetProperty("coverageReport").ValueKind);
			Assert.Equal(0, run.GetProperty("coverageReportCopies").GetInt32());
		});
		var samples = Assert.Single(testRuns, run => run.GetProperty("project").GetString() == "test/Htmxor.Samples.Tests/Htmxor.Samples.Tests.csproj");
		Assert.False(samples.GetProperty("coverageRequired").GetBoolean());
		Assert.Equal(JsonValueKind.Null, samples.GetProperty("coverageReport").ValueKind);
		Assert.Equal(0, samples.GetProperty("coverageReportCopies").GetInt32());
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

	private static void WriteValidPolicyFiles(string repositoryRoot) =>
		QualityPolicyFiles.WriteValid(repositoryRoot);

	private static void WriteInvalidPolicy(string repositoryRoot, string invalidFile)
	{
		if (invalidFile == "manifest")
		{
			File.WriteAllText(
				Path.Combine(repositoryRoot, ".config", "dotnet-tools.json"),
				QualityPolicyFiles.ValidManifest.Replace("4.16.0", "4.15.0", StringComparison.Ordinal));
			return;
		}

		var property = invalidFile == "unexpected"
			? "\"test-case-filter\": \"Category!=Browser\""
			: "\"baseline\": { \"enabled\": true }";
		File.WriteAllText(
			Path.Combine(repositoryRoot, "stryker-config.json"),
			QualityPolicyFiles.ValidMutationConfig.Replace(
				"\"concurrency\":1",
				$"\"concurrency\":1,{property}",
				StringComparison.Ordinal));
	}

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
				var output = ProcessCommandArguments.After(command.Arguments, "--output");
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
				var resultsDirectory = ProcessCommandArguments.After(command.Arguments, "--results-directory");
				var logger = ProcessCommandArguments.After(command.Arguments, "--logger");
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
