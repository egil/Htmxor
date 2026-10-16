using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Independent_shape_faults_report_every_reason_once_at_the_action(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static int Save<T>() => 1;");

		AssertFeedback(await AnalyzeFeedbackAsync(source, local), source,
			("HTMXOR013", "is static"), ("HTMXOR022", "is generic"), ("HTMXOR026", "returns a value"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Distinct_parameter_causes_sharing_an_ID_are_not_collapsed(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(int first, string second) { } [Htmxor.HtmxAction(\"PATCH\")] private void Other(int value) { }");

		AssertFeedback(await AnalyzeFeedbackAsync(source, local), source,
			("HTMXOR025", "has unsupported parameters"),
			("HTMXOR025", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Return_and_independent_parameter_faults_are_all_reported(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private int Save(System.EventArgs args = null!) => 1;");

		AssertFeedback(await AnalyzeFeedbackAsync(source, local), source,
			("HTMXOR026", "returns a value"), ("HTMXOR024", "optional parameter"), ("HTMXOR020", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Verb_owner_normal_only_and_shape_faults_are_independent(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"GET\")] private static int Save() => 1;",
			extraAttribute: "[Htmxor.DisableHtmxDirectRouting]", route: "");

		AssertFeedback(await AnalyzeFeedbackAsync(source, local), source, ("HTMXOR029", "method must be"),
			("HTMXOR030", "owns its route"), ("HTMXOR034", "DisableHtmxDirectRouting"),
			("HTMXOR013", "is static"), ("HTMXOR026", "returns a value"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Allowlist_and_shape_faults_are_independent(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static void Save() { }",
			route: "[Htmxor.HtmxRoute(\"/explicit\", Methods = new[] { \"GET\" })]");

		AssertFeedback(await AnalyzeFeedbackAsync(source, local), source,
			("HTMXOR013", "is static"), ("HTMXOR033", "does not allow the PUT"));
	}

	[Theory]
	[InlineData("static void Save() { }", "HTMXOR013", "is static")]
	[InlineData("System.Threading.Tasks.ValueTask Save() => default;", "HTMXOR016", "returns a value")]
	[InlineData("void Save() { } private void Save(int value) { }", "HTMXOR017", "is overloaded")]
	[InlineData("async void Save() => await System.Threading.Tasks.Task.Yield();", "HTMXOR018", "async void")]
	[InlineData("System.Threading.Tasks.Task<int> Save() => System.Threading.Tasks.Task.FromResult(1);", "HTMXOR019", "returns a value")]
	[InlineData("void Save(System.EventArgs args) { }", "HTMXOR020", "not HtmxEventArgs")]
	[InlineData("void Save(object args) { }", "HTMXOR021", "not HtmxEventArgs")]
	[InlineData("void Save<T>() { }", "HTMXOR022", "is generic")]
	[InlineData("void Save(ref Htmxor.HtmxEventArgs args) { }", "HTMXOR023", "unsupported parameters")]
	[InlineData("void Save(Htmxor.HtmxEventArgs args = null!) { }", "HTMXOR024", "optional parameter")]
	[InlineData("void Save(int value) { }", "HTMXOR025", "not HtmxEventArgs")]
	[InlineData("int Save() => 1;", "HTMXOR026", "returns a value")]
	public async Task Every_ordinary_shape_cause_is_available_locally(string member, string id, string reason)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private " + member);

		AssertFeedback(await AnalyzeFeedbackAsync(source, true), source, (id, reason));
	}

	[Theory]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get => 1; }")]
	[InlineData("public Htmxor.HtmxEventArgs Value { get => null!; [Htmxor.HtmxAction(\"PUT\")] set { } }")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] ~Owner() { }")]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] void Save() { } Save(); }")]
	[InlineData("private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] () => { };")]
	[InlineData("private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] static () => { };")]
	public async Task Nonordinary_member_and_operation_placements_report_locally(string member)
	{
		var source = Owner(member);

		AssertFeedback(await AnalyzeFeedbackAsync(source, true), source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Unrelated_compiler_error_does_not_hide_a_validly_bound_action(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		var broken = CSharpSyntaxTree.ParseText("class Broken { Missing value; }", path: "/Other.cs");

		AssertFeedback(await AnalyzeFeedbackAsync(source, local, broken, "CS0246"), source, ("HTMXOR013", "is static"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Compiler_owned_malformed_attribute_is_silent_beside_a_reportable_action(bool local)
	{
		var source = Owner("private static string Verb => \"PUT\"; [Htmxor.HtmxAction(Verb)] private void Broken() { } [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0182");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Unresolved_return_type_stays_compiler_owned_beside_a_reportable_action(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PATCH\")] private Missing Broken() => null!; [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0246");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repeated_action_attributes_preserve_each_authored_occurrence(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\"), Htmxor.HtmxAction(\"PATCH\")] private static void Save() { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"), ("HTMXOR013", "is static"));
		Assert.Equal(2, diagnostics.Select(diagnostic => diagnostic.Location.SourceSpan).Distinct().Count());
	}

	[Fact]
	public async Task Explicit_interface_placement_reports_locally()
	{
		var source = InterfaceOwner("void IHandler.Save() { }");

		AssertFeedback(await AnalyzeFeedbackAsync(source, true), source, ("HTMXOR035", "ordinary callback method"));
	}

	[Fact]
	public async Task Agreeing_explicit_declarations_remain_valid_in_complete_and_local_analysis()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\"), Htmxor.HtmxAction(\"put\")] private void Save() { }");

		Assert.Empty(await AnalyzeFeedbackAsync(source, false));
		Assert.Empty(await AnalyzeFeedbackAsync(source, true));
	}

	[Fact]
	public async Task Cross_source_handler_conflicts_remain_aggregate()
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }");
		var other = CSharpSyntaxTree.ParseText("namespace Consumer; public partial class Owner { [Htmxor.HtmxAction(\"PUT\")] private void Other() { } }", path: "/Other.cs");

		Assert.Empty(await AnalyzeFeedbackAsync(source, true, other));
		var complete = await AnalyzeFeedbackAsync(source, false, other);

		Assert.Equal(2, complete.Length);
		Assert.All(complete, diagnostic =>
		{
			Assert.Equal("HTMXOR031", diagnostic.Id);
			Assert.Contains("same handler", diagnostic.GetMessage(), StringComparison.Ordinal);
			Assert.Contains(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
		Assert.Equal(new[] { "/Other.cs", SourcePath }, complete.Select(diagnostic => diagnostic.Location.GetMappedLineSpan().Path).Order(StringComparer.Ordinal));
	}

	private static void AssertFeedback(ImmutableArray<Diagnostic> diagnostics, string source,
		params (string Id, string Reason)[] expected)
	{
		Assert.Equal(expected.Length, diagnostics.Length);
		foreach (var (id, reason) in expected)
		{
			Assert.Equal(expected.Count(item => item == (id, reason)), diagnostics.Count(diagnostic => diagnostic.Id == id && diagnostic.GetMessage().Contains(reason, StringComparison.Ordinal)));
		}
		var attributes = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<AttributeSyntax>()
			.Where(attribute => attribute.Name.ToString() == "Htmxor.HtmxAction").Select(attribute => attribute.Span).ToArray();
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Contains(diagnostic.Location.SourceSpan, attributes);
			Assert.Equal(SourcePath, diagnostic.Location.GetMappedLineSpan().Path);
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
			Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	private static async Task<ImmutableArray<Diagnostic>> AnalyzeFeedbackAsync(string source, bool local,
		SyntaxTree? extra = null, string? compilerError = null)
	{
		var compilation = CreateCompilation(source);
		if (extra is not null) compilation = compilation.AddSyntaxTrees(extra);
		var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		if (compilerError is null) Assert.Empty(errors);
		else Assert.Contains(errors, diagnostic => diagnostic.Id == compilerError);
		var driver = compilation.WithAnalyzers([new HtmxorActionDeclarationAnalyzer()]);
		return local
			? await driver.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(compilation.SyntaxTrees.First()), null, CancellationToken.None)
			: await driver.GetAnalyzerDiagnosticsAsync();
	}
}
