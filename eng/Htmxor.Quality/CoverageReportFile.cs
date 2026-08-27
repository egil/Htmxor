namespace Htmxor.Quality;

internal sealed record CoverageReportEvidence(string CanonicalPath, int CopyCount);

internal static class CoverageReportFile
{
	public static CoverageReportEvidence FindConsistent(string output)
	{
		var reports = Directory.Exists(output)
			? Directory.EnumerateFiles(
				output,
				"coverage.cobertura.xml",
				SearchOption.AllDirectories)
				.Order(StringComparer.Ordinal)
				.ToArray()
			: [];
		if (reports.Length == 0)
		{
			throw new InvalidOperationException(
				$"Expected at least one fresh coverage.cobertura.xml under '{output}', found none.");
		}

		if (reports.Any(report => new FileInfo(report).Length == 0))
		{
			throw new InvalidOperationException(
				$"Every coverage.cobertura.xml under '{output}' must be nonempty.");
		}

		var canonical = File.ReadAllBytes(reports[0]);
		if (reports.Skip(1).Any(report => !canonical.AsSpan().SequenceEqual(File.ReadAllBytes(report))))
		{
			throw new InvalidOperationException(
				$"Fresh coverage.cobertura.xml files under '{output}' are not byte-identical; coverage evidence is ambiguous.");
		}

		return new(reports[0], reports.Length);
	}
}
