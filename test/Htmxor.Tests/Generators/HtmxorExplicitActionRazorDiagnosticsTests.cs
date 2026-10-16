using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Real_Razor_code_action_reports_independent_faults_at_its_authored_attribute(bool local)
	{
		var diagnostics = await AnalyzeExplicitRazorAsync("Shape", local);

		Assert.Equal(new[] { "HTMXOR013", "HTMXOR022", "HTMXOR026" }, diagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		Assert.All(diagnostics, diagnostic =>
		{
			AssertRazorActionLocation(diagnostic, "Shape", 2);
			Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	[Fact]
	public async Task Real_Razor_binding_and_named_form_each_conflict_with_explicit_POST_at_compilation_end()
	{
		Assert.Empty(await AnalyzeExplicitRazorAsync("Conflict", local: true));

		var diagnostics = await AnalyzeExplicitRazorAsync("Conflict", local: false);

		Assert.Equal(new[] { "HTMXOR031", "HTMXOR032" }, diagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage().Contains("same handler", StringComparison.Ordinal));
		Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage().Contains("named forms", StringComparison.Ordinal));
		Assert.All(diagnostics, diagnostic =>
		{
			AssertRazorActionLocation(diagnostic, "Conflict", 4);
			Assert.Contains(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	private static void AssertRazorActionLocation(Diagnostic diagnostic, string fixture, int line)
	{
		var mapped = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal("/tmp/issue332-razor-capture/" + fixture + ".razor", mapped.Path);
		Assert.Equal(line, mapped.StartLinePosition.Line);
		Assert.Equal(5, mapped.StartLinePosition.Character);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal("Htmxor.HtmxAction(\"" + (fixture == "Shape" ? "PUT" : "POST") + "\")",
			diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
	}

	private static async Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> AnalyzeExplicitRazorAsync(string fixture, bool local)
	{
		var tree = ReadExplicitRazorTree(fixture, "declaration", "_razor.decl.g.cs");
		var body = ReadExplicitRazorTree(fixture, "generated", "_razor.g.cs");
		var compilation = CSharpCompilation.Create("ExplicitRazorFeedback", [tree, body], References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var driver = compilation.WithAnalyzers([new HtmxorActionDeclarationAnalyzer()]);
		return local
			? await driver.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(tree), null, CancellationToken.None)
			: await driver.GetAnalyzerDiagnosticsAsync();
	}

	private static SyntaxTree ReadExplicitRazorTree(string fixture, string part, string suffix)
	{
		using var stream = typeof(HtmxorExplicitActionAnalyzerTests).Assembly.GetManifestResourceStream("Htmxor.Generators.ExplicitActionFixtures." + fixture + "." + part + ".fixture")!;
		using var reader = new StreamReader(stream);
		return CSharpSyntaxTree.ParseText(reader.ReadToEnd(), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
			"Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/" + fixture + suffix);
	}
}
