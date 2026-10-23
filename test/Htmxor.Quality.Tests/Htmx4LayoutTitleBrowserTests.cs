using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class Htmx4LayoutTitleBrowserTests
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task Application_owned_htmx4_applies_a_partial_title()
	{
		using var workspace = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());
		workspace.UseLayoutTitleScenario();
		var result = await workspace.RunAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-283", "browser");
		Directory.CreateDirectory(evidence);
		if (File.Exists(workspace.TrxPath)) File.Copy(workspace.TrxPath, Path.Combine(evidence, "browser.trx"), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + result.StandardError);
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError);
		Assert.Equal(new TrxTestRun(1, 1, 1, 0, 0, 0, 0), TrxTestRun.Read(workspace.TrxPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		Htmx4PackageBrowserEvidence.AssertConsumer(workspace);
	}
}

internal sealed partial class Htmx4PackageBrowserWorkspace
{
	public void UseLayoutTitleScenario()
	{
		UseFramework("net10.0", "10.0.402", "10.0.13");
		testFilter = "FullyQualifiedName~Issue283TitleBrowserTests";
	}
}
