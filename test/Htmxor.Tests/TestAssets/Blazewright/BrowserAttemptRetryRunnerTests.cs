using Microsoft.Playwright;

namespace Htmxor.TestAssets.Blazewright;

public sealed class BrowserAttemptRetryRunnerTests
{
	[Fact]
	public async Task A_successful_first_attempt_is_not_retried_and_writes_no_note()
	{
		var attempts = new RecordedAttempts([new BrowserAttemptOutcome(null, [])]);
		var notes = new List<string>();

		var result = await BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, notes.Add);

		Assert.Same(attempts.Outcomes[0], result);
		Assert.Equal(1, attempts.CallCount);
		Assert.Empty(notes);
	}

	[Fact]
	public async Task A_non_qualifying_failure_is_not_retried()
	{
		var exception = new TimeoutException("Timeout 5000ms exceeded.");
		var attempts = new RecordedAttempts([new BrowserAttemptOutcome(exception, ["net::ERR_CONNECTION_REFUSED"])]);
		var notes = new List<string>();

		var thrown = await Assert.ThrowsAsync<TimeoutException>(() =>
			BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, notes.Add));

		Assert.Same(exception, thrown);
		Assert.Equal(1, attempts.CallCount);
		Assert.Empty(notes);
	}

	[Fact]
	public async Task A_qualifying_failure_is_retried_once_and_the_second_attempt_is_reported()
	{
		var firstException = new PlaywrightException("net::ERR_NETWORK_CHANGED at https://127.0.0.1/EventHandlers");
		var attempts = new RecordedAttempts([
			new BrowserAttemptOutcome(firstException, ["net::ERR_NETWORK_CHANGED"]),
			new BrowserAttemptOutcome(null, []),
		]);
		var notes = new List<string>();

		var result = await BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, notes.Add);

		Assert.Same(attempts.Outcomes[1], result);
		Assert.Equal(2, attempts.CallCount);
		AssertRetryNoteWasWritten(notes, BrowserAttemptRetryScope.NetworkChangedError);
	}

	[Fact]
	public async Task A_second_qualifying_failure_is_not_retried_again_and_surfaces_as_a_failure()
	{
		var firstException = new PlaywrightException("net::ERR_CONNECTION_CLOSED at https://127.0.0.1/EventHandlers");
		var secondException = new PlaywrightException("net::ERR_CONNECTION_CLOSED at https://127.0.0.1/EventHandlers");
		var attempts = new RecordedAttempts([
			new BrowserAttemptOutcome(firstException, ["net::ERR_CONNECTION_CLOSED"]),
			new BrowserAttemptOutcome(secondException, ["net::ERR_CONNECTION_CLOSED"]),
		]);
		var notes = new List<string>();

		var thrown = await Assert.ThrowsAsync<PlaywrightException>(() =>
			BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, notes.Add));

		Assert.Same(secondException, thrown);
		Assert.Equal(2, attempts.CallCount);
		AssertRetryNoteWasWritten(notes, BrowserAttemptRetryScope.ConnectionClosedError);
	}

	// Pins the rethrow mechanism itself (https://github.com/egil/Htmxor/issues/252): a bare
	// `throw first.Exception;` resets the stack trace to the rethrow site inside this runner,
	// discarding the frame that actually failed. `ThrowFromAttempt` is a real throw site, so its own
	// exception carries a real captured stack trace to preserve or lose.
	[Fact]
	public async Task A_non_qualifying_failures_rethrow_still_names_the_attempts_own_frame()
	{
		var attempts = new RecordedAttempts([CapturedThrowOutcome()]);

		var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, _ => { }));

		Assert.Contains(nameof(ThrowFromAttempt), thrown.StackTrace, StringComparison.Ordinal);
	}

	// Same pin as above, for the second rethrow site: a qualifying first outcome must actually reach
	// a retry before this can exercise the second attempt's rethrow, so it stays red at the shell (the
	// stub never retries, so the first outcome's own exception surfaces instead) and green once the
	// scoping decision lands.
	[Fact]
	public async Task A_retried_attempts_failure_rethrow_still_names_its_own_frame()
	{
		var firstException = new PlaywrightException("net::ERR_NETWORK_CHANGED at https://127.0.0.1/EventHandlers");
		var attempts = new RecordedAttempts([
			new BrowserAttemptOutcome(firstException, ["net::ERR_NETWORK_CHANGED"]),
			CapturedThrowOutcome(),
		]);
		var notes = new List<string>();

		var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			BrowserAttemptRetryRunner.RunAsync(attempts.NextAsync, notes.Add));

		Assert.Contains(nameof(ThrowFromAttempt), thrown.StackTrace, StringComparison.Ordinal);
	}

	private static BrowserAttemptOutcome CapturedThrowOutcome()
	{
		try
		{
			ThrowFromAttempt();
			throw new InvalidOperationException("Unreachable: ThrowFromAttempt always throws.");
		}
		catch (InvalidOperationException ex)
		{
			return new BrowserAttemptOutcome(ex, []);
		}
	}

	private static void ThrowFromAttempt() => throw new InvalidOperationException("boom");

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
