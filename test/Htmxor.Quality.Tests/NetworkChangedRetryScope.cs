using System.Xml.Linq;
using Htmxor.Quality;

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
	private const string AbortText = "The active test run was aborted.";

	// The decision reads only the TRX. A retry needs at least one failed test, so a non-zero exit
	// with no failed test in the TRX never retries, and the exit code adds nothing to the decision.
	public static bool ShouldRetry(string trxPath)
	{
		var run = TrxTestRun.Read(trxPath);
		if (run.Failed == 0)
		{
			return false;
		}

		var document = XDocument.Load(trxPath);
		return !HasAbortRunInfo(document) && ReadFailedMessages(document).All(IsNetworkChanged);
	}

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
