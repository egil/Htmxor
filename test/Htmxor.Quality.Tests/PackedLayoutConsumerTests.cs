using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedLayoutConsumerTests
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Package_only_consumer_wraps_sample_and_htmx_only_components(string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseLayoutScenario(framework, sdk, runtime);
		var result = await workspace.RunAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-283", "packed", framework);
		Directory.CreateDirectory(evidence);
		File.Copy(workspace.TrxPath, Path.Combine(evidence, "consumer.trx"), overwrite: true);
		File.Copy(Path.Combine(workspace.ConsumerDirectory, "obj", "project.assets.json"), Path.Combine(evidence, "project.assets.json"), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + result.StandardError);
		File.Copy(workspace.PackagePath, Path.Combine(evidence, Path.GetFileName(workspace.PackagePath)), overwrite: true);
		foreach (var name in new[] { "global.json", "Htmxor.PackageConsumer.csproj" })
		{
			File.Copy(Path.Combine(workspace.ConsumerDirectory, name), Path.Combine(evidence, name), overwrite: true);
		}
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError);
		Assert.Equal(new TrxTestRun(4, 4, 4, 0, 0, 0, 0), TrxTestRun.Read(workspace.TrxPath));
	}
}

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
