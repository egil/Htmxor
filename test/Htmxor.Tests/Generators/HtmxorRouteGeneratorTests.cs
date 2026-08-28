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
	private const string SecondSupportedComponent = """
		@attribute [Htmxor.HtmxRoute("/summaries/{SummaryId:int}", Methods = new[] { "GET" })]
		@attribute [Authorize(Policy = "issue-97-summary-policy")]

		<section>Summary</section>
		""";
	private const string SecondUnsupportedMethodComponent = """
		@attribute [Htmxor.HtmxRoute("/summaries/{SummaryId:int}", Methods = new[] { "POST" })]
		@attribute [Authorize(Policy = "issue-97-summary-policy")]

		<section>Summary</section>
		""";

	private const string RuntimeStubs = """
		using System;

		namespace Htmxor.AspNetCore10
		{
			internal sealed class Issue91HtmxOnlyComponent;
			internal sealed class Issue97SummaryComponent;
		}

		namespace Microsoft.AspNetCore.Routing
		{
			internal interface IEndpointRouteBuilder;
			internal sealed class RouteGroupBuilder : IEndpointRouteBuilder;
		}

		namespace Microsoft.AspNetCore.Builder
		{
			internal sealed class RazorComponentsEndpointConventionBuilder;
			internal interface IEndpointConventionBuilder;

			internal static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				internal static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
					this RazorComponentsEndpointConventionBuilder builder,
					Routing.IEndpointRouteBuilder endpoints)
					=> builder;

				internal static IEndpointConventionBuilder MapHtmxorGeneratedComponentEndpoint(
					Routing.IEndpointRouteBuilder endpoints,
					Type componentType,
					string normalizedRoute,
					string authorizationPolicy)
					=> null!;
			}
		}
		""";

	[Fact]
	public void Supported_declaration_emits_compiling_application_registration_overload()
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
			"\"/reports/{ReportId:int}\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"\"issue-91-policy\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"MapHtmxorGeneratedComponentEndpoint(",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Fact]
	public void Two_supported_declarations_emit_one_deterministic_compiling_registration()
	{
		var forward = RunGenerator(
			("Issue91HtmxOnlyComponent.razor", SupportedComponent),
			("Issue97SummaryComponent.razor", SecondSupportedComponent));
		var reverse = RunGenerator(
			("Issue97SummaryComponent.razor", SecondSupportedComponent),
			("Issue91HtmxOnlyComponent.razor", SupportedComponent));

		Assert.Empty(forward.DriverDiagnostics);
		var generatorResult = Assert.Single(forward.RunResult.Results);
		Assert.Empty(generatorResult.Diagnostics);
		var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();
		Assert.Equal(
			generatedSource,
			Assert.Single(Assert.Single(reverse.RunResult.Results).GeneratedSources).SourceText.ToString());
		Assert.Contains(
			"typeof(global::Htmxor.AspNetCore10.Issue91HtmxOnlyComponent)",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"typeof(global::Htmxor.AspNetCore10.Issue97SummaryComponent)",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains("\"/reports/{ReportId:int}\"", generatedSource, StringComparison.Ordinal);
		Assert.Contains("\"/summaries/{SummaryId:int}\"", generatedSource, StringComparison.Ordinal);
		Assert.Contains("\"issue-91-policy\"", generatedSource, StringComparison.Ordinal);
		Assert.Contains("\"issue-97-summary-policy\"", generatedSource, StringComparison.Ordinal);
		Assert.Empty(forward.OutputCompilation.GetDiagnostics().Where(
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

	[Fact]
	public void Supported_and_unsupported_declarations_fail_closed_with_one_diagnostic()
	{
		var run = RunGenerator(
			("Issue91HtmxOnlyComponent.razor", SupportedComponent),
			("Issue97SummaryComponent.razor", SecondUnsupportedMethodComponent));

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal("Issue97SummaryComponent.razor", Path.GetFileName(diagnostic.Location.GetLineSpan().Path));
		Assert.Contains("explicit GET", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	private static GeneratorRun RunGenerator(string componentSource)
		=> RunGenerator(("Issue91HtmxOnlyComponent.razor", componentSource));

	private static GeneratorRun RunGenerator(params (string FileName, string Source)[] components)
	{
		var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-generator-probe"));
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.AspNetCore10.Tests",
			new[] { CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new HtmxorRouteGenerator().AsSourceGenerator() },
			components
				.Select(component => new TestAdditionalText(
					Path.Combine(projectDirectory, component.FileName),
					component.Source))
				.ToArray<AdditionalText>(),
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
