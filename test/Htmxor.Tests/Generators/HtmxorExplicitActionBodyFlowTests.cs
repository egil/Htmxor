using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData("{ }", "Save", false)]
	[InlineData("{ }", "Save", true)]
	[InlineData("{ return; }", "return;", false)]
	[InlineData("{ return; }", "return;", true)]
	public async Task Unassigned_out_parameter_preserves_all_bound_method_causes(string body, string errorToken, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static void Save<T>(out int value) " + body);
		AssertCompilerErrorText(source, "CS0177", errorToken);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0177");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"), ("HTMXOR022", "is generic"),
			("HTMXOR023", "unsupported parameters"), ("HTMXOR025", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData("{ }", "Broken", false)]
	[InlineData("{ }", "Broken", true)]
	[InlineData("{ return; }", "return;", false)]
	[InlineData("{ return; }", "return;", true)]
	public async Task Unassigned_out_parameter_preserves_local_function_feedback(string body, string errorToken, bool local)
	{
		var source = Owner("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static void Broken<T>(out int value) " + body + " }");
		AssertCompilerErrorText(source, "CS0177", errorToken);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0177");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Iterator_body_error_preserves_all_bound_method_causes(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static int Save<T>() { yield return 1; }");
		AssertCompilerErrorText(source, "CS1624", "Save");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1624");

		AssertFeedback(diagnostics, source,
			("HTMXOR013", "is static"), ("HTMXOR022", "is generic"), ("HTMXOR026", "returns a value"));
	}

	[Theory]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static int Broken<T>() { yield return 1; } }", "Broken", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static int Broken<T>() { yield return 1; } }", "Broken", true)]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get { yield return 1; } }", "get", false)]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get { yield return 1; } }", "get", true)]
	public async Task Iterator_body_error_preserves_nonordinary_feedback(string member, string errorToken, bool local)
	{
		var source = Owner(member);
		AssertCompilerErrorText(source, "CS1624", errorToken);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1624");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repairing_iterator_body_preserves_the_same_method_header_causes(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static int Save<T>() { return 1; }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source,
			("HTMXOR013", "is static"), ("HTMXOR022", "is generic"), ("HTMXOR026", "returns a value"));
	}

	[Theory]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] Missing Broken() => null!; }", "CS0246", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] Missing Broken() => null!; }", "CS0246", true)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken<T>() where T : Missing { } }", "CS0246", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken<T>() where T : Missing { } }", "CS0246", true)]
	[InlineData("private System.Action Factory() => [Htmxor.HtmxAction(Missing)] () => { };", "CS0103", false)]
	[InlineData("private System.Action Factory() => [Htmxor.HtmxAction(Missing)] () => { };", "CS0103", true)]
	public async Task Body_phase_binding_errors_in_action_declarations_remain_compiler_owned(string member, string compilerError, bool local)
	{
		var source = Owner(member + " [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		AssertCompilerErrorText(source, compilerError, "Missing");
		var compilation = CreateCompilation(source);
		Assert.Contains(compilation.GetSemanticModel(compilation.SyntaxTrees.Single()).GetMethodBodyDiagnostics(),
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id == compilerError);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: compilerError);

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}
}
