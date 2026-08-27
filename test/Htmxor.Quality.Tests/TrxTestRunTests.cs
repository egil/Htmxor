using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class TrxTestRunTests
{
	[Fact]
	public void Read_returns_exact_counts()
	{
		using var directory = new TemporaryDirectory();
		var report = WriteTrx(directory.Path, "152", "152", "150", "1", "1", "0", "0");

		var run = TrxTestRun.Read(report);

		Assert.Equal(new TrxTestRun(152, 152, 150, 1, 1, 0, 0), run);
	}

	[Fact]
	public void EnsureHasTests_rejects_a_zero_test_run()
	{
		using var directory = new TemporaryDirectory();
		var report = WriteTrx(directory.Path, "0", "0", "0", "0", "0", "0", "0");
		var run = TrxTestRun.Read(report);

		var exception = Assert.Throws<InvalidOperationException>(() => run.EnsureHasTests("tests.csproj"));

		Assert.Contains("discovered 0 and executed 0", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Read_rejects_a_missing_counter()
	{
		using var directory = new TemporaryDirectory();
		var report = WriteTrx(directory.Path, "1", "1", "1", "0", "0", "0", null);

		var exception = Assert.Throws<InvalidOperationException>(() => TrxTestRun.Read(report));

		Assert.Contains("timeout", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Read_rejects_a_malformed_counter()
	{
		using var directory = new TemporaryDirectory();
		var report = WriteTrx(directory.Path, "many", "1", "1", "0", "0", "0", "0");

		Assert.Throws<InvalidOperationException>(() => TrxTestRun.Read(report));
	}

	[Fact]
	public void Read_rejects_a_missing_report()
	{
		using var directory = new TemporaryDirectory();

		Assert.Throws<InvalidOperationException>(
			() => TrxTestRun.Read(Path.Combine(directory.Path, "missing.trx")));
	}

	private static string WriteTrx(
		string directory,
		string total,
		string executed,
		string passed,
		string failed,
		string notExecuted,
		string error,
		string? timeout)
	{
		var timeoutAttribute = timeout is null ? string.Empty : $" timeout=\"{timeout}\"";
		var xml = $"""
			<TestRun>
			  <ResultSummary>
			    <Counters total="{total}" executed="{executed}" passed="{passed}" failed="{failed}" notExecuted="{notExecuted}" error="{error}"{timeoutAttribute} />
			  </ResultSummary>
			</TestRun>
			""";
		var path = Path.Combine(directory, "result.trx");
		File.WriteAllText(path, xml);
		return path;
	}
}
