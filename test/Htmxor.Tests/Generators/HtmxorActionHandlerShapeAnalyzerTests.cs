using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

/// <summary>
/// Handler-shape resolution for an inferred action binding (#308,
/// https://github.com/egil/Htmxor/issues/308#issuecomment-6040725984): Htmxor accepts exactly
/// <c>void M()</c>, <c>void M(HtmxEventArgs)</c>, <c>Task M()</c> and <c>Task M(HtmxEventArgs)</c>,
/// on an accessible instance method of the component or a base type, at any accessibility. Every
/// other shape that real Razor itself compiles fails with its own cause-specific HTMXOR002,
/// including an overload pair C# resolves to one applicable method, a member hidden with
/// <c>new</c>, and a generic method whose type parameter C# can infer from <c>HtmxEventArgs</c>. A
/// shape real Razor itself rejects (<c>ValueTask</c>, wrong parameter type, two or more parameters,
/// an uninferable generic, <c>ref</c>/<c>in</c>, ambiguous overloads) gets no Htmxor diagnostic at
/// all.
///
/// This seam compiles a real component (plus, where needed, a real base type) directly as C#, the
/// same way <see cref="HtmxorRouteDeclarationAnalyzerTests"/> does: handler resolution reads
/// compiled symbols, not Razor-scanned text, so only a real compiled member exercises it. Every
/// fixture also includes <see cref="BindMethod"/>, the exact call real Razor generates for
/// <c>@onput="M"</c>: that ties the "Razor decides" contract to real C# overload resolution against
/// the real <c>Microsoft.AspNetCore.Components</c> assembly, instead of letting a member-only
/// compilation accept a binding no real component could ever contain.
/// </summary>
public sealed class HtmxorActionHandlerShapeAnalyzerTests
{
	private const string RootNamespace = "Htmxor.Consumer";

	/// <summary>
	/// The exact call real Razor generates for <c>@onput="M"</c> (confirmed against the real
	/// <c>Microsoft.AspNetCore.Components</c> assembly's <c>_razor.g.cs</c> output). Every fixture
	/// below adds this to the class that owns the binding, so real C# overload resolution — not this
	/// suite's own guess — decides whether <c>M</c> is bindable.
	/// </summary>
	private const string BindMethod =
		"private void Bind() => " +
		"global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, M);";

	private const string HandlerRazorContent = """
		@page "/reports/{Id:int}"
		<button @onput="M">Save</button>
		""";

	private static readonly string ProjectDirectory = Path.GetFullPath(
		Path.Combine(Path.GetTempPath(), "htmxor-action-handler-shape-tests"));
	private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

