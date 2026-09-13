using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class Htmx4NamedSelectionBrowserTests
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task One_package_preserves_named_delivery_lifecycle_and_cache_isolation_on_both_frameworks()
	{
		using var net10 = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());
		net10.UseNamedSelectionScenario("net10.0", "10.0.400", "10.0.11");
		await AssertConsumerAsync(net10, "net10.0");
		using var net11 = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());
		net11.UseNamedSelectionScenario("net11.0", "11.0.100-rc.1.26425.128", "11.0.0-rc.1.26425.128");
		net11.UseSharedPackage(net10);
		await AssertConsumerAsync(net11, "net11.0");
	}

	private static async Task AssertConsumerAsync(Htmx4PackageBrowserWorkspace workspace, string framework)
	{
		var result = await workspace.RunAsync();
		NamedSelectionEvidence.Retain(workspace.ConsumerDirectory, workspace.TrxPath, result, "browser-cache", framework);
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError);
		Assert.Equal(new TrxTestRun(4, 4, 4, 0, 0, 0, 0), TrxTestRun.Read(workspace.TrxPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
	}
}

internal sealed partial class Htmx4PackageBrowserWorkspace
{
	public void UseNamedSelectionScenario(string framework, string sdk, string runtime)
	{
		UseFramework(framework, sdk, runtime);
		testFilter = "FullyQualifiedName~Named_selections_wait_for_sibling_data_and_keep_overlapping_requests_isolated" +
			"|FullyQualifiedName~Named_fragment_preserves_native_deliveries_independently_of_client_identities" +
			"|FullyQualifiedName~Output_cache_keeps_normal_direct_whole_and_named_fragment_representations_distinct";
	}
}
