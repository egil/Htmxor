using System.Security.Cryptography;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class Htmx4PackageBrowserTests
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task Package_only_net10_application_uses_application_owned_htmx4_for_unsafe_actions()
	{
		using var workspace = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(2, 2, 2, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		Htmx4PackageBrowserEvidence.AssertConsumer(workspace);
	}
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PackageConsumerCollection
{
	public const string Name = "Package consumers";
}

internal sealed class Htmx4PackageBrowserWorkspace : IDisposable
{
	private const string PackageVersionToken = "__HTMXOR_PACKAGE_VERSION__";
	private const string TemplateSuffix = ".template";
	private readonly TemporaryDirectory temporaryDirectory = new();
	private readonly string repositoryRoot;
	private readonly string consumerDirectory;
	private readonly string packageDirectory;
	private readonly string packagesDirectory;
	private readonly string resultsDirectory;
	private readonly string projectPath;
	private readonly string nugetConfigPath;
	private readonly ProcessRunner runner = new();

	public Htmx4PackageBrowserWorkspace(string repositoryRoot)
	{
		this.repositoryRoot = repositoryRoot;
		consumerDirectory = Path.Combine(temporaryDirectory.Path, "consumer");
		packageDirectory = Path.Combine(temporaryDirectory.Path, "packages");
		packagesDirectory = Path.Combine(temporaryDirectory.Path, "global-packages");
		resultsDirectory = Path.Combine(temporaryDirectory.Path, "results");
		projectPath = Path.Combine(consumerDirectory, "Htmxor.Htmx4Browser.csproj");
		nugetConfigPath = Path.Combine(consumerDirectory, "NuGet.config");
		Directory.CreateDirectory(consumerDirectory);
		Directory.CreateDirectory(packageDirectory);
		StageConsumer();
	}

	public string ConsumerDirectory => consumerDirectory;

	public string PackagePath => Assert.Single(Directory.EnumerateFiles(packageDirectory, "*.nupkg"));

	public string PackageVersion { get; } = $"0.0.0-issue56-{Guid.NewGuid():N}";

	public string ProjectPath => projectPath;

	public string TrxPath => Path.Combine(resultsDirectory, "htmx4-browser.trx");

	public async Task<ProcessResult> RunAsync()
	{
		await PackAsync();
		await RestoreAsync();
		await BuildAsync();

		return await TestAsync();
	}

	private void StageConsumer()
	{
		var assets = Path.Combine(
			repositoryRoot,
			"test",
			"Htmxor.Quality.Tests",
			"Htmx4PackageBrowser");
		foreach (var sourcePath in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(assets, sourcePath);
			var isTemplate = relativePath.EndsWith(TemplateSuffix, StringComparison.Ordinal);
			var stagedPath = isTemplate
				? relativePath[..^TemplateSuffix.Length]
				: relativePath;
			var destinationPath = Path.Combine(consumerDirectory, stagedPath);
			Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
			if (isTemplate)
			{
				var content = File.ReadAllText(sourcePath)
					.Replace(PackageVersionToken, PackageVersion, StringComparison.Ordinal);
				File.WriteAllText(destinationPath, content);
			}
			else
			{
				File.Copy(sourcePath, destinationPath);
			}
		}

		File.WriteAllText(nugetConfigPath, CreateNugetConfig());
	}

	private string CreateNugetConfig() =>
		$"""
		<?xml version="1.0" encoding="utf-8"?>
		<configuration>
		  <packageSources>
		    <clear />
		    <add key="htmxor-local" value="{new Uri(packageDirectory).AbsoluteUri}" />
		    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
		  </packageSources>
		  <packageSourceMapping>
		    <packageSource key="htmxor-local">
		      <package pattern="Htmxor" />
		    </packageSource>
		    <packageSource key="nuget.org">
		      <package pattern="Microsoft.*" />
		      <package pattern="Newtonsoft.Json" />
		      <package pattern="System.ComponentModel.Annotations" />
		      <package pattern="xunit" />
		      <package pattern="xunit.*" />
		    </packageSource>
		  </packageSourceMapping>
		</configuration>
		""";

	private Task<ProcessResult> PackAsync() =>
		RunRequiredAsync(
			"pack",
			Path.Combine(repositoryRoot, "src", "Htmxor", "Htmxor.csproj"),
			"--configuration",
			"Release",
			"--no-restore",
			"--output",
			packageDirectory,
			$"-p:MinVerVersionOverride={PackageVersion}");

	private Task<ProcessResult> RestoreAsync() =>
		RunRequiredAsync(
			"restore",
			projectPath,
			"--configfile",
			nugetConfigPath,
			"--packages",
			packagesDirectory);

	private Task<ProcessResult> BuildAsync() =>
		RunRequiredAsync(
			"build",
			projectPath,
			"--configuration",
			"Release",
			"--no-restore");

	private Task<ProcessResult> TestAsync() =>
		runner.RunAsync(new(
			"dotnet",
			consumerDirectory,
			[
				"test",
				projectPath,
				"--configuration",
				"Release",
				"--no-build",
				"--no-restore",
				"--blame-hang",
				"--blame-hang-timeout",
				"5min",
				"--logger",
				"trx;LogFileName=htmx4-browser.trx",
				"--results-directory",
				resultsDirectory,
			],
			EnsureSuccess: false));

	private async Task<ProcessResult> RunRequiredAsync(params string[] arguments)
	{
		var result = await runner.RunAsync(new(
			"dotnet",
			repositoryRoot,
			arguments,
			EnsureSuccess: false));
		if (result.ExitCode != 0)
		{
			throw new InvalidOperationException(
				result.StandardOutput + Environment.NewLine + result.StandardError);
		}

		return result;
	}

	public void Dispose() => temporaryDirectory.Dispose();
}

internal static class Htmx4PackageBrowserEvidence
{
	private const string HtmxSha256 =
		"E484D9171A9DB30A39C8F16E3D709D4137F3211C659F8E6125816635033D593F";

	public static void AssertConsumer(Htmx4PackageBrowserWorkspace workspace)
	{
		var project = XDocument.Load(workspace.ProjectPath);
		var references = project.Descendants()
			.Where(element => element.Name.LocalName == "PackageReference")
			.ToArray();
		var htmxor = Assert.Single(
			references,
			reference => string.Equals(
				reference.Attribute("Include")?.Value,
				"Htmxor",
				StringComparison.Ordinal));
		Assert.Equal(workspace.PackageVersion, htmxor.Attribute("Version")?.Value);
		Assert.Equal(
			"net10.0",
			Assert.Single(project.Descendants()
				.Where(element => element.Name.LocalName == "TargetFramework")).Value);
		var playwright = Assert.Single(
			references,
			reference => string.Equals(
				reference.Attribute("Include")?.Value,
				"Microsoft.Playwright",
				StringComparison.Ordinal));
		Assert.Equal("1.62.0", playwright.Attribute("Version")?.Value);
		Assert.Empty(project.Descendants().Where(element => element.Name.LocalName == "ProjectReference"));
		Assert.Empty(project.Descendants().Where(element => element.Name.LocalName == "InternalsVisibleTo"));

		var assetPath = Path.Combine(
			workspace.ConsumerDirectory,
			"wwwroot",
			"htmx-4.0.0.min.js");
		var assetHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assetPath)));
		Assert.Equal(HtmxSha256, assetHash);
	}
}
