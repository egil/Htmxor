using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

// Console.SetOut mutates process-global state. Every test that redirects it must share one
// serialized collection so no concurrently running test observes, or restores over, a swap it
// did not make.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class QualityConsoleCaptureCollection
{
	public const string Name = "Quality console capture";
}

/// <summary>
/// Protects https://github.com/egil/Htmxor/issues/234: a quality profile receipt must describe
/// the window it actually measured, not just the tree it sampled before the run began. The
/// worktree-state comparison follows the decision recorded at
/// https://github.com/egil/Htmxor/issues/234#issuecomment-5854112160: compare HEAD and the full
/// porcelain text between the opening and closing captures, not the `dirty` booleans, so a tree
/// that was already dirty and changes further during the run is still reported as changed.
/// </summary>
[Collection(QualityConsoleCaptureCollection.Name)]
public sealed class RepositoryEvidenceWindowTests
{
	private const string OpeningHead = "0123456789abcdef0123456789abcdef01234567";
	private const string ClosingHead = "abcdefabcdefabcdefabcdefabcdefabcdefabcd";
	private const string CleanStatus = "";
	private const string DirtyStatus = " M docs/agents/testing.md\n";
	private const string DirtyStatusFurtherChanged = " M docs/agents/testing.md\n M eng/Htmxor.Quality/QualityCommand.cs\n";

	public static TheoryData<string, string, string, string, string, string> WorktreeStates() => new()
	{
		// Unchanged tree, either state: opening and closing agree on both HEAD and porcelain text.
		{ OpeningHead, OpeningHead, CleanStatus, CleanStatus, "cleanThroughout", "clean throughout" },
		{ OpeningHead, OpeningHead, DirtyStatus, DirtyStatus, "dirtyBeforeRun", "dirty before the run began" },
		// The issue's own reproduction: HEAD never moves, a file is edited mid-run.
		{ OpeningHead, OpeningHead, CleanStatus, DirtyStatus, "changedDuringRun", "changed during the run" },
		// HEAD moves while the tree stays reported clean at both samples.
		{ OpeningHead, ClosingHead, CleanStatus, CleanStatus, "changedDuringRun", "changed during the run" },
		// Both HEAD and porcelain text move together.
		{ OpeningHead, ClosingHead, CleanStatus, DirtyStatus, "changedDuringRun", "changed during the run" },
		// Already dirty at the opening capture, then further edited on the same HEAD: the dirty
		// booleans agree (both true) but the porcelain text differs — see the decision linked above.
		{ OpeningHead, OpeningHead, DirtyStatus, DirtyStatusFurtherChanged, "changedDuringRun", "changed during the run" },
		// HEAD moves on a tree that was, and remains, reported dirty by the same unchanged porcelain text.
		{ OpeningHead, ClosingHead, DirtyStatus, DirtyStatus, "changedDuringRun", "changed during the run" },
		// Dirty at the opening capture, clean at the closing capture on the same HEAD: the edit was reverted or stashed mid-run.
		{ OpeningHead, OpeningHead, DirtyStatus, CleanStatus, "changedDuringRun", "changed during the run" },
	};

	[Theory]
	[MemberData(nameof(WorktreeStates))]
	public async Task ExecuteAsync_fast_distinguishes_three_worktree_states(
		string openingHead,
		string closingHead,
		string openingStatus,
		string closingStatus,
		string expectedState,
		string expectedWording)
	{
		using var repository = CreateFastRepository();
		var console = new StringWriter();
		var runner = new WorktreeAwareTestRunner(openingHead, closingHead, openingStatus, closingStatus, failing: false, console);
		await RunAsync(repository.Path, runner, QualityProfile.Fast, console);

		AssertReceipt(
			FastSummaryPath(repository.Path),
			FastSummaryMarkdownPath(repository.Path),
			console.ToString(),
			openingHead,
			closingHead,
			openingStatus,
			closingStatus,
			expectedState,
			expectedWording);
		AssertMeasuredCommandSnapshot(runner.MeasuredCommandConsoleSnapshot, openingHead, openingStatus);
		Assert.False(runner.MeasuredCommandRanAfterClosingCapture);
	}

