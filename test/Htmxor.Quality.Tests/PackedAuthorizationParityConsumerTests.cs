using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Through a package-only consumer: every accepted authorization shape on an <c>HtmxRoute</c>
/// component is authorized exactly as its stock <c>@page</c> twin, across identities that separate
/// each shape's requirements from one another, with and without the application's
/// <c>FallbackPolicy</c> (96 cases, see <c>Issue284AuthorizationParityTests.cs.scenario</c>); and
/// an unsafe action on an <c>[Authorize(Roles = ...)]</c> <c>HtmxRoute</c> component keeps
/// authorization ahead of antiforgery, binding, and the callback (4 cases, see
/// <c>Issue284UnsafeActionOrderingTests.cs.scenario</c>). One scenario, one consumer build, and
/// one <c>dotnet test</c> invocation carry both.
/// </summary>
[Collection(PackageConsumerCollection.Name)]
public sealed class PackedAuthorizationParityConsumerTests
{
	[Fact]
	public async Task Package_only_application_matches_stock_authorization_across_every_accepted_shape()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue284AuthorizationParityScenario();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(100, 100, 100, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
