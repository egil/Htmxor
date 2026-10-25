using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedContextConstructionConsumerTests
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Packed_wrappers_hide_construction_and_preserve_action_factory_metadata(
		string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue299ContextConstructionScenario(framework, sdk, runtime);

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);
		NamedSelectionEvidence.Retain(workspace.ConsumerDirectory, workspace.TrxPath, result, "issue-299-context", framework);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.Equal(new TrxTestRun(5, 5, 5, 0, 0, 0, 0), testRun);
	}

	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Packed_consumer_cannot_construct_detached_wrappers(string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue299ContextConstructionScenario(framework, sdk, runtime);
		var supported = await workspace.BuildForDiagnosticAsync();
		Assert.True(supported.ExitCode == 0, supported.StandardOutput + supported.StandardError);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);

		workspace.AddDetachedWrapperConstruction();
		var rejected = await workspace.BuildDetachedWrapperProbeAsync();
		var output = rejected.StandardOutput + Environment.NewLine + rejected.StandardError;
		Assert.True(rejected.ExitCode != 0, "The packed consumer still compiled direct construction of all four wrappers." + Environment.NewLine + output);
		Assert.All(new[] { 8, 9, 10, 11 }, line =>
			Assert.Matches(@"DetachedWrappers\.cs\(" + line + @",\d+\): error CS(0122|1729)", output));
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue299ContextConstructionScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue299");
		UseFramework(framework, sdk, runtime);
	}

	public Task<ProcessResult> BuildDetachedWrapperProbeAsync() => BuildAsync();

	public void AddDetachedWrapperConstruction() =>
		File.WriteAllText(Path.Combine(consumerDirectory, "DetachedWrappers.cs"), """
			using Htmxor;
			using Htmxor.Http;
			using Microsoft.AspNetCore.Http;
			public static class DetachedWrappers
			{
			    public static object[] Create(HttpContext http, HtmxContext context) => new object[]
			    {
			        new HtmxContext(http),
			        new HtmxRequest(http),
			        new HtmxResponse(http),
			        new HtmxEventArgs(context),
			    };
			}
			""");
}
