using System.Text.Json;

namespace Htmxor.Quality;

internal sealed class QualityCommand(
	string repositoryRoot,
	IProcessRunner runner,
	Func<string, string, QualityOptions, QualityPlan> createPlan)
{
	public QualityCommand(string repositoryRoot, IProcessRunner runner)
		: this(repositoryRoot, runner, QualityPlanFactory.Create)
	{
	}
	public async Task ExecuteAsync(
		QualityOptions options,
		CancellationToken cancellationToken = default)
	{
		RepositoryPolicyValidator.Validate(repositoryRoot);
		var repository = await RepositoryEvidence.CaptureAsync(
			repositoryRoot,
			runner,
			cancellationToken);
		Console.WriteLine($"Repository HEAD: {repository.Head}");
		Console.WriteLine($"Dirty worktree: {repository.Dirty.ToString().ToLowerInvariant()}");

		var profileName = options.Action == QualityAction.Fix
			? "fix"
			: options.Profile.ToString().ToLowerInvariant();
		var output = ArtifactDirectory.Reset(repositoryRoot, profileName);
		var plan = createPlan(repositoryRoot, output, options);
		await RunPreparationAsync(plan.Preparation, cancellationToken);

		if (options.Action == QualityAction.Fix)
		{
			Console.WriteLine("Analyzer and style error fixes completed. Whitespace normalization was not run.");
			return;
		}

		if (options.Profile == QualityProfile.Mutation)
		{
			await RunMutationAsync(plan, output, repository, cancellationToken);
			return;
		}

		await RunTestsAsync(plan.Tests, output, options.Profile, repository, cancellationToken);
		if (options.Profile == QualityProfile.Upstream)
		{
			await RunUpstreamAsync(plan, cancellationToken);
		}
	}

	private async Task RunUpstreamAsync(QualityPlan plan, CancellationToken cancellationToken)
	{
		var command = plan.UpstreamMonitor
			?? throw new InvalidOperationException("The upstream profile did not construct a monitor command.");
		var result = await runner.RunAsync(command, cancellationToken);
		if (result.ExitCode != 0)
		{
			var category = result.ExitCode == 1 ? "drift" : "infrastructure";
			var error = new InvalidOperationException($"Upstream {category} result: monitor exited with code {result.ExitCode}.");
			error.Data["ExitCode"] = result.ExitCode;
			throw error;
		}
	}

	private async Task RunPreparationAsync(
		IEnumerable<ProcessCommand> commands,
		CancellationToken cancellationToken)
	{
		foreach (var command in commands)
		{
			await runner.RunAsync(command, cancellationToken);
		}
	}

	private async Task RunTestsAsync(
		IEnumerable<TestCommand> tests,
		string output,
		QualityProfile profile,
		RepositoryEvidence repository,
		CancellationToken cancellationToken)
	{
		var runs = new List<TestRunEvidence>();
		var failures = new List<string>();
		foreach (var test in tests)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(test.TrxPath)!);
			var result = await runner.RunAsync(test.Command, cancellationToken);
			var coverage = CollectCoverageEvidence(test, output, failures);
			CollectTestEvidence(test, result, coverage, runs, failures);
		}

		await QualitySummaryWriter.WriteTestsAsync(
			output,
			profile,
			repository,
			runs,
			failures,
			cancellationToken);
		PrintTestCounts(runs);
		ThrowIfInvalid(failures, "Test verification failed");
	}

	private static void CollectTestEvidence(
		TestCommand test,
		ProcessResult result,
		CoverageReportEvidence? coverage,
		ICollection<TestRunEvidence> runs,
		ICollection<string> failures)
	{
		try
		{
			var run = TrxTestRun.Read(test.TrxPath);
			runs.Add(new(
				test.Project,
				run,
				result.ExitCode,
				test.RequiresCoverage,
				coverage?.CanonicalPath,
				coverage?.CopyCount ?? 0));
			run.EnsureHasTests(test.Project);
		}
		catch (Exception exception) when (exception is
			InvalidOperationException or FormatException or System.Xml.XmlException)
		{
			failures.Add(exception.Message);
		}

		if (result.ExitCode != 0)
		{
			failures.Add($"Tests for '{test.Project}' exited with code {result.ExitCode}.");
		}
	}

	private static CoverageReportEvidence? CollectCoverageEvidence(
		TestCommand test,
		string output,
		ICollection<string> failures)
	{
		if (!test.RequiresCoverage)
		{
			return null;
		}

		try
		{
			var evidence = CoverageReportFile.FindConsistent(Path.GetDirectoryName(test.TrxPath)!);
			return evidence with
			{
				CanonicalPath = Path.GetRelativePath(output, evidence.CanonicalPath).Replace('\\', '/'),
			};
		}
		catch (InvalidOperationException exception)
		{
			failures.Add(exception.Message);
			return null;
		}
	}

	private async Task RunMutationAsync(
		QualityPlan plan,
		string output,
		RepositoryEvidence repository,
		CancellationToken cancellationToken)
	{
		var command = plan.Mutation
			?? throw new InvalidOperationException("The mutation profile did not construct a Stryker command.");
		var process = await runner.RunAsync(command, cancellationToken);
		var failures = new List<string>();
		MutationCharacterization? characterization = null;
		var jsonReportGenerated = false;
		var reportFiles = MutationReportFile.DiscoverRequired(output);
		failures.AddRange(reportFiles.Failures);
		if (reportFiles.Json is not null)
		{
			jsonReportGenerated = true;
			try
			{
				var json = await File.ReadAllTextAsync(reportFiles.Json, cancellationToken);
				characterization = MutationReport.Characterize(json);
				failures.AddRange(characterization.GetValidityFailures());
				await QualitySummaryWriter.WriteCharacterizationAsync(
					output,
					characterization,
					cancellationToken);
			}
			catch (Exception exception) when (exception is InvalidOperationException or JsonException)
			{
				failures.Add(exception.Message);
			}
		}

		if (process.ExitCode != 0)
		{
			failures.Add($"Stryker exited with code {process.ExitCode}.");
		}

		await QualitySummaryWriter.WriteMutationAsync(
			output,
			repository,
			characterization,
			process.ExitCode,
			jsonReportGenerated,
			failures,
			cancellationToken);
		PrintMutationCounts(characterization);
		ThrowIfInvalid(failures, "Mutation verification failed");
	}

	private static void PrintTestCounts(IEnumerable<TestRunEvidence> runs)
	{
		foreach (var run in runs)
		{
			Console.WriteLine(
				$"{run.Project}: {run.Counts.Total} total, {run.Counts.Executed} executed, " +
				$"{run.Counts.Passed} passed, {run.Counts.Failed} failed, {run.Counts.Skipped} skipped, " +
				$"{run.Counts.Errors} error, {run.Counts.TimedOut} timeout.");
		}
	}

	private static void PrintMutationCounts(MutationCharacterization? result)
	{
		if (result is null)
		{
			Console.WriteLine("Mutation counts unavailable because no valid report was parsed.");
			return;
		}

		Console.WriteLine(
			$"Mutation: {result.Generated} generated, {result.Eligible} eligible, {result.Killed} killed, " +
			$"{result.Survived} survived, {result.Skipped} skipped, {result.TimedOut} timeout, " +
			$"{result.Errors} error, {result.Pending} pending.");
	}

	private static void ThrowIfInvalid(IReadOnlyCollection<string> failures, string prefix)
	{
		if (failures.Count > 0)
		{
			throw new InvalidOperationException($"{prefix}: {string.Join(" ", failures)}");
		}
	}
}
