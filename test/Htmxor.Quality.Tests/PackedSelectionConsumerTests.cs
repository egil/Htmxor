using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedSelectionConsumerTests
{
	[Theory]
	[InlineData("net10.0", "10.0.400", "10.0.11")]
	[InlineData("net11.0", "11.0.100-rc.1.26425.128", "11.0.0-rc.1.26425.128")]
	public async Task Package_only_application_serializes_selected_fragment_bodies(string framework, string sdk, string runtime)
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
		Assert.Equal(new TrxTestRun(12, 12, 12, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
	}
}
