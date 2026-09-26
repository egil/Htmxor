namespace Htmxor.TestAssets.Blazewright;

/// <summary>
/// Decides whether <see cref="BrowserAttemptRetryRunner"/> retries a failed browser E2E attempt
/// once, scoped to <see cref="NetworkChangedError"/> and <see cref="ConnectionClosedError"/>
/// (https://github.com/egil/Htmxor/issues/252). It retries only when the attempt observed a
/// Playwright <c>RequestFailed</c> error text equal to one of those two errors, or when the thrown
/// exception's message carries one of them. Anything else -- a different network error such as
/// <c>net::ERR_CONNECTION_REFUSED</c>, or an assertion or timeout failure with no qualifying
/// request failure -- is not retried.
/// </summary>
public static class BrowserAttemptRetryScope
{
	public const string NetworkChangedError = "net::ERR_NETWORK_CHANGED";
	public const string ConnectionClosedError = "net::ERR_CONNECTION_CLOSED";

	// TODO(https://github.com/egil/Htmxor/issues/252): always false until the real scoping decision
	// lands. Every attempt is retried zero times, matching today's actual behavior.
	public static bool ShouldRetry(BrowserAttemptOutcome outcome) => false;
}
