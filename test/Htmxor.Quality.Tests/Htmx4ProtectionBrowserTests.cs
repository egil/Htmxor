using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class Htmx4ProtectionBrowserTests
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task Issue211_one_package_preserves_application_owned_browser_protection_on_both_frameworks()
	{
		using var net10 = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());
		net10.UseProtectionScenario("net10.0", "10.0.400", "10.0.11");
		await AssertConsumerAsync(net10, "net10.0", 13);
		using var net11 = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());
		net11.UseProtectionScenario("net11.0", "11.0.100-rc.1.26425.128", "11.0.0-rc.1.26425.128");
		net11.UseSharedPackage(net10);
		await AssertConsumerAsync(net11, "net11.0", 20);
	}

	private static async Task AssertConsumerAsync(Htmx4PackageBrowserWorkspace workspace, string framework, int count)
	{
		var result = await workspace.RunAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue211-browser", framework);
		Directory.CreateDirectory(evidence);
		File.Copy(workspace.TrxPath, Path.Combine(evidence, "browser.trx"), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		var tests = TrxTestRun.Read(workspace.TrxPath);
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError);
		Assert.Equal(new TrxTestRun(count, count, count, 0, 0, 0, 0), tests);
		using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace.ConsumerDirectory, "obj", "project.assets.json")));
		var target = assets.RootElement.GetProperty("targets").GetProperty(framework).GetProperty("Htmxor/" + workspace.PackageVersion);
		Assert.Equal("lib/" + framework + "/Htmxor.dll", Assert.Single(target.GetProperty("runtime").EnumerateObject()).Name);
	}
}

internal sealed partial class Htmx4PackageBrowserWorkspace
{
	private bool usesSharedPackage;
	private string testFilter = "FullyQualifiedName!~Issue211BrowserTests";

	public void UseProtectionScenario(string framework, string sdk, string runtime)
	{
		testFilter = "FullyQualifiedName~Issue211BrowserTests";
		var source = File.ReadAllText(projectPath).Replace("<TargetFramework>net10.0</TargetFramework>",
			$"<TargetFramework>{framework}</TargetFramework><RuntimeFrameworkVersion>{runtime}</RuntimeFrameworkVersion><RollForward>Disable</RollForward>", StringComparison.Ordinal);
		File.WriteAllText(projectPath, source);
		File.WriteAllText(Path.Combine(consumerDirectory, "global.json"),
			JsonSerializer.Serialize(new { sdk = new { version = sdk, rollForward = "disable", allowPrerelease = true } }));
	}

	public void UseSharedPackage(Htmx4PackageBrowserWorkspace owner)
	{
		File.Copy(owner.PackagePath, Path.Combine(packageDirectory, Path.GetFileName(owner.PackagePath)));
		File.WriteAllText(projectPath, File.ReadAllText(projectPath).Replace(PackageVersion, owner.PackageVersion, StringComparison.Ordinal));
		PackageVersion = owner.PackageVersion;
		usesSharedPackage = true;
	}
}
