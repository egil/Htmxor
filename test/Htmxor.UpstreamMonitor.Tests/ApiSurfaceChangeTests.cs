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
			Fixture.Watch(StaticRendererPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Mirrors),
			Fixture.Watch(InvokerPath, apiSurface: ApiSurface.Interface, relationship: WatchRelationship.Implements));
		var request = new MonitorRequest(
			manifest,
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Equal(ExpectedMonitorArtifacts.MixedSourceChanges(), result.SourceChanges);
		Assert.Equal(
			ExpectedMonitorArtifacts.MixedApiChanges(),
			result.ApiChanges.OrderBy(change => change.TypeName).ThenBy(change => change.Kind).ThenBy(change => change.SymbolKind).ThenBy(change => change.Signature));
		Assert.Equal(ExpectedMonitorArtifacts.MixedIssue(), result.Issue);
		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.MixedDriftReport());
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
		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.TypeDisappearanceReport());
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
