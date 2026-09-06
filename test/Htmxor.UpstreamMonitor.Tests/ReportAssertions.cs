using System.Text.Json.Nodes;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

internal static class ReportAssertions
{
	public static void Equal(MonitorResult result, ReportExpectation expectation) =>
		Equal(result.JsonReport, result.MarkdownReport, expectation);

	public static void Equal(string json, string markdown, ReportExpectation expectation)
	{
		var expectedJson = Json(expectation);
		var actualJson = ParseJson(json);
		Assert.True(
			JsonNode.DeepEquals(expectedJson, actualJson),
			$"Expected JSON:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual JSON:{Environment.NewLine}{actualJson}");
		Assert.Equal(Normalize(Markdown(expectation)), Normalize(markdown));
	}

	private static JsonObject Json(ReportExpectation expectation)
	{
		var json = new JsonObject
		{
			["status"] = expectation.Status,
			["baseline"] = Revision(expectation.Baseline),
			["upstream"] = expectation.Upstream is null ? null : Revision(expectation.Upstream),
			["sourceChanges"] = new JsonArray(expectation.SourceChanges.Select(SourceJson).ToArray()),
			["apiChanges"] = new JsonArray(expectation.ApiChanges.Select(ApiJson).ToArray()),
		};
		if (expectation.InfrastructureError is not null)
		{
			json["infrastructureError"] = expectation.InfrastructureError;
		}

		return json;
	}

	private static JsonObject Revision(RevisionExpectation revision) => new()
	{
		["tag"] = revision.Tag,
		["commit"] = revision.Commit,
	};

	private static JsonNode SourceJson(SourceReportRow change) => new JsonObject
	{
		["path"] = change.Path,
		["kind"] = change.Kind,
		["classification"] = change.Classification,
	};

	private static JsonNode ApiJson(ApiReportRow change) => new JsonObject
	{
		["type"] = change.Type,
		["kind"] = change.Kind,
		["symbolKind"] = change.SymbolKind,
		["signature"] = change.Signature,
		["classification"] = change.Classification,
	};

	private static string Markdown(ReportExpectation expectation) => string.Join('\n',
	[
		"# ASP.NET Core upstream monitor",
		string.Empty,
		$"Status: {expectation.Status}",
		$"Baseline: {RevisionLine(expectation.Baseline)}",
		$"Upstream: {UpstreamLine(expectation.Upstream)}",
		.. ErrorLines(expectation.InfrastructureError),
		string.Empty,
		"## Source changes",
		string.Empty,
		.. SourceLines(expectation.SourceChanges),
		string.Empty,
		"## API changes",
		string.Empty,
		.. ApiLines(expectation.ApiChanges),
	]);

	private static string RevisionLine(RevisionExpectation revision) =>
		$"{revision.Tag} ({revision.Commit})";

	private static string UpstreamLine(RevisionExpectation? revision) =>
		revision is null ? "unavailable" : RevisionLine(revision);

	private static IEnumerable<string> ErrorLines(string? error) =>
		error is null ? [] : [$"Infrastructure error: {error}"];

	private static IEnumerable<string> SourceLines(IReadOnlyList<SourceReportRow> changes) =>
		changes.Count == 0
			? ["None."]
			: changes.Select(change => $"- {change.Classification} | {change.Kind} | {change.Path}");

	private static IEnumerable<string> ApiLines(IReadOnlyList<ApiReportRow> changes) =>
		changes.Count == 0
			? ["None."]
			: changes.Select(change => $"- {change.Classification} | {change.Kind} | {change.SymbolKind} | {change.Type} | {change.Signature}");

	private static JsonNode? ParseJson(string report)
	{
		try
		{
			return JsonNode.Parse(report);
		}
		catch (System.Text.Json.JsonException exception)
		{
			return JsonValue.Create($"invalid-json:{exception.Message}");
		}
	}

	private static string Normalize(string report) =>
		report.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
