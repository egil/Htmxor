using System.Text.Json;
using System.Text.Json.Nodes;

namespace Htmxor.UpstreamMonitor;

internal static class MonitorReports
{
	public static MonitorResult Create(MonitorRequest request, MonitorStatus status, UpstreamRevision? upstream,
		IReadOnlyList<SourceChange> sourceChanges, IReadOnlyList<ApiChange> apiChanges, string? error = null,
		IReadOnlyList<string>? unresolvedWatchPaths = null)
	{
		var unresolved = (unresolvedWatchPaths ?? []).OrderBy(path => path, StringComparer.Ordinal).ToArray();
		var sources = sourceChanges.OrderBy(change => change.Path, StringComparer.Ordinal).ThenBy(change => change.Kind).ToArray();
		var apis = apiChanges.Distinct().OrderBy(change => change.TypeName, StringComparer.Ordinal).ThenBy(change => change.Kind)
			.ThenBy(change => change.SymbolKind).ThenBy(change => change.Signature, StringComparer.Ordinal).ToArray();
		var baselineCommit = request.BaselineCommit ?? request.Framework.ReviewedCommit;
		var baselineTag = baselineCommit == request.Framework.ReviewedCommit ? request.Framework.ReviewedTag : "unresolved";
		var baseline = new UpstreamRevision(baselineTag, baselineCommit);
		return new(status, upstream, sources, apis, Json(status, baseline, upstream, sources, apis, error, unresolved),
			Markdown(status, baseline, upstream, sources, apis, error, unresolved),
			IssuesFor(request, status, baseline, upstream, sources, apis, unresolved), error, unresolved);
	}

	// An unresolved watch is a manifest defect, not upstream drift: the path names a dependency the
	// monitor cannot see at the reviewed commit, so every later run would report it current. It gets
	// its own status, its own report section and its own review issue rather than a SourceChange, so
	// a reader can never mistake it for a change in a file that does exist. The separate issue
	// identity also keeps the two kinds out of one upserted body, where a full replace would drop
	// whichever ran first — see issue #238.
	// A run reports one issue per finding kind it actually found, not one issue for its summary
	// status. A single unresolved path must not suppress the drift issue for every other watch: that
	// would make the review issue a property of the run, which is the same shape as the defect this
	// change exists to remove. Current and InfrastructureError report nothing.
	private static IReadOnlyList<IssueUpsertInput> IssuesFor(MonitorRequest request, MonitorStatus status,
		UpstreamRevision baseline, UpstreamRevision? upstream, SourceChange[] sources, ApiChange[] apis, string[] unresolved)
	{
		if (status is not (MonitorStatus.Drift or MonitorStatus.UnresolvedWatch))
		{
			return [];
		}
		var issues = new List<IssueUpsertInput>();
		if (sources.Length > 0)
		{
			issues.Add(Issue(request, baseline, upstream!, sources, apis));
		}
		if (unresolved.Length > 0)
		{
			issues.Add(UnresolvedIssue(request, baseline, upstream!, unresolved));
		}
		return issues;
	}

	private static IssueUpsertInput UnresolvedIssue(MonitorRequest request, UpstreamRevision baseline,
		UpstreamRevision upstream, string[] unresolved)
	{
		var identity = $"aspnetcore-{request.SupportedMajorVersion}-unresolved-watch";
		var url = $"https://github.com/{request.Manifest.Repository}";
		var body = string.Join('\n',
		[
			"## ASP.NET Core watch paths that do not exist upstream", string.Empty, $"Identity: {identity}", string.Empty,
			$"- Reviewed: [{Revision(baseline)}]({url}/tree/{baseline.Commit})",
			$"- Current: [{Revision(upstream)}]({url}/tree/{upstream.Commit})",
			"- These dependencies are unmonitored: a path that cannot resolve never appears in a compare,",
			"  so drift in it is reported as current on every run until the manifest is corrected.",
			string.Empty, "### Unresolved watch paths", string.Empty,
			.. unresolved.Select(path => $"- [{path}]({url}/tree/{baseline.Commit}/{path})"),
			string.Empty, "### Review checklist", string.Empty,
			"- [ ] Correct or remove each path above", "- [ ] Confirm the corrected path is the right upstream dependency",
			"- [ ] Re-run the monitor and confirm the watch reports against real content",
		]);
		return new(identity, $"repo:egil/Htmxor is:issue label:upstream-monitor \"{identity}\" in:body",
			$"Htmxor upstream watch paths do not exist at {upstream.Tag}", body);
	}

	private static string Json(MonitorStatus status, UpstreamRevision baseline, UpstreamRevision? upstream,
		SourceChange[] sources, ApiChange[] apis, string? error, string[] unresolved)
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
		if (unresolved.Length > 0)
		{
			report["unresolvedWatchPaths"] = JsonSerializer.SerializeToNode(unresolved);
		}
		if (error is not null)
		{
			report["infrastructureError"] = error;
		}
		return report.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
	}

	private static JsonObject RevisionJson(UpstreamRevision revision) => new() { ["tag"] = revision.Tag, ["commit"] = revision.Commit };

	private static string Markdown(MonitorStatus status, UpstreamRevision baseline, UpstreamRevision? upstream,
		SourceChange[] sources, ApiChange[] apis, string? error, string[] unresolved) => string.Join('\n',
		[
			"# ASP.NET Core upstream monitor", string.Empty, $"Status: {Name(status)}",
			$"Baseline: {Revision(baseline)}", $"Upstream: {(upstream is null ? "unavailable" : Revision(upstream))}",
			.. error is null ? Array.Empty<string>() : [$"Infrastructure error: {error}"],
			string.Empty, "## Source changes", string.Empty,
			.. sources.Length == 0 ? ["None."] : sources.Select(change => $"- {Name(change.Classification)} | {SourceRow(change)}"),
			string.Empty, "## API changes", string.Empty,
			.. apis.Length == 0 ? ["None."] : apis.Select(change => $"- {Name(change.Classification)} | {ApiRow(change)}"),
			.. unresolved.Length == 0 ? Array.Empty<string>() :
				[string.Empty, "## Unresolved watch paths", string.Empty,
					.. unresolved.Select(path => $"- does-not-exist-upstream | {path}")],
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
