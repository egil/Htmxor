using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedStatusCodeBodyConsumerTests
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Packed_status_code_body_and_redirect_contract_uses_real_Kestrel(
		string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue291StatusCodeScenario(framework, sdk, runtime);

		var result = await workspace.RunAsync();
		var tests = TrxTestRun.Read(workspace.TrxPath);
		NamedSelectionEvidence.Retain(workspace.ConsumerDirectory, workspace.TrxPath, result, "issue-291-status-code-body-redirect", framework);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.Equal(new TrxTestRun(66, 66, 66, 0, 0, 0, 0), tests);
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue291StatusCodeScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue291");
		UseFramework(framework, sdk, runtime);
	}
}
