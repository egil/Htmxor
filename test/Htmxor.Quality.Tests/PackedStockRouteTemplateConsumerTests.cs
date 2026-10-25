using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedStockRouteTemplateConsumerTests
{
	[Fact]
	public async Task Packaged_compiler_accepts_stock_route_template_shapes()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue286StockRouteTemplateScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError);
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
