using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorRouteDeclarationAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Real_Razor_route_faults_remain_available_at_the_authored_mapped_location(bool local)
	{
		var source = ReadRouteFixture("RazorOwner.generated").Replace("Methods = new[] { \"GET\" }", "Methods = [], CurrentUrl = \"ftp://example.test/items\", Target = \"section#\"", StringComparison.Ordinal);
		var tree = ParseRouteFixture(source, "RazorOwner");
		var files = RouteAdditionalFiles("RazorOwner");

		var diagnostics = await AnalyzeRouteTreesAsync([tree], files, local);

		AssertRouteReasons(diagnostics, "HTMXOR005", MethodsReason, CurrentUrlReason, TargetReason);
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Equal(ComponentPath("RazorOwner.razor"), diagnostic.Location.GetMappedLineSpan().Path);
			Assert.Equal(0, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
			Assert.Equal(12, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Character);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CSharp_partial_origin_is_decidable_from_the_real_compiled_Razor_symbol(bool local)
	{
		var attribute = CSharpSyntaxTree.ParseText("namespace Htmxor.Consumer; [global::Htmxor.HtmxRoute(\"/items\", Methods = [\"GET\"])] public partial class CodeBehindOwner;",
			CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), ComponentPath("Other.cs"));
		var razor = ParseRouteFixture(ReadRouteFixture("CodeBehindOwner.generated"), "CodeBehindOwner");

		var diagnostics = await AnalyzeRouteTreesAsync([attribute, razor], RouteAdditionalFiles("CodeBehindOwner"), local);

		AssertRouteReasons(diagnostics, "HTMXOR004", "a C# HtmxRoute declaration on a Razor component must use the matching .razor.cs partial");
		Assert.Equal(ComponentPath("Other.cs"), Assert.Single(diagnostics).Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Missing_Razor_manifest_is_reported_only_by_complete_analysis()
	{
		var tree = ParseRouteFixture(ReadRouteFixture("RazorOwner.generated"), "RazorOwner");

		var local = await AnalyzeRouteTreesAsync([tree], [], local: true);
		var complete = await AnalyzeRouteTreesAsync([tree], [], local: false);

		Assert.Empty(local);
		AssertRouteReasons(complete, "HTMXOR004", "the HtmxRoute component must compile from the matching Razor component; its type name is derived from the file's folder and @namespace");
		Assert.Contains(WellKnownDiagnosticTags.CompilationEnd, Assert.Single(complete).Descriptor.CustomTags);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Imports_route_origin_is_local_even_on_real_generated_Razor(bool local)
	{
		var source = ReadRouteFixture("RazorOwner.generated").Replace(ComponentPath("RazorOwner.razor"), ComponentPath("_Imports.razor"), StringComparison.Ordinal);
		var tree = ParseRouteFixture(source, "RazorOwner");

		var diagnostics = await AnalyzeRouteTreesAsync([tree], RouteAdditionalFiles("RazorOwner"), local);

		AssertRouteReasons(diagnostics, "HTMXOR004", "HtmxRoute declarations from _Imports.razor are not supported; move HtmxRoute onto the route-owning component");
		Assert.Equal(ComponentPath("_Imports.razor"), Assert.Single(diagnostics).Location.GetMappedLineSpan().Path);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Real_Razor_and_matching_code_behind_declarations_remain_valid(bool codeBehind)
	{
		var owner = codeBehind ? "CodeBehindOwner" : "RazorOwner";
		var trees = new List<SyntaxTree> { ParseRouteFixture(ReadRouteFixture(owner + ".generated"), owner) };
		if (codeBehind)
		{
			trees.Add(CSharpSyntaxTree.ParseText("namespace Htmxor.Consumer; [global::Htmxor.HtmxRoute(\"/items\", Methods = [\"GET\"])] public partial class CodeBehindOwner;",
				CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), ComponentPath(owner + ".razor.cs")));
		}

		var diagnostics = await AnalyzeRouteTreesAsync(trees.ToArray(), RouteAdditionalFiles(owner), local: false);

		Assert.Empty(diagnostics);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task Compiler_owned_invalid_member_gets_no_Htmxor_duplicate(bool local, bool invalidTarget)
	{
		var target = invalidTarget ? ", Target = \"section#\"" : "";
		var tree = CSharpSyntaxTree.ParseText("[global::Htmxor.HtmxRoute(\"/items\", Methods = \"GET\"" + target + ")] public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;",
			CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), ComponentPath("Item.cs"));
		var compilation = CSharpCompilation.Create("MalformedRouteTests", [tree], References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Contains(compilation.GetDiagnostics(), diagnostic => diagnostic.Id == "CS0029");
		var driver = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer()));

		var diagnostics = local
			? await driver.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(tree), null, CancellationToken.None)
			: await driver.GetAnalyzerDiagnosticsAsync();

		if (invalidTarget)
		{
			AssertRouteReasons(diagnostics, "HTMXOR005", TargetReason);
		}
		else
		{
			Assert.Empty(diagnostics);
		}
	}

	private static string ReadRouteFixture(string name)
	{
		using var stream = typeof(HtmxorRouteDeclarationAnalyzerTests).Assembly.GetManifestResourceStream("Htmxor.Generators.RouteFixtures." + name + ".fixture")!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd().Replace("/tmp/issue331-razor-capture10/", ProjectDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal);
	}

	private static SyntaxTree ParseRouteFixture(string source, string owner)
		=> CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), RazorGeneratedPath(owner));

	private static ImmutableArray<AdditionalText> RouteAdditionalFiles(string owner)
		=> [new SourceAdditionalText(ComponentPath(owner + ".razor"), ReadRouteFixture(owner + ".razor"))];
}
