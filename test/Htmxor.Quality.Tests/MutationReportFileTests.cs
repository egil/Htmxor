using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class MutationReportFileTests
{
	[Fact]
	public void DiscoverRequired_returns_exactly_one_nonempty_report_of_each_promised_format()
	{
		using var directory = new TemporaryDirectory();
		var reports = Directory.CreateDirectory(Path.Combine(directory.Path, "reports"));
		var json = WriteReport(reports.FullName, "mutation-report.json", "{}");
		var html = WriteReport(reports.FullName, "mutation-report.html", "<html></html>");
		var markdown = WriteReport(reports.FullName, "mutation-report.md", "# Mutation report");

		var reportsFound = MutationReportFile.DiscoverRequired(directory.Path);

		Assert.Equal(json, reportsFound.Json);
		Assert.Equal(html, reportsFound.Html);
		Assert.Equal(markdown, reportsFound.Markdown);
		Assert.Empty(reportsFound.Failures);
	}

	[Theory]
	[InlineData("mutation-report.json")]
	[InlineData("mutation-report.html")]
	[InlineData("mutation-report.md")]
	public void DiscoverRequired_records_a_missing_promised_format(string missingFile)
	{
		using var directory = new TemporaryDirectory();
		WriteRequiredReports(directory.Path, missingFile);

		var reports = MutationReportFile.DiscoverRequired(directory.Path);

		Assert.Null(SelectReport(reports, missingFile));
		Assert.Contains(reports.Failures, failure =>
			failure.Contains(missingFile, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("mutation-report.json")]
	[InlineData("mutation-report.html")]
	[InlineData("mutation-report.md")]
	public void DiscoverRequired_records_an_empty_promised_format(string emptyFile)
	{
		using var directory = new TemporaryDirectory();
		WriteRequiredReports(directory.Path);
		File.WriteAllText(Path.Combine(directory.Path, emptyFile), string.Empty);

		var reports = MutationReportFile.DiscoverRequired(directory.Path);

		Assert.Null(SelectReport(reports, emptyFile));
		Assert.Contains(reports.Failures, failure =>
			failure.Contains(emptyFile, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("mutation-report.json")]
	[InlineData("mutation-report.html")]
	[InlineData("mutation-report.md")]
	public void DiscoverRequired_records_multiple_reports_of_a_promised_format(string duplicateFile)
	{
		using var directory = new TemporaryDirectory();
		WriteRequiredReports(directory.Path);
		var duplicate = Directory.CreateDirectory(Path.Combine(directory.Path, "duplicate"));
		WriteReport(duplicate.FullName, duplicateFile, "duplicate");

		var reports = MutationReportFile.DiscoverRequired(directory.Path);

		Assert.Null(SelectReport(reports, duplicateFile));
		Assert.Contains(reports.Failures, failure =>
			failure.Contains(duplicateFile, StringComparison.Ordinal));
	}

	private static void WriteRequiredReports(string root, string? excludedFile = null)
	{
		foreach (var file in new[] { "mutation-report.json", "mutation-report.html", "mutation-report.md" })
		{
			if (file != excludedFile)
			{
				WriteReport(root, file, "report");
			}
		}
	}

	private static string WriteReport(string root, string fileName, string content)
	{
		var path = Path.Combine(root, fileName);
		File.WriteAllText(path, content);
		return path;
	}

	private static string? SelectReport(MutationReportFiles reports, string fileName) =>
		fileName switch
		{
			"mutation-report.json" => reports.Json,
			"mutation-report.html" => reports.Html,
			"mutation-report.md" => reports.Markdown,
			_ => throw new ArgumentOutOfRangeException(nameof(fileName)),
		};
}
