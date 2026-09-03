using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
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
		Assert.Equal(new TrxTestRun(93, 93, 93, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumer(workspace.ConsumerDirectory, workspace.PackageVersion);
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
		Assert.Equal(new TrxTestRun(96, 96, 96, 0, 0, 0, 0), testRun);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
	}

	[Fact]
	public async Task Package_only_public_surface_matches_retained_allow_list()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		await workspace.PackOnlyAsync();

		var allowListPath = Path.Combine(
			RepositoryLocator.Find(),
			"docs",
			"roadmap",
			"v1",
			"issue-154-package-public-surface.txt");
		var allowList = File.ReadAllText(allowListPath);
		var marker = allowList.IndexOf("ASSEMBLY ", StringComparison.Ordinal);
		Assert.True(marker >= 0, "The retained package surface must contain an assembly marker.");

		Assert.Equal(
			allowList[marker..].Trim(),
			PackagePublicSurface.Format(workspace.PackagePath));
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

	public Task<ProcessResult> PackOnlyAsync() => PackAsync("1.0.0");

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

	private Task<ProcessResult> PackAsync() => PackAsync(PackageVersion);

	private Task<ProcessResult> PackAsync(string packageVersion) =>
		RunRequiredAsync(
			"pack",
			Path.Combine(repositoryRoot, "src", "Htmxor", "Htmxor.csproj"),
			"--configuration",
			"Release",
			"--no-restore",
			"--output",
			packageDirectory,
			$"-p:MinVerVersionOverride={packageVersion}");

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

internal static class PackagePublicSurface
{
	public static string Format(string packagePath)
	{
		using var package = ZipFile.OpenRead(packagePath);
		using var entry = Assert.Single(package.Entries, entry => entry.FullName == "lib/net10.0/Htmxor.dll").Open();
		using var assemblyImage = new MemoryStream();
		entry.CopyTo(assemblyImage);
		assemblyImage.Position = 0;
		using var peReader = new PEReader(assemblyImage);
		var reader = peReader.GetMetadataReader();
		var assembly = reader.GetAssemblyDefinition();
		var output = new StringBuilder();
		output.AppendLine($"ASSEMBLY {reader.GetString(assembly.Name)} {assembly.Version}");

		foreach (var typeHandle in reader.TypeDefinitions
			.Where(handle => IsExported(reader, handle))
			.OrderBy(handle => FormatTypeName(reader, handle), StringComparer.Ordinal))
		{
			var type = reader.GetTypeDefinition(typeHandle);
			var context = SignatureContext.ForType(reader, type);
			output.AppendLine($"TYPE {FormatType(reader, typeHandle, type, context)}");
			foreach (var member in GetMembers(reader, type, context)
				.OrderBy(member => member.Kind)
				.ThenBy(member => member.Name, StringComparer.Ordinal)
				.ThenBy(member => member.Signature, StringComparer.Ordinal))
			{
				output.AppendLine(member.Output);
			}
		}

		return output.ToString().TrimEnd();
	}

	private static string FormatType(
		MetadataReader reader,
		TypeDefinitionHandle handle,
		TypeDefinition type,
		SignatureContext context) =>
		$"{FormatTypeName(reader, handle)} [kind={GetTypeKind(reader, type, context)};abstract={type.Attributes.HasFlag(TypeAttributes.Abstract)};sealed={type.Attributes.HasFlag(TypeAttributes.Sealed)};genericArity={type.GetGenericParameters().Count};base={FormatEntityType(reader, type.BaseType, context)}]";

	private static string GetTypeKind(MetadataReader reader, TypeDefinition type, SignatureContext context)
	{
		if (type.Attributes.HasFlag(TypeAttributes.Interface))
		{
			return "interface";
		}

		return FormatEntityType(reader, type.BaseType, context) switch
		{
			"System.Enum" => "enum",
			"System.MulticastDelegate" => "delegate",
			"System.ValueType" => "struct",
			_ => "class",
		};
	}

	private static IEnumerable<SurfaceMember> GetMembers(
		MetadataReader reader,
		TypeDefinition type,
		SignatureContext typeContext) =>
		GetMethods(reader, type, typeContext)
			.Concat(GetEvents(reader, type, typeContext))
			.Concat(GetFields(reader, type, typeContext))
			.Concat(GetProperties(reader, type, typeContext))
			.Concat(GetNestedTypes(reader, type));

	private static IEnumerable<SurfaceMember> GetMethods(
		MetadataReader reader,
		TypeDefinition type,
		SignatureContext typeContext)
	{
		var provider = new SurfaceSignatureProvider(reader);
		foreach (var methodHandle in type.GetMethods())
		{
			var method = reader.GetMethodDefinition(methodHandle);
			if ((method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
			{
				continue;
			}

			var name = reader.GetString(method.Name);
			var context = typeContext.ForMethod(reader, method);
			var signature = method.DecodeSignature(provider, context);
			var displayName = name + FormatGenericParameters(context.MethodParameters);
			var displaySignature = $"{signature.ReturnType} {displayName}({string.Join(", ", signature.ParameterTypes)})";
			var kind = name is ".ctor" or ".cctor" ? "Constructor" : "Method";
			var kindOrder = kind == "Constructor" ? 1 : 4;
			var shape = FormatMethodShape(method.Attributes, method.GetGenericParameters().Count, signature.ParameterTypes.Length);
			yield return new(kindOrder, name, displaySignature, $"  {kind} [{shape}] {displaySignature}");
		}
	}

	private static IEnumerable<SurfaceMember> GetEvents(
		MetadataReader reader,
		TypeDefinition type,
		SignatureContext typeContext)
	{
		foreach (var eventHandle in type.GetEvents())
		{
			var @event = reader.GetEventDefinition(eventHandle);
			var accessors = @event.GetAccessors();
			if (!IsPublic(reader, accessors.Adder) && !IsPublic(reader, accessors.Remover))
			{
				continue;
			}

			var name = reader.GetString(@event.Name);
			var signature = $"{FormatEntityType(reader, @event.Type, typeContext)} {name}";
			var shape = $"visibility={FormatAccessorVisibility(reader, accessors.Adder)}|{FormatAccessorVisibility(reader, accessors.Remover)};static={IsStatic(reader, accessors.Adder, accessors.Remover)};add={FormatAccessor(reader, accessors.Adder)};remove={FormatAccessor(reader, accessors.Remover)}";
			yield return new(2, name, signature, $"  Event [{shape}] {signature}");
		}
	}

	private static IEnumerable<SurfaceMember> GetFields(
		MetadataReader reader,
		TypeDefinition type,
		SignatureContext typeContext)
	{
		var provider = new SurfaceSignatureProvider(reader);
		foreach (var fieldHandle in type.GetFields())
		{
			var field = reader.GetFieldDefinition(fieldHandle);
			if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public)
			{
				continue;
			}

			var name = reader.GetString(field.Name);
			var signature = $"{field.DecodeSignature(provider, typeContext)} {name}";
			var shape = $"visibility=public;static={field.Attributes.HasFlag(FieldAttributes.Static)};literal={field.Attributes.HasFlag(FieldAttributes.Literal)};readonly={field.Attributes.HasFlag(FieldAttributes.InitOnly)}";
			yield return new(3, name, signature, $"  Field [{shape}] {signature}");
		}
	}

	private static IEnumerable<SurfaceMember> GetProperties(
		MetadataReader reader,
		TypeDefinition type,
		SignatureContext typeContext)
	{
		var provider = new SurfaceSignatureProvider(reader);
		foreach (var propertyHandle in type.GetProperties())
		{
			var property = reader.GetPropertyDefinition(propertyHandle);
			var accessors = property.GetAccessors();
			if (!IsPublic(reader, accessors.Getter) && !IsPublic(reader, accessors.Setter))
			{
				continue;
			}

			var name = reader.GetString(property.Name);
			var decoded = property.DecodeSignature(provider, typeContext);
			var parameters = decoded.ParameterTypes.Length == 0
				? string.Empty
				: $"({string.Join(", ", decoded.ParameterTypes)})";
			var signature = $"{decoded.ReturnType} {name}{parameters}";
			var shape = $"visibility={FormatAccessorVisibility(reader, accessors.Getter)}|{FormatAccessorVisibility(reader, accessors.Setter)};static={IsStatic(reader, accessors.Getter, accessors.Setter)};get={FormatAccessor(reader, accessors.Getter)};set={FormatAccessor(reader, accessors.Setter)}";
			yield return new(5, name, signature, $"  Property [{shape}] {signature}");
		}
	}

	private static IEnumerable<SurfaceMember> GetNestedTypes(
		MetadataReader reader,
		TypeDefinition type)
	{
		foreach (var nestedHandle in type.GetNestedTypes())
		{
			var nested = reader.GetTypeDefinition(nestedHandle);
			if ((nested.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NestedPublic)
			{
				continue;
			}

			var name = reader.GetString(nested.Name);
			var signature = FormatTypeName(reader, nestedHandle);
			var shape = $"visibility=public;abstract={nested.Attributes.HasFlag(TypeAttributes.Abstract)};sealed={nested.Attributes.HasFlag(TypeAttributes.Sealed)}";
			yield return new(6, name, signature, $"  NestedType [{shape}] {signature}");
		}
	}

	private static string FormatMethodShape(MethodAttributes attributes, int genericArity, int parameterCount) =>
		$"visibility={FormatMethodVisibility(attributes)};static={attributes.HasFlag(MethodAttributes.Static)};abstract={attributes.HasFlag(MethodAttributes.Abstract)};virtual={attributes.HasFlag(MethodAttributes.Virtual)};final={attributes.HasFlag(MethodAttributes.Final)};genericArity={genericArity};parameters={parameterCount}";

	private static string FormatGenericParameters(ImmutableArray<string> parameters) =>
		parameters.Length == 0 ? string.Empty : $"[{string.Join(",", parameters)}]";

	private static bool IsExported(MetadataReader reader, TypeDefinitionHandle handle)
	{
		var type = reader.GetTypeDefinition(handle);
		return (type.Attributes & TypeAttributes.VisibilityMask) switch
		{
			TypeAttributes.Public => true,
			TypeAttributes.NestedPublic => IsExported(reader, type.GetDeclaringType()),
			_ => false,
		};
	}

	private static string FormatTypeName(MetadataReader reader, TypeDefinitionHandle handle)
	{
		var type = reader.GetTypeDefinition(handle);
		var name = reader.GetString(type.Name);
		var declaringType = type.GetDeclaringType();
		if (!declaringType.IsNil)
		{
			return $"{FormatTypeName(reader, declaringType)}+{name}";
		}

		var @namespace = reader.GetString(type.Namespace);
		return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
	}

	private static string FormatTypeName(MetadataReader reader, TypeReferenceHandle handle)
	{
		var type = reader.GetTypeReference(handle);
		var name = reader.GetString(type.Name);
		if (type.ResolutionScope.Kind == HandleKind.TypeReference)
		{
			return $"{FormatTypeName(reader, (TypeReferenceHandle)type.ResolutionScope)}+{name}";
		}

		var @namespace = reader.GetString(type.Namespace);
		return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
	}

	private static string FormatEntityType(MetadataReader reader, EntityHandle handle, SignatureContext context)
	{
		if (handle.IsNil)
		{
			return "none";
		}

		return handle.Kind switch
		{
			HandleKind.TypeDefinition => FormatTypeName(reader, (TypeDefinitionHandle)handle),
			HandleKind.TypeReference => FormatTypeName(reader, (TypeReferenceHandle)handle),
			HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)handle)
				.DecodeSignature(new SurfaceSignatureProvider(reader), context),
			_ => throw new InvalidOperationException($"Unsupported type handle: {handle.Kind}."),
		};
	}

	private static bool IsPublic(MetadataReader reader, MethodDefinitionHandle handle) =>
		!handle.IsNil && (reader.GetMethodDefinition(handle).Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

	private static bool IsStatic(MetadataReader reader, MethodDefinitionHandle first, MethodDefinitionHandle second)
	{
		var handle = first.IsNil ? second : first;
		return !handle.IsNil && reader.GetMethodDefinition(handle).Attributes.HasFlag(MethodAttributes.Static);
	}

	private static string FormatAccessor(MetadataReader reader, MethodDefinitionHandle handle) =>
		handle.IsNil
			? "none"
			: $"{FormatMethodVisibility(reader.GetMethodDefinition(handle).Attributes)},{(reader.GetMethodDefinition(handle).Attributes.HasFlag(MethodAttributes.Static) ? "static" : "instance")}";

	private static string FormatAccessorVisibility(MetadataReader reader, MethodDefinitionHandle handle) =>
		handle.IsNil ? "none" : FormatMethodVisibility(reader.GetMethodDefinition(handle).Attributes);

	private static string FormatMethodVisibility(MethodAttributes attributes) =>
		(attributes & MethodAttributes.MemberAccessMask) switch
		{
			MethodAttributes.Public => "public",
			MethodAttributes.Family => "protected",
			MethodAttributes.Assembly => "internal",
			_ => "private",
		};

	private readonly record struct SurfaceMember(int Kind, string Name, string Signature, string Output);

	private readonly record struct SignatureContext(
		ImmutableArray<string> TypeParameters,
		ImmutableArray<string> MethodParameters)
	{
		public static SignatureContext ForType(MetadataReader reader, TypeDefinition type) =>
			new(GetParameterNames(reader, type.GetGenericParameters()), []);

		public SignatureContext ForMethod(MetadataReader reader, MethodDefinition method) =>
			this with { MethodParameters = GetParameterNames(reader, method.GetGenericParameters()) };

		private static ImmutableArray<string> GetParameterNames(
			MetadataReader reader,
			GenericParameterHandleCollection handles) =>
			handles.Select(handle => reader.GetString(reader.GetGenericParameter(handle).Name)).ToImmutableArray();
	}

	private sealed class SurfaceSignatureProvider(MetadataReader reader) : ISignatureTypeProvider<string, SignatureContext>
	{
		public string GetArrayType(string elementType, ArrayShape shape) =>
			$"{elementType}[{new string(',', shape.Rank - 1)}]";

		public string GetByReferenceType(string elementType) => $"{elementType} ByRef";

		public string GetFunctionPointerType(MethodSignature<string> signature) =>
			$"methodptr({string.Join(", ", signature.ParameterTypes)})->{signature.ReturnType}";

		public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
			$"{genericType}[{string.Join(",", typeArguments.Select(QualifyGenericArgument))}]";

		public string GetGenericMethodParameter(SignatureContext genericContext, int index) =>
			index < genericContext.MethodParameters.Length ? genericContext.MethodParameters[index] : $"!!{index}";

		public string GetGenericTypeParameter(SignatureContext genericContext, int index) =>
			index < genericContext.TypeParameters.Length ? genericContext.TypeParameters[index] : $"!{index}";

		public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) =>
			$"{unmodifiedType} {(isRequired ? "modreq" : "modopt")}({modifier})";

		public string GetPinnedType(string elementType) => $"{elementType} pinned";

		public string GetPointerType(string elementType) => $"{elementType}*";

		public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
		{
			PrimitiveTypeCode.Boolean => "Boolean",
			PrimitiveTypeCode.Byte => "Byte",
			PrimitiveTypeCode.Char => "Char",
			PrimitiveTypeCode.Double => "Double",
			PrimitiveTypeCode.Int16 => "Int16",
			PrimitiveTypeCode.Int32 => "Int32",
			PrimitiveTypeCode.Int64 => "Int64",
			PrimitiveTypeCode.IntPtr => "System.IntPtr",
			PrimitiveTypeCode.Object => "System.Object",
			PrimitiveTypeCode.SByte => "SByte",
			PrimitiveTypeCode.Single => "Single",
			PrimitiveTypeCode.String => "System.String",
			PrimitiveTypeCode.TypedReference => "System.TypedReference",
			PrimitiveTypeCode.UInt16 => "UInt16",
			PrimitiveTypeCode.UInt32 => "UInt32",
			PrimitiveTypeCode.UInt64 => "UInt64",
			PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
			PrimitiveTypeCode.Void => "Void",
			_ => throw new InvalidOperationException($"Unsupported primitive type: {typeCode}."),
		};

		public string GetSZArrayType(string elementType) => $"{elementType}[]";

		public string GetTypeFromDefinition(
			MetadataReader metadataReader,
			TypeDefinitionHandle handle,
			byte rawTypeKind) => FormatTypeName(reader, handle);

		public string GetTypeFromReference(
			MetadataReader metadataReader,
			TypeReferenceHandle handle,
			byte rawTypeKind) => FormatTypeName(reader, handle);

		public string GetTypeFromSpecification(
			MetadataReader metadataReader,
			SignatureContext genericContext,
			TypeSpecificationHandle handle,
			byte rawTypeKind) => metadataReader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

		private static string QualifyGenericArgument(string type) => type switch
		{
			"Boolean" or "Byte" or "Char" or "Double" or "Int16" or "Int32" or "Int64" or "SByte" or "Single" or "UInt16" or "UInt32" or "UInt64" => $"System.{type}",
			_ => type,
		};
	}
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

		Assert.Equal(2, Count(applicationSource, "AddHtmxor()"));
		Assert.Equal(2, Count(applicationSource, "AddHtmxorEndpoints()"));
		Assert.Equal(1, Count(applicationSource, "MapGroup(RoutePrefix)"));
		Assert.Equal(1, Count(applicationSource, "routes.MapRazorComponents<Issue97App>()"));
		Assert.Equal(1, Count(applicationSource, "app.MapRazorComponents<Issue97App>()"));
		Assert.DoesNotContain("AddHtmx()", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("AddHtmxorComponentEndpoints()", applicationSource, StringComparison.Ordinal);
		Assert.DoesNotContain("MapGroup(string.Empty)", applicationSource, StringComparison.Ordinal);
		Assert.Equal(3, Count(applicationSource, "HtmxRoute("));
		Assert.Equal(1, Count(razorSource, "HtmxRoute("));
		Assert.Equal(3, Count(razorSource, "@attribute [Authorize"));
		Assert.Equal(3, Count(razorSource, "hx-put="));
		Assert.Equal(2, Count(razorSource, "hx-patch="));
		Assert.Equal(2, Count(razorSource, "hx-delete="));
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
