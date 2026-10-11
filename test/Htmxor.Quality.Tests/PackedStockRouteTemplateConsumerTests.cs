using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Through a package-only consumer: every stock Blazor route template shape that
/// <c>HtmxorRouteTemplateContract</c> must accept -- a static path, an unconstrained
/// (<c>string</c>) parameter, every stock route constraint plus a multi-constraint segment, an
/// optional parameter present and absent, a catch-all, an inline default value present and
/// absent, and a complex segment -- binds route values through the real stock routing and
/// parameter-binding seam on an <c>HtmxRoute</c> component exactly as it does on a paired stock
/// <c>@page</c> twin, and a non-matching constrained value is rejected with the same status (10
/// cases, see <c>Issue286ScenarioTests.cs.scenario</c>).
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

	/// <summary>
	/// A malformed HtmxRoute template is a build-time grammar error stock's own ASP0017 analyzer
	/// reports with stock wording, not a diagnostic Htmxor adds. The consumer project's
	/// <c>TreatWarningsAsErrors</c> (Release only) turns that warning into the build failure this
	/// asserts.
	/// </summary>
	[Fact]
	public async Task Package_only_application_reports_a_malformed_HtmxRoute_template_with_stocks_ASP0017()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue286StockRouteTemplateScenario();
		workspace.UseIssue286MalformedTemplate();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("ASP0017", output, StringComparison.Ordinal);
		Assert.Contains(
			"There is an incomplete parameter in the route template. Check that each '{' character has a " +
			"matching '}' character",
			output,
			StringComparison.Ordinal);
		Assert.Contains("Issue286StaticHtmxPage.razor", output, StringComparison.Ordinal);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}
}
