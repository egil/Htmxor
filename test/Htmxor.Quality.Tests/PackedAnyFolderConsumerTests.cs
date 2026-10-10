using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Through a package-only consumer: an <c>HtmxRoute</c> component or an action-owning component
/// anywhere in the application project -- project root, <c>Components/Pages</c>, a two-level
/// nested folder, a <c>.razor.cs</c> partial, an all-C# component outside the root namespace, an
/// in-file <c>@namespace</c> override, an ancestor <c>_Imports.razor</c> override (including a
/// nearer one winning over a farther one, and an in-file override winning over an ancestor one)
/// -- serves its direct GET and its declared action exactly as a project-root file does, with no
/// cross-wiring between two components that share a class name in different namespaces (see
/// <c>Issue285AnyFolderScenarioTests.cs.scenario</c>). <c>Components/Pages/Counter.razor</c>'s
/// <c>@onput="IncrementCount"</c> is the brief's own required behavioral red: today
/// <c>PUT /counter</c> never reaches <c>IncrementCount</c> because the path-only manifest
/// generator's project-root-only guess returns no type name for a subfolder file.
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
		Assert.Equal(new TrxTestRun(11, 11, 11, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
