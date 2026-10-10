using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

/// <summary>
/// Through a package-only consumer: every convention chained onto <c>MapRazorComponents&lt;App&gt;()</c>
/// -- <c>RequireAuthorization()</c> before and after <c>AddHtmxorEndpoints()</c>, a non-authorization
/// <c>WithMetadata</c> marker, and the framework's own <c>WithStaticAssets()</c> -- applies to a
/// generated <c>HtmxRoute</c> endpoint exactly as it applies to its stock <c>@page</c> twin. An
/// anonymous unsafe action on an otherwise unprotected <c>HtmxRoute</c> component is rejected by the
/// chained <c>RequireAuthorization()</c> before antiforgery, binding, or its callback run. A route
/// group's <c>RequireAuthorization()</c> (<c>MapGroup("").RequireAuthorization()</c>) is kept as a
/// green guard (9 cases, see <c>Issue316ConventionsTests.cs.scenario</c>).
/// </summary>
[Collection(PackageConsumerCollection.Name)]
public sealed class PackedConventionsConsumerTests
{
	[Fact]
	public async Task Package_only_application_applies_every_chained_convention_to_HtmxRoute_endpoints()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue316ConventionsScenario();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(9, 9, 9, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}
