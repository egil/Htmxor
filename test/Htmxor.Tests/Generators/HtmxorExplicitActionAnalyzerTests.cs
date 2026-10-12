using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorExplicitActionAnalyzerTests
{
	private const string SourcePath = "/htmxor-explicit-action-tests/Owner.cs";
	private static readonly ImmutableArray<MetadataReference> References =
		((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
			.Concat([typeof(ComponentBase).Assembly.Location, typeof(HtmxActionAttribute).Assembly.Location])
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

	[Theory]
	[InlineData("GET")]
	[InlineData("HEAD")]
	[InlineData("PROPFIND")]
	[InlineData("")]
	[InlineData(" PUT ")]
	[InlineData(null)]
	public async Task Unsupported_explicit_method_fails_at_its_declaration(string? method)
	{
		var argument = method is null ? "null" : $"\"{method}\"";
		var source = Owner($"[Htmxor.HtmxAction({argument})] private void Save() {{ }}");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "method");
	}

	[Theory]
	[InlineData("static void Save() { }", "static")]
	[InlineData("int Save() => 1;", "return")]
	[InlineData("System.Threading.Tasks.Task<int> Save() => System.Threading.Tasks.Task.FromResult(1);", "return")]
	[InlineData("void Save(System.EventArgs args) { }", "HtmxEventArgs")]
	[InlineData("void Save(object args) { }", "HtmxEventArgs")]
	[InlineData("void Save(out Htmxor.HtmxEventArgs args) { args = null!; }", "parameter")]
	[InlineData("System.Threading.Tasks.ValueTask Save() => default;", "return")]
	[InlineData("void Save(int value) { }", "HtmxEventArgs")]
	[InlineData("void Save(Htmxor.HtmxEventArgs args, int value) { }", "parameter")]
	[InlineData("void Save<T>() { }", "generic")]
	[InlineData("async void Save() => await System.Threading.Tasks.Task.Yield();", "async void")]
	[InlineData("void Save(Htmxor.HtmxEventArgs args = null!) { }", "optional")]
	[InlineData("void Save(ref Htmxor.HtmxEventArgs args) { }", "parameter")]
	public async Task Unsupported_explicit_signature_fails_at_its_declaration(string member, string cause)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private " + member);
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, cause);
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("put")]
	[InlineData("PaTcH")]
	[InlineData("DELETE")]
	[InlineData("query")]
	public async Task Supported_method_names_are_case_insensitive(string method)
	{
		var diagnostics = await AnalyzeAsync(Owner($"[Htmxor.HtmxAction(\"{method}\")] private void Save() {{ }}"));

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Explicit_action_on_normal_only_component_is_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }", extraAttribute: "[Htmxor.DisableHtmxDirectRouting]");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "DisableHtmxDirectRouting");
	}

	[Fact]
	public async Task Base_class_action_has_no_route_authority()
	{
		const string source = """
			namespace Consumer;
			public class Base : Microsoft.AspNetCore.Components.ComponentBase
			{
				[Htmxor.HtmxAction("PUT")] protected void Save() { }
			}
			[Microsoft.AspNetCore.Components.Route("/explicit")]
			public class Owner : Base { }
			""";
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "route");
	}

	[Fact]
	public async Task Explicit_action_outside_the_route_allowlist_is_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }", route: "[Htmxor.HtmxRoute(\"/explicit\", Methods = new[] { \"GET\" })]");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "Methods");
	}

	[Fact]
	public async Task Different_explicit_handlers_for_one_method_are_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { } [Htmxor.HtmxAction(\"put\")] private void Other() { }");
		var diagnostics = await AnalyzeAsync(source);

		Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "HTMXOR002" && diagnostic.Severity == DiagnosticSeverity.Error);
		Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage().Contains("PUT", StringComparison.OrdinalIgnoreCase) &&
			diagnostic.GetMessage().Contains("handler", StringComparison.OrdinalIgnoreCase));
		Assert.All(diagnostics, diagnostic => AssertAttributeLocation(diagnostic, source));
	}

	[Fact]
	public async Task Overloaded_explicit_handler_is_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { } private void Save(int value) { }");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "overload");
	}

	[Fact]
	public async Task Nonconstant_argument_keeps_the_compiler_error_without_a_duplicate_action_error()
	{
		var source = Owner("private static string Verb => \"PUT\"; [Htmxor.HtmxAction(Verb)] private void Save() { }");
		var diagnostics = await AnalyzeAsync(source, compilerError: "CS0182");

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Incorrect_attribute_target_keeps_the_compiler_error_without_a_duplicate_action_error()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private int Value { get; set; }");
		var diagnostics = await AnalyzeAsync(source, compilerError: "CS0592");

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Attributed_getter_with_a_return_value_is_rejected_at_its_declaration()
	{
		var source = Owner("public int Value { [Htmxor.HtmxAction(\"PUT\")] get => 42; }");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "return");
	}

	private static string Owner(string members, string extraAttribute = "", string route = "[Microsoft.AspNetCore.Components.Route(\"/explicit\")]") => $$"""
		namespace Consumer;
		{{route}}
		{{extraAttribute}}
		public partial class Owner : Microsoft.AspNetCore.Components.ComponentBase
		{
			{{members}}
		}
		""";

	private static void AssertDeclarationError(ImmutableArray<Diagnostic> diagnostics, string source, string cause)
	{
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		AssertAttributeLocation(diagnostic, source);
		Assert.Contains(cause, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
	}

	private static void AssertAttributeLocation(Diagnostic diagnostic, string source)
	{
		Assert.Equal(SourcePath, diagnostic.Location.GetLineSpan().Path);
		var attributes = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<AttributeSyntax>()
			.Where(static attribute => attribute.Name.ToString() == "Htmxor.HtmxAction");
		Assert.Contains(attributes, attribute => attribute.Parent!.Span.Contains(diagnostic.Location.SourceSpan));
	}

	private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, string? compilerError = null)
	{
		var compilation = CSharpCompilation.Create("ExplicitActionContract", [CSharpSyntaxTree.ParseText(source, path: SourcePath)], References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var errors = compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		if (compilerError is null) Assert.Empty(errors);
		else Assert.Contains(errors, diagnostic => diagnostic.Id == compilerError);

		return await compilation.WithAnalyzers([new HtmxorActionDeclarationAnalyzer()]).GetAnalyzerDiagnosticsAsync();
	}
}
