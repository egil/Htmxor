using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ApiSurfaceChangeTests
{
	private const string StaticRendererPath = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs";
	private const string InvokerPath = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs";

	[Fact]
	public async Task Subclass_and_interface_surfaces_report_public_and_protected_API_changes()
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-api-files.json");
		AddSourceResponses(transport, StaticRendererPath, "StaticHtmlRenderer.cs");
		AddSourceResponses(transport, InvokerPath, "IRazorComponentEndpointInvoker.cs");
		var application = Fixture.Application(transport);
		var manifest = Fixture.Manifest(
			Fixture.Watch(StaticRendererPath, apiSurface: ApiSurface.Subclass),
			Fixture.Watch(InvokerPath, apiSurface: ApiSurface.Interface));
		var request = new MonitorRequest(
			manifest,
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Equal(
			[
				new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "Task WarmAsync(CancellationToken cancellationToken)", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "ValueTask InvokeAsync(HttpContext context)", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Removed, ApiSymbolKind.Member, "Task InvokeAsync(HttpContext context)", ReviewClassification.CompatibilityRisk),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.BaseType, "RendererV2", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Constraint, "where T : notnull, IDisposable", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Constructor, "public StaticHtmlRenderer(IServiceProvider services)", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Member, "protected abstract ValueTask RenderAsync(T value)", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Member, "protected virtual bool CanRender(T value)", ReviewClassification.ExtensibilityOpportunity),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.BaseType, "Renderer", ReviewClassification.CompatibilityRisk),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Constraint, "where T : class", ReviewClassification.CompatibilityRisk),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Constructor, "protected StaticHtmlRenderer(IServiceProvider services)", ReviewClassification.CompatibilityRisk),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Member, "protected virtual Task RenderAsync(T value)", ReviewClassification.CompatibilityRisk),
				new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Member, "public abstract string Format(T value)", ReviewClassification.CompatibilityRisk),
			],
			result.ApiChanges.OrderBy(change => change.TypeName).ThenBy(change => change.Kind).ThenBy(change => change.SymbolKind).ThenBy(change => change.Signature));
		ReportAssertions.EqualApiReport(result);
	}

	[Fact]
	public async Task Watched_interface_disappearance_is_a_compatibility_risk()
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-interface-disappeared.json");
		AddSourceResponses(transport, InvokerPath, "IRazorComponentEndpointInvoker.Disappeared.cs");
		var application = Fixture.Application(transport);
		var request = new MonitorRequest(
			Fixture.Manifest(Fixture.Watch(
				InvokerPath,
				apiSurface: ApiSurface.Interface,
				relationship: WatchRelationship.Implements)),
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.Equal(
			[
				new ApiChange(
					"IRazorComponentEndpointInvoker",
					ChangeKind.Removed,
					ApiSymbolKind.Type,
					"public interface IRazorComponentEndpointInvoker",
					ReviewClassification.CompatibilityRisk),
			],
			result.ApiChanges);
	}

	private static void AddSourceResponses(
		FakeGitHubTransport transport,
		string upstreamPath,
		string fixtureName)
	{
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{upstreamPath}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent($"source/baseline/{fixtureName}"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{upstreamPath}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent($"source/target/{fixtureName}"));
	}
}
