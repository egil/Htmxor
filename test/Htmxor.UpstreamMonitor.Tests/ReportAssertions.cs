using System.Text.Json.Nodes;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

internal static class ReportAssertions
{
	public static void EqualApiReport(MonitorResult result)
	{
		var expected = string.Join('\n',
			"status|drift",
			$"baseline|v10.0.11|{Fixture.BaselineCommit}",
			$"upstream|v10.0.12|{Fixture.TargetCommit}",
			"api|IRazorComponentEndpointInvoker|added|member|Task WarmAsync(CancellationToken cancellationToken)|extensibility-opportunity",
			"api|IRazorComponentEndpointInvoker|added|member|ValueTask InvokeAsync(HttpContext context)|extensibility-opportunity",
			"api|IRazorComponentEndpointInvoker|removed|member|Task InvokeAsync(HttpContext context)|compatibility-risk",
			"api|StaticHtmlRenderer<T>|added|base-type|RendererV2|extensibility-opportunity",
			"api|StaticHtmlRenderer<T>|added|constraint|where T : notnull, IDisposable|extensibility-opportunity",
			"api|StaticHtmlRenderer<T>|added|constructor|public StaticHtmlRenderer(IServiceProvider services)|extensibility-opportunity",
			"api|StaticHtmlRenderer<T>|added|member|protected abstract ValueTask RenderAsync(T value)|extensibility-opportunity",
			"api|StaticHtmlRenderer<T>|added|member|protected virtual bool CanRender(T value)|extensibility-opportunity",
			"api|StaticHtmlRenderer<T>|removed|base-type|Renderer|compatibility-risk",
			"api|StaticHtmlRenderer<T>|removed|constraint|where T : class|compatibility-risk",
			"api|StaticHtmlRenderer<T>|removed|constructor|protected StaticHtmlRenderer(IServiceProvider services)|compatibility-risk",
			"api|StaticHtmlRenderer<T>|removed|member|protected virtual Task RenderAsync(T value)|compatibility-risk",
			"api|StaticHtmlRenderer<T>|removed|member|public abstract string Format(T value)|compatibility-risk");

		Assert.Equal(expected, ProjectApiJson(result.JsonReport));
		Assert.Equal(expected, ProjectApiMarkdown(result.MarkdownReport));
	}

	public static void EqualDriftReport(MonitorResult result)
	{
		var expectedJson = JsonNode.Parse(
			$$"""
			{
			  "status": "drift",
			  "baseline": {
			    "tag": "v10.0.11",
			    "commit": "{{Fixture.BaselineCommit}}"
			  },
			  "upstream": {
			    "tag": "v10.0.12",
			    "commit": "{{Fixture.TargetCommit}}"
			  },
			  "sourceChanges": [
			    {
			      "path": "src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs",
			      "kind": "changed",
			      "classification": "parity-required"
			    },
			    {
			      "path": "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Diagnostics.cs",
			      "kind": "added",
			      "classification": "parity-required"
			    },
			    {
			      "path": "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs",
			      "kind": "removed",
			      "classification": "parity-required"
			    },
			    {
			      "path": "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs",
			      "kind": "changed",
			      "classification": "parity-required"
			    },
			    {
			      "path": "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs",
			      "kind": "removed",
			      "classification": "parity-required"
			    }
			  ],
			  "apiChanges": []
			}
			""");
		var actualJson = ParseJson(result.JsonReport);
		Assert.True(
			JsonNode.DeepEquals(expectedJson, actualJson),
			$"Expected JSON:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual JSON:{Environment.NewLine}{actualJson}");

		Assert.Equal(
			NormalizeMarkdown(
				$$"""
				# ASP.NET Core upstream monitor

				Status: drift
				Baseline: v10.0.11 ({{Fixture.BaselineCommit}})
				Upstream: v10.0.12 ({{Fixture.TargetCommit}})

				## Source changes

				- parity-required | changed | src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs
				- parity-required | added | src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Diagnostics.cs
				- parity-required | removed | src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs
				- parity-required | changed | src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs
				- parity-required | removed | src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs

				## API changes

				None.
				"""),
			NormalizeMarkdown(result.MarkdownReport));
	}

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

	private static string ProjectApiJson(string report)
	{
		try
		{
			using var document = System.Text.Json.JsonDocument.Parse(report);
			var root = document.RootElement;
			var lines = new List<string>
			{
				$"status|{root.GetProperty("status").GetString()}",
				$"baseline|{Revision(root.GetProperty("baseline"))}",
				$"upstream|{Revision(root.GetProperty("upstream"))}",
			};
			lines.AddRange(root.GetProperty("apiChanges").EnumerateArray().Select(ApiLine));
			return string.Join('\n', lines);
		}
		catch (System.Text.Json.JsonException exception)
		{
			return $"invalid-json:{exception.Message}";
		}
	}

	private static string ProjectApiMarkdown(string report) =>
		string.Join('\n', report.Split('\n')
			.Select(line => line.Trim())
			.Where(line =>
				line.StartsWith("status|", StringComparison.OrdinalIgnoreCase)
				|| line.StartsWith("baseline|", StringComparison.OrdinalIgnoreCase)
				|| line.StartsWith("upstream|", StringComparison.OrdinalIgnoreCase)
				|| line.StartsWith("api|", StringComparison.OrdinalIgnoreCase)));

	private static string Revision(System.Text.Json.JsonElement revision) =>
		$"{revision.GetProperty("tag").GetString()}|{revision.GetProperty("commit").GetString()}";

	private static string ApiLine(System.Text.Json.JsonElement change) => string.Join('|',
		"api",
		change.GetProperty("type").GetString(),
		change.GetProperty("kind").GetString(),
		change.GetProperty("symbolKind").GetString(),
		change.GetProperty("signature").GetString(),
		change.GetProperty("classification").GetString());

	private static string NormalizeMarkdown(string report) =>
		report.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
