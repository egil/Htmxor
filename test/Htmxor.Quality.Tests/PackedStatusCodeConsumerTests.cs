using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue287StatusCodeScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue287");
		UseFramework(framework, sdk, runtime);
	}
}
