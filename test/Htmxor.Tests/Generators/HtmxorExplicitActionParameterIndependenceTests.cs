namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_preserves_both_independent_wrong_parameter_types(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(int first, string second) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR025", "not HtmxEventArgs"), ("HTMXOR025", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData("int first, Htmxor.HtmxEventArgs second", false)]
	[InlineData("int first, Htmxor.HtmxEventArgs second", true)]
	[InlineData("Htmxor.HtmxEventArgs first, string second", false)]
	[InlineData("Htmxor.HtmxEventArgs first, string second", true)]
	[InlineData("Htmxor.HtmxEventArgs args, int value", false)]
	[InlineData("Htmxor.HtmxEventArgs args, int value", true)]
	public async Task Excess_arity_and_one_remaining_type_mismatch_are_both_reported(string parameters, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + parameters + ") { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"), ("HTMXOR025", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData("int first, ref Htmxor.HtmxEventArgs second", false)]
	[InlineData("int first, ref Htmxor.HtmxEventArgs second", true)]
	[InlineData("ref Htmxor.HtmxEventArgs first, string second", false)]
	[InlineData("ref Htmxor.HtmxEventArgs first, string second", true)]
	public async Task Excess_arity_preserves_wrong_type_and_separate_ref_parameter(string parameters, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + parameters + ") { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR025", "not HtmxEventArgs"), ("HTMXOR023", "by-value HtmxEventArgs"));
	}

	[Theory]
	[InlineData("string first, Htmxor.HtmxEventArgs second = null", false)]
	[InlineData("string first, Htmxor.HtmxEventArgs second = null", true)]
	[InlineData("Htmxor.HtmxEventArgs first, string second = null", false)]
	[InlineData("Htmxor.HtmxEventArgs first, string second = null", true)]
	public async Task Excess_arity_preserves_wrong_type_and_optional_parameter(string parameters, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + parameters + ") { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR025", "not HtmxEventArgs"), ("HTMXOR024", "optional parameter"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_preserves_both_types_ref_and_optional_causes(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(ref int first, string second = null) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR025", "not HtmxEventArgs"), ("HTMXOR025", "not HtmxEventArgs"),
			("HTMXOR023", "by-value HtmxEventArgs"), ("HTMXOR024", "optional parameter"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Excess_arity_preserves_existing_specific_parameter_type_IDs(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(System.EventArgs first, object second) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "zero parameters or one required"),
			("HTMXOR020", "not HtmxEventArgs"), ("HTMXOR021", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData("int first", false)]
	[InlineData("int first", true)]
	[InlineData("string second", false)]
	[InlineData("string second", true)]
	public async Task Removing_either_wrong_parameter_leaves_the_other_type_mismatch(string parameter, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + parameter + ") { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "not HtmxEventArgs"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repairing_arity_keeps_wrong_type_and_ref_cause(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(ref int first) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "not HtmxEventArgs"), ("HTMXOR023", "by-value HtmxEventArgs"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repairing_arity_keeps_wrong_type_and_optional_cause(bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private void Save(string second = null) { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		AssertFeedback(diagnostics, source, ("HTMXOR025", "not HtmxEventArgs"), ("HTMXOR024", "optional parameter"));
	}
}
