using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Fact]
	public async Task Explicit_interface_action_fails_at_its_declaration()
	{
		var source = InterfaceOwner("void IHandler.Save() { }");
		var compilation = CreateCompilation(source);
		Assert.Empty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var method = Assert.Single(compilation.GetTypeByMetadataName("Consumer.Owner")!.GetMembers().OfType<IMethodSymbol>()
			.Where(static member => member.ExplicitInterfaceImplementations.Length > 0));
		Assert.Equal(MethodKind.ExplicitInterfaceImplementation, method.MethodKind);

		AssertDeclarationError(await AnalyzeAsync(source), source, "ordinary", "HTMXOR035");
	}

	[Fact]
	public async Task Implicit_interface_action_remains_an_ordinary_method()
	{
		var source = InterfaceOwner("public void Save() { }");
		var compilation = CreateCompilation(source);
		Assert.Empty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var method = Assert.Single(compilation.GetTypeByMetadataName("Consumer.Owner")!.GetMembers("Save").OfType<IMethodSymbol>());
		Assert.Equal(MethodKind.Ordinary, method.MethodKind);

		Assert.Empty(await AnalyzeAsync(source));
	}

	[Fact]
	public async Task Special_name_ordinary_action_remains_valid()
	{
		var source = Owner("[System.Runtime.CompilerServices.SpecialName] [Htmxor.HtmxAction(\"PUT\")] private void Save() { }");
		var compilation = CreateCompilation(source);
		Assert.Empty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var method = Assert.Single(compilation.GetTypeByMetadataName("Consumer.Owner")!.GetMembers("Save").OfType<IMethodSymbol>());
		Assert.Equal(MethodKind.Ordinary, method.MethodKind);

		Assert.Empty(await AnalyzeAsync(source));
	}

	private static string InterfaceOwner(string method) => $$"""
		namespace Consumer;
		public interface IHandler { void Save(); }
		[Microsoft.AspNetCore.Components.Route("/explicit")]
		public class Owner : Microsoft.AspNetCore.Components.ComponentBase, IHandler
		{
			[Htmxor.HtmxAction("PUT")] {{method}}
		}
		""";

	[Theory]
	[InlineData("setter", "public Htmxor.HtmxEventArgs Value { get => null!; [Htmxor.HtmxAction(\"PUT\")] set { } }")]
	[InlineData("finalizer", "[Htmxor.HtmxAction(\"PUT\")] ~Owner() { }")]
	[InlineData("local-instance", "private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] void Save() { _ = this.GetHashCode(); } Save(); }")]
	[InlineData("local-static", "private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static void Save() { } Save(); }")]
	[InlineData("lambda-instance", "private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] () => { _ = this.GetHashCode(); };")]
	[InlineData("lambda-static", "private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] static () => { };")]
	public async Task Nonordinary_explicit_action_fails_at_its_declaration(string scenario, string member)
	{
		var source = Owner(member);
		var diagnostics = await AnalyzeAsync(source);
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR035", diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		AssertAttributeLocation(diagnostic, source);
		Assert.True(diagnostic.GetMessage().Contains("ordinary", StringComparison.OrdinalIgnoreCase),
			scenario + ": " + diagnostic.GetMessage());
	}

	[Theory]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private void Save(Htmxor.HtmxEventArgs args) { }")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private System.Threading.Tasks.Task Save() => System.Threading.Tasks.Task.CompletedTask;")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private System.Threading.Tasks.Task Save(Htmxor.HtmxEventArgs args) => System.Threading.Tasks.Task.CompletedTask;")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private void Finalize() { }")]
	[InlineData("[System.Runtime.CompilerServices.CompilerGenerated] [Htmxor.HtmxAction(\"PUT\")] private void Save() { }")]
	public async Task Ordinary_explicit_action_remains_valid(string member)
	{
		Assert.Empty(await AnalyzeAsync(Owner(member)));
	}
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

		AssertDeclarationError(diagnostics, source, "method", "HTMXOR029");
	}

	[Theory]
	[InlineData("static void Save() { }", "static", "HTMXOR013")]
	[InlineData("int Save() => 1;", "return", "HTMXOR026")]
	[InlineData("System.Threading.Tasks.Task<int> Save() => System.Threading.Tasks.Task.FromResult(1);", "return", "HTMXOR019")]
	[InlineData("void Save(System.EventArgs args) { }", "HtmxEventArgs", "HTMXOR020")]
	[InlineData("void Save(object args) { }", "HtmxEventArgs", "HTMXOR021")]
	[InlineData("void Save(out Htmxor.HtmxEventArgs args) { args = null!; }", "parameter", "HTMXOR023")]
	[InlineData("System.Threading.Tasks.ValueTask Save() => default;", "return", "HTMXOR016")]
	[InlineData("void Save(int value) { }", "HtmxEventArgs", "HTMXOR025")]
	[InlineData("void Save(Htmxor.HtmxEventArgs args, int value) { }", "parameter", "HTMXOR025")]
	[InlineData("void Save<T>() { }", "generic", "HTMXOR022")]
	[InlineData("async void Save() => await System.Threading.Tasks.Task.Yield();", "async void; return Task instead", "HTMXOR018")]
	[InlineData("void Save(Htmxor.HtmxEventArgs args = null!) { }", "optional", "HTMXOR024")]
	[InlineData("void Save(ref Htmxor.HtmxEventArgs args) { }", "parameter", "HTMXOR023")]
	public async Task Unsupported_explicit_signature_fails_at_its_declaration(string member, string cause, string expectedId)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private " + member);
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, cause, expectedId);
	}
	[Theory]
	[InlineData("System.Threading.Tasks.ValueTask", "HTMXOR016")]
	[InlineData("System.Threading.Tasks.ValueTask<int>", "HTMXOR016")]
	public async Task Explicit_ValueTask_return_reports_the_ValueTask_cause_at_its_attribute(
		string returnType, string expectedId)
	{
		var source = Owner($"[Htmxor.HtmxAction(\"PUT\")] private {returnType} Save() => default;");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "returns a value; return void or Task", expectedId);
		var attribute = Assert.Single(CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
			.OfType<AttributeSyntax>().Where(static attribute => attribute.Name.ToString() == "Htmxor.HtmxAction"));
		Assert.Equal(attribute.Span, Assert.Single(diagnostics).Location.SourceSpan);
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

		AssertDeclarationError(diagnostics, source, "DisableHtmxDirectRouting", "HTMXOR034");
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

		AssertDeclarationError(diagnostics, source, "route", "HTMXOR030");
	}

	[Fact]
	public async Task Explicit_action_outside_the_route_allowlist_is_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }", route: "[Htmxor.HtmxRoute(\"/explicit\", Methods = new[] { \"GET\" })]");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "Methods", "HTMXOR033");
	}

	[Fact]
	public async Task Null_explicit_allowlist_reports_a_declaration_error_without_an_analyzer_failure()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }", route: "[Htmxor.HtmxRoute(\"/explicit\", Methods = null)]");
		var diagnostics = await AnalyzeAsync(source);

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
		AssertDeclarationError(diagnostics, source, "Methods", "HTMXOR033");
	}

	[Fact]
	public async Task Different_explicit_handlers_for_one_method_are_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { } [Htmxor.HtmxAction(\"put\")] private void Other() { }");
		var diagnostics = await AnalyzeAsync(source);

		Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "HTMXOR031" && diagnostic.Severity == DiagnosticSeverity.Error);
		Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage().Contains("PUT", StringComparison.OrdinalIgnoreCase) &&
			diagnostic.GetMessage().Contains("handler", StringComparison.OrdinalIgnoreCase));
		Assert.All(diagnostics, diagnostic => AssertAttributeLocation(diagnostic, source));
	}

	[Fact]
	public async Task Overloaded_explicit_handler_is_rejected()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { } private void Save(int value) { }");
		var diagnostics = await AnalyzeAsync(source);

		AssertDeclarationError(diagnostics, source, "overload", "HTMXOR017");
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

		AssertDeclarationError(diagnostics, source, "ordinary callback method", "HTMXOR035");
		var message = Assert.Single(diagnostics).GetMessage();
		Assert.True(message.Contains("declare an ordinary instance method", StringComparison.Ordinal), message);
		Assert.DoesNotContain("returns a value; return void or Task", message, StringComparison.Ordinal);
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

	private static void AssertDeclarationError(ImmutableArray<Diagnostic> diagnostics, string source, string cause, string expectedId)
	{
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(expectedId, diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
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
		var compilation = CreateCompilation(source);
		var errors = compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		if (compilerError is null) Assert.Empty(errors);
		else Assert.Contains(errors, diagnostic => diagnostic.Id == compilerError);

		return await compilation.WithAnalyzers([new HtmxorActionDeclarationAnalyzer()]).GetAnalyzerDiagnosticsAsync();
	}

	private static CSharpCompilation CreateCompilation(string source)
		=> CSharpCompilation.Create("ExplicitActionContract", [CSharpSyntaxTree.ParseText(source, path: SourcePath)], References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
