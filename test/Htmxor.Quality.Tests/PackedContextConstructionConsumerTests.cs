using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackedConsumerCollection.Name)]
public sealed class PackedContextConstructionConsumerTests(PackedPackageFixture package)
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Packed_consumer_cannot_construct_detached_wrappers(string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);
		workspace.UseIssue299ContextConstructionScenario(framework, sdk, runtime);
		workspace.AddDetachedWrapperConstruction();
		var rejected = await workspace.BuildForDiagnosticAsync();
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
