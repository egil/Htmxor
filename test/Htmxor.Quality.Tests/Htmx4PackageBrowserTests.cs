using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Htmxor.Quality;
using Xunit.Abstractions;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class Htmx4PackageBrowserTests(ITestOutputHelper output)
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task Package_only_net10_production_publish_preserves_assets_and_component_actions()
	{
		using var workspace = new Htmx4PackageBrowserWorkspace(RepositoryLocator.Find());

		var result = await NetworkChangedRetryRunner.RunAsync(workspace.RunAsync, _ => workspace.TrxPath, output.WriteLine);
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(43, 43, 43, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		Htmx4PackageBrowserEvidence.AssertConsumer(workspace);
	}
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PackageConsumerCollection
{
	public const string Name = "Package consumers";
}

internal sealed partial class Htmx4PackageBrowserWorkspace : IDisposable
{
	private const string PackageVersionToken = "__HTMXOR_PACKAGE_VERSION__";
	private const string TemplateSuffix = ".template";
	private readonly TemporaryDirectory temporaryDirectory = new();
	private readonly string repositoryRoot;
	private readonly string consumerDirectory;
	private readonly string packageDirectory;
	private readonly string packagesDirectory;
	private readonly string resultsDirectory;
	private readonly string publishDirectory;
	private readonly string projectPath;
	private readonly string nugetConfigPath;
	private readonly ProcessRunner runner = new();
	private readonly List<object> evidenceCommands = new();

	public Htmx4PackageBrowserWorkspace(string repositoryRoot)
	{
		this.repositoryRoot = repositoryRoot;
		consumerDirectory = Path.Combine(temporaryDirectory.Path, "consumer");
		packageDirectory = Path.Combine(temporaryDirectory.Path, "packages");
		packagesDirectory = Path.Combine(temporaryDirectory.Path, "global-packages");
		resultsDirectory = Path.Combine(temporaryDirectory.Path, "results");
		publishDirectory = Path.Combine(temporaryDirectory.Path, "publish");
		projectPath = Path.Combine(consumerDirectory, "Htmxor.Htmx4Browser.csproj");
		nugetConfigPath = Path.Combine(consumerDirectory, "NuGet.config");
		Directory.CreateDirectory(consumerDirectory);
		Directory.CreateDirectory(packageDirectory);
		StageConsumer();
	}

	public string ConsumerDirectory => consumerDirectory;

	public string PackagePath => Assert.Single(Directory.EnumerateFiles(packageDirectory, "*.nupkg"));

	public string PackageVersion { get; private set; } = $"0.0.0-issue56-{Guid.NewGuid():N}";

	public string ProjectPath => projectPath;

	public string TrxPath => Path.Combine(resultsDirectory, "htmx4-browser.trx");

	public async Task<ProcessResult> RunAsync()
	{
		try
		{
			await RunRequiredAsync("--info");
			if (!usesSharedPackage)
			{
				await PackAsync();
			}
			await RestoreAsync();
			await PublishAsync();

			return await TestAsync();
		}
		finally
		{
			RetainEvidence();
		}
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

	private Task<ProcessResult> PublishAsync() =>
		RunRequiredAsync(
			"publish",
			projectPath,
			"--configuration",
			"Release",
			"--no-restore",
			"--output",
			publishDirectory,
			"-p:EmitCompilerGeneratedFiles=true",
			$"-p:CompilerGeneratedFilesOutputPath={Path.Combine(consumerDirectory, "obj", "generated")}");

	private async Task<ProcessResult> TestAsync()
	{
		string[] arguments =
		[
			"test",
			projectPath,
			"--configuration",
			"Release",
			"--no-build",
			"--no-restore",
			$"-p:OutputPath={publishDirectory}",
			"--filter",
			testFilter,
			"--environment",
			"ASPNETCORE_ENVIRONMENT=Production",
			"--blame-hang",
			"--blame-hang-timeout",
			"5min",
			"--logger",
			"trx;LogFileName=htmx4-browser.trx",
			"--results-directory",
			resultsDirectory,
		];
		var result = await runner.RunAsync(new("dotnet", consumerDirectory, arguments, EnsureSuccess: false));
		RecordCommand(consumerDirectory, arguments, result);
		return result;
	}

	private async Task<ProcessResult> RunRequiredAsync(params string[] arguments)
	{
		var result = await runner.RunAsync(new(
			"dotnet",
			arguments[0] == "pack" ? repositoryRoot : consumerDirectory,
			arguments,
			EnsureSuccess: false));
		RecordCommand(arguments[0] == "pack" ? repositoryRoot : consumerDirectory, arguments, result);
		if (result.ExitCode != 0)
		{
			throw new InvalidOperationException(
				result.StandardOutput + Environment.NewLine + result.StandardError);
		}

		return result;
	}

	private void RecordCommand(string workingDirectory, string[] arguments, ProcessResult result) =>
		evidenceCommands.Add(new { Executable = "dotnet", WorkingDirectory = workingDirectory, Arguments = arguments, result.ExitCode, result.StandardOutput, result.StandardError, CapturedAt = DateTimeOffset.UtcNow });

	private void RetainEvidence()
	{
		var directory = Path.Combine(repositoryRoot, "artifacts", "results", "issue-179-browser", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var files = new List<object>();
		foreach (var source in Directory.EnumerateFiles(consumerDirectory))
		{
			RetainFile(source, Path.Combine(directory, "consumer", Path.GetFileName(source)), files);
		}
		RetainDirectory(packageDirectory, Path.Combine(directory, "packages"), files);
		RetainDirectory(Path.Combine(consumerDirectory, "obj", "generated"), Path.Combine(directory, "generated"), files);
		var assemblyName = Path.GetFileNameWithoutExtension(projectPath);
		var boundary = new[] { Path.Combine(consumerDirectory, "obj", "project.assets.json"), Path.Combine(publishDirectory, assemblyName + ".dll"), Path.Combine(publishDirectory, assemblyName + ".deps.json"), Path.Combine(publishDirectory, assemblyName + ".runtimeconfig.json"), TrxPath };
		foreach (var source in boundary)
		{
			RetainFile(source, Path.Combine(directory, "boundary", Path.GetFileName(source)), files);
		}
		File.WriteAllText(Path.Combine(directory, "evidence.json"), JsonSerializer.Serialize(new { PackageVersion, ConsumerDirectory = consumerDirectory, PublishDirectory = publishDirectory, TestFilter = testFilter, Commands = evidenceCommands, Files = files, CapturedAt = DateTimeOffset.UtcNow }, new JsonSerializerOptions { WriteIndented = true }));
	}

	private static void RetainDirectory(string sourceDirectory, string destinationDirectory, List<object> files)
	{
		if (!Directory.Exists(sourceDirectory)) return;
		foreach (var source in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			RetainFile(source, Path.Combine(destinationDirectory, Path.GetRelativePath(sourceDirectory, source)), files);
		}
	}

	private static void RetainFile(string source, string destination, List<object> files)
	{
		var exists = File.Exists(source);
		var hash = exists ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))) : null;
		if (exists)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(source, destination);
		}
		files.Add(new { SourcePath = source, RetainedPath = destination, Exists = exists, Sha256 = hash, CapturedAt = DateTimeOffset.UtcNow });
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
