using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedNormalOnlyConsumerTests
{
	[Fact]
	public async Task Package_only_application_enforces_normal_only_reachability_for_all_authoring_forms()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue175NormalOnlyScenario();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		// The packed consumer reproduces the same 404 gap as the in-repo matrix: the matcher
		// policy does not recognize the marker in the packaged analyzer/runtime either. The 3
		// stock-page cases (one per authoring form) pass; the 3 direct-GET and 3 direct-POST
		// cases fail because today's response is not 404.
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal(new TrxTestRun(9, 9, 3, 6, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}
}
