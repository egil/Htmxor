using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class MutationReportTests
{
	[Fact]
	public void Characterize_maps_every_Stryker_4_16_status()
	{
		var json = Report(
			"Pending",
			"Killed",
			"Survived",
			"Timeout",
			"CompileError",
			"Ignored",
			"NoCoverage",
			"RuntimeError");

		var result = MutationReport.Characterize(json);

		Assert.Equal(new MutationCharacterization(8, 5, 1, 1, 3, 1, 1, 1), result);
	}

	[Fact]
	public void Validity_allows_survivors_without_inventing_a_score_floor()
	{
		var result = MutationReport.Characterize(Report("Killed", "Survived", "NoCoverage"));

		Assert.Empty(result.GetValidityFailures());
		Assert.Equal(1, result.Survived);
		Assert.Equal(1, result.Skipped);
	}

	[Fact]
	public void Characterize_preserves_the_old_hosted_baseline_categories()
	{
		var statuses = Enumerable.Repeat("Killed", 455)
			.Concat(Enumerable.Repeat("Survived", 65))
			.Concat(Enumerable.Repeat("Timeout", 73))
			.Concat(Enumerable.Repeat("NoCoverage", 625))
			.Concat(Enumerable.Repeat("Ignored", 327))
			.Concat(Enumerable.Repeat("CompileError", 151))
			.ToArray();

		var result = MutationReport.Characterize(Report(statuses));

		Assert.Equal(new MutationCharacterization(1696, 593, 455, 65, 1103, 73, 0, 0), result);
	}

	[Theory]
	[InlineData(new string[0], "zero mutants")]
	[InlineData(new[] { "Ignored" }, "zero eligible")]
	[InlineData(new[] { "Survived" }, "killed zero")]
	[InlineData(new[] { "Killed", "Timeout" }, "timed-out")]
	[InlineData(new[] { "Killed", "RuntimeError" }, "error mutants")]
	[InlineData(new[] { "Killed", "Pending" }, "pending")]
	public void Validity_rejects_invalid_results(string[] statuses, string expected)
	{
		var result = MutationReport.Characterize(Report(statuses));

		Assert.Contains(result.GetValidityFailures(), failure =>
			failure.Contains(expected, StringComparison.OrdinalIgnoreCase));
	}

	[Theory]
	[InlineData("NewStatus")]
	[InlineData("1")]
	public void Characterize_rejects_an_unknown_status(string status)
	{
		var exception = Assert.Throws<InvalidOperationException>(
			() => MutationReport.Characterize(Report(status)));

		Assert.Contains(status, exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Characterize_rejects_a_missing_status()
	{
		const string report = """
			{"files":{"source":{"mutants":[{}]}}}
			""";

		var exception = Assert.Throws<InvalidOperationException>(
			() => MutationReport.Characterize(report));

		Assert.Contains("<missing>", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Characterize_rejects_malformed_json()
	{
		Assert.ThrowsAny<JsonException>(() => MutationReport.Characterize("{"));
	}

	private static string Report(params string[] statuses)
	{
		var mutants = statuses.Select(status => new { status }).ToArray();
		return JsonSerializer.Serialize(new { files = new { source = new { mutants } } });
	}
}
