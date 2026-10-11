using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Through a package-only consumer: every stock Blazor route template shape that
/// <c>HtmxorRouteTemplateContract</c> must accept -- a static path, an unconstrained
/// (<c>string</c>) parameter, every stock route constraint plus a multi-constraint segment, a
/// non-matching constrained value, an optional parameter present and absent, a catch-all, an
/// inline default value present and absent, and a complex segment -- binds route values through
/// the real stock routing and parameter-binding seam on an <c>HtmxRoute</c> component exactly as
/// it does on a paired stock <c>@page</c> twin (10 cases, see
/// <c>Issue286ScenarioTests.cs.scenario</c>).
/// </summary>
[Collection(PackageConsumerCollection.Name)]
public sealed class PackedStockRouteTemplateConsumerTests
{
	[Fact]
	public async Task Package_only_application_binds_every_stock_route_template_shape_like_its_stock_twin()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue286StockRouteTemplateScenario();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(10, 10, 10, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
