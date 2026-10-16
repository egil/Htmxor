using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	private const string LambdaBodyLeadingComment = "\n/* This long multiline comment belongs to the lambda body leading trivia.\n" +
		"Its text keeps the original body position beyond the shortened replacement tree, so dropping it loses the original diagnostic boundary. */\n";

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Lambda_body_leading_comment_preserves_feedback_without_analyzer_exception(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PUT\")] () =>" + LambdaBodyLeadingComment + "{ };");
		AssertCompilerErrorText(source, "CS1643", "=>");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1643");

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repaired_lambda_body_with_leading_comment_retains_nonordinary_feedback(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PUT\")] () =>" + LambdaBodyLeadingComment + "{ return 1; };");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Out_lambda_body_flow_preserves_original_nonordinary_feedback(bool local)
	{
		var source = Owner("private void Init() { var callback = [Htmxor.HtmxAction(\"PUT\")] (out int value) => 1; }");
		AssertCompilerErrorText(source, "CS0177", "1");
		var compilation = CreateCompilation(source);
		var tree = compilation.SyntaxTrees.Single();
		var lambda = Assert.Single(tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>());
		var attribute = Assert.Single(lambda.AttributeLists.SelectMany(list => list.Attributes));
		var error = Assert.Single(compilation.GetDiagnostics(), diagnostic => diagnostic.Id == "CS0177");
		Assert.Equal(lambda.Body.SpanStart, error.Location.SourceSpan.Start);
		Assert.Contains(compilation.GetSemanticModel(tree).GetDiagnostics(TextSpan.FromBounds(attribute.SpanStart, lambda.Body.SpanStart)),
			diagnostic => diagnostic.Id == "CS0177" && diagnostic.Location.SourceSpan == error.Location.SourceSpan);
		var throwSource = source.Replace("=> 1;", "=> throw null;", StringComparison.Ordinal);
		var throwCompilation = CreateCompilation(throwSource);
		var introduced = Assert.Single(throwCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id == "CS8917");
		var throwLambda = Assert.Single(throwCompilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>());
		Assert.Equal(throwLambda.Span, introduced.Location.SourceSpan);
		Assert.NotEqual(error.Id, introduced.Id);
		Assert.NotEqual(error.Location.SourceSpan, introduced.Location.SourceSpan);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0177");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Assigning_out_lambda_parameter_retains_nonordinary_feedback(bool local)
	{
		var source = Owner("private void Init() { var callback = [Htmxor.HtmxAction(\"PUT\")] (out int value) => (value = 1); }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Short_lambda_body_comment_does_not_capture_neighboring_body_error(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PUT\")] () =>\n // This comment belongs to the original body.\n { }; private void Neighbor() { Missing(); }");
		AssertCompilerErrorText(source, "CS1643", "=>");
		AssertCompilerErrorText(source, "CS0103", "Missing");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1643");

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Commented_lambda_and_valid_action_remain_visible_beside_malformed_neighbor(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PATCH\")] () =>" + LambdaBodyLeadingComment +
			"{ return 1; }; private Missing Neighbor() => null!; [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");
		AssertCompilerErrorText(source, "CS0246", "Missing");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0246");

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"), ("HTMXOR013", "is static"));
		Assert.Equal(source.IndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics, diagnostic => diagnostic.Id == "HTMXOR035").Location.SourceSpan.Start);
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics, diagnostic => diagnostic.Id == "HTMXOR013").Location.SourceSpan.Start);
	}

}
