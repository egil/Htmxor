using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorRouteGeneratorTests
{
	private const string RuntimeStubs = """
		using System.Reflection;
		using System.Collections.Generic;

		namespace Htmxor.Builder
		{
			internal sealed class HtmxorGeneratedComponentAction;
		}

		namespace Microsoft.AspNetCore.Routing
		{
			internal sealed class RouteGroupBuilder;
		}

		namespace Microsoft.AspNetCore.Builder
		{
			internal sealed class RazorComponentsEndpointConventionBuilder;

			internal static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				internal static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
					this RazorComponentsEndpointConventionBuilder builder,
					Routing.RouteGroupBuilder endpoints,
					Assembly applicationAssembly,
					IReadOnlyList<string> projectRootComponentTypeNames,
					IReadOnlyList<Htmxor.Builder.HtmxorGeneratedComponentAction> generatedActions)
					=> builder;
			}
		}
		""";

	[Fact]
	public void Project_root_paths_emit_one_sorted_runtime_manifest_without_reading_Razor_content()
	{
		var forward = RunGenerator(
			"ZetaComponent.razor",
			"Nested/NestedComponent.razor",
			"_Imports.razor",
			"AlphaComponent.razor");
		var reverse = RunGenerator(
			"AlphaComponent.razor",
			"_Imports.razor",
			"Nested/NestedComponent.razor",
			"ZetaComponent.razor");

		Assert.Empty(forward.DriverDiagnostics);
		var result = Assert.Single(forward.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();
		var reverseSource = Assert.Single(
			Assert.Single(reverse.RunResult.Results).GeneratedSources).SourceText.ToString();

		Assert.Equal(generatedSource, reverseSource);
		Assert.Equal(1, Count(generatedSource, "AddHtmxorAttributedComponentEndpoints("));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.AlphaComponent\""));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.ZetaComponent\""));
		AssertInOrder(
			generatedSource,
			"\"Htmxor.Consumer.AlphaComponent\"",
			"\"Htmxor.Consumer.ZetaComponent\"");
		Assert.Contains(
			"typeof(HtmxorGeneratedRouteRegistrationExtensions).Assembly",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains("ProjectRootComponentTypeNames", generatedSource, StringComparison.Ordinal);
		Assert.Contains("AddGeneratedActions(generatedActions)", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("NestedComponent", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("_Imports", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("HtmxRoute", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("Authorize", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("policy", generatedSource, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("MapHtmxorGeneratedComponentEndpoint", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("typeof(global::Htmxor.Consumer.AlphaComponent)", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("typeof(global::Htmxor.Consumer.ZetaComponent)", generatedSource, StringComparison.Ordinal);
		Assert.Empty(forward.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	private static void AssertInOrder(string source, string first, string second)
	{
		var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
		var secondIndex = source.IndexOf(second, StringComparison.Ordinal);

		Assert.True(firstIndex >= 0 && firstIndex < secondIndex, source);
	}

	private static int Count(string source, string value)
		=> source.Split(value, StringSplitOptions.None).Length - 1;

	private static GeneratorRun RunGenerator(params string[] relativePaths)
	{
		var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-generator-probe"));
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.Consumer.Tests",
			new[] { CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new HtmxorRouteGenerator().AsSourceGenerator() },
			relativePaths
				.Select(relativePath => new ThrowingAdditionalText(
					Path.Combine(projectDirectory, relativePath)))
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

	private sealed class ThrowingAdditionalText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("The path-only generator must not read Razor content.");
	}

	private sealed class TestAnalyzerConfigOptionsProvider(string projectDirectory)
		: AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal));

		public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["build_property.RootNamespace"] = "Htmxor.Consumer",
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
