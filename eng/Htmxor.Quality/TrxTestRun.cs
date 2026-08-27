using System.Globalization;
using System.Xml.Linq;

namespace Htmxor.Quality;

internal sealed record TrxTestRun(
	int Total,
	int Executed,
	int Passed,
	int Failed,
	int Skipped,
	int Errors,
	int TimedOut)
{
	public static TrxTestRun Read(string path)
	{
		if (!File.Exists(path))
		{
			throw new InvalidOperationException($"TRX report '{path}' does not exist.");
		}

		var document = XDocument.Load(path);
		var counters = document.Descendants()
			.Where(element => element.Name.LocalName == "Counters")
			.ToArray();
		if (counters.Length != 1)
		{
			throw new InvalidOperationException(
				$"TRX report '{path}' must contain exactly one Counters element.");
		}

		return new(
			ReadCount(counters[0], "total"),
			ReadCount(counters[0], "executed"),
			ReadCount(counters[0], "passed"),
			ReadCount(counters[0], "failed"),
			ReadCount(counters[0], "notExecuted"),
			ReadCount(counters[0], "error"),
			ReadCount(counters[0], "timeout"));
	}

	public void EnsureHasTests(string project)
	{
		if (Total == 0 || Executed == 0)
		{
			throw new InvalidOperationException(
				$"Test project '{project}' discovered {Total} and executed {Executed} tests.");
		}
	}

	private static int ReadCount(XElement counters, string name)
	{
		var value = counters.Attribute(name)?.Value;
		if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count < 0)
		{
			throw new InvalidOperationException(
				$"TRX Counters attribute '{name}' is missing or invalid.");
		}

		return count;
	}
}
