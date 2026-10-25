using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedBindingAnywhereConsumerTests
{
	[Fact]
	public async Task Packaged_compiler_accepts_bindings_anywhere_in_owner_markup()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue306BindingAnywhereScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
