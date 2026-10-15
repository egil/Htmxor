using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorRouteDeclarationAnalyzerTests
{
	private const string MethodsReason = "explicit HtmxRoute Methods must resolve to a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY";
	private const string CurrentUrlReason = "HtmxRoute named argument 'CurrentUrl' must resolve to a valid relative or absolute HTTP(S) URI or null";
	private const string TargetReason = "HtmxRoute named argument 'Target' must resolve to a valid element identity or null or whitespace";
	private const string NormalRouteReason = "an HTMX-only component cannot also declare a normal Blazor route";
	private const string MarkerRouteReason = "DisableHtmxDirectRouting requires a local stock @page or Route declaration";
	private const string MarkerCombinationReason = "DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type";

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Independent_route_member_faults_each_report_their_reason_at_the_authored_attribute(bool local)
	{
		const string attribute = "global::Htmxor.HtmxRoute(\"/items\", Methods = [], CurrentUrl = \"ftp://example.test/items\", Target = \"section#\")";
		var source = $$"""
			namespace Htmxor.Consumer;
			[{{attribute}}]
			public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await AnalyzeRouteSourceAsync(source, local);

		AssertRouteReasons(diagnostics, "HTMXOR005", MethodsReason, CurrentUrlReason, TargetReason);
		foreach (var diagnostic in diagnostics)
		{
			Assert.Equal(attribute, source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
			Assert.Equal(ComponentPath("Item.cs"), diagnostic.Location.GetMappedLineSpan().Path);
			Assert.Equal(1, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
		}
	}

	[Theory]
	[InlineData("\"/items\"", "", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", "a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods")]
	[InlineData("null", ", Methods = [\"GET\"]", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", "HtmxRoute must resolve one nonblank constant route template")]
	[InlineData("\" \"", ", Methods = [\"GET\"]", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", "HtmxRoute must resolve one nonblank constant route template")]
	[InlineData("\"items\"", ", Methods = [\"GET\"]", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", "begin with the '/' character")]
	[InlineData("\"/items\"", ", Methods = null", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", MethodsReason)]
	[InlineData("\"/items\"", ", Methods = []", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", MethodsReason)]
	[InlineData("\"/items\"", ", Methods = [\"GET\", \"get\"]", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", MethodsReason)]
	[InlineData("\"/items\"", ", Methods = [\"TRACE\"]", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", MethodsReason)]
	[InlineData("\"/items\"", ", Methods = [\"GET\"], CurrentUrl = \"ftp://example.test/items\"", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", CurrentUrlReason)]
	[InlineData("\"/items\"", ", Methods = [\"GET\"], Target = \"section#\"", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", TargetReason)]
	[InlineData("\"/items\"", ", Methods = [\"GET\"], Targets = [\" \" ]", "public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR005", "HtmxRoute named argument 'Targets' must resolve to a constant array of valid element identities")]
	[InlineData("\"/items\"", ", Methods = [\"GET\"]", "public sealed class Item;", "HTMXOR004", "the HtmxRoute target must be a concrete, non-generic Blazor component")]
	[InlineData("\"/items\"", ", Methods = [\"GET\"]", "public abstract class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR004", "the HtmxRoute target must be a concrete, non-generic Blazor component")]
	[InlineData("\"/items\"", ", Methods = [\"GET\"]", "public class Item<T> : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR004", "the HtmxRoute target must be a concrete, non-generic Blazor component")]
	[InlineData("\"/items\"", ", Methods = [\"GET\"]", "[global::Microsoft.AspNetCore.Components.Route(\"/normal\")] public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;", "HTMXOR004", NormalRouteReason)]
	public async Task Symbol_local_route_reasons_are_available_without_complete_analysis(string template, string members, string declaration, string id, string reason)
	{
		var source = $$"""
			namespace Htmxor.Consumer;
			[global::Htmxor.HtmxRoute({{template}}{{members}})]
			{{declaration}}
			""";

		var diagnostics = await AnalyzeRouteSourceAsync(source, local: true);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(id, diagnostic.Id);
		Assert.Contains(reason, diagnostic.GetMessage(), StringComparison.Ordinal);
		AssertLocalDiagnostic(diagnostic);
		Assert.Equal(ComponentPath("Item.cs"), diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Nested_route_target_reports_its_local_origin_fault()
	{
		const string source = "public class Outer { [global::Htmxor.HtmxRoute(\"/items\", Methods = [\"GET\"])] public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase; }";

		var diagnostics = await AnalyzeRouteSourceAsync(source, local: true);

		AssertRouteReasons(diagnostics, "HTMXOR004", "the HtmxRoute component must be a top-level class");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Independent_normal_only_faults_all_report_at_the_mapped_marker(bool local)
	{
		var imports = ComponentPath("_Imports.razor");
		var source = $$"""
			namespace Htmxor.Consumer;
			#line 7 "{{EscapePath(imports)}}"
			[global::Htmxor.DisableHtmxDirectRouting]
			#line default
			[global::Htmxor.HtmxRoute("/items", Methods = ["GET"])]
			public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await AnalyzeRouteSourceAsync(source, local);

		AssertRouteReasons(diagnostics, "HTMXOR003",
			"DisableHtmxDirectRouting declarations from _Imports.razor are not supported", MarkerCombinationReason, MarkerRouteReason);
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Equal(imports, diagnostic.Location.GetMappedLineSpan().Path);
			Assert.Equal(6, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
		});
	}

	[Theory]
	[InlineData("public sealed class Item;")]
	[InlineData("public abstract class Item : global::Microsoft.AspNetCore.Components.ComponentBase;")]
	[InlineData("public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;")]
	public async Task Normal_only_marker_requires_a_local_stock_route_during_local_analysis(string declaration)
	{
		var diagnostics = await AnalyzeRouteSourceAsync("[global::Htmxor.DisableHtmxDirectRouting] " + declaration, local: true);

		AssertRouteReasons(diagnostics, "HTMXOR003", MarkerRouteReason);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Invalid_component_shape_does_not_hide_independent_route_member_faults(bool local)
	{
		const string source = "[global::Htmxor.HtmxRoute(\"/items\", Methods = [])] public abstract class Item : global::Microsoft.AspNetCore.Components.ComponentBase;";

		var diagnostics = await AnalyzeRouteSourceAsync(source, local);

		Assert.Equal(new[] { "HTMXOR004", "HTMXOR005" }, diagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage() == "HTMX-only route is not available: " + MethodsReason);
		Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage() == "HTMX-only route is not available: the HtmxRoute target must be a concrete, non-generic Blazor component");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Missing_template_does_not_hide_independent_route_member_faults(bool local)
	{
		const string source = "[global::Htmxor.HtmxRoute(\" \" , Methods = [])] public sealed class Item : global::Microsoft.AspNetCore.Components.ComponentBase;";

		var diagnostics = await AnalyzeRouteSourceAsync(source, local);

		AssertRouteReasons(diagnostics, "HTMXOR005", "HtmxRoute must resolve one nonblank constant route template", MethodsReason);
	}

	private static void AssertRouteReasons(ImmutableArray<Diagnostic> diagnostics, string id, params string[] reasons)
	{
		Assert.Equal(reasons.Order(StringComparer.Ordinal), diagnostics.Select(diagnostic => diagnostic.GetMessage().Split(": ", 2)[1]).Order(StringComparer.Ordinal));
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Equal(id, diagnostic.Id);
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		});
	}

	private static void AssertLocalDiagnostic(Diagnostic diagnostic)
	{
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
	}

	private static async Task<ImmutableArray<Diagnostic>> AnalyzeRouteSourceAsync(string source, bool local)
	{
		var tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), ComponentPath("Item.cs"));
		return await AnalyzeRouteTreesAsync([tree], [], local);
	}

	private static async Task<ImmutableArray<Diagnostic>> AnalyzeRouteTreesAsync(SyntaxTree[] trees, ImmutableArray<AdditionalText> files, bool local)
	{
		var compilation = CSharpCompilation.Create("RouteLocalTests", trees, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var driver = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer()),
			new AnalyzerOptions(files, new TestAnalyzerConfigOptionsProvider(ProjectDirectory)));
		var diagnostics = local
			? await driver.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(trees[0]), null, CancellationToken.None)
			: await driver.GetAnalyzerDiagnosticsAsync();
		if (local) Assert.All(diagnostics, AssertLocalDiagnostic);
		return diagnostics;
	}
}
