namespace Htmxor.UpstreamMonitor;

internal enum MonitorStatus
{
	Current,
	Drift,
	InfrastructureError,
}

internal enum WatchMatch
{
	File,
	Prefix,
}

internal enum ApiSurface
{
	None,
	Subclass,
	Interface,
}

internal enum ChangeKind
{
	Added,
	Removed,
	Changed,
}

internal enum ApiSymbolKind
{
	BaseType,
	Constraint,
	Constructor,
	Member,
}

internal sealed record WatchTarget(
	string Path,
	WatchMatch Match,
	ApiSurface ApiSurface,
	IReadOnlyList<string> LocalDependencies);

internal sealed record WatchManifest(
	string Repository,
	string ReviewedTag,
	string ReviewedCommit,
	IReadOnlyList<WatchTarget> Targets);

internal sealed record MonitorRequest(
	WatchManifest Manifest,
	int SupportedMajorVersion,
	string? RequestedTag = null,
	string? BaselineCommit = null);

internal sealed record UpstreamRevision(string Tag, string Commit);

internal sealed record SourceChange(string Path, ChangeKind Kind);

internal sealed record ApiChange(
	string TypeName,
	ChangeKind Kind,
	ApiSymbolKind SymbolKind,
	string Signature);

internal sealed record IssueUpsertInput(
	string Identity,
	string SearchQuery,
	string Title,
	string Body);

internal sealed record MonitorResult(
	MonitorStatus Status,
	UpstreamRevision? Upstream,
	IReadOnlyList<SourceChange> SourceChanges,
	IReadOnlyList<ApiChange> ApiChanges,
	string JsonReport,
	string MarkdownReport,
	IssueUpsertInput? Issue,
	string? InfrastructureError);
