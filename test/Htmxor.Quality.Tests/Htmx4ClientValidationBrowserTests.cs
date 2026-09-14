using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class Htmx4ClientValidationBrowserTests
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task Issue217_packed_net11_forms_preserve_client_validation_and_submission()
	{
		using var workspace = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());
		workspace.UseClientValidationScenario();
		var result = await workspace.RunAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue217-browser");
		Directory.CreateDirectory(evidence);
		File.Copy(workspace.TrxPath, Path.Combine(evidence, "browser.trx"), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError);
		Assert.Equal(new TrxTestRun(5, 5, 5, 0, 0, 0, 0), TrxTestRun.Read(workspace.TrxPath));
	}
}

internal sealed partial class Htmx4PackageBrowserWorkspace
{
	public void UseClientValidationScenario()
	{
		testFilter = "FullyQualifiedName~Issue217BrowserTests";
		UseFramework("net11.0", "11.0.100-rc.1.26425.128", "11.0.0-rc.1.26425.128");
		var project = File.ReadAllText(projectPath)
			.Replace("Microsoft.NET.Sdk.Razor", "Microsoft.NET.Sdk.Web", StringComparison.Ordinal)
			.Replace("<IsPackable>false</IsPackable>", "<IsPackable>false</IsPackable><OutputType>Library</OutputType>", StringComparison.Ordinal)
			.Replace("<FrameworkReference Include=\"Microsoft.AspNetCore.App\" />", "", StringComparison.Ordinal);
		File.WriteAllText(projectPath, project);
	}
}
