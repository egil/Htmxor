using System.Text.Json;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseGetScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue209");
		UseFramework(framework, sdk, runtime);
	}

	public void UseFramework(string framework, string sdk, string runtime)
	{
		var source = File.ReadAllText(projectPath).Replace("net10.0", framework, StringComparison.Ordinal);
		source = source.Replace("<TargetFramework>" + framework + "</TargetFramework>",
			$"<TargetFramework>{framework}</TargetFramework><RuntimeFrameworkVersion>{runtime}</RuntimeFrameworkVersion><RollForward>Disable</RollForward>", StringComparison.Ordinal);
		File.WriteAllText(projectPath, source);
		File.WriteAllText(Path.Combine(consumerDirectory, "global.json"),
			JsonSerializer.Serialize(new { sdk = new { version = sdk, rollForward = "disable", allowPrerelease = true } }));
	}

	public async Task AssertGeneratorIsRequiredAsync()
	{
		var original = File.ReadAllText(projectPath);
		var document = XDocument.Parse(original);
		var analyzersPath = Path.Combine(consumerDirectory, "obj", "generator-control-analyzers.txt");
		// ExcludeAssets alone can retain the packaged generator in the compiler's analyzer items.
		document.Root!.Add(new XElement("Target",
			new XAttribute("Name", "ExcludePackagedGenerator"),
			new XAttribute("BeforeTargets", "CoreCompile"),
			new XElement("ItemGroup", new XElement("Analyzer",
				new XAttribute("Remove", "@(Analyzer->WithMetadataValue('Filename', 'Htmxor.Generators'))"))),
			new XElement("WriteLinesToFile",
				new XAttribute("File", analyzersPath),
				new XAttribute("Lines", "@(Analyzer)"),
				new XAttribute("Overwrite", "true"))));
		Assert.Single(document.Descendants("PackageReference"), element => element.Attribute("Include")?.Value == "Htmxor")
			.SetAttributeValue("ExcludeAssets", "analyzers");
		try
		{
			document.Save(projectPath);
			await RestoreAsync();
			await RunRequiredAsync("clean", projectPath, "--configuration", "Release");
			var result = await BuildAsync();
			var evidence = Path.Combine(repositoryRoot, "artifacts", "results", "package-get", document.Descendants("TargetFramework").Single().Value);
			Directory.CreateDirectory(evidence);
			File.Copy(analyzersPath, Path.Combine(evidence, "generator-control-analyzers.txt"), overwrite: true);
			File.WriteAllText(Path.Combine(evidence, "generator-control.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
			Assert.DoesNotContain(File.ReadAllLines(analyzersPath), path => Path.GetFileName(path) == "Htmxor.Generators.dll");
			Assert.Contains(File.ReadAllLines(analyzersPath), path => Path.GetFileName(path) == "Microsoft.CodeAnalysis.Razor.Compiler.dll");
			Assert.NotEqual(0, result.ExitCode);
			Assert.Contains("AddHtmxorEndpoints", result.StandardOutput, StringComparison.Ordinal);
			Assert.Contains("CS1061", result.StandardOutput, StringComparison.Ordinal);
		}
		finally
		{
			File.WriteAllText(projectPath, original);
		}
	}
}
