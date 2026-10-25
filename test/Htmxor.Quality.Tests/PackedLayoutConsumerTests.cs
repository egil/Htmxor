using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseLayoutScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue283");
		UseFramework(framework, sdk, runtime);
		var fixtures = Path.Combine(repositoryRoot, "test", "Htmxor.AspNetCore10.Tests");
		foreach (var name in new[] { "Issue283App.razor", "Issue283Components.cs", "Issue283Host.cs" })
		{
			StageLayoutFixture(Path.Combine(fixtures, name), Path.Combine(consumerDirectory, name));
		}
		foreach (var source in Directory.EnumerateFiles(Path.Combine(fixtures, "Issue283")))
		{
			StageLayoutFixture(source, Path.Combine(consumerDirectory, "Issue283", Path.GetFileName(source)));
		}
		File.WriteAllText(Path.Combine(consumerDirectory, "_Imports.razor"), "@namespace Htmxor.PackageConsumer\n@using Microsoft.AspNetCore.Components\n@using Microsoft.AspNetCore.Components.Routing\n");
		StageMinimalSample();
	}

	private static void StageLayoutFixture(string source, string destination)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, File.ReadAllText(source).Replace("Htmxor.AspNetCore10", "Htmxor.PackageConsumer", StringComparison.Ordinal));
	}

	private void StageMinimalSample()
	{
		var sample = Path.Combine(repositoryRoot, "samples", "MinimalHtmxorApp", "Components");
		var destination = Path.Combine(consumerDirectory, "MinimalSample");
		Directory.CreateDirectory(destination);
		// Keep the sample's actual inherited layout declaration and layout body; add only an observable marker.
		var imports = File.ReadAllLines(Path.Combine(sample, "_Imports.razor"))
			.Where(line => line.StartsWith("@attribute", StringComparison.Ordinal));
		File.WriteAllText(Path.Combine(destination, "_Imports.razor"),
			"@using Htmxor\n@using Htmxor.Components\n@using Microsoft.AspNetCore.Components.Web\n" + string.Join('\n', imports));
		File.WriteAllText(Path.Combine(destination, "HtmxorLayout.razor"),
			File.ReadAllText(Path.Combine(sample, "Layout", "HtmxorLayout.razor")) + "\n<aside data-minimal-layout-marker>sample layout</aside>\n");
		File.WriteAllText(Path.Combine(destination, "Home.razor"),
			File.ReadAllText(Path.Combine(sample, "Pages", "Home.razor")).Replace("@page \"/\"", "@page \"/issue-283/minimal-home\"", StringComparison.Ordinal));
	}
}
