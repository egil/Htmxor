using System.Text.RegularExpressions;

namespace Htmxor.Quality;

// Stryker continues after a failing unmutated test run, and its JSON report records no baseline
// status. Its warning carries the authoritative count. At debug verbosity it also logs one entry,
// "Initial test run output: <name>\n\n<message>\n<name>\n\n<message>...", from which names are read.
internal static partial class StrykerInitialTestRun
{
	public static string? GetValidityFailure(string consoleOutput)
	{
		var warning = FailingWarning().Match(consoleOutput);
		if (!warning.Success)
		{
			return null;
		}

		var count = int.Parse(warning.Groups["count"].Value, System.Globalization.CultureInfo.InvariantCulture);
		var names = FailingTestNames(consoleOutput).Take(count).ToArray();
		var detail = names.Length == 0
			? "no failing test names were found in Stryker's debug output"
			: string.Join("; ", names);
		return $"Stryker's initial unmutated test run had {count} failing tests, " +
			$"so mutant outcomes are not trustworthy: {detail}.";
	}

	private static IEnumerable<string> FailingTestNames(string consoleOutput)
	{
		var output = OutputEntry().Match(consoleOutput);
		if (!output.Success)
		{
			return [];
		}

		var lines = output.Groups["body"].Value.ReplaceLineEndings("\n").Split('\n');
		return lines
			.Where((line, index) => index + 1 < lines.Length && lines[index + 1].Length == 0 && TestName().IsMatch(line))
			.Distinct(StringComparer.Ordinal);
	}

	[GeneratedRegex(@"\] (?<count>\d+) tests are failing\. Stryker will continue", RegexOptions.CultureInvariant)]
	private static partial Regex FailingWarning();

	[GeneratedRegex(
		@"\] Initial test run output: (?<body>.*?)\r?\n(?=\[\d{2}:\d{2}:\d{2} [A-Z]{3}\] )",
		RegexOptions.Singleline | RegexOptions.CultureInvariant)]
	private static partial Regex OutputEntry();

	// A test display name: a dotted identifier with no spaces, optionally followed by theory arguments.
	[GeneratedRegex(@"^[\p{L}_][\w`<>,+]*(\.[\w`<>,+]+)+(\(.*\))?$", RegexOptions.CultureInvariant)]
	private static partial Regex TestName();
}
