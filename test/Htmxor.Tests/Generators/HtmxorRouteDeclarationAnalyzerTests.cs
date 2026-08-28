using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorRouteDeclarationAnalyzerTests
{
	private const string RootNamespace = "Htmxor.Consumer";
	private static readonly string ProjectDirectory = Path.GetFullPath(
		Path.Combine(Path.GetTempPath(), "htmxor-analyzer-tests"));
	private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

	[Fact]
	public async Task Unsupported_second_declaration_reports_one_mapped_nonconfigurable_diagnostic()
	{
		var reportPath = ComponentPath("ReportComponent.razor");
		var summaryPath = ComponentPath("SummaryComponent.razor");
		var report = ComponentSource(
			"ReportComponent",
			reportPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/reports/{ReportId:int}\", Methods = [\"GET\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"reports.read\")]");
		var summary = ComponentSource(
			"SummaryComponent",
			summaryPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/summaries/{SummaryId:int}\", Methods = [\"POST\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"summaries.read\")]");

		var diagnostics = await RunAnalyzerAsync(
			new[] { report, summary },
			new[] { reportPath, summaryPath });
		var reversedDiagnostics = await RunAnalyzerAsync(
			new[] { summary, report },
			new[] { summaryPath, reportPath });

		var diagnostic = Assert.Single(diagnostics);
		var reversedDiagnostic = Assert.Single(reversedDiagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains("GET", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(summaryPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(diagnostic.GetMessage(), reversedDiagnostic.GetMessage());
		Assert.Equal(
			diagnostic.Location.GetMappedLineSpan().Path,
			reversedDiagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Compiler_bound_aliases_constants_and_array_forms_are_supported()
	{
		var reportPath = ComponentPath("ReportComponent.razor");
		var summaryPath = ComponentPath("SummaryComponent.razor");
		var report = BoundComponentSource(
			"ReportComponent",
			reportPath,
			"/reports/{ReportId:int}",
			"reports.read",
			"new[] { GetMethod }",
			usePolicyConstructor: false);
		var summary = BoundComponentSource(
			"SummaryComponent",
			summaryPath,
			"/summaries/{SummaryId:int}",
			"summaries.read",
			"[GetMethod]",
			usePolicyConstructor: true);

		var diagnostics = await RunAnalyzerAsync(
			new[] { summary, report },
			new[] { summaryPath, reportPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Custom_authorization_metadata_alongside_standard_policy_fails_closed()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace CustomSecurity
			{
			public sealed class ExtraAuthorizationAttribute :
				global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
			}

			namespace {{RootNamespace}}
			{
			#line 30 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("items.read")]
			[global::CustomSecurity.ExtraAuthorizationAttribute]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("exactly one effective authorization", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Custom_anonymous_metadata_fails_closed()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace CustomSecurity
			{
			[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = true)]
			public sealed class ExtraAnonymousAttribute :
				global::System.Attribute,
				global::Microsoft.AspNetCore.Authorization.IAllowAnonymous;
			}

			namespace {{RootNamespace}}
			{
			#line 30 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("items.read")]
			[global::CustomSecurity.ExtraAnonymousAttribute]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("anonymous", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"POST\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"GET only")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\")]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"explicitly declare Methods")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]\n[global::Htmxor.HtmxRouteAttribute(\"/other/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"exactly one HtmxRoute")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"], Target = \"result\")]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"Target")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\", Roles = \"admin\")]",
		"Roles")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\", AuthenticationSchemes = \"scheme\")]",
		"AuthenticationSchemes")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id=foo:bar}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}/{broken\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]\n[global::Microsoft.AspNetCore.Components.RouteAttribute(\"/normal/{Id:int}\")]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"normal Blazor route")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]\n[global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]",
		"anonymous")]
	public async Task Unsupported_bound_metadata_fails_closed(
		string routeAttribute,
		string authorizationAttribute,
		string expectedReason)
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			routeAttribute,
			authorizationAttribute);

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains(expectedReason, diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Route_target_that_is_not_a_component_fails_closed()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
			"global::System.Object");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("Blazor component", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Exact_route_outside_project_root_fails_closed_while_lookalike_is_ignored()
	{
		var nestedPath = ComponentPath(Path.Combine("Nested", "NestedComponent.razor"));
		var lookalikePath = ComponentPath("LookalikeComponent.razor");
		var nested = $$"""
			namespace {{RootNamespace}}.Nested
			{
			#line 12 "{{EscapePath(nestedPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/nested/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("nested.read")]
			#line default
			public sealed class NestedComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";
		var lookalike = $$"""
			namespace Lookalike
			{
			[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
			public sealed class HtmxRouteAttribute(string template) : global::System.Attribute;
			}

			namespace {{RootNamespace}}
			{
			#line 40 "{{EscapePath(lookalikePath)}}"
			[global::Lookalike.HtmxRouteAttribute("/lookalike/{Id:int}")]
			#line default
			public sealed class LookalikeComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { lookalike, nested },
			new[] { lookalikePath, nestedPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("project-root", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(nestedPath, diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task More_than_two_supported_components_reports_every_declaration()
	{
		var paths = new[]
		{
			ComponentPath("AlphaComponent.razor"),
			ComponentPath("BetaComponent.razor"),
			ComponentPath("GammaComponent.razor"),
		};
		var sources = paths.Select((path, index) => ComponentSource(
			Path.GetFileNameWithoutExtension(path),
			path,
			$"[global::Htmxor.HtmxRouteAttribute(\"/items/{{Id{index}:int}}\", Methods = [\"GET\"])]",
			$"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.{index}.read\")]"));

		var diagnostics = await RunAnalyzerAsync(sources, paths);

		Assert.Equal(3, diagnostics.Length);
		Assert.All(diagnostics, diagnostic =>
			Assert.Contains("at most two", diagnostic.GetMessage(), StringComparison.Ordinal));
		Assert.Equal(paths, diagnostics.Select(diagnostic => diagnostic.Location.GetMappedLineSpan().Path));
	}

	private static string ComponentPath(string relativePath)
		=> Path.Combine(ProjectDirectory, relativePath);

	private static string ComponentSource(
		string componentName,
		string mappedPath,
		string routeAttribute,
		string authorizationAttribute,
		string baseType = "global::Microsoft.AspNetCore.Components.ComponentBase")
		=> $$"""
			namespace {{RootNamespace}}
			{
			#line 20 "{{EscapePath(mappedPath)}}"
			{{routeAttribute}}
			{{authorizationAttribute}}
			#line default
			public sealed class {{componentName}} : {{baseType}};
			}
			""";

	private static string BoundComponentSource(
		string componentName,
		string mappedPath,
		string route,
		string policy,
		string methodsExpression,
		bool usePolicyConstructor)
	{
		var authorization = usePolicyConstructor
			? "[AuthAlias(\"constructor.policy\", Policy = RequiredPolicy)]"
			: "[AuthAlias(Policy = RequiredPolicy)]";
		return $$"""
			using RouteAlias = global::Htmxor.HtmxRouteAttribute;
			using AuthAlias = global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

			namespace {{RootNamespace}}
			{
			#line 73 "{{EscapePath(mappedPath)}}"
			[RouteAlias(RouteTemplate, Methods = {{methodsExpression}})]
			{{authorization}}
			#line default
			public sealed class {{componentName}} : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private const string RouteTemplate = "{{route}}";
				private const string GetMethod = "GET";
				private const string RequiredPolicy = "{{policy}}";
			}
			}
			""";
	}

	private static string EscapePath(string path) => path.Replace("\"", "\\\"");

	private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
		IEnumerable<string> sources,
		IEnumerable<string> razorPaths)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var trees = sources
			.Select((source, index) => CSharpSyntaxTree.ParseText(
				source,
				parseOptions,
				$"Component{index}.razor.g.cs"))
			.ToImmutableArray();
		var compilation = CSharpCompilation.Create(
			"Htmxor.Analyzer.Tests",
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var additionalFiles = razorPaths
			.Select(static path => new ThrowingAdditionalText(path))
			.ToImmutableArray<AdditionalText>();
		var analyzerOptions = new AnalyzerOptions(
			additionalFiles,
			new TestAnalyzerConfigOptionsProvider(ProjectDirectory));

		var diagnostics = await compilation
			.WithAnalyzers(
				ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer()),
				analyzerOptions)
			.GetAnalyzerDiagnosticsAsync();

		return diagnostics
			.OrderBy(diagnostic => diagnostic.Location.GetMappedLineSpan().Path, StringComparer.Ordinal)
			.ThenBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
			.ToImmutableArray();
	}

	private static ImmutableArray<MetadataReference> CreateReferences()
	{
		var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
			.Split(Path.PathSeparator) ?? Array.Empty<string>();
		var requiredAssemblies = new[]
		{
			typeof(object).Assembly.Location,
			typeof(ComponentBase).Assembly.Location,
			typeof(HtmxRouteAttribute).Assembly.Location,
			typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location,
		};

		return platformAssemblies
			.Concat(requiredAssemblies)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();
	}

	private sealed class ThrowingAdditionalText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("The analyzer must not read Razor content.");
	}

	private sealed class TestAnalyzerConfigOptionsProvider(string projectDirectory)
		: AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal));

		public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["build_property.RootNamespace"] = RootNamespace,
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
