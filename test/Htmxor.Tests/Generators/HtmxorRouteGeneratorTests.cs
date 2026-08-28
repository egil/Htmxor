using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorRouteGeneratorTests
{
	private const string RootImports = "@using Microsoft.AspNetCore.Authorization";
	private const string SupportedComponent = """
		@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}", Methods = new[] { "GET" })]
		@attribute [Authorize(Policy = "issue-91-policy")]

		<section>Report</section>
		""";
	private const string UnsupportedMethodComponent = """
		@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}", Methods = [ "POST" ])]
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
		@using Microsoft.AspNetCore.Authorization
		@attribute [Authorize(Policy = "issue-97-summary-policy")]
		@attribute [Htmxor.HtmxRoute("/summaries/{SummaryId:int}", Methods = [ "GET" ])]

		<section>Summary</section>
		""";
	private const string SecondUnsupportedMethodComponent = """
		@attribute [Htmxor.HtmxRoute("/summaries/{SummaryId:int}", Methods = [ "POST" ])]
		@attribute [Authorize(Policy = "issue-97-summary-policy")]

		<section>Summary</section>
		""";
	private const string SemanticallyEquivalentSupportedComponent = """
		<section>Report</section>

		@attribute [
			AuthorizeAlias("ignored-policy", Policy = ReportPolicy),
			HtmxRouteAlias(
				ReportRoute,
				Methods = new string[] { Get })
		]
		@using HtmxRouteAlias = Htmxor.HtmxRouteAttribute
		@using AuthorizeAlias = Microsoft.AspNetCore.Authorization.AuthorizeAttribute
		@using static Htmxor.AspNetCore10.RouteConstants
		""";
	private const string LookalikeAuthorizationComponent = """
		@using Authorize = Lookalikes.AuthorizeAttribute
		@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}", Methods = [ "GET" ])]
		@attribute [Authorize(Policy = "issue-91-policy")]

		<section>Report</section>
		""";
	private const string LookalikeRouteComponent = """
		@using HtmxRoute = Lookalikes.HtmxRouteAttribute
		@attribute [HtmxRoute("/reports/{ReportId:int}", Methods = [ "GET" ])]
		@attribute [Authorize(Policy = "issue-91-policy")]

		<section>Report</section>
		""";
	private const string CommentedRouteComponent = """
		@*
		@attribute [Htmxor.HtmxRoute("/ignored/{ReportId:int}", Methods = [ "GET" ])]
		@attribute [Authorize(Policy = "ignored-policy")]
		*@

		<section>Unrouted</section>
		""";
	private const string NestedAliasComponent = """
		@attribute [NestedRoute("/nested/{ReportId:int}", Methods = [ "GET" ])]
		@attribute [Authorize(Policy = "nested-policy")]

		<section>Nested</section>
		""";

	private const string RuntimeStubs = """
		using System;

		namespace Htmxor
		{
			[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
			public sealed class HtmxRouteAttribute : Attribute
			{
				public HtmxRouteAttribute(string template) => Template = template;

				public string Template { get; }

				public string[] Methods { get; init; } = Array.Empty<string>();
			}
		}

		namespace Microsoft.AspNetCore.Authorization
		{
			[AttributeUsage(
				AttributeTargets.Class | AttributeTargets.Method,
				AllowMultiple = true,
				Inherited = true)]
			public sealed class AuthorizeAttribute : Attribute
			{
				public AuthorizeAttribute()
				{
				}

				public AuthorizeAttribute(string policy) => Policy = policy;

				public string? Policy { get; set; }
			}
		}

		namespace Lookalikes
		{
			[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
			public sealed class HtmxRouteAttribute : Attribute
			{
				public HtmxRouteAttribute(string template) => Template = template;

				public string Template { get; }

				public string[] Methods { get; init; } = Array.Empty<string>();
			}

			[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
			public sealed class AuthorizeAttribute : Attribute
			{
				public string? Policy { get; set; }
			}
		}

		namespace Htmxor.AspNetCore10
		{
			internal sealed class Issue91HtmxOnlyComponent;
			internal sealed class Issue97SummaryComponent;

			internal static class RouteConstants
			{
				internal const string ReportRoute = "/reports/{ReportId:int}";
				internal const string ReportPolicy = "issue-91-policy";
				internal const string Get = "GET";
			}
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
		Assert.Equal(2, Count(generatedSource, "MapHtmxorGeneratedComponentEndpoint("));
		AssertRegistrationTuple(
			generatedSource,
			"Issue91HtmxOnlyComponent",
			"/reports/{ReportId:int}",
			"issue-91-policy");
		AssertRegistrationTuple(
			generatedSource,
			"Issue97SummaryComponent",
			"/summaries/{SummaryId:int}",
			"issue-97-summary-policy");
		Assert.Empty(forward.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Fact]
	public void Equivalent_compiler_resolved_attribute_syntax_emits_the_same_registration_tuple()
	{
		var run = RunGenerator(SemanticallyEquivalentSupportedComponent);

		Assert.Empty(run.DriverDiagnostics);
		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.Diagnostics);
		var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();
		Assert.Equal(1, Count(generatedSource, "MapHtmxorGeneratedComponentEndpoint("));
		AssertRegistrationTuple(
			generatedSource,
			"Issue91HtmxOnlyComponent",
			"/reports/{ReportId:int}",
			"issue-91-policy");
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Fact]
	public void Lookalike_authorization_attribute_fails_closed_with_one_deterministic_diagnostic()
	{
		var run = RunGenerator(LookalikeAuthorizationComponent);
		var repeatedRun = RunGenerator(LookalikeAuthorizationComponent);

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		var repeatedResult = Assert.Single(repeatedRun.RunResult.Results);
		Assert.Empty(repeatedResult.GeneratedSources);
		var repeatedDiagnostic = Assert.Single(repeatedResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal("Issue91HtmxOnlyComponent.razor", Path.GetFileName(diagnostic.Location.GetLineSpan().Path));
		Assert.Contains("Authorize", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(diagnostic.GetMessage(), repeatedDiagnostic.GetMessage());
		Assert.Equal(diagnostic.Location.GetLineSpan(), repeatedDiagnostic.Location.GetLineSpan());
	}

	[Fact]
	public void Lookalike_route_attribute_fails_closed_with_one_deterministic_diagnostic()
	{
		var run = RunGenerator(LookalikeRouteComponent);
		var repeatedRun = RunGenerator(LookalikeRouteComponent);

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		var repeatedResult = Assert.Single(repeatedRun.RunResult.Results);
		Assert.Empty(repeatedResult.GeneratedSources);
		var repeatedDiagnostic = Assert.Single(repeatedResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(diagnostic.GetMessage(), repeatedDiagnostic.GetMessage());
		Assert.Equal(diagnostic.Location.GetLineSpan(), repeatedDiagnostic.Location.GetLineSpan());
	}

	[Fact]
	public void Razor_commented_route_declaration_is_ignored()
	{
		var run = RunGenerator(CommentedRouteComponent);

		Assert.Empty(run.DriverDiagnostics);
		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.Diagnostics);
		Assert.Empty(generatorResult.GeneratedSources);
	}

	[Fact]
	public void Root_import_alias_does_not_create_a_route_for_an_unrelated_component()
	{
		var run = RunGeneratorWithImports(
			RootImports + Environment.NewLine +
			"@using RouteAlias = Htmxor.HtmxRouteAttribute",
			("Issue91HtmxOnlyComponent.razor", "<section>Unrouted</section>"));

		Assert.Empty(run.DriverDiagnostics);
		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.Diagnostics);
		Assert.Empty(generatorResult.GeneratedSources);
	}

	[Fact]
	public void Root_import_aliases_bind_the_supported_route_and_policy()
	{
		var run = RunGeneratorWithImports(
			RootImports + Environment.NewLine +
			"@using RouteAlias = Htmxor.HtmxRouteAttribute" + Environment.NewLine +
			"@using AuthorizeAlias = Microsoft.AspNetCore.Authorization.AuthorizeAttribute" + Environment.NewLine +
			"@using static Htmxor.AspNetCore10.RouteConstants",
			("Issue91HtmxOnlyComponent.razor", """
				<section>Report</section>

				@attribute [RouteAlias(ReportRoute, Methods = [ Get ])]
				@attribute [AuthorizeAlias(ReportPolicy)]
				"""));

		Assert.Empty(run.DriverDiagnostics);
		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.Diagnostics);
		var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();
		AssertRegistrationTuple(
			generatedSource,
			"Issue91HtmxOnlyComponent",
			"/reports/{ReportId:int}",
			"issue-91-policy");
	}

	[Fact]
	public void Nested_import_alias_route_fails_closed_at_the_component_declaration()
	{
		var run = RunGenerator(
			("Nested/_Imports.razor", "@using NestedRoute = Htmxor.HtmxRouteAttribute"),
			("Nested/NestedComponent.razor", NestedAliasComponent));

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal("NestedComponent.razor", Path.GetFileName(diagnostic.Location.GetLineSpan().Path));
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
		Assert.Contains("one Authorize policy", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public void Supported_and_unsupported_declarations_fail_closed_with_one_diagnostic()
	{
		var run = RunGenerator(
			("Issue91HtmxOnlyComponent.razor", SupportedComponent),
			("Issue97SummaryComponent.razor", SecondUnsupportedMethodComponent));
		var reverse = RunGenerator(
			("Issue97SummaryComponent.razor", SecondUnsupportedMethodComponent),
			("Issue91HtmxOnlyComponent.razor", SupportedComponent));

		var generatorResult = Assert.Single(run.RunResult.Results);
		Assert.Empty(generatorResult.GeneratedSources);
		var diagnostic = Assert.Single(generatorResult.Diagnostics);
		var reverseResult = Assert.Single(reverse.RunResult.Results);
		Assert.Empty(reverseResult.GeneratedSources);
		var reverseDiagnostic = Assert.Single(reverseResult.Diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal("Issue97SummaryComponent.razor", Path.GetFileName(diagnostic.Location.GetLineSpan().Path));
		Assert.Contains("explicit GET", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(diagnostic.GetMessage(), reverseDiagnostic.GetMessage());
		Assert.Equal(diagnostic.Location.GetLineSpan(), reverseDiagnostic.Location.GetLineSpan());
	}

	private static GeneratorRun RunGenerator(string componentSource)
		=> RunGenerator(("Issue91HtmxOnlyComponent.razor", componentSource));

	private static void AssertRegistrationTuple(
		string generatedSource,
		string componentName,
		string route,
		string policy)
	{
		var tuple =
			"MapHtmxorGeneratedComponentEndpoint(\n" +
			"\t\t\tendpoints,\n" +
			$"\t\t\ttypeof(global::Htmxor.AspNetCore10.{componentName}),\n" +
			$"\t\t\t\"{route}\",\n" +
			$"\t\t\t\"{policy}\");";

		Assert.Contains(tuple, generatedSource, StringComparison.Ordinal);
	}

	private static int Count(string source, string value)
		=> source.Split(value, StringSplitOptions.None).Length - 1;

	private static GeneratorRun RunGenerator(params (string FileName, string Source)[] components)
		=> RunGeneratorWithImports(RootImports, components);

	private static GeneratorRun RunGeneratorWithImports(
		string rootImports,
		params (string FileName, string Source)[] components)
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
				.Append((FileName: "_Imports.razor", Source: rootImports))
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
