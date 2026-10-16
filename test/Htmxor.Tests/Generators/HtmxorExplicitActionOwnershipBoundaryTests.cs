using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Naturally_typed_lambda_missing_return_preserves_nonordinary_feedback(bool local)
	{
		var source = Owner("private void Initialize() { var callback = [Htmxor.HtmxAction(\"PUT\")] () => { if (System.DateTime.Now.Ticks > 0) return 1; }; }");
		AssertCompilerErrorText(source, "CS1643", "=>");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1643");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData("private void Initialize() { var callback = [Htmxor.HtmxAction(\"PUT\")] () => 1; }", false)]
	[InlineData("private void Initialize() { var callback = [Htmxor.HtmxAction(\"PUT\")] () => 1; }", true)]
	[InlineData("private void Initialize() { var callback = [Htmxor.HtmxAction(\"PUT\")] () => { return 1; }; }", false)]
	[InlineData("private void Initialize() { var callback = [Htmxor.HtmxAction(\"PUT\")] () => { return 1; }; }", true)]
	[InlineData("private static T Use<T>(System.Func<T> callback) => callback(); private void Initialize() { var result = Use([Htmxor.HtmxAction(\"PUT\")] () => 1); }", false)]
	[InlineData("private static T Use<T>(System.Func<T> callback) => callback(); private void Initialize() { var result = Use([Htmxor.HtmxAction(\"PUT\")] () => 1); }", true)]
	public async Task Bound_inferred_lambda_return_types_preserve_original_nonordinary_feedback(string member, bool local)
	{
		var source = Owner(member);

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Malformed_lambda_delegate_signature_remains_compiler_owned(bool local)
	{
		var source = Owner("private System.Func<int, int> Factory() => [Htmxor.HtmxAction(\"PATCH\")] (string value) => { return 1; }; [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		AssertCompilerErrorText(source, "CS1661", "=>");
		AssertCompilerErrorText(source, "CS1678", "value");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1661");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken(int value, int value) { } }", "CS0100", "value", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken(int value, int value) { } }", "CS0100", "value", true)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken<T>() where T : struct, class { } }", "CS0449", "class", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken<T>() where T : struct, class { } }", "CS0449", "class", true)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken(int value = \"bad\") { } }", "CS1750", "value", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken(int value = \"bad\") { } }", "CS1750", "value", true)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] async int Broken() { return 1; } }", "CS1983", "Broken", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] async int Broken() { return 1; } }", "CS1983", "Broken", true)]
	public async Task Fully_bound_malformed_local_signatures_remain_compiler_owned(string member, string compilerError, string token, bool local)
	{
		var source = Owner(member + " [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		AssertCompilerErrorText(source, compilerError, token);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: compilerError);

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] async int Broken() { } }", "CS1983", "Broken", false)]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] async int Broken() { } }", "CS1983", "Broken", true)]
	[InlineData("[Htmxor.HtmxAction(\"PATCH\")] private static int Broken<T>() where T : Missing { }", "CS0246", "Missing", false)]
	[InlineData("[Htmxor.HtmxAction(\"PATCH\")] private static int Broken<T>() where T : Missing { }", "CS0246", "Missing", true)]
	public async Task Missing_return_does_not_hide_a_simultaneous_malformed_signature(string member, string compilerError, string token, bool local)
	{
		var source = Owner(member + " [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		AssertCompilerErrorText(source, "CS0161", "Broken");
		AssertCompilerErrorText(source, compilerError, token);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: compilerError);

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData("private void Use(System.Func<int, int> callback) { } private void Use(System.Func<string, string> callback) { } private void Initialize() { Use([Htmxor.HtmxAction(\"PUT\")] (int value) => { return \"bad\"; }); }", "CS0029", "\"bad\"", false)]
	[InlineData("private void Use(System.Func<int, int> callback) { } private void Use(System.Func<string, string> callback) { } private void Initialize() { Use([Htmxor.HtmxAction(\"PUT\")] (int value) => { return \"bad\"; }); }", "CS0029", "\"bad\"", true)]
	[InlineData("private void Initialize<T>() { void Outer() { [Htmxor.HtmxAction(\"PUT\")] int Broken(T value) { } } }", "CS0161", "Broken", false)]
	[InlineData("private void Initialize<T>() { void Outer() { [Htmxor.HtmxAction(\"PUT\")] int Broken(T value) { } } }", "CS0161", "Broken", true)]
	[InlineData("[method: Htmxor.HtmxAction(\"PUT\")] public event System.Action Changed = Missing;", "CS0103", "Missing", false)]
	[InlineData("[method: Htmxor.HtmxAction(\"PUT\")] public event System.Action Changed = Missing;", "CS0103", "Missing", true)]
	public async Task Lexically_bound_body_errors_preserve_original_nonordinary_feedback(string member, string compilerError, string token, bool local)
	{
		var source = Owner(member);
		AssertCompilerErrorText(source, compilerError, token);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: compilerError);

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Multiple_lambda_actions_preserve_each_attribute_despite_missing_return(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PUT\"), Htmxor.HtmxAction(\"PATCH\")] () => { };");
		AssertCompilerErrorText(source, "CS1643", "=>");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1643");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"), ("HTMXOR035", "ordinary callback method"));
		Assert.Equal(2, diagnostics.Select(diagnostic => diagnostic.Location.SourceSpan).Distinct().Count());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Malformed_local_parameter_syntax_stays_compiler_owned(bool local)
	{
		var source = Owner("private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] void Broken(int first int second) { } } [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		var error = Assert.Single(CreateCompilation(source).GetDiagnostics(), diagnostic => diagnostic.Id == "CS1003");
		Assert.Equal(source.IndexOf("int second", StringComparison.Ordinal), error.Location.SourceSpan.Start);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1003");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}
}