	[Theory]
	[MemberData(nameof(WorktreeStates))]
	public async Task ExecuteAsync_mutation_distinguishes_three_worktree_states(
		string openingHead,
		string closingHead,
		string openingStatus,
		string closingStatus,
		string expectedState,
		string expectedWording)
	{
		using var repository = CreateMutationRepository();
		var console = new StringWriter();
		var runner = new WorktreeAwareMutationRunner(openingHead, closingHead, openingStatus, closingStatus, console);
		await RunAsync(repository.Path, runner, QualityProfile.Mutation, console);

		AssertReceipt(
			MutationSummaryPath(repository.Path),
			MutationSummaryMarkdownPath(repository.Path),
			console.ToString(),
			openingHead,
			closingHead,
			openingStatus,
			closingStatus,
			expectedState,
			expectedWording);
		AssertMeasuredCommandSnapshot(runner.MeasuredCommandConsoleSnapshot, openingHead, openingStatus);
		Assert.False(runner.MeasuredCommandRanAfterClosingCapture);
	}

	[Fact]
	public async Task ExecuteAsync_fast_still_fails_when_a_test_fails_even_though_the_worktree_changed_during_the_run()
	{
		using var repository = CreateFastRepository();
		var runner = new WorktreeAwareTestRunner(OpeningHead, ClosingHead, CleanStatus, DirtyStatus, failing: true, new StringWriter());
		var command = new QualityCommand(repository.Path, runner);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => command.ExecuteAsync(new(QualityAction.Check, QualityProfile.Fast)));

