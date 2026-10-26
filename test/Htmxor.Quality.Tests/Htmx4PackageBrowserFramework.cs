using System.Text.Json;

namespace Htmxor.Quality.Tests;

internal sealed partial class Htmx4PackageBrowserWorkspace
{
	private const string TestFilter = "FullyQualifiedName!~Issue211BrowserTests&FullyQualifiedName!~Issue283TitleBrowserTests" +
		"&FullyQualifiedName!~Named_selections_wait_for_sibling_data_and_keep_overlapping_requests_isolated" +
		"&FullyQualifiedName!~Named_fragment_preserves_native_deliveries_independently_of_client_identities" +
		"&FullyQualifiedName!~Output_cache_keeps_normal_direct_whole_and_named_fragment_representations_distinct";

	private void UseFramework(string framework, string sdk, string runtime)
	{
		var source = File.ReadAllText(projectPath).Replace("<TargetFramework>net10.0</TargetFramework>",
			$"<TargetFramework>{framework}</TargetFramework><RuntimeFrameworkVersion>{runtime}</RuntimeFrameworkVersion><RollForward>Disable</RollForward>", StringComparison.Ordinal);
		File.WriteAllText(projectPath, source);
		File.WriteAllText(Path.Combine(consumerDirectory, "global.json"),
			JsonSerializer.Serialize(new { sdk = new { version = sdk, rollForward = "disable", allowPrerelease = true } }));
	}
}
