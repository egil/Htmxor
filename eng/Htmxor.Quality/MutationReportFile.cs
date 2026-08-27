namespace Htmxor.Quality;

internal sealed record MutationReportFiles(
	string? Json,
	string? Html,
	string? Markdown,
	IReadOnlyList<string> Failures);

internal static class MutationReportFile
{
	public static MutationReportFiles DiscoverRequired(string output)
	{
		var failures = new List<string>();
		var json = FindSingle(output, "mutation-report.json", failures);
		var html = FindSingle(output, "mutation-report.html", failures);
		var markdown = FindSingle(output, "mutation-report.md", failures);
		return new(json, html, markdown, failures);
	}

	private static string? FindSingle(
		string output,
		string fileName,
		ICollection<string> failures)
	{
		var reports = Directory.Exists(output)
			? Directory.EnumerateFiles(
				output,
				fileName,
				SearchOption.AllDirectories).ToArray()
			: [];
		if (reports.Length != 1)
		{
			failures.Add(
				$"Expected one fresh {fileName} under '{output}', found {reports.Length}.");
			return null;
		}

		if (new FileInfo(reports[0]).Length == 0)
		{
			failures.Add($"Mutation report '{reports[0]}' is empty.");
			return null;
		}

		return reports[0];
	}
}
