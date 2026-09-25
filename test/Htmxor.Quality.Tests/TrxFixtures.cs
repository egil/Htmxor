using System.Xml.Linq;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Builds a constructed nested-run TRX that reproduces the real VSTest default namespace
/// (`http://microsoft.com/schemas/VisualStudio/TeamTest/2010`), so a reader that ignores it is
/// exercised the same way it would be against a real report
/// (https://github.com/egil/Htmxor/issues/248). A passing or ordinary failing run captured with
/// the nested project's own flags carries a Blame `outcome="Warning"` `RunInfo`; a failing run
/// also carries one `outcome="Error"` `RunInfo` per failed test (text ending `[FAIL]`). A real
/// abort always adds one more `Error` `RunInfo` with the abort text, but its Blame entry varies:
/// a `--blame-hang` timeout still writes the Blame collector's hang-dump text, while a test-host
/// crash (`Environment.FailFast`) writes no Blame entry at all. The abort text is therefore the
/// one entry every captured abort carries and no non-abort run does; the contract's decision keys
/// on it.
/// </summary>
internal static class TrxFixtures
{
	private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

	public const string BlameFinishedMessage =
		"Data collector 'Blame' message: All tests finished running, Sequence file will not be generated.";

	public const string BlameHangDumpMessage =
		"Data collector 'Blame' message: The specified inactivity time of 20 seconds has elapsed. " +
		"Collecting hang dumps from testhost and its child processes.";

	public static string Write(
		string directory,
		int total,
		int passed,
		IReadOnlyList<string> failedErrorMessages,
		string? runAbortedMessage = null,
		string? blameMessage = BlameFinishedMessage,
		string fileName = "nested.trx")
	{
		var results = new XElement(Trx + "Results");
		AddPassedResults(results, passed);
		AddFailedResults(results, failedErrorMessages);

		var resultSummary = new XElement(
			Trx + "ResultSummary",
			new XElement(
				Trx + "Counters",
				new XAttribute("total", total),
				new XAttribute("executed", total),
				new XAttribute("passed", passed),
				new XAttribute("failed", failedErrorMessages.Count),
				new XAttribute("notExecuted", 0),
				new XAttribute("error", 0),
				new XAttribute("timeout", 0)));
		AddRunInfos(resultSummary, failedErrorMessages, runAbortedMessage, blameMessage);

		var document = new XElement(Trx + "TestRun", results, resultSummary);

		var path = Path.Combine(directory, fileName);
		document.Save(path);
		return path;
	}

	private static void AddPassedResults(XElement results, int passed)
	{
		for (var index = 0; index < passed; index++)
		{
			results.Add(new XElement(
				Trx + "UnitTestResult",
				new XAttribute("testName", $"Passed_{index}"),
				new XAttribute("outcome", "Passed")));
		}
	}

	private static void AddFailedResults(XElement results, IReadOnlyList<string> failedErrorMessages)
	{
		for (var index = 0; index < failedErrorMessages.Count; index++)
		{
			results.Add(new XElement(
				Trx + "UnitTestResult",
				new XAttribute("testName", $"Failed_{index}"),
				new XAttribute("outcome", "Failed"),
				new XElement(
					Trx + "Output",
					new XElement(
						Trx + "ErrorInfo",
						new XElement(Trx + "Message", failedErrorMessages[index])))));
		}
	}

	// RunInfos in the order the real captures show: one Error `[FAIL]` entry per failed test, then
	// the optional abort text as one more Error entry, then the optional Blame collector entry. A
	// real test-host crash abort has no Blame entry at all, so `blameMessage` is nullable
	// independently of `runAbortedMessage`.
	private static void AddRunInfos(
		XElement resultSummary, IReadOnlyList<string> failedErrorMessages, string? runAbortedMessage, string? blameMessage)
	{
		var runInfos = new XElement(Trx + "RunInfos");
		for (var index = 0; index < failedErrorMessages.Count; index++)
		{
			runInfos.Add(CreateRunInfo("Error", $"Failed_{index} [FAIL]"));
		}

		if (runAbortedMessage is not null)
		{
			runInfos.Add(CreateRunInfo("Error", runAbortedMessage));
		}

		if (blameMessage is not null)
		{
			runInfos.Add(CreateRunInfo("Warning", blameMessage));
		}

		resultSummary.Add(runInfos);
	}

	private static XElement CreateRunInfo(string outcome, string text) =>
		new(
			Trx + "RunInfo",
			new XAttribute("outcome", outcome),
			new XElement(Trx + "Text", text));
}
