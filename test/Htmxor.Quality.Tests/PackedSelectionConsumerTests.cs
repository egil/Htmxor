using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedSelectionConsumerTests
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Package_only_application_discovers_the_selection_component(string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue168SelectionScenario();
		workspace.UseFramework(framework, sdk, runtime);

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);
		NamedSelectionEvidence.Retain(workspace.ConsumerDirectory, workspace.TrxPath, result, "selection", framework);

		Assert.True(result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(1, 1, 1, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
	}
}
