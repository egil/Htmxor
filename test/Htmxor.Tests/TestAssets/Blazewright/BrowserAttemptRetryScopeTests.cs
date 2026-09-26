using Microsoft.Playwright;

namespace Htmxor.TestAssets.Blazewright;

public sealed class BrowserAttemptRetryScopeTests
{
	// A real Playwright action timeout (System.TimeoutException) whose message never names a network
	// error, so an attempt that throws it can qualify only through an observed request failure.
	private static readonly Exception RealActionTimeout = new TimeoutException("Timeout 5000ms exceeded.");

	// A real Playwright locator-assertion failure, shaped like the one `ClickEachHandlerAndAssert`
	// throws when a swap never lands; used for the assertion exclusion below so that exclusion is
	// pinned against the exact exception type an `Expect(...).ToContainTextAsync` failure throws.
	private static readonly Exception RealLocatorAssertionFailure =
		new PlaywrightException("Locator expected to contain text 'OnGet'\nBut was: ''");

	[Fact]
	public void A_request_failure_carrying_network_changed_retries()
	{
		var outcome = new BrowserAttemptOutcome(RealActionTimeout, ["net::ERR_NETWORK_CHANGED"]);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.True(shouldRetry);
	}

	[Fact]
	public void A_request_failure_carrying_connection_closed_retries()
	{
		var outcome = new BrowserAttemptOutcome(RealLocatorAssertionFailure, ["net::ERR_CONNECTION_CLOSED"]);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.True(shouldRetry);
	}

	// Playwright cannot produce net::ERR_NETWORK_CHANGED on demand
	// (https://github.com/egil/Htmxor/issues/252's Evidence section), so this fact and
	// A_request_failure_carrying_network_changed_retries are its only scoping evidence. Here the
	// thrown exception names the error directly, as in the first-navigation variant.
	[Fact]
	public void A_thrown_exception_naming_network_changed_retries()
	{
		var exception = new PlaywrightException("net::ERR_NETWORK_CHANGED at https://127.0.0.1/EventHandlers");
		var outcome = new BrowserAttemptOutcome(exception, []);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.True(shouldRetry);
	}

	[Fact]
	public void A_thrown_exception_naming_connection_closed_retries()
	{
		var exception = new PlaywrightException("net::ERR_CONNECTION_CLOSED at https://127.0.0.1/EventHandlers");
		var outcome = new BrowserAttemptOutcome(exception, []);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.True(shouldRetry);
	}

	[Fact]
	public void A_different_network_error_such_as_connection_refused_does_not_retry()
	{
		var outcome = new BrowserAttemptOutcome(RealActionTimeout, ["net::ERR_CONNECTION_REFUSED"]);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.False(shouldRetry);
	}

	// Issue #252 acceptance criterion: "a timeout with no qualifying request failure" must not
	// retry. Uses the real exception type a Playwright action timeout throws, not a plain Exception,
	// so this exclusion is not satisfied only by accident.
	[Fact]
	public void A_real_action_timeout_with_no_qualifying_request_failure_does_not_retry()
	{
		var outcome = new BrowserAttemptOutcome(RealActionTimeout, []);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.False(shouldRetry);
	}

	// Issue #252 acceptance criterion: "an assertion failure with no qualifying request failure"
	// must not retry. Uses the real exception type a failed `Expect(...).ToContainTextAsync` throws.
	[Fact]
	public void A_real_locator_assertion_failure_with_no_qualifying_request_failure_does_not_retry()
	{
		var outcome = new BrowserAttemptOutcome(RealLocatorAssertionFailure, []);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.False(shouldRetry);
	}
}
