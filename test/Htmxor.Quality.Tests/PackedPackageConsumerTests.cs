using System.IO.Compression;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedPackageConsumerTests
{
	private const string UnsupportedPutHandlerMessage =
		"@onput must use one double-quoted simple method-group name";
	private const string AmbiguousStockRouteMessage =
		"exactly one stock route and no HtmxRoute";
	private const string ExplicitMethodsConflictMessage =
		"explicit HtmxRoute.Methods is authoritative";
	private const string MissingCSharpMethodsMessage =
		"a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods";
	private const string MismatchedCSharpPartialMessage =
		"a C# HtmxRoute declaration on a Razor component must use the matching .razor.cs partial";

	[Fact]
	public async Task Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseLaterPageDirectiveLikeComment();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(13, 13, 13, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumer(workspace.ConsumerDirectory, workspace.PackageVersion);
	}

	[Fact]
	public async Task Package_only_grouped_application_preserves_stock_page_behavior()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseLegacyDestinationRegistration();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(13, 13, 13, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}

	[Fact]
	public async Task Package_only_application_preserves_actionless_unsafe_route_antiforgery()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseActionlessUnsafeSummaryRoute();

		var result = await workspace.RunAsync();
		var testRun = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(
			result.ExitCode == 0,
			result.StandardOutput + Environment.NewLine + result.StandardError +
			Environment.NewLine + $"TRX: {testRun}");
		Assert.Equal(new TrxTestRun(15, 15, 15, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
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

	[Fact]
	public async Task Package_only_application_rejects_a_CSharp_route_without_methods()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseAllCSharpRouteWithoutMethods();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("HTMXOR001", output, StringComparison.Ordinal);
		Assert.Contains("Issue97SummaryComponent.cs", output, StringComparison.Ordinal);
		Assert.Contains(MissingCSharpMethodsMessage, output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
		var routeRegistration = workspace.ReadGeneratedRouteRegistration();
		Assert.Contains("Issue97ReportComponent", routeRegistration, StringComparison.Ordinal);
		Assert.DoesNotContain("Issue97SummaryComponent", routeRegistration, StringComparison.Ordinal);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}

	[Fact]
	public async Task Package_only_application_rejects_an_explicit_route_in_a_nonmatching_partial()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseNonmatchingExplicitRoutePartial();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("HTMXOR001", output, StringComparison.Ordinal);
		Assert.Contains("Issue97ReportRoute.cs", output, StringComparison.Ordinal);
		Assert.Contains(MismatchedCSharpPartialMessage, output, StringComparison.Ordinal);
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
	private readonly string generatedSourcesDirectory;
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
		generatedSourcesDirectory = Path.Combine(consumerDirectory, "obj", "generated");
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

	public string ReadGeneratedRouteRegistration()
	{
		var path = Assert.Single(
			Directory.EnumerateFiles(
				generatedSourcesDirectory,
				"HtmxorGeneratedRouteRegistration.g.cs",
				SearchOption.AllDirectories));
		return File.ReadAllText(path);
	}

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

	public void UseLegacyDestinationRegistration()
	{
		var applicationPath = Path.Combine(
			consumerDirectory,
			"PackageConsumerTests.cs");
		var source = File.ReadAllText(applicationPath);
		const string acceptedRegistration = ".AddHtmxorComponentEndpoints();";
		const string legacyRegistration = ".AddHtmxorComponentEndpoints(routes);";
		var rewritten = source.Replace(
			acceptedRegistration,
			legacyRegistration,
			StringComparison.Ordinal);
		if (string.Equals(source, rewritten, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain the no-argument registration.");
		}

		File.WriteAllText(applicationPath, rewritten);
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
			"Issue97ReportComponent.razor.cs");
		var source = File.ReadAllText(componentPath);
		const string supportedMethods =
			"HtmxRoute(\"/htmx-reports/{ReportId:int}\", Methods = [\"GET\", \"PATCH\"])";
		const string conflictingMethods =
			"HtmxRoute(\"/htmx-reports/{ReportId:int}\", Methods = [\"GET\"])";
		var rewritten = source.Replace(
			supportedMethods,
			conflictingMethods,
			StringComparison.Ordinal);
		if (string.Equals(source, rewritten, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain one explicit GET/PATCH report route.");
		}

		File.WriteAllText(componentPath, rewritten);
	}

	public void UseAllCSharpRouteWithoutMethods()
	{
		var componentPath = Path.Combine(
			consumerDirectory,
			"Issue97SummaryComponent.cs");
		var source = File.ReadAllText(componentPath);
		const string explicitMethods =
			"HtmxRoute(\"/summaries/{SummaryId:int}\", Methods = [\"GET\"])";
		const string omittedMethods =
			"HtmxRoute(\"/summaries/{SummaryId:int}\")";
		var rewritten = source.Replace(
			explicitMethods,
			omittedMethods,
			StringComparison.Ordinal);
		if (string.Equals(source, rewritten, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain one explicit-Methods all-C# route.");
		}

		File.WriteAllText(componentPath, rewritten);

		var scenarioPath = Path.Combine(
			repositoryRoot,
			"test",
			"Htmxor.Quality.Tests",
			"PackageConsumer",
			"Issue97SummaryComponent.razor.scenario");
		File.Copy(
			scenarioPath,
			Path.Combine(consumerDirectory, "Issue97SummaryComponent.razor"));
	}

	public void UseNonmatchingExplicitRoutePartial()
	{
		var matchingPath = Path.Combine(
			consumerDirectory,
			"Issue97ReportComponent.razor.cs");
		var nonmatchingPath = Path.Combine(
			consumerDirectory,
			"Issue97ReportRoute.cs");
		File.Move(matchingPath, nonmatchingPath);
	}

	public void UseActionlessUnsafeSummaryRoute()
	{
		var componentPath = Path.Combine(
			consumerDirectory,
			"Issue97SummaryComponent.cs");
		var componentSource = File.ReadAllText(componentPath);
		const string getOnlyRoute =
			"HtmxRoute(\"/summaries/{SummaryId:int}\", Methods = [\"GET\"])";
		const string unsafeRoute =
			"HtmxRoute(\"/summaries/{SummaryId:int}\", Methods = [\"GET\", \"DELETE\"])";
		var rewrittenComponent = componentSource.Replace(
			getOnlyRoute,
			unsafeRoute,
			StringComparison.Ordinal);
		if (string.Equals(componentSource, rewrittenComponent, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer must contain one explicit-GET all-C# route.");
		}

		File.WriteAllText(componentPath, rewrittenComponent);

		var projectSource = File.ReadAllText(projectPath);
		const string rootNamespace =
			"<RootNamespace>Htmxor.PackageConsumer</RootNamespace>";
		var scenarioProperties =
			rootNamespace + Environment.NewLine +
			"\t\t<DefineConstants>ISSUE103_ACTIONLESS_UNSAFE</DefineConstants>";
		var rewrittenProject = projectSource.Replace(
			rootNamespace,
			scenarioProperties,
			StringComparison.Ordinal);
		if (string.Equals(projectSource, rewrittenProject, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				"The staged package consumer project must contain its root namespace property.");
		}

		File.WriteAllText(projectPath, rewrittenProject);

		var scenarioPath = Path.Combine(
			repositoryRoot,
			"test",
			"Htmxor.Quality.Tests",
			"PackageConsumer",
			"Issue103ActionlessUnsafeTests.cs.scenario");
		File.Copy(
			scenarioPath,
			Path.Combine(consumerDirectory, "Issue103ActionlessUnsafeTests.cs"));
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
				"-p:EmitCompilerGeneratedFiles=true",
				$"-p:CompilerGeneratedFilesOutputPath={generatedSourcesDirectory}",
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

		Assert.Contains("lib/net10.0/Htmxor.dll", paths);
		Assert.DoesNotContain("lib/net8.0/Htmxor.dll", paths);
		Assert.Contains("analyzers/dotnet/cs/Htmxor.Generators.dll", paths);
		Assert.Contains("analyzers/dotnet/cs/Htmxor.Generators.pdb", paths);
		Assert.Contains("staticwebassets/htmxor.js", paths);
		Assert.DoesNotContain(paths, IsHtmxRuntimeOrLegacyExtension);
		Assert.DoesNotContain(paths, IsForbiddenPackageAssembly);
		AssertNuspecDependencies(package);
	}

	public static void AssertConsumer(string consumerDirectory, string packageVersion)
	{
		var project = XDocument.Load(Path.Combine(consumerDirectory, "Htmxor.PackageConsumer.csproj"));
		var targetFramework = Assert.Single(
			project.Descendants(),
			element => element.Name.LocalName == "TargetFramework");
		var references = project.Descendants()
			.Where(element => element.Name.LocalName == "PackageReference")
			.ToArray();
		var htmxor = Assert.Single(references, IsHtmxorPackageReference);

		Assert.Equal("net10.0", targetFramework.Value);
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
		var summaryPath = Path.Combine(
			consumerDirectory,
			"Issue97SummaryComponent.cs");
		var summaryRazorPath = Path.Combine(
			consumerDirectory,
			"Issue97SummaryComponent.razor");
		Assert.True(
			File.Exists(summaryPath),
			"The summary tracer must be authored exclusively in C#.");
		Assert.False(
			File.Exists(summaryRazorPath),
			"The all-C# summary tracer must not have a companion Razor file.");
		var summarySource = File.ReadAllText(summaryPath);
		var reportSource = File.ReadAllText(Path.Combine(
			consumerDirectory,
			"Issue97ReportComponent.razor"));
		var auditSource = File.ReadAllText(Path.Combine(
			consumerDirectory,
			"Issue141AuditComponent.razor"));
		var reportPartialPath = Path.Combine(
			consumerDirectory,
			"Issue97ReportComponent.razor.cs");
		Assert.True(
			File.Exists(reportPartialPath),
			"The packaged PATCH handler must live in the matching Issue97ReportComponent.razor.cs partial.");
		var reportPartialSource = File.ReadAllText(reportPartialPath);
		var pageSource = File.ReadAllText(Path.Combine(
			consumerDirectory,
			"Issue100ReportPage.razor"));
		const string reportRoute =
			"[HtmxRoute(\"/htmx-reports/{ReportId:int}\", Methods = [\"GET\", \"PATCH\"])]";
		const string summaryRoute =
			"[HtmxRoute(\"/summaries/{SummaryId:int}\", Methods = [\"GET\"])]";
		const string auditRoute =
			"@attribute [HtmxRoute(\"/audits/{AuditId:int}\", Methods = [\"GET\", \"POST\"])]";
		const string pageRoute = "@page \"/reports/{ReportId:int}\"";

		Assert.Equal(1, Count(applicationSource, "AddHtmxorComponentEndpoints()"));
		Assert.Equal(1, Count(applicationSource, "MapGroup(RoutePrefix)"));
		Assert.Equal(1, Count(applicationSource, "routes.MapRazorComponents<Issue97App>()"));
		Assert.DoesNotContain("AddHtmxorComponentEndpoints(routes)", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("MapGroup(string.Empty)", applicationSource, StringComparison.Ordinal);
		Assert.Equal(3, Count(applicationSource, "HtmxRoute("));
		Assert.Equal(1, Count(razorSource, "HtmxRoute("));
		Assert.Equal(3, Count(razorSource, "@attribute [Authorize"));
		Assert.Equal(2, Count(razorSource, "hx-put="));
		Assert.Equal(1, Count(razorSource, "hx-patch="));
		Assert.Equal(1, Count(razorSource, "hx-delete="));
		Assert.Equal(1, Count(razorSource, "@onput=\"PutReport\""));
		Assert.Equal(1, Count(razorSource, "@onpatch=\"PatchReport\""));
		Assert.Equal(1, Count(razorSource, "@ondelete=\"DeleteReport\""));
		Assert.Equal(1, Count(pageSource, pageRoute));
		Assert.Equal(1, Count(pageSource, "@onput=\"PutReport\""));
		Assert.DoesNotContain("HtmxRoute(", reportSource, StringComparison.Ordinal);
		Assert.Equal(1, Count(reportSource, "hx-put="));
		Assert.Equal(1, Count(reportSource, "@onpatch=\"PatchReport\""));
		Assert.DoesNotContain("@onput", reportSource, StringComparison.Ordinal);
		Assert.Equal(1, Count(reportPartialSource, reportRoute));
		Assert.Equal(1, Count(reportPartialSource, "private void PatchReport(HtmxEventArgs _)"));
		Assert.Equal(1, Count(summarySource, summaryRoute));
		Assert.Equal(1, Count(auditSource, auditRoute));
		Assert.Equal(1, Count(summarySource, "[Authorize("));
		Assert.Contains(
			"protected override void BuildRenderTree(RenderTreeBuilder builder)",
			summarySource,
			StringComparison.Ordinal);
		Assert.Equal(1, Count(summarySource, "\"hx-delete\""));
		Assert.Equal(1, Count(summarySource, "\"ondelete\""));
		Assert.Equal(1, Count(summarySource, "EventCallback.Factory.Create<HtmxEventArgs>"));
		Assert.Equal(1, Count(summarySource, "private void DeleteSummary(HtmxEventArgs _)"));
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

	private static bool IsHtmxRuntimeOrLegacyExtension(string path) =>
		path.EndsWith("/htmx.min.js", StringComparison.Ordinal) ||
		path.EndsWith("/htmx.d.ts", StringComparison.Ordinal) ||
		path.EndsWith("/event-header.js", StringComparison.Ordinal);

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
