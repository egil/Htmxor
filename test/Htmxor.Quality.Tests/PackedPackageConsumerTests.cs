using System.IO.Compression;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class PackedPackageConsumerTests
{
	[Fact]
	public async Task Package_only_application_registers_generated_route()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.Equal(new TrxTestRun(1, 1, 1, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumer(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}

internal sealed class PackageConsumerWorkspace : IDisposable
{
	private const string PackageVersionToken = "__HTMXOR_PACKAGE_VERSION__";
	private readonly TemporaryDirectory temporaryDirectory = new();
	private readonly string repositoryRoot;
	private readonly string consumerDirectory;
	private readonly string packageDirectory;
	private readonly string packagesDirectory;
	private readonly string resultsDirectory;
	private readonly string projectPath;
	private readonly string nugetConfigPath;
	private readonly ProcessRunner runner = new();

	public PackageConsumerWorkspace(string repositoryRoot)
	{
		this.repositoryRoot = repositoryRoot;
		consumerDirectory = Path.Combine(temporaryDirectory.Path, "consumer");
		packageDirectory = Path.Combine(temporaryDirectory.Path, "packages");
		packagesDirectory = Path.Combine(temporaryDirectory.Path, "global-packages");
		resultsDirectory = Path.Combine(temporaryDirectory.Path, "results");
		projectPath = Path.Combine(consumerDirectory, "Htmxor.PackageConsumer.csproj");
		nugetConfigPath = Path.Combine(consumerDirectory, "NuGet.config");
		Directory.CreateDirectory(consumerDirectory);
		Directory.CreateDirectory(packageDirectory);
		StageConsumer();
	}

	public string ConsumerDirectory => consumerDirectory;

	public string PackagePath => Assert.Single(Directory.EnumerateFiles(packageDirectory, "*.nupkg"));

	public string PackageVersion { get; } = $"0.0.0-issue95-{Guid.NewGuid():N}";

	public string TrxPath => Path.Combine(resultsDirectory, "package-consumer.trx");

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
			"PackageConsumer");
		foreach (var templatePath in Directory.EnumerateFiles(assets, "*.template"))
		{
			var destination = Path.Combine(
				consumerDirectory,
				Path.GetFileNameWithoutExtension(templatePath));
			var content = File.ReadAllText(templatePath)
				.Replace(PackageVersionToken, PackageVersion, StringComparison.Ordinal);
			File.WriteAllText(destination, content);
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
		      <package pattern="Newtonsoft.*" />
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
				"trx;LogFileName=package-consumer.trx",
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

internal static class PackageConsumerEvidence
{
	public static void AssertPackage(string packagePath)
	{
		using var package = ZipFile.OpenRead(packagePath);
		var paths = package.Entries.Select(entry => entry.FullName).ToArray();

		Assert.Contains("lib/net8.0/Htmxor.dll", paths);
		Assert.Contains("analyzers/dotnet/cs/Htmxor.Generators.dll", paths);
		Assert.Contains("analyzers/dotnet/cs/Htmxor.Generators.pdb", paths);
		Assert.DoesNotContain(paths, IsForbiddenPackageAssembly);
		AssertNuspecDependencies(package);
	}

	public static void AssertConsumer(string consumerDirectory, string packageVersion)
	{
		var project = XDocument.Load(Path.Combine(consumerDirectory, "Htmxor.PackageConsumer.csproj"));
		var references = project.Descendants()
			.Where(element => element.Name.LocalName == "PackageReference")
			.ToArray();
		var htmxor = Assert.Single(references, IsHtmxorPackageReference);

		Assert.Equal(packageVersion, htmxor.Attribute("Version")?.Value);
		Assert.Empty(project.Descendants().Where(element => element.Name.LocalName == "ProjectReference"));
		Assert.Empty(project.Descendants().Where(element => element.Name.LocalName == "InternalsVisibleTo"));
		AssertSourceBoundary(consumerDirectory);
		AssertRuntimeDependencies(consumerDirectory);
	}

	private static void AssertNuspecDependencies(ZipArchive package)
	{
		var nuspec = Assert.Single(package.Entries, entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
		using var stream = nuspec.Open();
		var document = XDocument.Load(stream);
		var dependencies = document.Descendants()
			.Where(element => element.Name.LocalName == "dependency")
			.Select(element => element.Attribute("id")?.Value ?? string.Empty)
			.ToArray();

		Assert.DoesNotContain(dependencies, IsBuildOnlyDependency);
	}

	private static void AssertSourceBoundary(string consumerDirectory)
	{
		var source = Directory.EnumerateFiles(consumerDirectory)
			.Where(IsSourceFile)
			.Select(File.ReadAllText)
			.ToArray();
		var applicationSource = string.Join(Environment.NewLine, source);

		Assert.Equal(1, Count(applicationSource, "AddHtmxorComponentEndpoints(routes)"));
		Assert.DoesNotContain("InternalsVisibleTo", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("Issue91GeneratedRoute", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain(
			"HtmxorGeneratedRouteRegistrationExtensions",
			applicationSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain("MapHtmxorGeneratedComponentEndpoint", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("MapGet(", applicationSource, StringComparison.Ordinal);
	}

	private static void AssertRuntimeDependencies(string consumerDirectory)
	{
		var output = Path.Combine(consumerDirectory, "bin", "Release", "net10.0");
		var dependencies = File.ReadAllText(Path.Combine(output, "Htmxor.PackageConsumer.deps.json"));
		var files = Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
			.Select(Path.GetFileName)
			.ToArray();

		Assert.DoesNotContain("Htmxor.Generators", dependencies, StringComparison.Ordinal);
		Assert.DoesNotContain("Microsoft.CodeAnalysis", dependencies, StringComparison.Ordinal);
		Assert.DoesNotContain(files, IsBuildOnlyAssembly);
	}

	private static bool IsForbiddenPackageAssembly(string path) =>
		(IsBuildOnlyAssembly(Path.GetFileName(path)) &&
			!path.Equals("analyzers/dotnet/cs/Htmxor.Generators.dll", StringComparison.Ordinal)) ||
		path.StartsWith("lib/", StringComparison.Ordinal) &&
		path.EndsWith("Htmxor.Generators.dll", StringComparison.Ordinal);

	private static bool IsBuildOnlyAssembly(string? fileName) =>
		fileName is not null &&
		(fileName.Equals("Htmxor.Generators.dll", StringComparison.Ordinal) ||
			fileName.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));

	private static bool IsBuildOnlyDependency(string packageId) =>
		packageId.Equals("Htmxor.Generators", StringComparison.Ordinal) ||
		packageId.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal);

	private static bool IsHtmxorPackageReference(XElement element) =>
		string.Equals(element.Attribute("Include")?.Value, "Htmxor", StringComparison.Ordinal);

	private static bool IsSourceFile(string path) =>
		path.EndsWith(".cs", StringComparison.Ordinal) ||
		path.EndsWith(".razor", StringComparison.Ordinal);

	private static int Count(string source, string value) =>
		source.Split(value, StringSplitOptions.None).Length - 1;
}
