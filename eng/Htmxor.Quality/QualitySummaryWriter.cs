using System.Text;
using System.Text.Json;

namespace Htmxor.Quality;

internal sealed record TestRunEvidence(
	string Project,
	TrxTestRun Counts,
	int ProcessExitCode,
	bool CoverageRequired,
	string? CoverageReport,
	int CoverageReportCopies);

internal static class QualitySummaryWriter
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
	};

	public static async Task WriteTestsAsync(
		string output,
		QualityProfile profile,
		RepositoryEvidence repository,
		IReadOnlyList<TestRunEvidence> runs,
		IReadOnlyList<string> failures,
		CancellationToken cancellationToken)
	{
		var summary = new
		{
			schemaVersion = 1,
			profile = profile.ToString().ToLowerInvariant(),
			repository.Head,
			repository.Dirty,
			valid = failures.Count == 0,
			failures,
			testRuns = runs.Select(run => new
			{
				run.Project,
				run.ProcessExitCode,
				run.Counts.Total,
				run.Counts.Executed,
				run.Counts.Passed,
				run.Counts.Failed,
				run.Counts.Skipped,
				run.Counts.Errors,
				run.Counts.TimedOut,
				run.CoverageRequired,
				run.CoverageReport,
				run.CoverageReportCopies,
			}),
		};
		await WriteJsonAsync(Path.Combine(output, "summary.json"), summary, cancellationToken);
		await File.WriteAllTextAsync(
			Path.Combine(output, "summary.md"),
			BuildTestMarkdown(profile, repository, runs, failures),
			cancellationToken);
	}

	public static async Task WriteMutationAsync(
		string output,
		RepositoryEvidence repository,
		MutationCharacterization? result,
		int processExitCode,
		bool jsonReportGenerated,
		IReadOnlyList<string> failures,
		CancellationToken cancellationToken)
	{
		var summary = new
		{
			schemaVersion = 1,
			profile = "mutation",
			repository.Head,
			repository.Dirty,
			jsonReportGenerated,
			processExitCode,
			valid = failures.Count == 0,
			qualityFloor = (double?)null,
			qualityFloorReason = "Initial characterization; survivors are reported without an invented score floor.",
			failures,
			result,
		};
		await WriteJsonAsync(Path.Combine(output, "summary.json"), summary, cancellationToken);
		await File.WriteAllTextAsync(
			Path.Combine(output, "summary.md"),
			BuildMutationMarkdown(repository, result, processExitCode, jsonReportGenerated, failures),
			cancellationToken);
	}

	public static Task WriteCharacterizationAsync(
		string output,
		MutationCharacterization characterization,
		CancellationToken cancellationToken) =>
		WriteJsonAsync(
			Path.Combine(output, "characterization.json"),
			characterization,
			cancellationToken);

	private static async Task WriteJsonAsync(
		string path,
		object value,
		CancellationToken cancellationToken)
	{
		var json = JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine;
		await File.WriteAllTextAsync(path, json, cancellationToken);
	}

	private static string BuildTestMarkdown(
		QualityProfile profile,
		RepositoryEvidence repository,
		IReadOnlyList<TestRunEvidence> runs,
		IReadOnlyList<string> failures)
	{
		var text = Header(profile.ToString().ToLowerInvariant(), repository, failures);
		text.AppendLine("| Project | Total | Executed | Passed | Failed | Skipped | Error | Timeout | Coverage |");
		text.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
		foreach (var run in runs)
		{
			text.AppendLine(
				$"| `{run.Project}` | {run.Counts.Total} | {run.Counts.Executed} | " +
				$"{run.Counts.Passed} | {run.Counts.Failed} | {run.Counts.Skipped} | " +
				$"{run.Counts.Errors} | {run.Counts.TimedOut} | {CoverageValue(run)} |");
		}

		return text.ToString();
	}

	private static string CoverageValue(TestRunEvidence run)
	{
		if (!run.CoverageRequired)
		{
			return "not required";
		}

		return run.CoverageReport is null
			? "missing"
			: $"`{run.CoverageReport}` ({run.CoverageReportCopies} fresh copies)";
	}

	private static string BuildMutationMarkdown(
		RepositoryEvidence repository,
		MutationCharacterization? result,
		int processExitCode,
		bool jsonReportGenerated,
		IReadOnlyList<string> failures)
	{
		var text = Header("mutation", repository, failures);
		text.AppendLine($"- Stryker exit: `{processExitCode}`");
		text.AppendLine($"- JSON report generated: `{jsonReportGenerated.ToString().ToLowerInvariant()}`");
		text.AppendLine("- Score floor: none while the exact baseline is being characterized");
		if (result is not null)
		{
			text.AppendLine();
			text.AppendLine("| Generated | Eligible | Killed | Survived | Skipped | Timeout | Error | Pending |");
			text.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
			text.AppendLine(
				$"| {result.Generated} | {result.Eligible} | {result.Killed} | {result.Survived} | " +
				$"{result.Skipped} | {result.TimedOut} | {result.Errors} | {result.Pending} |");
		}

		return text.ToString();
	}

	private static StringBuilder Header(
		string profile,
		RepositoryEvidence repository,
		IReadOnlyCollection<string> failures)
	{
		var text = new StringBuilder();
		text.AppendLine($"# Htmxor {profile} verification");
		text.AppendLine();
		text.AppendLine($"- HEAD: `{repository.Head}`");
		text.AppendLine($"- Dirty worktree: `{repository.Dirty.ToString().ToLowerInvariant()}`");
		text.AppendLine($"- Valid: `{(failures.Count == 0).ToString().ToLowerInvariant()}`");
		foreach (var failure in failures)
		{
			text.AppendLine($"- Failure: {failure}");
		}

		text.AppendLine();
		return text;
	}
}
