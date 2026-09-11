using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedPackageGetTests
{
	[Fact]
	public async Task One_package_serves_generated_GETs_on_both_framework_toolchains()
	{
		using var net10 = new PackageConsumerWorkspace(RepositoryLocator.Find());
		net10.UseGetScenario("net10.0", "10.0.400", "10.0.11");
		var net10Failure = await ObserveConsumerAsync(net10, "net10.0");
		using var package = ZipFile.OpenRead(net10.PackagePath);
		Assert.Contains(package.Entries, entry => entry.FullName == "lib/net10.0/Htmxor.dll");
		Assert.Contains(package.Entries, entry => entry.FullName == "lib/net11.0/Htmxor.dll");
		Assert.Contains(package.Entries, entry => entry.FullName == "analyzers/dotnet/cs/Htmxor.Generators.dll");

		using var net11 = new PackageConsumerWorkspace(RepositoryLocator.Find());
		net11.UseGetScenario("net11.0", "11.0.100-rc.1.26425.128", "11.0.0-rc.1.26425.128");
		net11.UseSharedPackage(net10);
		var net11Failure = await ObserveConsumerAsync(net11, "net11.0");
		Assert.True(net10Failure.Length + net11Failure.Length == 0, net10Failure + net11Failure);
		await net10.AssertGeneratorIsRequiredAsync();
		await net11.AssertGeneratorIsRequiredAsync();
	}

	private static async Task<string> ObserveConsumerAsync(PackageConsumerWorkspace workspace, string framework)
	{
		var result = await workspace.RunAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "package-get", framework);
		Directory.CreateDirectory(evidence);
		File.Copy(workspace.TrxPath, Path.Combine(evidence, "package-consumer.trx"), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		var tests = TrxTestRun.Read(workspace.TrxPath);
		using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace.ConsumerDirectory, "obj", "project.assets.json")));
		var target = assets.RootElement.GetProperty("targets").GetProperty(framework).GetProperty("Htmxor/" + workspace.PackageVersion);
		Assert.Equal("lib/" + framework + "/Htmxor.dll", Assert.Single(target.GetProperty("compile").EnumerateObject()).Name);
		Assert.Equal("lib/" + framework + "/Htmxor.dll", Assert.Single(target.GetProperty("runtime").EnumerateObject()).Name);
		var project = XDocument.Load(Path.Combine(workspace.ConsumerDirectory, "Htmxor.PackageConsumer.csproj"));
		Assert.Empty(project.Descendants("ProjectReference"));
		Assert.Empty(project.Descendants("InternalsVisibleTo"));
		Assert.Contains(assets.RootElement.GetProperty("libraries").GetProperty("Htmxor/" + workspace.PackageVersion).GetProperty("files").EnumerateArray(),
			file => file.GetString() == "analyzers/dotnet/cs/Htmxor.Generators.dll");
		return result.ExitCode == 0 && tests == new TrxTestRun(8, 8, 8, 0, 0, 0, 0)
			? string.Empty
			: framework + ": " + tests + Environment.NewLine + result.StandardOutput + result.StandardError;
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	private bool usesSharedPackage;

	public void UseGetScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue209");
		var source = File.ReadAllText(projectPath).Replace("net10.0", framework, StringComparison.Ordinal);
		source = source.Replace("<TargetFramework>" + framework + "</TargetFramework>",
			$"<TargetFramework>{framework}</TargetFramework><RuntimeFrameworkVersion>{runtime}</RuntimeFrameworkVersion><RollForward>Disable</RollForward>", StringComparison.Ordinal);
		File.WriteAllText(projectPath, source);
		File.WriteAllText(Path.Combine(consumerDirectory, "global.json"),
			JsonSerializer.Serialize(new { sdk = new { version = sdk, rollForward = "disable", allowPrerelease = true } }));
	}

	public void UseSharedPackage(PackageConsumerWorkspace owner)
	{
		File.Copy(owner.PackagePath, Path.Combine(packageDirectory, Path.GetFileName(owner.PackagePath)));
		var source = File.ReadAllText(projectPath).Replace(PackageVersion, owner.PackageVersion, StringComparison.Ordinal);
		File.WriteAllText(projectPath, source);
		PackageVersion = owner.PackageVersion;
		usesSharedPackage = true;
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
