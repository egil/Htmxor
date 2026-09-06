using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ApiDeclarationRegressionTests
{
	[Fact]
	public async Task Interface_default_literals_preserve_implicitly_public_contract_changes()
	{
		var result = await CompareAsync(ExpectedMonitorArtifacts.InvokerInterface, ApiSurface.Interface,
			WatchRelationship.Implements,
			"public interface IRazorComponentEndpointInvoker { void Render(string scope = \"private\"); }",
			"public interface IRazorComponentEndpointInvoker { void Render(string scope = \"internal\"); }");

		Assert.Equal(new[]
		{
			new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member,
				"void Render(string scope = \"internal\")", ReviewClassification.ExtensibilityOpportunity),
			new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Removed, ApiSymbolKind.Member,
				"void Render(string scope = \"private\")", ReviewClassification.CompatibilityRisk),
		}, result.ApiChanges);
	}

	[Fact]
	public async Task Private_member_default_literals_do_not_expose_a_consumed_contract()
	{
		var result = await CompareAsync(ExpectedMonitorArtifacts.StaticRenderer, ApiSurface.Subclass,
			WatchRelationship.Subclasses,
			"public class StaticHtmlRenderer { private void Render(string scope = \"public\") { } }",
			"public class StaticHtmlRenderer { private void Render(string scope = \"protected\") { } }");

		Assert.Empty(result.ApiChanges);
		Assert.Equal([new SourceChange(ExpectedMonitorArtifacts.StaticRenderer, ChangeKind.Changed,
			ReviewClassification.ImplementationReview)], result.SourceChanges);
	}

	[Fact]
	public async Task Tuple_property_setter_addition_changes_the_consumed_contract()
	{
		var result = await CompareAsync(ExpectedMonitorArtifacts.StaticRenderer, ApiSurface.Subclass,
			WatchRelationship.Subclasses,
			"public class StaticHtmlRenderer { public (int Width, int Height) Size { get; } }",
			"public class StaticHtmlRenderer { public (int Width, int Height) Size { get; set; } }");

		Assert.Equal(new[]
		{
			new ApiChange("StaticHtmlRenderer", ChangeKind.Added, ApiSymbolKind.Member,
				"public (int Width, int Height) Size { get; set; }", ReviewClassification.ExtensibilityOpportunity),
			new ApiChange("StaticHtmlRenderer", ChangeKind.Removed, ApiSymbolKind.Member,
				"public (int Width, int Height) Size { get; }", ReviewClassification.CompatibilityRisk),
		}, result.ApiChanges);
	}

	private static async Task<MonitorResult> CompareAsync(string path, ApiSurface surface,
		WatchRelationship relationship, string before, string after)
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-api-files.json");
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{path}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContentText(before));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{path}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText(after));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(path, apiSurface: surface, relationship: relationship)));

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Null(result.InfrastructureError);
		return result;
	}
}