		Assert.Contains("Test verification failed", exception.Message, StringComparison.Ordinal);
		var root = ReadSummary(FastSummaryPath(repository.Path));
		Assert.Equal("changedDuringRun", root.GetProperty("worktreeState").GetString());
		Assert.False(root.GetProperty("valid").GetBoolean());
		Assert.False(runner.MeasuredCommandRanAfterClosingCapture);
	}

	private static async Task RunAsync(
		string repositoryPath,
		IProcessRunner runner,
		QualityProfile profile,
		StringWriter console)
	{
		var command = new QualityCommand(repositoryPath, runner);
		var originalOut = Console.Out;
		Console.SetOut(console);
		try
		{
			await command.ExecuteAsync(new(QualityAction.Check, profile));
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	// Pins issue #234's Work 1-2: both captures land in every surface, the removed `head`/`dirty`
	// pair stays removed, and the state is asserted before the incidental `schemaVersion` bump so a
	// missing closing capture, not a version constant, is what fails first.
	private static void AssertReceipt(
		string summaryPath,
		string markdownPath,
		string consoleText,
		string openingHead,
		string closingHead,
		string openingStatus,
		string closingStatus,
		string expectedState,
		string expectedWording)
	{
		var openingDirty = openingStatus.Length > 0;
		var closingDirty = closingStatus.Length > 0;
		var openingDirtyWord = openingDirty.ToString().ToLowerInvariant();
		var closingDirtyWord = closingDirty.ToString().ToLowerInvariant();

		var root = ReadSummary(summaryPath);
		Assert.Equal(expectedState, root.GetProperty("worktreeState").GetString());
		Assert.Equal(openingHead, root.GetProperty("openingHead").GetString());
		Assert.Equal(closingHead, root.GetProperty("closingHead").GetString());
		Assert.Equal(openingDirty, root.GetProperty("openingDirty").GetBoolean());
		Assert.Equal(closingDirty, root.GetProperty("closingDirty").GetBoolean());
		Assert.True(root.GetProperty("valid").GetBoolean());
		Assert.False(root.TryGetProperty("head", out _));
		Assert.False(root.TryGetProperty("dirty", out _));
		Assert.Equal(2, root.GetProperty("schemaVersion").GetInt32());

		var markdown = File.ReadAllText(markdownPath);
		Assert.Contains($"Opening HEAD: `{openingHead}`", markdown, StringComparison.Ordinal);
		Assert.Contains($"Closing HEAD: `{closingHead}`", markdown, StringComparison.Ordinal);
		Assert.Contains($"Opening dirty worktree: `{openingDirtyWord}`", markdown, StringComparison.Ordinal);
		Assert.Contains($"Closing dirty worktree: `{closingDirtyWord}`", markdown, StringComparison.Ordinal);
		Assert.Contains($"Worktree state: {expectedWording}", markdown, StringComparison.Ordinal);
		Assert.DoesNotContain("- HEAD: `", markdown, StringComparison.Ordinal);
		Assert.DoesNotContain("- Dirty worktree: `", markdown, StringComparison.Ordinal);

		Assert.Contains($"Repository opening HEAD: {openingHead}", consoleText, StringComparison.Ordinal);
		Assert.Contains($"Repository closing HEAD: {closingHead}", consoleText, StringComparison.Ordinal);
		Assert.Contains($"Repository opening dirty worktree: {openingDirtyWord}", consoleText, StringComparison.Ordinal);
		Assert.Contains($"Repository closing dirty worktree: {closingDirtyWord}", consoleText, StringComparison.Ordinal);
		Assert.Contains($"Repository worktree state: {expectedWording}", consoleText, StringComparison.Ordinal);
	}

	// Pins Work 2's "those print before the run and must read as opening values": by the time the
	// fake first serves a measured command, both opening lines (HEAD and dirty worktree) must
	// already be printed, and neither the closing lines nor the state line may exist yet.
	private static void AssertMeasuredCommandSnapshot(
		string snapshotAtMeasuredCommand,
		string openingHead,
		string openingStatus)
	{
		var openingDirtyWord = (openingStatus.Length > 0).ToString().ToLowerInvariant();
		Assert.Contains($"Repository opening HEAD: {openingHead}", snapshotAtMeasuredCommand, StringComparison.Ordinal);
		Assert.Contains($"Repository opening dirty worktree: {openingDirtyWord}", snapshotAtMeasuredCommand, StringComparison.Ordinal);
		Assert.DoesNotContain("Repository closing HEAD:", snapshotAtMeasuredCommand, StringComparison.Ordinal);
		Assert.DoesNotContain("Repository closing dirty worktree:", snapshotAtMeasuredCommand, StringComparison.Ordinal);
		Assert.DoesNotContain("Repository worktree state:", snapshotAtMeasuredCommand, StringComparison.Ordinal);
	}

	private static JsonElement ReadSummary(string path)
	{
		using var document = JsonDocument.Parse(File.ReadAllText(path));
		return document.RootElement.Clone();
	}

	private static string FastSummaryPath(string root) =>
		Path.Combine(root, "artifacts", "results", "fast", "summary.json");

	private static string FastSummaryMarkdownPath(string root) =>
		Path.Combine(root, "artifacts", "results", "fast", "summary.md");

	private static string MutationSummaryPath(string root) =>
		Path.Combine(root, "artifacts", "results", "mutation", "summary.json");

	private static string MutationSummaryMarkdownPath(string root) =>
		Path.Combine(root, "artifacts", "results", "mutation", "summary.md");

	private static RepositoryPolicyFixture CreateFastRepository()
	{
		var repository = RepositoryPolicyFixture.CreateCurrent(
			("test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj", "tests", true));
		QualityPolicyFiles.WriteValid(repository.Path);
		return repository;
	}

	private static RepositoryPolicyFixture CreateMutationRepository()
	{
		var repository = RepositoryPolicyFixture.CreateCurrent();
		QualityPolicyFiles.WriteValid(repository.Path);
		return repository;
	}

	/// <summary>
	/// Shared git dispatch and console-snapshot bookkeeping for a fake that must answer with the
	/// opening sample until its first measured command has run, and the closing sample from then on
	/// — a fake keyed on call order alone would also accept a closing capture taken before anything
	/// ran (issue #234). It does not know how many measured commands a profile issues; instead it
	/// records whether any measured command still arrives after it has already served a closing
	/// answer, via <see cref="MeasuredCommandRanAfterClosingCapture"/>, so a test can assert that
	/// never happens — catching a production capture taken partway through a multi-command profile
	/// without coupling the fake to the exact command count. The console is snapshotted once, at the
	/// first measured command, to pin that the opening lines already exist by then.
	/// </summary>
	private sealed class WorktreeGitCapture(string openingHead, string closingHead, string openingStatus, string closingStatus)
	{
		private bool measuredCommandRan;
		private bool closingServed;

		public string MeasuredCommandConsoleSnapshot { get; private set; } = string.Empty;

		public bool MeasuredCommandRanAfterClosingCapture { get; private set; }

		public void MarkMeasuredCommandRan(StringWriter console)
		{
			if (!measuredCommandRan)
			{
				MeasuredCommandConsoleSnapshot = console.ToString();
				measuredCommandRan = true;
			}

			MeasuredCommandRanAfterClosingCapture |= closingServed;
		}

		public ProcessResult RunGit(ProcessCommand command)
		{
			closingServed |= measuredCommandRan;
			return command.Arguments.Contains("rev-parse", StringComparer.Ordinal)
				? new(0, (measuredCommandRan ? closingHead : openingHead) + "\n", string.Empty)
				: new(0, measuredCommandRan ? closingStatus : openingStatus, string.Empty);
		}
	}

	private sealed class WorktreeAwareTestRunner(
		string openingHead,
		string closingHead,
		string openingStatus,
		string closingStatus,
		bool failing,
		StringWriter console) : IProcessRunner
	{
		private readonly WorktreeGitCapture capture = new(openingHead, closingHead, openingStatus, closingStatus);

		public string MeasuredCommandConsoleSnapshot => capture.MeasuredCommandConsoleSnapshot;

		public bool MeasuredCommandRanAfterClosingCapture => capture.MeasuredCommandRanAfterClosingCapture;

		public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
		{
			if (command.FileName == "git")
			{
				return Task.FromResult(capture.RunGit(command));
			}

			if (command.Arguments.Contains("--logger", StringComparer.Ordinal))
			{
				return Task.FromResult(WriteTrx(command));
			}

			return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
		}

		private ProcessResult WriteTrx(ProcessCommand command)
		{
			capture.MarkMeasuredCommandRan(console);
			var resultsDirectory = ProcessCommandArguments.After(command.Arguments, "--results-directory");
			var logger = ProcessCommandArguments.After(command.Arguments, "--logger");
			var fileName = logger[(logger.IndexOf('=') + 1)..];
			Directory.CreateDirectory(resultsDirectory);
			TrxFixtures.Write(
				resultsDirectory,
				total: 1,
				passed: failing ? 0 : 1,
				failedErrorMessages: failing ? ["Simulated failure for the changed-during-run contract test. [FAIL]"] : [],
				fileName: fileName);
			return new(failing ? 1 : 0, string.Empty, string.Empty);
		}
	}

	private sealed class WorktreeAwareMutationRunner(
		string openingHead,
		string closingHead,
		string openingStatus,
		string closingStatus,
		StringWriter console) : IProcessRunner
	{
		private readonly WorktreeGitCapture capture = new(openingHead, closingHead, openingStatus, closingStatus);

		public string MeasuredCommandConsoleSnapshot => capture.MeasuredCommandConsoleSnapshot;

		public bool MeasuredCommandRanAfterClosingCapture => capture.MeasuredCommandRanAfterClosingCapture;

		public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
		{
			if (command.FileName == "git")
			{
				return Task.FromResult(capture.RunGit(command));
			}

			if (command.Arguments.Contains("dotnet-stryker", StringComparer.Ordinal))
			{
				WriteReports(command);
			}

			return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
		}

		private void WriteReports(ProcessCommand command)
		{
			capture.MarkMeasuredCommandRan(console);
			var output = ProcessCommandArguments.After(command.Arguments, "--output");
			Directory.CreateDirectory(output);
			File.WriteAllText(
				Path.Combine(output, "mutation-report.json"),
				"{\"files\":{\"source\":{\"mutants\":[{\"status\":\"Killed\"}]}}}");
			File.WriteAllText(Path.Combine(output, "mutation-report.html"), "<html>report</html>");
			File.WriteAllText(Path.Combine(output, "mutation-report.md"), "# report");
		}
	}
}
