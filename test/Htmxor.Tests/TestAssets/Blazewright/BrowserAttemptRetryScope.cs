namespace Htmxor.TestAssets.Blazewright;

/// <summary>
/// Decides whether <see cref="BrowserAttemptRetryRunner"/> retries a failed browser E2E attempt
/// once, scoped to <see cref="NetworkChangedError"/> and <see cref="ConnectionClosedError"/>
/// (https://github.com/egil/Htmxor/issues/252). <see cref="QualifyingError"/> is the only place that
/// recognizes these errors, and is also the runner's only source for the retry note's reason: a
/// qualifying Playwright <c>RequestFailed</c> error text matches exactly, and a qualifying thrown
/// exception's message only needs to contain one, because Playwright wraps it with call-log text
/// such as the failing URL. The mid-test htmx variant's thrown exception is an assertion timeout
/// that never names the network error, so the recorded request failures must be consulted, not only
/// the exception. Anything else -- a different network error such as
/// <c>net::ERR_CONNECTION_REFUSED</c>, or an assertion or timeout failure with no qualifying request
/// failure -- does not qualify.
/// </summary>
public static class BrowserAttemptRetryScope
{
	public const string NetworkChangedError = "net::ERR_NETWORK_CHANGED";
	public const string ConnectionClosedError = "net::ERR_CONNECTION_CLOSED";

	public static bool ShouldRetry(BrowserAttemptOutcome outcome) => QualifyingError(outcome) is not null;

	public static string? QualifyingError(BrowserAttemptOutcome outcome) =>
		QualifyingRequestFailure(outcome) ?? QualifyingExceptionMessage(outcome);

	private static string? QualifyingRequestFailure(BrowserAttemptOutcome outcome)
	{
		if (outcome.RequestFailureErrors.Contains(NetworkChangedError))
		{
			return NetworkChangedError;
		}

		if (outcome.RequestFailureErrors.Contains(ConnectionClosedError))
		{
			return ConnectionClosedError;
		}

		return null;
	}

	private static string? QualifyingExceptionMessage(BrowserAttemptOutcome outcome)
	{
		if (outcome.Exception is null)
		{
			return null;
		}

		if (outcome.Exception.Message.Contains(NetworkChangedError, StringComparison.Ordinal))
		{
			return NetworkChangedError;
		}

		if (outcome.Exception.Message.Contains(ConnectionClosedError, StringComparison.Ordinal))
		{
			return ConnectionClosedError;
		}

		return null;
	}
}
