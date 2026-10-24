using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedResponseAuthoringConsumerTests
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Packed_integer_status_preserves_hosted_body_contract(string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue293ResponseAuthoringScenario(framework, sdk, runtime);

		var result = await workspace.RunAsync();
		var tests = TrxTestRun.Read(workspace.TrxPath);
		NamedSelectionEvidence.Retain(workspace.ConsumerDirectory, workspace.TrxPath, result, "issue-293-response-authoring", framework);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.Equal(new TrxTestRun(28, 28, 28, 0, 0, 0, 0), tests);
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue293ResponseAuthoringScenario(string framework, string sdk, string runtime)
	{
		UseIssue291StatusCodeScenario(framework, sdk, runtime);
		File.Delete(Path.Combine(consumerDirectory, "Issue291StatusCodeTests.cs"));
		var source = Path.Combine(repositoryRoot, "test", "Htmxor.Quality.Tests", "PackageConsumer", "Issue293StatusCodeTests.cs.scenario");
		File.Copy(source, Path.Combine(consumerDirectory, "Issue293StatusCodeTests.cs"));
		var page = Path.Combine(consumerDirectory, "Issue291Page.razor");
		var content = File.ReadAllText(page)
			.Replace("response.StatusCode((HttpStatusCode)Status)", "Issue293StatusCodeTests.Apply(response, Status)", StringComparison.Ordinal)
			.Replace("response.StatusCode((HttpStatusCode)FinalStatus)", "Issue293StatusCodeTests.Apply(response, FinalStatus)", StringComparison.Ordinal);
		File.WriteAllText(page, content);
	}
}
