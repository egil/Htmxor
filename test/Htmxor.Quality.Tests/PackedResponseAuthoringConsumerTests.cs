using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

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
