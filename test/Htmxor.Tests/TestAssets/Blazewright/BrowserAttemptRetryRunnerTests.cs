namespace Htmxor.TestAssets.Blazewright;

public sealed class BrowserAttemptRetryRunnerTests
{
	[Fact]
	public async Task A_successful_first_attempt_is_not_retried_and_writes_no_note()
	{
		var attempts = new RecordedAttempts([new BrowserAttemptOutcome(null, [])]);
		var notes = new List<string>();

		var result = await BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, outcome => outcome, notes.Add);

		Assert.Same(attempts.Outcomes[0], result);
		Assert.Equal(1, attempts.CallCount);
		Assert.Empty(notes);
	}

	[Fact]
	public async Task A_non_qualifying_failure_is_not_retried()
	{
		var exception = new Exception("Timeout 5000ms exceeded.");
		var attempts = new RecordedAttempts([new BrowserAttemptOutcome(exception, ["net::ERR_CONNECTION_REFUSED"])]);
		var notes = new List<string>();

		var thrown = await Assert.ThrowsAsync<Exception>(() =>
			BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, outcome => outcome, notes.Add));

		Assert.Same(exception, thrown);
		Assert.Equal(1, attempts.CallCount);
		Assert.Empty(notes);
	}

	[Fact]
	public async Task A_qualifying_failure_is_retried_once_and_the_second_attempt_is_reported()
	{
		var firstException = new Exception("net::ERR_NETWORK_CHANGED at https://127.0.0.1/EventHandlers");
		var attempts = new RecordedAttempts([
			new BrowserAttemptOutcome(firstException, ["net::ERR_NETWORK_CHANGED"]),
			new BrowserAttemptOutcome(null, []),
		]);
		var notes = new List<string>();

		var result = await BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, outcome => outcome, notes.Add);

		Assert.Same(attempts.Outcomes[1], result);
		Assert.Equal(2, attempts.CallCount);
		AssertRetryNoteWasWritten(notes, BrowserAttemptRetryScope.NetworkChangedError);
	}

	[Fact]
	public async Task A_second_qualifying_failure_is_not_retried_again_and_surfaces_as_a_failure()
	{
		var firstException = new Exception("net::ERR_CONNECTION_CLOSED at https://127.0.0.1/EventHandlers");
		var secondException = new Exception("net::ERR_CONNECTION_CLOSED at https://127.0.0.1/EventHandlers");
		var attempts = new RecordedAttempts([
			new BrowserAttemptOutcome(firstException, ["net::ERR_CONNECTION_CLOSED"]),
			new BrowserAttemptOutcome(secondException, ["net::ERR_CONNECTION_CLOSED"]),
		]);
		var notes = new List<string>();

		var thrown = await Assert.ThrowsAsync<Exception>(() =>
			BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, outcome => outcome, notes.Add));

		Assert.Same(secondException, thrown);
		Assert.Equal(2, attempts.CallCount);
		AssertRetryNoteWasWritten(notes, BrowserAttemptRetryScope.ConnectionClosedError);
	}

	// A retry, whether or not it ends up passing, must carry a recorded reason and the #252 link, so
	// a reader of the outer test's retained output can see why it happened even when it still fails.
	private static void AssertRetryNoteWasWritten(List<string> notes, string reason) =>
		Assert.Contains(notes, note =>
			note.Contains(reason, StringComparison.Ordinal) &&
			note.Contains(BrowserAttemptRetryRunner.IssueUrl, StringComparison.Ordinal));

	private sealed class RecordedAttempts(IReadOnlyList<BrowserAttemptOutcome> outcomes)
	{
		public IReadOnlyList<BrowserAttemptOutcome> Outcomes { get; } = outcomes;

		public int CallCount { get; private set; }

		public Task<BrowserAttemptOutcome> NextAsync()
		{
			Assert.True(CallCount < Outcomes.Count, $"The attempt body was run more than {Outcomes.Count} times.");
			return Task.FromResult(Outcomes[CallCount++]);
		}
	}
}
