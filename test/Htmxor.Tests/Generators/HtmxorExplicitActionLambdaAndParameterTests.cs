namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Lambda_missing_return_preserves_nonordinary_feedback(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PUT\")] () => { };");
		AssertCompilerErrorText(source, "CS1643", "=>");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS1643");

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repairing_lambda_return_preserves_nonordinary_feedback(bool local)
	{
		var source = Owner("private System.Func<int> Factory() => [Htmxor.HtmxAction(\"PUT\")] () => { return 1; };");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_preserves_independent_ref_and_optional_causes(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(ref Htmxor.HtmxEventArgs first, Htmxor.HtmxEventArgs second = null) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR023", "by-value HtmxEventArgs"), ("HTMXOR024", "optional parameter"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_preserves_each_ref_parameter_cause(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(ref Htmxor.HtmxEventArgs first, ref Htmxor.HtmxEventArgs second) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR023", "by-value HtmxEventArgs"), ("HTMXOR023", "by-value HtmxEventArgs"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_preserves_each_optional_parameter_cause(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(Htmxor.HtmxEventArgs first = null, Htmxor.HtmxEventArgs second = null) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR024", "optional parameter"), ("HTMXOR024", "optional parameter"));
	}

	[Theory]
	[InlineData("ref Htmxor.HtmxEventArgs first", "HTMXOR023", "by-value HtmxEventArgs", false)]
	[InlineData("ref Htmxor.HtmxEventArgs first", "HTMXOR023", "by-value HtmxEventArgs", true)]
	[InlineData("Htmxor.HtmxEventArgs second = null", "HTMXOR024", "optional parameter", false)]
	[InlineData("Htmxor.HtmxEventArgs second = null", "HTMXOR024", "optional parameter", true)]
	public async Task Single_parameter_repairs_retain_the_remaining_cause(string parameter, string id, string reason, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + parameter + ") { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, (id, reason));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_with_bound_required_value_parameters_reports_only_arity(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(Htmxor.HtmxEventArgs first, Htmxor.HtmxEventArgs second) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"));
	}
}
