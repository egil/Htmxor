using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Through a package-only consumer, an <c>HtmxRoute</c> component or an action-owning component
/// anywhere in the application project serves its direct GET and its declared action exactly as a
/// project-root file does: project root, <c>Components/Pages</c> (including a stock <c>@page</c>
/// <c>Counter.razor</c> with <c>@onput</c>), a two-level nested folder, a <c>.razor.cs</c> partial,
/// an all-C# component outside the root namespace, an in-file <c>@namespace</c>, an ancestor
/// <c>_Imports.razor</c> <c>@namespace</c> (the nearest ancestor wins, and an in-file value wins
/// over any ancestor), an ancestor <c>_Imports.razor</c> with no <c>@namespace</c> directive that
/// defers to a farther one that declares it, an <c>@namespace</c> directive that is not a file's
/// first line, a folder segment that needs sanitizing into a valid identifier, and two components
/// that share a class name in different namespaces. See <c>Issue285AnyFolderScenarioTests.cs.scenario</c>.
/// </summary>
[Collection(PackageConsumerCollection.Name)]
public sealed class PackedAnyFolderConsumerTests
{
	[Fact]
	public async Task Package_only_application_discovers_every_folder_placement_without_cross_wiring()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue285AnyFolderScenario();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(14, 14, 14, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
