using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// #309 through a package-only consumer: two agreeing <c>@onput</c> bindings in separate elements of
/// one route owner collapse into one generated action, so a single PUT request invokes the handler
/// exactly once.
/// </summary>
[Collection(PackageConsumerCollection.Name)]
public sealed class PackedMultiplicityConsumerTests
{
	[Fact]
	public async Task Package_only_application_invokes_a_collapsed_action_once_per_request()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue309MultiplicityScenario();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(1, 1, 1, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
