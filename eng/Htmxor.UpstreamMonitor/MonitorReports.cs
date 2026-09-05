using System.Text.Json;
using System.Text.Json.Nodes;

namespace Htmxor.UpstreamMonitor;

internal static class MonitorReports
{
	public static MonitorResult Create(MonitorRequest request, MonitorStatus status, UpstreamRevision? upstream,
		IReadOnlyList<SourceChange> sourceChanges, IReadOnlyList<ApiChange> apiChanges, string? error = null)
	{
		var sources = sourceChanges.OrderBy(change => change.Path, StringComparer.Ordinal).ThenBy(change => change.Kind).ToArray();
		var apis = apiChanges.Distinct().OrderBy(change => change.TypeName, StringComparer.Ordinal).ThenBy(change => change.Kind)
			.ThenBy(change => change.SymbolKind).ThenBy(change => change.Signature, StringComparer.Ordinal).ToArray();
		var baselineCommit = request.BaselineCommit ?? request.Manifest.ReviewedCommit;
		var baselineTag = baselineCommit == request.Manifest.ReviewedCommit ? request.Manifest.ReviewedTag : "unresolved";
		var baseline = new UpstreamRevision(baselineTag, baselineCommit);
		return new(status, upstream, sources, apis, Json(status, baseline, upstream, sources, apis, error),
			Markdown(status, baseline, upstream, sources, apis, error),
			status == MonitorStatus.Drift ? Issue(request, baseline, upstream!, sources, apis) : null, error);
	}

	private static string Json(MonitorStatus status, UpstreamRevision baseline, UpstreamRevision? upstream,
		SourceChange[] sources, ApiChange[] apis, string? error)
	{
		var report = new JsonObject
		{
			["status"] = Name(status),
			["baseline"] = RevisionJson(baseline),
			["upstream"] = upstream is null ? null : RevisionJson(upstream),
			["sourceChanges"] = JsonSerializer.SerializeToNode(sources.Select(change => new
			{
				path = change.Path, kind = Name(change.Kind), classification = Name(change.Classification),
			})),
			["apiChanges"] = JsonSerializer.SerializeToNode(apis.Select(change => new
			{
				type = change.TypeName, kind = Name(change.Kind), symbolKind = Name(change.SymbolKind),
				signature = change.Signature, classification = Name(change.Classification),
			})),
		};
		if (error is not null)
		{
			report["infrastructureError"] = error;
		}
		return report.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
	}

	private static JsonObject RevisionJson(UpstreamRevision revision) => new() { ["tag"] = revision.Tag, ["commit"] = revision.Commit };

	private static string Markdown(MonitorStatus status, UpstreamRevision baseline, UpstreamRevision? upstream,
		SourceChange[] sources, ApiChange[] apis, string? error) => string.Join('\n',
		[
			"# ASP.NET Core upstream monitor", string.Empty, $"Status: {Name(status)}",
			$"Baseline: {Revision(baseline)}", $"Upstream: {(upstream is null ? "unavailable" : Revision(upstream))}",
			.. error is null ? Array.Empty<string>() : [$"Infrastructure error: {error}"],
			string.Empty, "## Source changes", string.Empty,
			.. sources.Length == 0 ? ["None."] : sources.Select(change => $"- {Name(change.Classification)} | {SourceRow(change)}"),
			string.Empty, "## API changes", string.Empty,
			.. apis.Length == 0 ? ["None."] : apis.Select(change => $"- {Name(change.Classification)} | {ApiRow(change)}"),
		]);

	private static IssueUpsertInput Issue(MonitorRequest request, UpstreamRevision baseline, UpstreamRevision upstream,
		SourceChange[] sources, ApiChange[] apis)
	{
		var identity = $"aspnetcore-{request.SupportedMajorVersion}-upstream-drift";
		var url = $"https://github.com/{request.Manifest.Repository}";
		var body = string.Join('\n',
		[
			"## ASP.NET Core upstream drift", string.Empty, $"Identity: {identity}", string.Empty,
			$"- Previous: [{Revision(baseline)}]({url}/tree/{baseline.Commit})",
			$"- Current: [{Revision(upstream)}]({url}/tree/{upstream.Commit})",
			$"- Compare: {url}/compare/{baseline.Commit}...{upstream.Commit}", "- Parity tests: pending review",
			string.Empty, "### Classified changes", string.Empty,
			.. sources.Select(change => $"- {Display(change.Classification)} | {SourceRow(change)}"),
			.. apis.Select(change => $"- {Display(change.Classification)} | {ApiRow(change)}"),
			string.Empty, "### Review checklist", string.Empty,
			"- [ ] Review source changes", "- [ ] Review public/protected API changes",
			"- [ ] Run or update parity tests", "- [ ] Update the reviewed manifest baseline",
		]);
		return new(identity, $"repo:egil/Htmxor is:issue label:upstream-monitor \"{identity}\" in:body",
			$"ASP.NET Core {upstream.Tag} requires Htmxor upstream review", body);
	}

	private static string Revision(UpstreamRevision revision) => $"{revision.Tag} ({revision.Commit})";
	private static string SourceRow(SourceChange change) => $"{Name(change.Kind)} | {change.Path}";
	private static string ApiRow(ApiChange change) => $"{Name(change.Kind)} | {Name(change.SymbolKind)} | {change.TypeName} | {change.Signature}";
	private static string Display(ReviewClassification value)
	{
		var name = Name(value).Replace('-', ' ');
		return char.ToUpperInvariant(name[0]) + name[1..];
	}

	private static string Name<T>(T value) where T : Enum => JsonNamingPolicy.KebabCaseLower.ConvertName(value.ToString());
}
