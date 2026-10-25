using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedNormalOnlyConsumerTests
{
	[Fact]
	public async Task Packaged_compiler_accepts_normal_only_authoring_forms()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue175NormalOnlyScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
