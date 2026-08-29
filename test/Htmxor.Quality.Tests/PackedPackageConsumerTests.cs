using System.IO.Compression;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class PackedPackageConsumerTests
{
	private const string UnsupportedPutHandlerMessage =
		"@onput must use one double-quoted simple method-group name";
	private const string AmbiguousStockRouteMessage =
		"exactly one stock route and no HtmxRoute";
	private const string ExplicitMethodsConflictMessage =
		"explicit HtmxRoute.Methods is authoritative";

	[Fact]
	public async Task Package_only_application_infers_stock_and_htmx_only_unsafe_actions()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseLaterPageDirectiveLikeComment();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(14, 14, 14, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumer(workspace.ConsumerDirectory, workspace.PackageVersion);
	}

	[Fact]
	public async Task Package_only_application_rejects_a_component_with_two_compiled_stock_routes()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseSecondCompiledPageRoute();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("HTMXOR002", output, StringComparison.Ordinal);
		Assert.Contains("Issue100ReportPage.razor", output, StringComparison.Ordinal);
		Assert.Contains(AmbiguousStockRouteMessage, output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}

	[Fact]
	public async Task Package_only_application_rejects_a_computed_put_handler()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseComputedPutHandler();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("HTMXOR002", output, StringComparison.Ordinal);
		Assert.Contains("Issue100ReportPage.razor", output, StringComparison.Ordinal);
		Assert.Contains(UnsupportedPutHandlerMessage, output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}

	[Fact]
	public async Task Package_only_application_rejects_an_explicit_methods_conflict()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseExplicitMethodsConflict();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("HTMXOR002", output, StringComparison.Ordinal);
		Assert.Contains("Issue97ReportComponent.razor", output, StringComparison.Ordinal);
		Assert.Contains(ExplicitMethodsConflictMessage, output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
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

	public string ConsumerAssemblyPath => Path.Combine(
		consumerDirectory,
		"bin",
		"Release",
		"net10.0",
		"Htmxor.PackageConsumer.dll");

	public string PackagePath => Assert.Single(Directory.EnumerateFiles(packageDirectory, "*.nupkg"));

	public string PackageVersion { get; } = $"0.0.0-issue97-{Guid.NewGuid():N}";

	public string TrxPath => Path.Combine(resultsDirectory, "package-consumer.trx");

	public async Task<ProcessResult> RunAsync()
	{
		await PackAsync();
		await RestoreAsync();
		await BuildRequiredAsync();

		return await TestAsync();
	}

	public async Task<ProcessResult> BuildForDiagnosticAsync()
	{
		await PackAsync();
		await RestoreAsync();

		return await BuildAsync();
	}

	public void UseComputedPutHandler()
	{
		var componentPath = Path.Combine(
			consumerDirectory,
			"Issue100ReportPage.razor");
		var source = File.ReadAllText(componentPath);
		const string supportedHandler = "@onput=\"PutReport\"";
		const string computedHandler =
			"@onput=\"@((HtmxEventArgs args) => PutReport(args))\"";
		var handlerIndex = source.IndexOf(supportedHandler, StringComparison.Ordinal);
		if (handlerIndex < 0 ||
			handlerIndex != source.LastIndexOf(supportedHandler, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain exactly one supported PUT handler.");
		}

		var rewritten = source.Replace(
			supportedHandler,
			computedHandler,
			StringComparison.Ordinal);

		File.WriteAllText(componentPath, rewritten);
	}

	public void UseExplicitMethodsConflict()
	{
		var componentPath = Path.Combine(
			consumerDirectory,
			"Issue97ReportComponent.razor");
		var source = File.ReadAllText(componentPath);
		const string omittedMethods =
			"Htmxor.HtmxRoute(\"/htmx-reports/{ReportId:int}\")";
		const string explicitMethods =
			"Htmxor.HtmxRoute(\"/htmx-reports/{ReportId:int}\", Methods = new[] { \"GET\" })";
		var rewritten = source.Replace(
			omittedMethods,
			explicitMethods,
			StringComparison.Ordinal);
		if (string.Equals(source, rewritten, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain one omitted-Methods report route.");
		}

		File.WriteAllText(componentPath, rewritten);
	}

	public void UseLaterPageDirectiveLikeComment()
	{
		var componentPath = Path.Combine(
			consumerDirectory,
			"Issue100ReportPage.razor");
		var source = File.ReadAllText(componentPath);
		const string codeBlockStart = "@code {";
		var codeBlockIndex = source.IndexOf(codeBlockStart, StringComparison.Ordinal);
		if (codeBlockIndex < 0 ||
			codeBlockIndex != source.LastIndexOf(codeBlockStart, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain exactly one code block.");
		}

		const string codeBlockWithPageLikeComment = """
			@code {
			    /*
			    @page "/not-a-directive"
			    */
			""";
		var rewritten = source.Replace(
			codeBlockStart,
			codeBlockWithPageLikeComment,
			StringComparison.Ordinal);

		File.WriteAllText(componentPath, rewritten);
	}

	public void UseSecondCompiledPageRoute()
	{
		var assets = Path.Combine(
			repositoryRoot,
			"test",
			"Htmxor.Quality.Tests",
			"PackageConsumer");
		var partialTemplate = Path.Combine(
			assets,
			"Issue100ReportPage.razor.cs.scenario");
		File.Copy(
			partialTemplate,
			Path.Combine(consumerDirectory, "Issue100ReportPage.razor.cs"));

		var testsPath = Path.Combine(consumerDirectory, "PackageConsumerTests.cs");
		var source = File.ReadAllText(testsPath);
		var rewritten = source
			.Replace(
				"private const string PageDeclaredRoute = \"/reports/{ReportId:int}\";",
				"private const string PageDeclaredRoute = \"/alternate-reports/{ReportId:int}\";",
				StringComparison.Ordinal)
			.Replace(
				"private const string PageRequestPath = \"/reports/42\";",
				"private const string PageRequestPath = \"/alternate-reports/42\";",
				StringComparison.Ordinal);
		if (string.Equals(source, rewritten, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain the stock page route constants.");
		}

		File.WriteAllText(testsPath, rewritten);
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

	private Task<ProcessResult> BuildRequiredAsync() =>
		RunRequiredAsync(
			"build",
			projectPath,
			"--configuration",
			"Release",
			"--no-restore");

	private Task<ProcessResult> BuildAsync() =>
		runner.RunAsync(new(
			"dotnet",
			consumerDirectory,
			[
				"build",
				projectPath,
				"--configuration",
				"Release",
				"--no-restore",
			],
			EnsureSuccess: false));

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
		var razorSource = string.Join(
			Environment.NewLine,
			Directory.EnumerateFiles(consumerDirectory, "*.razor").Select(File.ReadAllText));
		var summarySource = File.ReadAllText(Path.Combine(
			consumerDirectory,
			"Issue97SummaryComponent.razor"));
		var reportSource = File.ReadAllText(Path.Combine(
			consumerDirectory,
			"Issue97ReportComponent.razor"));
		var pageSource = File.ReadAllText(Path.Combine(
			consumerDirectory,
			"Issue100ReportPage.razor"));
		const string reportRoute =
			"@attribute [Htmxor.HtmxRoute(\"/htmx-reports/{ReportId:int}\")]";
		const string summaryRoute =
			"@attribute [Htmxor.HtmxRoute(SummaryRoute, Methods = [ SummaryGetMethod, SummaryDeleteMethod ])]";
		const string summaryAuthorization = "@attribute [Authorize(SummaryPolicy)]";
		const string pageRoute = "@page \"/reports/{ReportId:int}\"";

		Assert.Equal(1, Count(applicationSource, "AddHtmxorComponentEndpoints(routes)"));
		Assert.Equal(1, Count(applicationSource, "MapGroup(RoutePrefix)"));
		Assert.Equal(1, Count(applicationSource, "MapRazorComponents<Issue97App>()"));
		Assert.Equal(2, Count(razorSource, "Htmxor.HtmxRoute"));
		Assert.Equal(3, Count(razorSource, "Authorize"));
		Assert.Equal(2, Count(razorSource, "hx-put="));
		Assert.Equal(1, Count(razorSource, "hx-patch="));
		Assert.Equal(1, Count(razorSource, "hx-delete="));
		Assert.Equal(1, Count(razorSource, "@onput=\"PutReport\""));
		Assert.Equal(1, Count(razorSource, "@onpatch=\"PatchReport\""));
		Assert.Equal(1, Count(razorSource, "@ondelete=\"DeleteReport\""));
		Assert.Equal(1, Count(pageSource, pageRoute));
		Assert.Equal(1, Count(pageSource, "@onput=\"PutReport\""));
		Assert.Equal(1, Count(reportSource, reportRoute));
		Assert.DoesNotContain("@onput", reportSource, StringComparison.Ordinal);
		Assert.Equal(1, Count(summarySource, summaryRoute));
		Assert.Equal(1, Count(summarySource, summaryAuthorization));
		AssertSummaryDirectiveOrdering(summarySource, summaryRoute, summaryAuthorization);
		Assert.DoesNotContain("InternalsVisibleTo", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("Issue91GeneratedRoute", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain(
			"HtmxorGeneratedRouteRegistrationExtensions",
			applicationSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain("MapHtmxorGeneratedComponentEndpoint", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("MapGet(", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("MapPut(", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("MapMethods(", applicationSource, StringComparison.Ordinal);
	}

	private static void AssertSummaryDirectiveOrdering(
		string summarySource,
		string summaryRoute,
		string summaryAuthorization)
	{
		var usingIndex = summarySource.IndexOf(
			"@using Microsoft.AspNetCore.Authorization",
			StringComparison.Ordinal);
		var codeIndex = summarySource.IndexOf("@code {", StringComparison.Ordinal);
		var commentLikeTextIndex = summarySource.IndexOf(
			"private const string RazorCommentLikeText = \"@*\";",
			StringComparison.Ordinal);
		var routeIndex = summarySource.IndexOf(summaryRoute, StringComparison.Ordinal);
		var authorizeIndex = summarySource.IndexOf(summaryAuthorization, StringComparison.Ordinal);
		var markupIndex = summarySource.IndexOf(
			"<section data-issue-97-summary-component>",
			StringComparison.Ordinal);

		int[] directiveOrder =
		[
			usingIndex,
			markupIndex,
			codeIndex,
			commentLikeTextIndex,
			routeIndex,
			authorizeIndex,
		];
		Assert.DoesNotContain(-1, directiveOrder);
		Assert.Equal(directiveOrder.Order(), directiveOrder);
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
