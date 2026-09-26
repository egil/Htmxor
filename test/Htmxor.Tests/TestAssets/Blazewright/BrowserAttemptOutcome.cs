namespace Htmxor.TestAssets.Blazewright;

/// <summary>
/// One browser E2E attempt's observations: the exception it threw, if any, and every Playwright
/// <c>RequestFailed</c> error text seen on that attempt's page
/// (https://github.com/egil/Htmxor/issues/252). <see cref="BrowserAttemptRetryScope"/> decides from
/// this. A null <see cref="Exception"/> means the attempt did not fail, and it is never retried.
/// </summary>
public sealed record BrowserAttemptOutcome(Exception? Exception, IReadOnlyList<string> RequestFailureErrors);
