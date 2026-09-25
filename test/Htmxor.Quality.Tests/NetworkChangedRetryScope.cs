using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Decides whether <see cref="Htmx4PackageBrowserTests"/> reruns its nested run (pack, restore,
/// publish, and `dotnet test`) once, scoped to a navigation aborted by Playwright's
/// `net::ERR_NETWORK_CHANGED`
/// (https://github.com/egil/Htmxor/issues/248). The nested run retries only when every failed
/// nested test's error carries that exact error, and the run's Counters show no error, no timeout,
/// and every discovered test executed. A failure that lacks the error, a mix of that error with any
/// other failure, a different network error, a hung or aborted run, a non-zero error or timeout
/// count, an executed count short of total, and a non-zero exit that reports no failed test in the
/// TRX all fail on the first attempt.
/// </summary>
internal static class NetworkChangedRetryScope
{
	public const string NetworkChangedError = "net::ERR_NETWORK_CHANGED";
	private const string AbortText = "The active test run was aborted.";

	// The decision reads only the TRX. A retry needs at least one failed test, so a non-zero exit
	// with no failed test in the TRX never retries, and the exit code adds nothing to the decision.
	// The counter guard below is defensive for the error and timeout counts: no captured real run
	// has shown either alongside net::ERR_NETWORK_CHANGED failures. An executed count short of
	// total is what a skipped nested test produces on this VSTest stack; the nested suite has no
	// skipped test today, but this rule keeps such a run from retrying. The guard costs nothing and
	// only narrows retry further, consistent with the owner excluding a timeout or aborted run
	// outright.
	public static bool ShouldRetry(string trxPath)
	{
		var run = TrxTestRun.Read(trxPath);
		if (run.Failed == 0 || HasAbnormalCounters(run))
		{
			return false;
		}

		var document = XDocument.Load(trxPath);
		return !HasAbortRunInfo(document) && ReadFailedMessages(document).All(IsNetworkChanged);
	}

	private static bool HasAbnormalCounters(TrxTestRun run) =>
		run.Errors > 0 || run.TimedOut > 0 || run.Executed < run.Total;

	// A real abort (a `--blame-hang` timeout or a test-host crash) always adds one more `RunInfo`
	// with this text. It is the one entry every observed abort carries and no non-abort run does,
	// so it is what the decision keys on, rather than any `RunInfo` or Blame text.
	private static bool HasAbortRunInfo(XDocument document) =>
		document.Descendants()
			.Where(e => e.Name.LocalName == "RunInfo")
			.Select(ReadRunInfoText)
			.Any(text => text.Contains(AbortText, StringComparison.Ordinal));

	private static string ReadRunInfoText(XElement runInfo) =>
		runInfo.Descendants().FirstOrDefault(e => e.Name.LocalName == "Text")?.Value ?? string.Empty;

	private static bool IsNetworkChanged(string message) =>
		message.Contains(NetworkChangedError, StringComparison.Ordinal);

	private static IEnumerable<string> ReadFailedMessages(XDocument document) =>
		document.Descendants()
			.Where(e => e.Name.LocalName == "UnitTestResult" && e.Attribute("outcome")?.Value == "Failed")
			.Select(ReadMessage);

	private static string ReadMessage(XElement result) =>
		result.Descendants().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value ?? string.Empty;
}
