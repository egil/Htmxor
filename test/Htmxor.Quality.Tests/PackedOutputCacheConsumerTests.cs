using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue295OutputCacheScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue295");
		UseFramework(framework, sdk, runtime);
	}
}
