using Microsoft.Playwright;

namespace Htmxor.TestAssets.Blazewright;

public sealed class BrowserAttemptRetryScopeTests
{
	// The mid-test htmx-fetch variant from https://github.com/egil/Htmxor/issues/252: the attempt
	// observed a qualifying request failure, but the thrown exception is an assertion timeout that
	// never names the network error. Only the observed request failure may qualify it.
	private static readonly Exception AssertionTimeout = new("Timeout 5000ms exceeded.");

	[Fact]
	public void A_request_failure_carrying_network_changed_retries()
	{
		var outcome = new BrowserAttemptOutcome(AssertionTimeout, ["net::ERR_NETWORK_CHANGED"]);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.True(shouldRetry);
	}

	[Fact]
	public void A_request_failure_carrying_connection_closed_retries()
	{
		var outcome = new BrowserAttemptOutcome(AssertionTimeout, ["net::ERR_CONNECTION_CLOSED"]);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.True(shouldRetry);
	}

	// Playwright cannot produce net::ERR_NETWORK_CHANGED on demand
	// (https://github.com/egil/Htmxor/issues/252's Evidence section), so its scoping is shown only
	// by this constructed input, both as an observed request failure and as the thrown exception's
	// own message (the first-navigation variant, where the exception names the error directly).
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
		var outcome = new BrowserAttemptOutcome(AssertionTimeout, ["net::ERR_CONNECTION_REFUSED"]);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void An_unrelated_assertion_failure_with_no_qualifying_request_failure_does_not_retry()
	{
		var outcome = new BrowserAttemptOutcome(AssertionTimeout, []);

		var shouldRetry = BrowserAttemptRetryScope.ShouldRetry(outcome);

		Assert.False(shouldRetry);
	}
}
