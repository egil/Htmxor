using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue291StatusCodeScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue291");
		UseFramework(framework, sdk, runtime);
	}
}
