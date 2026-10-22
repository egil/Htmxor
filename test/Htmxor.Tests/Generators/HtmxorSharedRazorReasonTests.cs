using Htmxor.Builder;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorSharedRazorReasonTests
{
	public static TheoryData<string, string, string> InferredCases()
	{
		var result = new TheoryData<string, string, string>();
		result.Add("Allowlist", "033", "explicit HtmxRoute.Methods is authoritative and does not allow the PUT binding; add the method or remove the binding");
		result.Add("Normal", "034", "an inferred binding on a component marked with DisableHtmxDirectRouting is not supported; remove the binding or marker");
		result.Add("LocalPage", "038", "a component with a local @page action must compile with exactly one stock route and no HtmxRoute");
		result.Add("Unrouted", "014", "the component owns no route, so the binding has no action to declare; add @page or HtmxRoute, or remove the binding");
		result.Add("StockRoute", "039", "a component action without a local @page cannot use a compiled stock route");
		return result;
	}

	[Theory]
	[MemberData(nameof(InferredCases))]
	public async Task Compiler_rejected_inferred_owner_emits_the_canonical_reason(string fixture, string id, string expected)
	{
		var result = await SharedRejectionReasonTestHost.Razor("InferredAction", fixture, "/tmp/issue338-razor-capture");
		var diagnostic = Assert.Single(result.Diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR" + id));
		SharedRejectionReasonTestHost.AssertDiagnostic(diagnostic, expected);
		Assert.Equal("/tmp/issue338-razor-capture/" + fixture + ".razor", diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Theory]
	[MemberData(nameof(InferredCases))]
	public async Task Runtime_rejected_inferred_owner_emits_the_canonical_reason(string fixture, string id, string expected)
	{
		var result = await SharedRejectionReasonTestHost.Razor("InferredAction", fixture, "/tmp/issue338-razor-capture");
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(result.Compilation, "Consumer." + fixture);
		var action = new HtmxorGeneratedComponentAction(owner, "PUT", "Consumer." + fixture + ".PUT.M", fixture is "Normal" or "LocalPage");
		var exception = Assert.Throws<InvalidOperationException>(() => RejectInferred(fixture, assembly, owner, action));
		AssertInferredException(fixture, exception, action, expected);
	}

	[Fact]
	public async Task Compiler_named_POST_form_outside_supported_allowlist_emits_the_canonical_reason()
	{
		const string expected = "explicit HtmxRoute.Methods does not allow POST for its form; add POST or remove the form";
		var result = await SharedRejectionReasonTestHost.Razor("FormDiagnostic", "NamedConflict", "/tmp/issue339-form-capture");
		var diagnostic = Assert.Single(result.Diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR033" && diagnostic.GetMessage().Contains("for its form", StringComparison.Ordinal)));
		SharedRejectionReasonTestHost.AssertDiagnostic(diagnostic, expected);
		Assert.Equal("/tmp/issue339-form-capture/NamedConflict.razor", diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Runtime_named_POST_form_outside_supported_allowlist_emits_the_canonical_reason()
	{
		const string expected = "explicit HtmxRoute.Methods does not allow POST for its form; add POST or remove the form";
		var result = await SharedRejectionReasonTestHost.Razor("FormDiagnostic", "NamedConflict", "/tmp/issue339-form-capture");
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(result.Compilation, "Consumer.NamedConflict");
		var exception = Assert.Throws<InvalidOperationException>(() => HtmxorAttributedRouteCatalog.Build(assembly, [owner.FullName!], [], [owner.FullName!]));
		SharedRejectionReasonTestHost.AssertException(exception, expected);
	}

	[Fact]
	public async Task Compiler_named_form_with_generated_POST_emits_the_canonical_reason()
	{
		const string expected = "named forms and a POST action cannot share one route owner; use named forms or a POST action";
		var result = await SharedRejectionReasonTestHost.Razor("FormDiagnostic", "NamedConflict", "/tmp/issue339-form-capture");
		SharedRejectionReasonTestHost.AssertDiagnostic(Assert.Single(result.Diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR032")), expected);
	}

	[Fact]
	public async Task Runtime_named_form_with_generated_POST_emits_the_canonical_reason()
	{
		const string expected = "named forms and a POST action cannot share one route owner; use named forms or a POST action";
		var result = await SharedRejectionReasonTestHost.Razor("FormDiagnostic", "NamedConflict", "/tmp/issue339-form-capture");
		var (_, owner) = SharedRejectionReasonTestHost.Emit(result.Compilation, "Consumer.NamedConflict");
		var action = new HtmxorGeneratedComponentAction(owner, "POST", "Consumer.NamedConflict.POST.Save", false);
		var exception = Assert.Throws<InvalidOperationException>(() => HtmxorNamedFormCatalog.Validate([owner.FullName!], [owner.FullName!], [action]));
		AssertReasonAfter(exception, $"Component '{owner.FullName}' ", expected);
	}

	[Fact]
	public async Task Compiler_named_form_with_explicit_POST_emits_the_canonical_reason()
	{
		const string expected = "named forms and a POST HtmxAction cannot share one route owner; choose named forms or the action";
		var result = await SharedRejectionReasonTestHost.Razor("FormDiagnostic", "ConsumerValidEdit", "/tmp/issue339-consumer-capture");
		var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR032" && diagnostic.GetMessage().Contains("POST HtmxAction", StringComparison.Ordinal));
		SharedRejectionReasonTestHost.AssertDiagnostic(Assert.Single(diagnostics), expected);
	}

	[Fact]
	public async Task Runtime_named_form_with_explicit_POST_emits_the_canonical_reason()
	{
		const string expected = "named forms and a POST HtmxAction cannot share one route owner; choose named forms or the action";
		var result = await SharedRejectionReasonTestHost.Razor("FormDiagnostic", "ConsumerValidEdit", "/tmp/issue339-consumer-capture");
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(result.Compilation, "Consumer.ConsumerValidEdit");
		var exception = Assert.Throws<InvalidOperationException>(() => HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], [owner.FullName!]));
		SharedRejectionReasonTestHost.AssertException(exception, expected);
	}

	private static void RejectInferred(string fixture, System.Reflection.Assembly assembly, Type owner, HtmxorGeneratedComponentAction action)
	{
		if (fixture == "Allowlist") _ = HtmxorAttributedRouteCatalog.Build(assembly, [owner.FullName!], [action]);
		else HtmxorGeneratedComponentActionCatalog.Validate(assembly, [owner.FullName!], [action]);
	}

	private static void AssertInferredException(string fixture, InvalidOperationException exception, HtmxorGeneratedComponentAction action, string expected)
	{
		if (fixture == "Allowlist") SharedRejectionReasonTestHost.AssertException(exception, expected);
		else AssertReasonAfter(exception, $"Generated component action '{action.HandlerIdentity}' on component '{action.ComponentType.FullName}' ", expected);
	}

	private static void AssertReasonAfter(InvalidOperationException exception, string prefix, string expected)
	{
		Assert.StartsWith(prefix, exception.Message, StringComparison.Ordinal);
		Assert.Equal(expected, exception.Message[prefix.Length..].TrimEnd('.'));
	}
}
