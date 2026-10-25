using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackedConsumerCollection.Name)]
public sealed class PackedStockRouteTemplateConsumerTests(PackedPackageFixture package)
{
	[Fact]
	public async Task Package_only_application_reports_a_malformed_HtmxRoute_template_with_stocks_ASP0017()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);
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