	[Theory]
	[InlineData("void_no_param", "void M() { }")]
	[InlineData("void_event_args_param", "void M(global::Htmxor.HtmxEventArgs args) { }")]
	[InlineData(
		"task_no_param",
		"global::System.Threading.Tasks.Task M() => global::System.Threading.Tasks.Task.CompletedTask;")]
	public async Task Approved_handler_signature_on_the_component_is_supported(
		string scenario,
		string handlerMember)
	{
		var diagnostics = await RunForHandlerAsync($"private {handlerMember}");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The fourth approved signature, <c>Task M(HtmxEventArgs)</c>, is already covered by the
	/// existing accessibility and base-type fixtures below and elsewhere in this suite (#306, #307).
	/// </summary>
	[Theory]
	[InlineData("private_on_component", "private")]
	[InlineData("protected_on_component", "protected")]
	[InlineData("public_on_component", "public")]
	public async Task Approved_handler_accessibility_on_the_component_is_supported(
		string scenario,
		string accessibility)
	{
		var handlerMember =
			$"{accessibility} global::System.Threading.Tasks.Task M(global::Htmxor.HtmxEventArgs args) " +
			"=> global::System.Threading.Tasks.Task.CompletedTask;";

		var diagnostics = await RunForHandlerAsync(handlerMember);

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A protected base handler is already covered by the existing route-declaration suite
	/// (#306, #307); a public one gets its own row here because accessibility, not just presence of
	/// a base type, must never block resolution.
	/// </summary>
	[Fact]
	public async Task Public_handler_on_an_accessible_base_type_is_supported()
	{
		var diagnostics = await RunWithBaseHandlerAsync(
			"public global::System.Threading.Tasks.Task M(global::Htmxor.HtmxEventArgs args) " +
				"=> global::System.Threading.Tasks.Task.CompletedTask;");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A private handler on a base type is inaccessible from the derived, request-owned component,
	/// so real Razor itself rejects the binding at the call site (CS0122, confirmed by a real
	/// Htmxor.TestApp probe at `main` `bd14996` and independently by both test-contract reviewers):
	/// Htmxor adds no diagnostic for a case the Razor compiler already decided (#308's "Decision and
	/// correction"). AC3 ("reported as an error and not accepted") is met by Razor's own CS0122.
	/// </summary>
	[Fact]
	public async Task Private_handler_on_a_base_type_is_not_flagged_by_Htmxor()
	{
		var diagnostics = await RunWithBaseHandlerAsync(
			"private global::System.Threading.Tasks.Task M(global::Htmxor.HtmxEventArgs args) " +
				"=> global::System.Threading.Tasks.Task.CompletedTask;",
			expectedCompilerErrorCode: "CS0122");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Each shape real Razor itself compiles (confirmed by a real Htmxor.TestApp probe at `main`
	/// `bd14996`, recorded in the owner's scope decision) must fail with its own cause, never a
	/// shared catch-all: asserting the expected fragment's presence and every other fragment's
	/// absence (<see cref="AssertHandlerCauseSpecificMessage"/>) rules out a message that would pass
	/// regardless of which cause actually fired.
	/// </summary>
	public static IEnumerable<object[]> RejectedHandlerShapeCases() =>
		new (string Scenario, string HandlerMember, string ExpectedCauseFragment)[]
		{
			(
				"returns_a_value",
				"private global::System.Threading.Tasks.Task<int> M(global::Htmxor.HtmxEventArgs args) " +
					"=> global::System.Threading.Tasks.Task.FromResult(0);",
				"returns a value"),
			(
				"async_void",
				"private async void M(global::Htmxor.HtmxEventArgs args) " +
					"=> await global::System.Threading.Tasks.Task.Yield();",
				"async void"),
			(
				"event_args_parameter",
				"private void M(global::System.EventArgs args) { }",
				"parameter must be HtmxEventArgs"),
			(
				"object_parameter",
				"private void M(object args) { }",
				"parameter must be HtmxEventArgs"),
			(
				"optional_parameter",
				"private void M(global::Htmxor.HtmxEventArgs args = null!) { }",
				"optional parameter"),
			(
				"static_method",
				"private static void M() { }",
				"static"),
			(
				"resolvable_overloads",
				"private void M() { }\n\tprivate void M(int value) { }",
				"overloaded"),
			(
				"event_callback_field_member",
				"private readonly global::Microsoft.AspNetCore.Components.EventCallback<global::Htmxor.HtmxEventArgs> " +
					"M = default;",
				"not a method"),
			(
				"action_property_member",
				"private global::System.Action M { get; set; } = () => { };",
				"not a method"),
		}.Select(static scenario => new object[] { scenario.Scenario, scenario.HandlerMember, scenario.ExpectedCauseFragment });

	[Theory]
	[MemberData(nameof(RejectedHandlerShapeCases))]
	public async Task Rejected_handler_shape_fails_closed_with_its_own_cause(
		string scenario,
		string handlerMember,
		string expectedCauseFragment)
	{
		var diagnostics = await RunForHandlerAsync(handlerMember);

		AssertSingleCauseAtBinding(diagnostics, expectedCauseFragment);
	}

	/// <summary>
	/// A shape real Razor itself rejects at the binding gets no Htmxor diagnostic at all: the owner's
	/// amended scope decision (#308) is that Htmxor adds nothing for a case the Razor compiler
	/// already decided. Each row's fixture carries <see cref="BindMethod"/>, the real Razor-generated
	/// call, so real C# overload resolution produces the asserted compiler-error code (confirmed by
	/// the owner's real Htmxor.TestApp probe at `main` `bd14996`) instead of this suite assuming it.
	/// One row per rejection branch that sits beside a diagnosed shape: <c>ValueTask</c> and a
	/// non-<c>int</c>-return (CS1503, beside the diagnosed "returns a value" row), a non-
	/// <c>HtmxEventArgs</c>-compatible parameter (CS1503, beside the diagnosed <c>EventArgs</c>/
	/// <c>object</c> rows), two parameters (CS1503), an uninferable generic method with no parameter
	/// to infer a type argument from (CS1503; a generic method whose type parameter C# <em>can</em>
	/// infer from <c>HtmxEventArgs</c>, such as <c>M&lt;TArg&gt;(TArg)</c>, compiles instead and gets
	/// its own "generic" cause — see <see cref="Inferred_generic_handler_fails_closed_as_generic"/>),
	/// a <c>ref</c> or <c>in</c> parameter (CS1503), and an ambiguous overload pair (CS0121, beside
	/// the diagnosed resolvable overloads).
	/// </summary>
	public static IEnumerable<object[]> RazorRejectedHandlerShapeCases() =>
		new (string Scenario, string HandlerMember, string ExpectedCompilerErrorCode)[]
		{
			(
				"value_task",
				"private global::System.Threading.Tasks.ValueTask M(global::Htmxor.HtmxEventArgs args) => default;",
				"CS1503"),
			(
				"int_return",
				"private int M() => 0;",
				"CS1503"),
			(
				"int_parameter",
				"private void M(int value) { }",
				"CS1503"),
			(
				"two_parameters",
				"private void M(global::Htmxor.HtmxEventArgs args, int value) { }",
				"CS1503"),
			(
				"generic",
				"private void M<T>() { }",
				"CS1503"),
			(
				"ref_parameter",
				"private void M(ref global::Htmxor.HtmxEventArgs args) { }",
				"CS1503"),
			(
				"in_parameter",
				"private void M(in global::Htmxor.HtmxEventArgs args) { }",
				"CS1503"),
			(
				"ambiguous_overloads",
				"private void M() { }\n\tprivate void M(global::Htmxor.HtmxEventArgs args) { }",
				"CS0121"),
		}.Select(static scenario => new object[]
			{ scenario.Scenario, scenario.HandlerMember, scenario.ExpectedCompilerErrorCode });

	[Theory]
	[MemberData(nameof(RazorRejectedHandlerShapeCases))]
	public async Task Razor_rejected_handler_shape_is_not_flagged_by_Htmxor(
		string scenario,
		string handlerMember,
		string expectedCompilerErrorCode)
	{
		var diagnostics = await RunForHandlerAsync(handlerMember, expectedCompilerErrorCode);

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Amended AC2 requires "overloads Razor resolves to one method" to fail with HTMXOR002
	/// "overloaded" (#308), the same cause as the existing <c>M()</c> plus <c>M(int)</c> row. C#
	/// method-group conversion does not require every overload to be individually bindable on its
	/// own: it runs ordinary overload resolution and can pick the better candidate whenever more than
	/// one declared method converts to a <c>Create&lt;HtmxEventArgs&gt;</c> parameter type, so each of
	/// these compiles cleanly under <see cref="BindMethod"/> exactly like the existing row
	/// (complete-change review finding LR-5192587-P001).
	/// </summary>
	public static IEnumerable<object[]> ResolvableOverloadPairCases() =>
		new (string Scenario, string HandlerMember)[]
		{
			(
				"approved_plus_object",
				"private void M(global::Htmxor.HtmxEventArgs a) { }\n\tprivate void M(object a) { }"),
			(
				"approved_plus_event_args",
				"private void M(global::Htmxor.HtmxEventArgs a) { }\n\t" +
					"private void M(global::System.EventArgs a) { }"),
			(
				"void_plus_task_event_args",
				"private void M() { }\n\tprivate global::System.Threading.Tasks.Task M(" +
					"global::Htmxor.HtmxEventArgs a) => global::System.Threading.Tasks.Task.CompletedTask;"),
		}.Select(static scenario => new object[] { scenario.Scenario, scenario.HandlerMember });

	[Theory]
	[MemberData(nameof(ResolvableOverloadPairCases))]
	public async Task Resolvable_overload_pair_fails_closed_as_overloaded(
		string scenario,
		string handlerMember)
	{
		var diagnostics = await RunForHandlerAsync(handlerMember);

		AssertSingleCauseAtBinding(diagnostics, "overloaded");
	}

	/// <summary>
	/// The same "overloaded" cause applies across a base/derived pair: C# member lookup still
	/// resolves the method group to exactly one applicable candidate (the derived <c>M(object)</c>,
	/// because a same-named derived candidate that applies hides the base ones from overload
	/// resolution), so this compiles cleanly and is an "overload Razor resolves to one method" under
	/// amended AC2, not a silently accepted shape (LR-5192587-P001).
	/// </summary>
	[Fact]
	public async Task Resolvable_overload_pair_across_base_and_derived_fails_closed_as_overloaded()
	{
		var diagnostics = await RunWithBaseAndDerivedHandlerAsync(
			"public void M(global::Htmxor.HtmxEventArgs a) { }",
			"private void M(object a) { }");

		AssertSingleCauseAtBinding(diagnostics, "overloaded");
	}

	/// <summary>
	/// A derived member declared <c>new</c> hides the base member of the same name from C# lookup
	/// entirely (#308): Htmxor must classify the member C# actually binds, not every same-named
	/// member up the base chain. A rejected shape reached only through the derived, hiding member
	/// must still get its own cause (LR-5192587-P002/S002), the same way an un-hidden declaration
	/// would.
	/// </summary>
	[Fact]
	public async Task Hiding_member_with_a_rejected_shape_fails_closed_as_async_void()
	{
		var diagnostics = await RunWithBaseAndDerivedHandlerAsync(
			"public void M() { }",
			"private new async void M() => await global::System.Threading.Tasks.Task.Yield();");

		AssertSingleCauseAtBinding(diagnostics, "async void");
	}

	/// <summary>
	/// The same hiding rule for a value-returning handler: the base <c>Task M()</c> is hidden, and
	/// the derived <c>Task&lt;int&gt; M()</c> is the only member C# binds (LR-5192587-P002/S002).
	/// </summary>
	[Fact]
	public async Task Hiding_member_with_a_rejected_shape_fails_closed_as_returns_a_value()
	{
		var diagnostics = await RunWithBaseAndDerivedHandlerAsync(
			"public global::System.Threading.Tasks.Task M() => global::System.Threading.Tasks.Task.CompletedTask;",
			"private new global::System.Threading.Tasks.Task<int> M() " +
				"=> global::System.Threading.Tasks.Task.FromResult(1);");

		AssertSingleCauseAtBinding(diagnostics, "returns a value");
	}

	/// <summary>
	/// Hiding also runs the other way: a derived method declared <c>new</c> hides a non-method base
	/// member of the same name, so the approved method is the only member C# binds and must be
	/// accepted, not rejected as "not a method" for a base property that is no longer part of lookup
	/// (LR-5192587-P002/S002).
	/// </summary>
	[Fact]
	public async Task Hiding_a_non_method_base_member_with_an_approved_method_is_supported()
	{
		var diagnostics = await RunWithBaseAndDerivedHandlerAsync(
			"protected global::System.Action M { get; set; } = () => { };",
			"private new void M() { }");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// An <c>override</c> member is already resolved correctly today: <see cref="BindMethod"/>
	/// proves it compiles, and the analyzer's <c>WithoutOverridden</c> step removes the overridden
	/// base method from the candidate list, leaving only the derived override. This row pins that
	/// behavior as a regression guard: the complete-change review found that removing
	/// <c>WithoutOverridden</c> left every existing test green (LR-5192587-S004), so nothing before
	/// this row would have caught a regression here.
	/// </summary>
	[Fact]
	public async Task Override_of_a_virtual_base_handler_fails_closed_as_async_void()
	{
		var diagnostics = await RunWithBaseAndDerivedHandlerAsync(
			"public virtual void M(global::Htmxor.HtmxEventArgs a) { }",
			"public override async void M(global::Htmxor.HtmxEventArgs a) " +
				"=> await global::System.Threading.Tasks.Task.Yield();");

		AssertSingleCauseAtBinding(diagnostics, "async void");
	}

	/// <summary>
	/// A generic method whose type parameter C# can infer from <c>HtmxEventArgs</c> is not the same
	/// as the Razor-rejected, uninferable <c>M&lt;T&gt;()</c> row above: method-group conversion
	/// infers the type argument from the delegate's own parameter type, so
	/// <c>M&lt;TArg&gt;(TArg)</c> binds with <c>TArg = HtmxEventArgs</c> and compiles cleanly under
	/// <see cref="BindMethod"/>. A generic handler is still outside the four approved signatures, so
	/// it needs its own "generic" cause rather than being silently accepted (LR-5192587-S003).
	/// </summary>
	[Fact]
	public async Task Inferred_generic_handler_fails_closed_as_generic()
	{
		var diagnostics = await RunForHandlerAsync("private void M<TArg>(TArg a) { }");

		AssertSingleCauseAtBinding(diagnostics, "generic");
	}

	/// <summary>
	/// A method-group conversion exists only through an identity or implicit reference conversion on
	/// the method's parameter type (#308): a user-defined implicit conversion from
	/// <c>HtmxEventArgs</c> does not make the method a valid <c>Create&lt;HtmxEventArgs&gt;</c>
	/// candidate, so real Razor itself rejects this binding and Htmxor must add nothing, the same
	/// "Razor decides" contract as the other Razor-rejected rows above (LR-5192587-S005).
	/// </summary>
	[Fact]
	public async Task Parameter_type_with_a_user_defined_conversion_is_not_flagged_by_Htmxor()
	{
		var diagnostics = await RunForHandlerAsync(
			"private sealed class Wrapper { public static implicit operator Wrapper(" +
				"global::Htmxor.HtmxEventArgs a) => new Wrapper(); }\n" +
				"\tprivate void M(Wrapper w) { }",
			expectedCompilerErrorCode: "CS0123");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A static method reached only through a metadata assembly's <c>using static</c> import (here,
	/// <c>System.Console.Beep</c>) must still be classified "not a member of the component", the same
	/// as a source-declared import (#308's "Decision and correction"):
	/// <c>Compilation.GetSymbolsWithName</c> only returns source-declared symbols, so a purely
	/// metadata import is a regression distinct from the source-declared imported-static pins in
	/// <see cref="HtmxorRouteDeclarationAnalyzerTests"/> (LR-5192587-P003).
	/// </summary>
	[Fact]
	public async Task Imported_static_handler_from_metadata_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			global using static System.Console;

			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private void Bind() => global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, Beep);
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @onput="Beep">Save</button>
			""";
		var razor = new SourceAdditionalText(ComponentPath("ReportComponent.razor"), razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'Beep' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}

		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@onput", StringComparison.Ordinal), "@onput".Length),
			diagnostic.Location.SourceSpan);
	}

	/// <summary>
	/// An unrelated accessible static method of the same name elsewhere in source, with no
	/// <c>using static</c> bringing it into scope and no member of that name anywhere on the
	/// component's own chain, must not add a second, misleading HTMXOR002 alongside Razor's own
	/// CS0103 (#308's duplicate-diagnostic rule, #307 comment 6039651773):
	/// <c>HasImportableStaticMethod</c> must not treat "an accessible static method exists somewhere
	/// in source" as "is in scope for this binding" (LR-5192587-P004).
	/// </summary>
	[Fact]
	public async Task Missing_handler_with_an_unrelated_unimported_static_of_the_same_name_is_not_flagged_by_Htmxor()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			public abstract class ReportComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase;

			public static class UnrelatedHelpers
			{
				public static void M() { }
			}

			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : ReportComponentBase
			{
				{{BindMethod}}
			}
			}
			""";
		var razor = new SourceAdditionalText(componentPath, HandlerRazorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor, expectedCompilerErrorCode: "CS0103");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A lambda or closure, a method call, and a conditional are the #307 value-grammar causes; the
	/// handler-shape causes pinned by #308 are a disjoint set, so this list deliberately only needs to
	/// rule out other handler-shape causes, not the value-grammar ones (a value-grammar cause can
	/// never fire here: every fixture below binds a plain method-group identifier). "Not accessible"
	/// is not a cause any production code path reports — the owner's "Decision and correction" has
	/// Razor's own CS0122 cover an inaccessible handler, so Htmxor never needs its own cause for it
	/// (complete-change review finding LR-5192587-S006) — and is therefore absent from this list.
	///
	/// This is the one owner of the cause-fragment list; <see cref="HtmxorRouteDeclarationAnalyzerTests"/>
	/// references it directly instead of keeping its own copy, so a cause rename or addition cannot
	/// silently weaken one file's absence check while the other is updated.
	/// </summary>
	internal static readonly string[] HandlerShapeCauseFragments =
	{
		"returns a value", "async void", "parameter must be HtmxEventArgs", "optional parameter",
		"static", "overloaded", "not a method", "generic",
	};

	internal static void AssertHandlerCauseSpecificMessage(Diagnostic diagnostic, string expectedFragment)
	{
		var message = diagnostic.GetMessage();
		Assert.Contains(expectedFragment, message, StringComparison.Ordinal);
		foreach (var otherFragment in HandlerShapeCauseFragments)
		{
			if (!string.Equals(otherFragment, expectedFragment, StringComparison.Ordinal))
			{
				Assert.DoesNotContain(otherFragment, message, StringComparison.Ordinal);
			}
		}
	}

	/// <summary>
	/// Shared assertion for every cause-specific HTMXOR002 pinned in this file: the ID, severity, the
	/// non-configurable tag, the binding's own <c>.razor</c> path and <c>@onput</c> span (never the
	/// member's own declaration location), and the exclusive cause fragment.
	/// </summary>
	private static void AssertSingleCauseAtBinding(ImmutableArray<Diagnostic> diagnostics, string expectedCauseFragment)
	{
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(ComponentPath("ReportComponent.razor"), diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(HandlerRazorContent.IndexOf("@onput", StringComparison.Ordinal), "@onput".Length),
			diagnostic.Location.SourceSpan);
		AssertHandlerCauseSpecificMessage(diagnostic, expectedCauseFragment);
	}

	private static string ComponentPath(string relativePath) => Path.Combine(ProjectDirectory, relativePath);

	private static async Task<ImmutableArray<Diagnostic>> RunForHandlerAsync(
		string handlerMember,
		string? expectedCompilerErrorCode = null)
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				{{handlerMember}}

				{{BindMethod}}
			}
			}
			""";
		var razor = new SourceAdditionalText(componentPath, HandlerRazorContent);

		return await RunActionAnalyzerAsync(source, razor, expectedCompilerErrorCode);
	}

	private static async Task<ImmutableArray<Diagnostic>> RunWithBaseHandlerAsync(
		string baseHandlerMember,
		string? expectedCompilerErrorCode = null)
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			public abstract class ReportComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				{{baseHandlerMember}}
			}

			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : ReportComponentBase
			{
				{{BindMethod}}
			}
			}
			""";
		var razor = new SourceAdditionalText(componentPath, HandlerRazorContent);

		return await RunActionAnalyzerAsync(source, razor, expectedCompilerErrorCode);
	}

	/// <summary>
	/// Declares a base member and a separate derived member under the same name, for hiding,
	/// overload-resolution-across-inheritance, and override fixtures: <see cref="RunWithBaseHandlerAsync"/>
	/// only declares one member, which cannot express "the base has one shape and the derived
	/// component adds a second, different declaration of the same name".
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunWithBaseAndDerivedHandlerAsync(
		string baseMember,
		string derivedMember,
		string? expectedCompilerErrorCode = null)
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			public abstract class ReportComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				{{baseMember}}
			}

			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : ReportComponentBase
			{
				{{derivedMember}}

				{{BindMethod}}
			}
			}
			""";
		var razor = new SourceAdditionalText(componentPath, HandlerRazorContent);

		return await RunActionAnalyzerAsync(source, razor, expectedCompilerErrorCode);
	}

	/// <summary>
	/// Compiles the fixture (which always carries <see cref="BindMethod"/>, the real Razor-generated
	/// binding call) and asserts real C#'s own verdict first: a clean compile when
	/// <paramref name="expectedCompilerErrorCode"/> is <see langword="null"/> (every shape real Razor
	/// itself compiles), or that exact compiler-error code when it is not (every shape real Razor
	/// itself rejects). Only then does it run the Htmxor analyzer, so a passing case can never be the
	/// product of this suite re-deriving C# binding rules on its own.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunActionAnalyzerAsync(
		string source,
		AdditionalText razor,
		string? expectedCompilerErrorCode = null)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.ActionHandlerShapeAnalyzer.Tests",
			new[] { CSharpSyntaxTree.ParseText(source, parseOptions, RazorGeneratedPath("ReportComponent")) },
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var compilerErrors = compilation.GetDiagnostics()
			.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.ToImmutableArray();
		if (expectedCompilerErrorCode is null)
		{
			Assert.Empty(compilerErrors);
		}
		else
		{
			Assert.Contains(compilerErrors, diagnostic => diagnostic.Id == expectedCompilerErrorCode);
		}

		var analyzerOptions = new AnalyzerOptions(
			ImmutableArray.Create(razor),
			new TestAnalyzerConfigOptionsProvider(ProjectDirectory));

		return await compilation
			.WithAnalyzers(
				ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorActionDeclarationAnalyzer()),
				analyzerOptions)
			.GetAnalyzerDiagnosticsAsync();
	}

	private static string RazorGeneratedPath(string componentName)
		=> Path.Combine(
			"Microsoft.CodeAnalysis.Razor.Compiler",
			"Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator",
			componentName + "_razor.g.cs");

	private static ImmutableArray<MetadataReference> CreateReferences()
	{
		var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
			.Split(Path.PathSeparator) ?? Array.Empty<string>();
		var requiredAssemblies = new[]
		{
			typeof(object).Assembly.Location,
			typeof(ComponentBase).Assembly.Location,
			typeof(HtmxRouteAttribute).Assembly.Location,
		};

		return platformAssemblies
			.Concat(requiredAssemblies)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();
	}

	private sealed class SourceAdditionalText(string path, string content) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> SourceText.From(content);
	}

	private sealed class TestAnalyzerConfigOptionsProvider(string projectDirectory)
		: AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal));

		public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["build_property.RootNamespace"] = RootNamespace,
				["build_property.MSBuildProjectDirectory"] = projectDirectory,
			});

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;
	}

	private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
		: AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
			=> values.TryGetValue(key, out value!);
	}
}
