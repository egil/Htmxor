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
	IReadOnlyList<string> LocalDependencies)
{
	// Null means the watch applies to every configured framework. WatchManifestFile rejects an
	// empty list, so a non-null list always names at least one.
	//
	// Only coverage honours this. The drift comparison walk stays framework-blind on purpose: it
	// reports what a framework's own compare already says changed, which is a true observation
	// whichever framework the watch names, while a scope applied there would suppress one. See #241.
	public IReadOnlyList<string>? Frameworks { get; init; }
}

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
	IssueUpsertInput? Issue,
	string? InfrastructureError);

internal enum IssueWriteAction
{
	None,
	Created,
	Updated,
	ReopenedAndUpdated,
}

internal sealed record IssueWriteResult(IssueWriteAction Action, long? IssueNumber, string? Error);
