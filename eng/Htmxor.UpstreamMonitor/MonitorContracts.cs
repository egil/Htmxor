namespace Htmxor.UpstreamMonitor;

// The value is the process exit code: Program returns (int)status, QualityCommand classifies it,
// and docs/agents/testing.md documents each one. They are written out so reordering the members
// cannot silently change what a run reports to its callers.
internal enum MonitorStatus
{
	Current = 0,
	Drift = 1,
	InfrastructureError = 2,
	UnresolvedWatch = 3,
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

internal enum WatchRelationship
{
	Mirrors,
	Reimplements,
	Subclasses,
	Implements,
	PrivateAccesses,
}

internal enum ReviewClassification
{
	ParityRequired,
	CompatibilityRisk,
	ExtensibilityOpportunity,
	ImplementationReview,
}

internal enum ChangeKind
{
	Added,
	Removed,
	Changed,
}

internal enum ApiSymbolKind
{
	Type,
	BaseType,
	Constraint,
	Constructor,
	Member,
}

internal sealed record WatchTarget(
	string Path,
	WatchMatch Match,
	ApiSurface ApiSurface,
	WatchRelationship Relationship,
	IReadOnlyList<string> LocalDependencies);

internal sealed record WatchManifest(
	string Repository,
	IReadOnlyList<FrameworkBaseline> Frameworks,
	IReadOnlyList<WatchTarget> Targets)
{
	public WatchManifest(string repository, string reviewedTag, string reviewedCommit, IReadOnlyList<WatchTarget> targets)
		: this(repository, [new("net10.0", 10, false, "10.0.11", reviewedTag, reviewedCommit)], targets)
	{
	}

	public string ReviewedTag => Frameworks.Single(framework => framework.MajorVersion == 10).ReviewedTag;
	public string ReviewedCommit => Frameworks.Single(framework => framework.MajorVersion == 10).ReviewedCommit;
}

internal sealed record FrameworkBaseline(
	string TargetFramework,
	int MajorVersion,
	bool AllowsPrerelease,
	string ReferencePackVersion,
	string ReviewedTag,
	string ReviewedCommit);

internal sealed record LocalFrameworkDependency(
	string LocalPath,
	string UpstreamPath,
	WatchRelationship Relationship);

internal sealed record MonitorRequest(
	WatchManifest Manifest,
	FrameworkBaseline Framework,
	string? RequestedTag = null,
	string? BaselineCommit = null)
{
	public MonitorRequest(WatchManifest manifest, int SupportedMajorVersion, string? RequestedTag = null, string? BaselineCommit = null)
		: this(manifest, manifest.Frameworks.Single(framework => framework.MajorVersion == SupportedMajorVersion), RequestedTag, BaselineCommit)
	{
	}

	public int SupportedMajorVersion => Framework.MajorVersion;
}

internal sealed record UpstreamRevision(string Tag, string Commit);

internal sealed record SourceChange(
	string Path,
	ChangeKind Kind,
	ReviewClassification Classification);

internal sealed record ApiChange(
	string TypeName,
	ChangeKind Kind,
	ApiSymbolKind SymbolKind,
	string Signature,
	ReviewClassification Classification);

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
	IReadOnlyList<IssueUpsertInput> Issues,
	string? InfrastructureError,
	IReadOnlyList<string>? UnresolvedWatchPaths = null);

internal enum IssueWriteAction
{
	None,
	Created,
	Updated,
	ReopenedAndUpdated,
}

internal sealed record IssueWriteResult(IssueWriteAction Action, long? IssueNumber, string? Error);
