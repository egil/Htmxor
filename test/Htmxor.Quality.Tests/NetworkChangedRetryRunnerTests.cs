using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class NetworkChangedRetryRunnerTests
{
	// The first line of the instance 12 message in NetworkChangedRetryScopeTests; the wiring facts
	// below only need the decision to say "retry", not the scoping facts themselves.
	private const string NetworkChangedMessage =
		"Microsoft.Playwright.PlaywrightException : net::ERR_NETWORK_CHANGED at http://127.0.0.1:46349/issue-154/navigation";

	private const string UnrelatedMessage =
		"Microsoft.Playwright.PlaywrightException : Execution context was destroyed, most likely because of a navigation.";

	[Fact]
	public async Task A_successful_first_run_is_not_retried_and_writes_no_note()
	{
		using var directory = new TemporaryDirectory();
		var sharedTrxPath = Path.Combine(directory.Path, "shared.trx");
		var firstRun = new ProcessResult(0, "first run output", string.Empty);
		var runs = new RecordedRuns<ProcessResult>([
			() => WriteThenReturn(directory.Path, firstRun, total: 42, passed: 42, failedErrorMessages: []),
		]);
		var notes = new List<string>();

		var result = await NetworkChangedRetryRunner.RunAsync(runs.NextAsync, _ => sharedTrxPath, notes.Add);

		Assert.Equal(firstRun, result);
		Assert.Equal(1, runs.CallCount);
		Assert.Empty(notes);
	}

	[Fact]
	public async Task A_non_retryable_first_failure_is_not_retried()
	{
		using var directory = new TemporaryDirectory();
		var sharedTrxPath = Path.Combine(directory.Path, "shared.trx");
		var firstRun = new ProcessResult(1, "first run output", string.Empty);
		var runs = new RecordedRuns<ProcessResult>([
			() => WriteThenReturn(directory.Path, firstRun, total: 42, passed: 41, failedErrorMessages: [UnrelatedMessage]),
		]);
		var notes = new List<string>();

		var result = await NetworkChangedRetryRunner.RunAsync(runs.NextAsync, _ => sharedTrxPath, notes.Add);

		Assert.Equal(firstRun, result);
		Assert.Equal(1, runs.CallCount);
		Assert.Empty(notes);
	}

	[Fact]
	public async Task A_network_changed_failure_is_retried_once_and_the_second_run_is_reported()
	{
		using var directory = new TemporaryDirectory();
		var sharedTrxPath = Path.Combine(directory.Path, "shared.trx");
		var firstRun = new ProcessResult(1, "first run output", string.Empty);
		var secondRun = new ProcessResult(0, "second run output", string.Empty);
		var runs = new RecordedRuns<ProcessResult>([
			() => WriteThenReturn(directory.Path, firstRun, total: 42, passed: 40, failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage]),
			() => WriteThenReturn(directory.Path, secondRun, total: 42, passed: 42, failedErrorMessages: []),
		]);
		var notes = new List<string>();

		var result = await NetworkChangedRetryRunner.RunAsync(runs.NextAsync, _ => sharedTrxPath, notes.Add);

		Assert.Equal(secondRun, result);
		Assert.Equal(2, runs.CallCount);
		AssertRetryNoteWasWritten(notes);
	}

	[Fact]
	public async Task A_second_network_changed_failure_is_not_retried_again_and_surfaces_as_a_failure()
	{
		using var directory = new TemporaryDirectory();
		var sharedTrxPath = Path.Combine(directory.Path, "shared.trx");
		var firstRun = new ProcessResult(1, "first run output", string.Empty);
		var secondRun = new ProcessResult(1, "second run output", string.Empty);
		var runs = new RecordedRuns<ProcessResult>([
			() => WriteThenReturn(directory.Path, firstRun, total: 42, passed: 40, failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage]),
			() => WriteThenReturn(directory.Path, secondRun, total: 42, passed: 40, failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage]),
		]);
		var notes = new List<string>();

		var result = await NetworkChangedRetryRunner.RunAsync(runs.NextAsync, _ => sharedTrxPath, notes.Add);

		Assert.Equal(secondRun, result);
		Assert.Equal(2, runs.CallCount);
		AssertRetryNoteWasWritten(notes);
	}

	// A retry, whether or not it ends up passing, must carry a recorded reason and the #248 link
	// (acceptance criterion 3 and the owner's 2026-09-24 constraint); a retry that ends in failure
	// is exactly the case a reader of the outer test's retained output needs this note for.
	private static void AssertRetryNoteWasWritten(List<string> notes) =>
		Assert.Contains(notes, note =>
			note.Contains(NetworkChangedRetryScope.NetworkChangedError, StringComparison.Ordinal) &&
			note.Contains("https://github.com/egil/Htmxor/issues/248", StringComparison.Ordinal));

	// Writes the scenario's TRX to the shared, fixed path (both runs overwrite the same file, as
	// the real nested `dotnet test` invocation does), then returns the run value the delegate
	// reports for that call.
	private static ProcessResult WriteThenReturn(
		string directory, ProcessResult run, int total, int passed, IReadOnlyList<string> failedErrorMessages)
	{
		TrxFixtures.Write(directory, total, passed, failedErrorMessages, fileName: "shared.trx");
		return run;
	}

	private sealed class RecordedRuns<TRun>(IReadOnlyList<Func<TRun>> runs)
	{
		public int CallCount { get; private set; }

		public Task<TRun> NextAsync()
		{
			Assert.True(CallCount < runs.Count, $"The nested suite was run more than {runs.Count} times.");
			return Task.FromResult(runs[CallCount++]());
		}
	}
}
