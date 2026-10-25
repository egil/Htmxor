using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedAnyFolderConsumerTests
{
	[Fact]
	public async Task Packaged_compiler_accepts_component_folder_and_import_topology()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue285AnyFolderScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
