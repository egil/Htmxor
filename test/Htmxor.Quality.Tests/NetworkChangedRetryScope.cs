namespace Htmxor.Quality.Tests;

/// <summary>
/// Decides whether <see cref="Htmx4PackageBrowserTests"/> reruns its nested `dotnet test`
/// invocation once, scoped to a navigation aborted by Playwright's `net::ERR_NETWORK_CHANGED`
/// (https://github.com/egil/Htmxor/issues/248). The nested run retries only when every failed
/// nested test's error carries that exact error. A failure that lacks it, a mix of that error with
/// any other failure, a different network error, a hung or aborted run, and a non-zero exit that
/// reports no failed test in the TRX all fail on the first attempt.
/// </summary>
internal static class NetworkChangedRetryScope
{
	public const string NetworkChangedError = "net::ERR_NETWORK_CHANGED";

	// The decision reads only the TRX. A retry needs at least one failed test, so a non-zero exit
	// with no failed test in the TRX never retries, and the exit code adds nothing to the decision.

	// Characterizes today's behavior: the outer test never retries. Issue #248's scoping decision
	// is not implemented yet. Returning false unconditionally keeps every no-retry case a faithful
	// baseline of the current system, rather than an unconditional throw that would be red for every
	// case regardless of what the contract asks for.
	public static bool ShouldRetry(string trxPath) => false;
}
