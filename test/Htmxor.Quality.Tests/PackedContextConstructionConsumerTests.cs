using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

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
