using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Missing_method_return_preserves_all_bound_header_causes(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static int Save<T>() { }");
		AssertCompilerErrorText(source, "CS0161", "Save");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0161");

		AssertFeedback(diagnostics, source,
			("HTMXOR013", "is static"), ("HTMXOR022", "is generic"), ("HTMXOR026", "returns a value"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Missing_local_function_return_preserves_nonordinary_feedback(bool local)
	{
		var source = Owner("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static int Broken<T>() { } }");
		AssertCompilerErrorText(source, "CS0161", "Broken");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0161");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData("public Missing Value { [Htmxor.HtmxAction(\"PATCH\")] get => null!; }", false)]
	[InlineData("public Missing Value { [Htmxor.HtmxAction(\"PATCH\")] get => null!; }", true)]
	[InlineData("public Missing this[int index] { [Htmxor.HtmxAction(\"PATCH\")] get => null!; }", false)]
	[InlineData("public Missing this[int index] { [Htmxor.HtmxAction(\"PATCH\")] get => null!; }", true)]
	[InlineData("public event Missing Changed { [Htmxor.HtmxAction(\"PATCH\")] add { } remove { } }", false)]
	[InlineData("public event Missing Changed { [Htmxor.HtmxAction(\"PATCH\")] add { } remove { } }", true)]
	[InlineData("[method: Htmxor.HtmxAction(\"PATCH\")] public event Missing Changed;", false)]
	[InlineData("[method: Htmxor.HtmxAction(\"PATCH\")] public event Missing Changed;", true)]
	public async Task Unresolved_enclosing_member_type_suppresses_only_its_action(string member, bool local)
	{
		var source = Owner(member + " [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		AssertCompilerErrorText(source, "CS0246", "Missing");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0246");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get => Missing(); }", false)]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get => Missing(); }", true)]
	[InlineData("public int this[int index] { [Htmxor.HtmxAction(\"PUT\")] get => Missing(); }", false)]
	[InlineData("public int this[int index] { [Htmxor.HtmxAction(\"PUT\")] get => Missing(); }", true)]
	[InlineData("public event System.Action Changed { [Htmxor.HtmxAction(\"PUT\")] add { Missing(); } remove { } }", false)]
	[InlineData("public event System.Action Changed { [Htmxor.HtmxAction(\"PUT\")] add { Missing(); } remove { } }", true)]
	public async Task Accessor_body_binding_errors_preserve_nonordinary_feedback(string member, bool local)
	{
		var source = Owner(member);
		AssertCompilerErrorText(source, "CS0103", "Missing");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0103");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get { } }", false)]
	[InlineData("public int Value { [Htmxor.HtmxAction(\"PUT\")] get { } }", true)]
	[InlineData("public int this[int index] { [Htmxor.HtmxAction(\"PUT\")] get { } }", false)]
	[InlineData("public int this[int index] { [Htmxor.HtmxAction(\"PUT\")] get { } }", true)]
	public async Task Missing_accessor_return_preserves_nonordinary_feedback(string member, bool local)
	{
		var source = Owner(member);
		AssertCompilerErrorText(source, "CS0161", "get");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0161");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	private static void AssertCompilerErrorText(string source, string id, string text)
	{
		var diagnostic = Assert.Single(CreateCompilation(source).GetDiagnostics(),
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id == id);
		Assert.Equal(text, source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
	}
}
