using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class CoverageReportFileTests
{
	[Fact]
	public void FindConsistent_returns_one_nonempty_report()
	{
		using var directory = new TemporaryDirectory();
		var reports = Directory.CreateDirectory(Path.Combine(directory.Path, "run"));
		var expected = Path.Combine(reports.FullName, "coverage.cobertura.xml");
		File.WriteAllText(expected, "<coverage />");

		Assert.Equal(new CoverageReportEvidence(expected, 1), CoverageReportFile.FindConsistent(directory.Path));
	}

	[Fact]
	public void FindConsistent_accepts_identical_copies_and_chooses_a_deterministic_path()
	{
		using var directory = new TemporaryDirectory();
		var second = WriteReport(directory.Path, "second", "<coverage />");
		var first = WriteReport(directory.Path, "first", "<coverage />");

		var evidence = CoverageReportFile.FindConsistent(directory.Path);

		Assert.Equal(first, evidence.CanonicalPath);
		Assert.Equal(2, evidence.CopyCount);
		Assert.True(File.Exists(second));
	}

	[Fact]
	public void FindConsistent_rejects_divergent_copies()
	{
		using var directory = new TemporaryDirectory();
		WriteReport(directory.Path, "first", "<coverage lines-valid=\"1\" />");
		WriteReport(directory.Path, "second", "<coverage lines-valid=\"2\" />");

		var exception = Assert.Throws<InvalidOperationException>(
			() => CoverageReportFile.FindConsistent(directory.Path));

		Assert.Contains("ambiguous", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void FindConsistent_rejects_a_missing_report()
	{
		using var directory = new TemporaryDirectory();

		Assert.Throws<InvalidOperationException>(() => CoverageReportFile.FindConsistent(directory.Path));
	}

	[Fact]
	public void FindConsistent_rejects_an_empty_report()
	{
		using var directory = new TemporaryDirectory();
		File.WriteAllText(Path.Combine(directory.Path, "coverage.cobertura.xml"), string.Empty);

		Assert.Throws<InvalidOperationException>(() => CoverageReportFile.FindConsistent(directory.Path));
	}

	private static string WriteReport(string root, string name, string content)
	{
		var directory = Directory.CreateDirectory(Path.Combine(root, name));
		var path = Path.Combine(directory.FullName, "coverage.cobertura.xml");
		File.WriteAllText(path, content);
		return path;
	}
}
