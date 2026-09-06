using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ApiSurfaceChangeTests
{
	private const string StaticRendererPath = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs";
	private const string InvokerPath = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs";

	[Theory]
	[InlineData("protected void Render(int[] values) { }", "protected void Render(int values) { }",
		"protected void Render(int[] values)", "protected void Render(int values)")]
	[InlineData("public int Count { get; }", "public int Count { get; set; }",
		"public int Count { get; }", "public int Count { get; set; }")]
	[InlineData("protected void Render(string value = \"a\") { }", "protected void Render(string value = \"b\") { }",
		"protected void Render(string value = \"a\")", "protected void Render(string value = \"b\")")]
	public async Task Changed_parameter_accessor_or_default_reports_exact_consumed_contracts(
		string beforeSource, string afterSource, string before, string after)
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-api-files.json");
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{StaticRendererPath}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContentText($"public abstract class StaticHtmlRenderer {{ {beforeSource} }}"));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{StaticRendererPath}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText($"public abstract class StaticHtmlRenderer {{ {afterSource} }}"));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(StaticRendererPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses)));

		Assert.Equal(new[]
		{
			new ApiChange("StaticHtmlRenderer", ChangeKind.Added, ApiSymbolKind.Member, after, ReviewClassification.ExtensibilityOpportunity),
			new ApiChange("StaticHtmlRenderer", ChangeKind.Removed, ApiSymbolKind.Member, before, ReviewClassification.CompatibilityRisk),
		}, result.ApiChanges);
		ReportAssertions.Equal(result, new ReportExpectation("drift", new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
			[new(StaticRendererPath, "changed", "compatibility-risk")],
			[new("StaticHtmlRenderer", "added", "member", after, "extensibility-opportunity"),
			 new("StaticHtmlRenderer", "removed", "member", before, "compatibility-risk")], null));
	}

	[Fact]
	public async Task Primary_constructor_parameter_change_reports_exact_consumed_contracts()
	{
		const string rendererPath = "src/Components/Components/src/RenderTree/Renderer.cs";
		var transport = SourceChangeTests.DriftTransport("github/compare-renderer-implementation.json");
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{rendererPath}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContentText("public class Renderer(int value) { }"));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{rendererPath}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText("public class Renderer(string value) { }"));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(rendererPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses)));

		Assert.Equal(new[]
		{
			new ApiChange("Renderer", ChangeKind.Added, ApiSymbolKind.Constructor, "public Renderer(string value)", ReviewClassification.ExtensibilityOpportunity),
			new ApiChange("Renderer", ChangeKind.Removed, ApiSymbolKind.Constructor, "public Renderer(int value)", ReviewClassification.CompatibilityRisk),
		}, result.ApiChanges);
		ReportAssertions.Equal(result, new ReportExpectation("drift", new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
			[new(rendererPath, "changed", "compatibility-risk")],
			[new("Renderer", "added", "constructor", "public Renderer(string value)", "extensibility-opportunity"),
			 new("Renderer", "removed", "constructor", "public Renderer(int value)", "compatibility-risk")], null));
	}

	[Fact]
	public async Task Virtual_to_abstract_modifier_alone_changes_the_consumed_contract()
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-api-files.json");
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{StaticRendererPath}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContentText("public abstract class StaticHtmlRenderer { protected virtual void Render() { } }"));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{StaticRendererPath}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText("public abstract class StaticHtmlRenderer { protected abstract void Render(); }"));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(StaticRendererPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses)));

		Assert.Equal(new[]
		{
			new ApiChange("StaticHtmlRenderer", ChangeKind.Added, ApiSymbolKind.Member, "protected abstract void Render()", ReviewClassification.ExtensibilityOpportunity),
			new ApiChange("StaticHtmlRenderer", ChangeKind.Removed, ApiSymbolKind.Member, "protected virtual void Render()", ReviewClassification.CompatibilityRisk),
		}, result.ApiChanges.OrderBy(change => change.Kind));
		ReportAssertions.Equal(result, new ReportExpectation("drift", new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
			[new(StaticRendererPath, "changed", "compatibility-risk")],
			[new("StaticHtmlRenderer", "added", "member", "protected abstract void Render()", "extensibility-opportunity"),
			 new("StaticHtmlRenderer", "removed", "member", "protected virtual void Render()", "compatibility-risk")], null));
	}

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
