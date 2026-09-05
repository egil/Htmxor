using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

internal static class ExpectedMonitorArtifacts
{
	public const string Invoker = "src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs";
	public const string InvokerInterface = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs";
	public const string Renderer = "src/Components/Components/src/RenderTree/Renderer.cs";
	public const string StaticRenderer = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs";
	public const string InfrastructureError = "GitHub API returned 503 Service Unavailable.";

	public static IReadOnlyList<SourceChange> MixedSourceChanges() =>
	[
		new(InvokerInterface, ChangeKind.Changed, ReviewClassification.CompatibilityRisk),
		new(StaticRenderer, ChangeKind.Changed, ReviewClassification.ParityRequired),
	];

	public static IReadOnlyList<ApiChange> MixedApiChanges() =>
	[
		new("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "Task WarmAsync(CancellationToken cancellationToken)", ReviewClassification.ExtensibilityOpportunity),
		new("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "ValueTask InvokeAsync(HttpContext context)", ReviewClassification.ExtensibilityOpportunity),
		new("IRazorComponentEndpointInvoker", ChangeKind.Removed, ApiSymbolKind.Member, "Task InvokeAsync(HttpContext context)", ReviewClassification.CompatibilityRisk),
		new("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.BaseType, "RendererV2", ReviewClassification.ExtensibilityOpportunity),
		new("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Constraint, "where T : notnull, IDisposable", ReviewClassification.ExtensibilityOpportunity),
		new("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Constructor, "public StaticHtmlRenderer(IServiceProvider services)", ReviewClassification.ExtensibilityOpportunity),
		new("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Member, "protected abstract ValueTask RenderAsync(T value)", ReviewClassification.ExtensibilityOpportunity),
		new("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Member, "protected virtual bool CanRender(T value)", ReviewClassification.ExtensibilityOpportunity),
		new("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.BaseType, "Renderer", ReviewClassification.CompatibilityRisk),
		new("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Constraint, "where T : class", ReviewClassification.CompatibilityRisk),
		new("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Constructor, "protected StaticHtmlRenderer(IServiceProvider services)", ReviewClassification.CompatibilityRisk),
		new("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Member, "protected virtual Task RenderAsync(T value)", ReviewClassification.CompatibilityRisk),
		new("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Member, "public abstract string Format(T value)", ReviewClassification.CompatibilityRisk),
	];

	public static IReadOnlyList<ApiChange> InterfaceApiChanges() =>
	[
		new("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "Task WarmAsync(CancellationToken cancellationToken)", ReviewClassification.ExtensibilityOpportunity),
		new("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "ValueTask InvokeAsync(HttpContext context)", ReviewClassification.ExtensibilityOpportunity),
		new("IRazorComponentEndpointInvoker", ChangeKind.Removed, ApiSymbolKind.Member, "Task InvokeAsync(HttpContext context)", ReviewClassification.CompatibilityRisk),
	];

	public static IssueUpsertInput SingleFileIssue() => Issue(
		[new SourceReportRow(Invoker, "changed", "parity-required")],
		[]);

	public static IssueUpsertInput MixedIssue() => Issue(
		[
			new SourceReportRow(InvokerInterface, "changed", "compatibility-risk"),
			new SourceReportRow(StaticRenderer, "changed", "parity-required"),
		],
		MixedApiReportRows());

	public static ReportExpectation SingleFileDriftReport() => Drift(
		[new SourceReportRow(Invoker, "changed", "parity-required")],
		[]);

	public static ReportExpectation WatchedFilesReport() => Drift(
		[
			new SourceReportRow(Invoker, "changed", "parity-required"),
			new SourceReportRow("src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Diagnostics.cs", "added", "parity-required"),
			new SourceReportRow("src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs", "removed", "parity-required"),
			new SourceReportRow("src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs", "changed", "parity-required"),
			new SourceReportRow(StaticRenderer, "removed", "parity-required"),
		],
		[]);

	public static ReportExpectation MixedDriftReport() => Drift(
		[
			new SourceReportRow(InvokerInterface, "changed", "compatibility-risk"),
			new SourceReportRow(StaticRenderer, "changed", "parity-required"),
		],
		MixedApiReportRows());

	public static ReportExpectation InterfaceApiReport() => Drift(
		[new SourceReportRow(InvokerInterface, "changed", "compatibility-risk")],
		MixedApiReportRows().Take(3).ToArray());

	public static ReportExpectation ImplementationReviewReport() => Drift(
		[new SourceReportRow(Renderer, "changed", "implementation-review")],
		[]);

	public static ReportExpectation TypeDisappearanceReport() => Drift(
		[new SourceReportRow(InvokerInterface, "changed", "compatibility-risk")],
		[
			new ApiReportRow(
				"IRazorComponentEndpointInvoker",
				"removed",
				"type",
				"public interface IRazorComponentEndpointInvoker",
				"compatibility-risk"),
		]);

	public static ReportExpectation CurrentReport() => new(
		"current",
		new("v10.0.11", Fixture.ReviewedCommit),
		new("v10.0.11", Fixture.ReviewedCommit),
		[],
		[],
		null);

	public static ReportExpectation InfrastructureReport() => new(
		"infrastructure-error",
		new("v10.0.11", Fixture.ReviewedCommit),
		null,
		[],
		[],
		InfrastructureError);

	private static ReportExpectation Drift(
		IReadOnlyList<SourceReportRow> sourceChanges,
		IReadOnlyList<ApiReportRow> apiChanges) => new(
		"drift",
		new("v10.0.11", Fixture.BaselineCommit),
		new("v10.0.12", Fixture.TargetCommit),
		sourceChanges,
		apiChanges,
		null);

	private static IssueUpsertInput Issue(
		IReadOnlyList<SourceReportRow> sourceChanges,
		IReadOnlyList<ApiReportRow> apiChanges) => new(
		"aspnetcore-10-upstream-drift",
		"repo:egil/Htmxor is:issue label:upstream-monitor \"aspnetcore-10-upstream-drift\" in:body",
		"ASP.NET Core v10.0.12 requires Htmxor upstream review",
		IssueBody(sourceChanges, apiChanges));

	private static string IssueBody(
		IReadOnlyList<SourceReportRow> sourceChanges,
		IReadOnlyList<ApiReportRow> apiChanges) => string.Join('\n',
		[
			"## ASP.NET Core upstream drift",
			string.Empty,
			"Identity: aspnetcore-10-upstream-drift",
			string.Empty,
			$"- Previous: [v10.0.11 ({Fixture.BaselineCommit})](https://github.com/dotnet/aspnetcore/tree/{Fixture.BaselineCommit})",
			$"- Current: [v10.0.12 ({Fixture.TargetCommit})](https://github.com/dotnet/aspnetcore/tree/{Fixture.TargetCommit})",
			$"- Compare: https://github.com/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			"- Parity tests: pending review",
			string.Empty,
			"### Classified changes",
			string.Empty,
			.. sourceChanges.Select(SourceIssueLine),
			.. apiChanges.Select(ApiIssueLine),
			string.Empty,
			"### Review checklist",
			string.Empty,
			"- [ ] Review source changes",
			"- [ ] Review public/protected API changes",
			"- [ ] Run or update parity tests",
			"- [ ] Update the reviewed manifest baseline",
		]);

	private static string SourceIssueLine(SourceReportRow change) =>
		$"- {Display(change.Classification)} | {change.Kind} | {change.Path}";

	private static string ApiIssueLine(ApiReportRow change) =>
		$"- {Display(change.Classification)} | {change.Kind} | {change.SymbolKind} | {change.Type} | {change.Signature}";

	private static string Display(string classification) =>
		string.Join(' ', classification.Split('-').Select((part, index) => index == 0
			? char.ToUpperInvariant(part[0]) + part[1..]
			: part));

	private static IReadOnlyList<ApiReportRow> MixedApiReportRows() =>
	[
		new("IRazorComponentEndpointInvoker", "added", "member", "Task WarmAsync(CancellationToken cancellationToken)", "extensibility-opportunity"),
		new("IRazorComponentEndpointInvoker", "added", "member", "ValueTask InvokeAsync(HttpContext context)", "extensibility-opportunity"),
		new("IRazorComponentEndpointInvoker", "removed", "member", "Task InvokeAsync(HttpContext context)", "compatibility-risk"),
		new("StaticHtmlRenderer<T>", "added", "base-type", "RendererV2", "extensibility-opportunity"),
		new("StaticHtmlRenderer<T>", "added", "constraint", "where T : notnull, IDisposable", "extensibility-opportunity"),
		new("StaticHtmlRenderer<T>", "added", "constructor", "public StaticHtmlRenderer(IServiceProvider services)", "extensibility-opportunity"),
		new("StaticHtmlRenderer<T>", "added", "member", "protected abstract ValueTask RenderAsync(T value)", "extensibility-opportunity"),
		new("StaticHtmlRenderer<T>", "added", "member", "protected virtual bool CanRender(T value)", "extensibility-opportunity"),
		new("StaticHtmlRenderer<T>", "removed", "base-type", "Renderer", "compatibility-risk"),
		new("StaticHtmlRenderer<T>", "removed", "constraint", "where T : class", "compatibility-risk"),
		new("StaticHtmlRenderer<T>", "removed", "constructor", "protected StaticHtmlRenderer(IServiceProvider services)", "compatibility-risk"),
		new("StaticHtmlRenderer<T>", "removed", "member", "protected virtual Task RenderAsync(T value)", "compatibility-risk"),
		new("StaticHtmlRenderer<T>", "removed", "member", "public abstract string Format(T value)", "compatibility-risk"),
	];
}

internal sealed record RevisionExpectation(string Tag, string Commit);

internal sealed record SourceReportRow(string Path, string Kind, string Classification);

internal sealed record ApiReportRow(
	string Type,
	string Kind,
	string SymbolKind,
	string Signature,
	string Classification);

internal sealed record ReportExpectation(
	string Status,
	RevisionExpectation Baseline,
	RevisionExpectation? Upstream,
	IReadOnlyList<SourceReportRow> SourceChanges,
	IReadOnlyList<ApiReportRow> ApiChanges,
	string? InfrastructureError);
