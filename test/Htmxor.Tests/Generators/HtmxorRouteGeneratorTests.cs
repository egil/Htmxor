using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorRouteGeneratorTests
{
	private const string SupportedComponent = """
		@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}", Methods = new[] { "GET" })]
		@attribute [Authorize(Policy = "issue-91-policy")]

		<section>Report</section>
		""";
	private const string UnsupportedMethodComponent = """
		@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}", Methods = new[] { "POST" })]
		@attribute [Authorize(Policy = "issue-91-policy")]

		<section>Report</section>
		""";
	private const string MultipleAuthorizationPoliciesComponent = """
		@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}", Methods = new[] { "GET" })]
		@attribute [Authorize(Policy = "issue-91-policy")]
		@attribute [Authorize(Policy = "second-policy")]

		<section>Report</section>
		""";

	private const string RuntimeStubs = """
		using System;
		using System.Collections.Generic;

		namespace Htmxor.AspNetCore10
		{
			internal sealed class Issue91HtmxOnlyComponent;
		}

		namespace Htmxor
		{
			internal sealed class HtmxRouteAttribute(string template) : Attribute
			{
				public string Template { get; } = template;
				public string[] Methods { get; set; } = Array.Empty<string>();
			}
		}

		namespace Htmxor.Builder
		{
			internal sealed class HtmxorComponentGetRouteDescriptor(
				Type componentType,
				string normalizedRoute,
				IReadOnlyList<object> metadata);
		}

		namespace Microsoft.AspNetCore.Authorization
		{
			internal sealed class AuthorizeAttribute(string policy) : Attribute;
		}

		namespace Microsoft.AspNetCore.Http
		{
			internal static class HttpMethods
			{
				public const string Get = "GET";
			}
		}

		namespace Microsoft.AspNetCore.Routing
		{
			internal interface IEndpointRouteBuilder;
		}

		namespace Microsoft.AspNetCore.Builder
		{
			internal interface IEndpointConventionBuilder;

			internal static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				internal static IEndpointConventionBuilder MapHtmxorComponentEndpoint(
					this Routing.IEndpointRouteBuilder endpoints,
					Htmxor.Builder.HtmxorComponentGetRouteDescriptor descriptor)
					=> null!;
			}
		}
		""";

	[Fact]
	public void Supported_declaration_emits_compiling_descriptor_and_group_registration()
	{
		var run = RunGenerator(SupportedComponent);

		Assert.Empty(run.DriverDiagnostics);
		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.Diagnostics);
		var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();
		Assert.Contains(
			"typeof(global::Htmxor.AspNetCore10.Issue91HtmxOnlyComponent)",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"public const string NormalizedRoute = \"/reports/{ReportId:int}\";",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"public const string PolicyName = \"issue-91-policy\";",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"MapHtmxorComponentEndpoint(endpoints, Descriptor)",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Fact]
	public void Non_get_declaration_reports_one_deterministic_diagnostic()
	{
		var run = RunGenerator(UnsupportedMethodComponent);

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal("Issue91HtmxOnlyComponent.razor", Path.GetFileName(diagnostic.Location.GetLineSpan().Path));
		Assert.Equal(0, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
		Assert.Contains("explicit GET", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public void Multiple_authorization_policies_report_one_deterministic_diagnostic()
	{
		var run = RunGenerator(MultipleAuthorizationPoliciesComponent);

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains("one literal Authorize policy", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	private static GeneratorRun RunGenerator(string componentSource)
	{
		var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-generator-probe"));
		var componentPath = Path.Combine(projectDirectory, "Issue91HtmxOnlyComponent.razor");
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.AspNetCore10.Tests",
			new[] { CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new HtmxorRouteGenerator().AsSourceGenerator() },
			new[] { new TestAdditionalText(componentPath, componentSource) },
			parseOptions,
			new TestAnalyzerConfigOptionsProvider(projectDirectory));

		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var driverDiagnostics);

		return new GeneratorRun(driver.GetRunResult(), outputCompilation, driverDiagnostics);
	}

	private sealed record GeneratorRun(
		GeneratorDriverRunResult RunResult,
		Compilation OutputCompilation,
		ImmutableArray<Diagnostic> DriverDiagnostics);

	private sealed class TestAdditionalText(string path, string source) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> SourceText.From(source);
	}

	private sealed class TestAnalyzerConfigOptionsProvider(string projectDirectory)
		: AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal));

		public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["build_property.RootNamespace"] = "Htmxor.AspNetCore10",
				["build_property.MSBuildProjectDirectory"] = projectDirectory,
			});

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;
	}

	private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
		: AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
			=> values.TryGetValue(key, out value!);
	}
}
