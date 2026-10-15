using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorRouteDeclarationAnalyzerTests
{
	private const string NestedRazorRouteAttribute = "[global::Htmxor.HtmxRoute(\"/nested\", Methods = new[] { \"GET\" })]";

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Nested_Razor_route_reports_one_local_top_level_origin_fault(bool local)
	{
		var source = ReadRouteFixture("NestedOwner.generated");
		var tree = ParseRouteFixture(source, "NestedOwner");

		var diagnostics = await AnalyzeRouteTreesAsync([tree], RouteAdditionalFiles("NestedOwner"), local);

		AssertRouteReasons(diagnostics, "HTMXOR004", "the HtmxRoute component must be a top-level class");
		var diagnostic = Assert.Single(diagnostics);
		AssertLocalDiagnostic(diagnostic);
		Assert.Equal(ComponentPath("NestedOwner.razor"), diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Equal(3, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
		Assert.Equal(5, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Character);
		Assert.Equal(NestedRazorRouteAttribute[1..^1], tree.GetText().ToString(diagnostic.Location.SourceSpan));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Nested_Razor_fixture_with_only_the_outer_route_is_valid(bool local)
	{
		var source = ReadRouteFixture("NestedOwner.generated").Replace(NestedRazorRouteAttribute, "", StringComparison.Ordinal);
		var razor = ReadRouteFixture("NestedOwner.razor").Replace(NestedRazorRouteAttribute, "", StringComparison.Ordinal);
		var tree = ParseRouteFixture(source, "NestedOwner");
		var files = ImmutableArray.Create<AdditionalText>(new SourceAdditionalText(ComponentPath("NestedOwner.razor"), razor));

		var diagnostics = await AnalyzeRouteTreesAsync([tree], files, local);

		Assert.Empty(diagnostics);
	}

	[Fact]
	public void Nested_Razor_route_fault_leaves_the_outer_route_manifest_unchanged()
	{
		var source = ReadRouteFixture("NestedOwner.generated");
		var razor = ReadRouteFixture("NestedOwner.razor");
		var outerOnlySource = source.Replace(NestedRazorRouteAttribute, "", StringComparison.Ordinal);
		var outerOnlyRazor = razor.Replace(NestedRazorRouteAttribute, "", StringComparison.Ordinal);

		var manifest = GenerateNestedRazorManifest(source, razor);
		var outerOnlyManifest = GenerateNestedRazorManifest(outerOnlySource, outerOnlyRazor);

		Assert.Equal(outerOnlyManifest, manifest);
		Assert.Contains("\"Htmxor.Consumer.NestedOwner\"", manifest, StringComparison.Ordinal);
		Assert.DoesNotContain("NestedOwner+Nested", manifest, StringComparison.Ordinal);
	}

	private static string GenerateNestedRazorManifest(string source, string razor)
	{
		var tree = ParseRouteFixture(source, "NestedOwner");
		var compilation = CSharpCompilation.Create("NestedRazorManifestTests", [tree], References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[new HtmxorRouteGenerator().AsSourceGenerator()],
			[new SourceAdditionalText(ComponentPath("NestedOwner.razor"), razor)],
			(CSharpParseOptions)tree.Options,
			new TestAnalyzerConfigOptionsProvider(ProjectDirectory));
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
		Assert.Empty(diagnostics);
		Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		return Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString();
	}
}
