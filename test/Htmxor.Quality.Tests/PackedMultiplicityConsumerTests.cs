using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// #309's request-level risk: a real packed consumer, not the in-repo generator-driver harness, is
/// the cheaper real host named by the #309 Verification contract ("the AspNetCore10
/// component-endpoint suite or a packed-consumer cell, whichever is the cheaper real host"). Two
/// agreeing <c>@onput</c> bindings in separate elements of one route owner must collapse into one
/// generated action that a single PUT request invokes exactly once, proving no duplicate dispatch
/// survives packaging. At the base this consumer fails to build at all with the removed "at most
/// one @onput binding per component is supported" HTMXOR002, so the real risk - double invocation
/// of a collapsed action - cannot yet be observed; it can only be observed once the build succeeds.
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
