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
	/// An <c>override</c> of a virtual base handler is the member C# binds, so its own shape decides
	/// the cause: an approved virtual base method does not make an <c>async void</c> override
	/// acceptable.
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
	/// as a source-declared import (#308's "Decision and correction"): the imported method is not a
	/// member of the component at all, whether declared in source or in metadata, which is distinct
	/// from the source-declared imported-static pins in <see cref="HtmxorRouteDeclarationAnalyzerTests"/>.
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
	/// CS0103 (#308's duplicate-diagnostic rule, #307 comment 6039651773): an accessible static method
	/// existing somewhere in source is not the same as being in scope for this binding.
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
	/// Production finds Razor's own generated <c>Create</c> call only in the Razor-generated
	/// declaration, never in a code-behind file. A nested type in the code-behind that happens to
	/// declare a same-named, approved-shaped method (here, a plain <c>void Save()</c> on a nested
	/// <c>Row</c>) is never searched at all: the real handler is the Razor tree's <c>async void
	/// Save</c>, and that is the shape Htmxor must classify (LR-a6f7218-P001/S001).
	/// </summary>
	[Fact]
	public async Task Nested_type_in_code_behind_does_not_steal_the_bind_anchor_from_the_razor_tree()
	{
		var diagnostics = await RunWithCodeBehindAsync(
			codeBehindUsings: "",
			codeBehindMembers: "private sealed class Row { public void Save() { } }",
			codeBehindExtra: "",
			razorUsings: "",
			razorMembers: "private async void Save(global::Htmxor.HtmxEventArgs a) " +
				"=> await global::System.Threading.Tasks.Task.Yield();\n\n\t\t" +
				"private void Bind() => global::Microsoft.AspNetCore.Components.EventCallback.Factory." +
				"Create<global::Htmxor.HtmxEventArgs>(this, Save);",
			razorExtra: "",
			handlerValue: "Save");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		AssertHandlerCauseSpecificMessage(diagnostic, "async void");
	}

	/// <summary>
	/// A <c>using static</c> declared only in the code-behind file is not in scope where Razor itself
	/// binds the handler, so real Razor's own build already fails (CS0103) when the component has no
	/// <c>M</c> of its own. Htmxor must add nothing: the owner's "Razor decides" rule, not a second,
	/// misleading diagnostic from reading the Razor-generated call's own scope correctly
	/// (LR-a6f7218-P001/S001).
	/// </summary>
	[Fact]
	public async Task Using_static_declared_only_in_the_code_behind_is_not_flagged_by_Htmxor()
	{
		var diagnostics = await RunWithCodeBehindAsync(
			codeBehindUsings: "using static " + RootNamespace + ".Helpers;",
			codeBehindMembers: "private void Helper() { }",
			codeBehindExtra: "",
			razorUsings: "",
			razorMembers: BindMethod,
			razorExtra: "public static class Helpers { public static void M() { } }",
			handlerValue: "M",
			expectedCompilerErrorCode: "CS0103");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The mirror of the row above: a <c>using static</c> declared only in the Razor-generated tree
	/// (what Razor itself emits for a page-level <c>@using static</c>) is exactly where Razor binds
	/// the handler, so this compiles cleanly and the imported static is not a member of the component
	/// — the owner's decision keeps the generic "request-owned component" message. Production must
	/// not go silent just because a code-behind file also exists and happens to hold the first
	/// instance method across every tree (LR-a6f7218-P001/S001).
	/// </summary>
	[Fact]
	public async Task Using_static_declared_only_in_the_razor_tree_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var diagnostics = await RunWithCodeBehindAsync(
			codeBehindUsings: "",
			codeBehindMembers: "private void Helper() { }",
			codeBehindExtra: "",
			razorUsings: "using static " + RootNamespace + ".Helpers;",
			razorMembers: BindMethod,
			razorExtra: "public static class Helpers { public static void M() { } }",
			handlerValue: "M");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'M' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// The code-behind's first instance method declares a parameter named <c>M</c>. Production never
	/// looks at the code-behind at all: it reads Razor's own generated <c>Create</c> call directly out
	/// of the Razor-generated declaration, so a same-named parameter in a different file's method can
	/// never be in scope for it. The Razor tree's actual handler, <c>Task&lt;int&gt; M()</c>, is what
	/// Razor itself binds and is the shape Htmxor must classify (LR-a6f7218-P001/S001). A same-named
	/// local inside the first method's body has the identical effect, for the same reason.
	/// </summary>
	[Fact]
	public async Task Code_behind_parameter_sharing_the_handlers_name_does_not_shadow_the_razor_tree_handler()
	{
		var diagnostics = await RunWithCodeBehindAsync(
			codeBehindUsings: "",
			codeBehindMembers: "private void Load(int M) { }",
			codeBehindExtra: "",
			razorUsings: "",
			razorMembers: "private global::System.Threading.Tasks.Task<int> M() " +
				"=> global::System.Threading.Tasks.Task.FromResult(1);\n\n\t\t" + BindMethod,
			razorExtra: "",
			handlerValue: "M");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		AssertHandlerCauseSpecificMessage(diagnostic, "returns a value");
	}

	/// <summary>
	/// <c>@onput="this.M"</c> is an approved slice-2 spelling (#307), and Razor emits
	/// <c>Create&lt;HtmxEventArgs&gt;(this, this.M)</c> for it — member access through <c>this</c>,
	/// not a simple name. A static <c>M</c> on the component is not a valid target of <c>this.M</c>
	/// (CS1503: member access requires an instance), so real Razor already rejects this binding.
	/// Production reads the exact spelling Razor itself generated, not the simple name
	/// <c>RazorBindingValue.TryReadHandler</c> reduces it to, or it would add a second, misleading
	/// diagnostic for a binding Razor already rejected (LR-a6f7218-P002).
	/// </summary>
	[Fact]
	public async Task This_qualified_static_handler_is_not_flagged_by_Htmxor()
	{
		var diagnostics = await RunForThisQualifiedHandlerAsync(
			"private static void M() { }",
			usings: "",
			expectedCompilerErrorCode: "CS1503");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The same spelling rule for an imported static: member access through <c>this</c> never sees a
	/// <c>using static</c> import (CS1061: <c>ReportComponent</c> has no member <c>M</c>), so real
	/// Razor already rejects <c>this.M</c> here too, even though the simple name <c>M</c> would have
	/// bound to the import. Htmxor must add nothing (LR-a6f7218-P002).
	/// </summary>
	[Fact]
	public async Task This_qualified_imported_static_handler_is_not_flagged_by_Htmxor()
	{
		var diagnostics = await RunForThisQualifiedHandlerAsync(
			"",
			usings: "global using static " + RootNamespace + ".Helpers;\n\n" +
				"namespace " + RootNamespace + "\n{\npublic static class Helpers { public static void M() { } }\n}\n",
			expectedCompilerErrorCode: "CS1061");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// <c>@onput="this.M"</c> reaches the same shape rules as <c>M</c>: an <c>async void</c> instance
	/// handler written through <c>this</c> compiles in Razor, so Htmxor must still fail closed with
	/// its cause.
	/// </summary>
	[Fact]
	public async Task This_qualified_async_void_handler_fails_closed_as_async_void()
	{
		var diagnostics = await RunForThisQualifiedHandlerAsync(
			"private async void M() => await global::System.Threading.Tasks.Task.Yield();",
			usings: "");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		AssertHandlerCauseSpecificMessage(diagnostic, "async void");
	}

	/// <summary>
	/// Razor binds the handler name at the attribute's own position in <c>BuildRenderTree</c>, not at
	/// the top of the method: inside a <c>@foreach</c>, that position is the nested block the loop
	/// becomes. A loop variable with no matching component member is a non-member handler, the same
	/// "request-owned component" cause as any other name that is not an instance member — not a
	/// silent accept (LR-72dcffe-P001).
	/// </summary>
	[Fact]
	public async Task Foreach_loop_variable_with_no_component_member_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "save",
			members: "private global::Microsoft.AspNetCore.Components.EventCallback<global::Htmxor.HtmxEventArgs>[] Saves " +
				"{ get; } = new global::Microsoft.AspNetCore.Components.EventCallback<global::Htmxor.HtmxEventArgs>[0];",
			wrapOpen: "foreach (var save in Saves)\n\t\t\t{",
			wrapClose: "}");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'save' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// The same nested-scope rule applies when the loop variable happens to share its name with an
	/// approved component member: C# resolves the bare name inside the loop body to the local loop
	/// variable, never to the member, so the binding is still to a non-member and still gets the
	/// generic cause — not the member's own (approved) shape (LR-72dcffe-P001).
	/// </summary>
	[Fact]
	public async Task Foreach_loop_variable_shadowing_an_approved_component_member_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "save",
			members: "private global::Microsoft.AspNetCore.Components.EventCallback<global::Htmxor.HtmxEventArgs>[] Saves " +
				"{ get; } = new global::Microsoft.AspNetCore.Components.EventCallback<global::Htmxor.HtmxEventArgs>[0];\n\n\t\t" +
				"private void save() { }",
			wrapOpen: "foreach (var save in Saves)\n\t\t\t{",
			wrapClose: "}");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'save' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// A component can bind more than one handler, so more than one Razor-generated
	/// <c>Create&lt;HtmxEventArgs&gt;</c> call can exist in the same declaration. Production must find
	/// the one call whose argument text matches this binding's handler, not merely "the first such call
	/// in the file": an unrelated, earlier binding to an approved, differently-named handler must never
	/// substitute for the real, differently-shaped handler this declaration names. This row's two calls
	/// bind different handler names (<c>Other</c> and <c>M</c>), so only the text match distinguishes
	/// them — it says nothing about line- or column-mapped selection between two calls that bind the
	/// <em>same</em> text, which is a separate risk (see the <c>#line</c>-mapped rows above). A
	/// green-finalization mutation sweep found no existing row with more than one <c>Create</c> call in
	/// one declaration to catch a regression here, so this row is a deliberate regression guard, not
	/// evidence of a defect.
	/// </summary>
	[Fact]
	public async Task Multiple_create_calls_in_one_declaration_resolve_by_the_binding_not_by_order()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private void Other() { }\n\n\t\tprivate async void M(global::Htmxor.HtmxEventArgs a) " +
				"=> await global::System.Threading.Tasks.Task.Yield();",
			wrapOpen: "__builder.OpenElement(5, \"button\");\n\t\t\t__builder.AddAttribute(6, \"onput\", " +
				"global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, Other));\n\t\t\t__builder.CloseElement();");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		AssertHandlerCauseSpecificMessage(diagnostic, "async void");
	}

	/// <summary>
	/// Real Razor copies a parenthesized or whitespace-separated approved spelling into the generated
	/// call exactly as written: <c>@onput="(M)"</c> emits <c>Create&lt;HtmxEventArgs&gt;(this, (M))</c>,
	/// and <c>@onput="this . M"</c> emits <c>Create&lt;HtmxEventArgs&gt;(this, this . M)</c> — neither
	/// the parentheses nor the extra whitespace disappear. Both are approved #307 spellings (the
	/// <c>paren_without_at</c> row in <see cref="HtmxorActionGeneratorTests.ApprovedSpellingCases"/>
	/// pins <c>(M)</c> specifically), so an <c>async void</c> handler reached through either spelling
	/// must still fail closed with its own cause, the same as the plain <c>M</c> spelling
	/// (LR-88fa9d8-S001/P001).
	/// </summary>
	[Theory]
	[InlineData("(M)")]
	[InlineData("this . M")]
	public async Task Approved_spelling_of_an_async_void_handler_fails_closed_as_async_void(string handlerValue)
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: handlerValue,
			members: "private async void M(global::Htmxor.HtmxEventArgs a) " +
				"=> await global::System.Threading.Tasks.Task.Yield();");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		AssertHandlerCauseSpecificMessage(diagnostic, "async void");
	}

	/// <summary>
	/// One element can bind the same handler name to a DOM event and to an htmx verb
	/// (<c>&lt;input @onchange="M" @onput="M" /&gt;</c>), so the Razor-generated declaration carries
	/// two <c>Create</c> calls with identical argument text but different type arguments
	/// (<c>Create&lt;ChangeEventArgs&gt;</c> for <c>onchange</c>, <c>Create&lt;HtmxEventArgs&gt;</c> for
	/// <c>onput</c>). <c>void M(ChangeEventArgs)</c> satisfies the <c>onchange</c> call but not the
	/// <c>onput</c> one, so real Razor's own <c>@onput</c> binding already fails with CS1503: Htmxor
	/// must add nothing for it, not classify the unrelated, successfully-bound <c>onchange</c> call
	/// instead (LR-88fa9d8-P002).
	/// </summary>
	[Fact]
	public async Task Handler_bound_to_a_different_event_on_the_same_element_is_left_to_Razor()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private void M(global::Microsoft.AspNetCore.Components.ChangeEventArgs e) { }",
			wrapOpen: "__builder.OpenElement(5, \"input\");\n\t\t\t__builder.AddAttribute(6, \"onchange\", " +
				"global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.ChangeEventArgs>(this, M));",
			wrapClose: "__builder.CloseElement();",
			expectedCompilerErrorCode: "CS1503");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Razor maps a handler's generated argument to the binding attribute's own value, which can sit on
	/// a later line than the attribute name — here, an earlier <c>@foreach</c> binds the same handler
	/// name to a DOM event's loop variable, and the real <c>@onput</c> value is split across two lines.
	/// A faithful two-<c>Create</c>-call declaration, with the real <c>#line (l,c)-(l,c)</c> directives
	/// Razor itself emits for this exact shape, must still resolve to the approved component method
	/// <c>Go()</c>, not the earlier loop-local candidate: the approved handler must stay approved
	/// (LR-88fa9d8-S002).
	/// </summary>
	[Fact]
	public async Task Earlier_foreach_variable_split_across_lines_does_not_shadow_the_binding()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Action[] Actions { get; } = new global::System.Action[0];
				private void Go() { }

				protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
				{
					foreach (var Go in Actions)
					{
						__builder.OpenElement(0, "a");
						__builder.AddAttribute(1, "onclick", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.ChangeEventArgs>(this,
			#line (4,14)-(4,16) "{{componentPath}}"
			Go

			#line default
			#line hidden
						));
						__builder.AddContent(2, "x");
						__builder.CloseElement();
					}

					__builder.OpenElement(3, "button");
					__builder.AddAttribute(4, "onput", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this,
			#line (7,2)-(7,4) "{{componentPath}}"
			Go

			#line default
			#line hidden
					));
					__builder.AddContent(5, "x");
					__builder.CloseElement();
				}
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			@foreach (var Go in Actions)
			{
			<a @onclick="Go">x</a>
			}
			<button @onput=
			"Go">x</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The same mapped-selection rule when the loop and the binding sit on one line: both the earlier
	/// <c>@onclick</c> call and the real <c>@onput</c> call map to the same line as the <c>@onput</c>
	/// attribute name itself, so a line-only comparison can match both — the real binding's own,
	/// later position within that line is what must win. A faithful declaration with the real
	/// <c>#line (l,c)-(l,c)</c> directives Razor emits for this exact shape must still resolve to the
	/// approved component method <c>Go()</c> (LR-88fa9d8-S002).
	/// </summary>
	[Fact]
	public async Task Earlier_foreach_variable_on_the_same_line_does_not_shadow_the_binding()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Action[] Actions { get; } = new global::System.Action[0];
				private void Go() { }

				protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
				{
					foreach (var Go in Actions)
					{
						__builder.OpenElement(0, "a");
						__builder.AddAttribute(1, "onclick", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.ChangeEventArgs>(this,
			#line (2,45)-(2,47) "{{componentPath}}"
			Go

			#line default
			#line hidden
						));
						__builder.AddContent(2, "x");
						__builder.CloseElement();
					}

					__builder.OpenElement(3, "button");
					__builder.AddAttribute(4, "onput", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this,
			#line (2,73)-(2,75) "{{componentPath}}"
			Go

			#line default
			#line hidden
					));
					__builder.AddContent(5, "x");
					__builder.CloseElement();
				}
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			@foreach (var Go in Actions) { <a @onclick="Go">x</a> } <button @onput="Go">x</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// C# only binds a bare name to a local, loop variable, or lambda parameter when the binding sits
	/// inside that declaration's own scope. A same-named local in a sibling <c>@if</c> block, which
	/// never encloses the binding, does not change what the attribute's position binds to: the
	/// approved member is still what Razor — and Htmxor — must resolve (LR-555fbcf-S001/P001).
	/// </summary>
	[Fact]
	public async Task Same_named_local_in_a_block_that_does_not_enclose_the_binding_leaves_the_member_handler_approved()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private bool Show { get; set; }\n\n\t\tprivate void M() { }",
			wrapOpen: "if (Show)\n\t\t\t{\n\t\t\t\tvar M = 1;\n\t\t\t\t__builder.AddContent(2, M);\n\t\t\t}");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The same sibling-scope rule for a <c>@foreach</c> loop variable that does not enclose the
	/// binding: the loop's own scope ends before the button is rendered, so it cannot shadow the
	/// approved member at the binding's position (LR-555fbcf-S001/P001).
	/// </summary>
	[Fact]
	public async Task Same_named_foreach_variable_in_a_loop_that_does_not_enclose_the_binding_leaves_the_member_handler_approved()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private void M() { }",
			wrapOpen: "foreach (var M in new int[0])\n\t\t\t{\n\t\t\t\t__builder.AddContent(2, M);\n\t\t\t}");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The same sibling-scope rule for a lambda parameter declared elsewhere in the method: a lambda's
	/// parameter is in scope only inside that lambda's own body, never at the binding's position
	/// (LR-555fbcf-S001/P001).
	/// </summary>
	[Fact]
	public async Task Same_named_lambda_parameter_elsewhere_leaves_the_member_handler_approved()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private void M() { }",
			wrapOpen: "__builder.AddContent(2, global::System.Linq.Enumerable.Count(new[] { 1 }, M => M > 0));");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A same-named local of a type that cannot convert to <c>EventCallback&lt;HtmxEventArgs&gt;</c>,
	/// declared before the binding with no component member, makes Razor itself reject the binding
	/// (CS1503): Htmxor must add nothing, the same "Razor decides" contract as every other
	/// Razor-rejected row, not a second, duplicate diagnostic (LR-555fbcf-S002/P001).
	/// </summary>
	[Fact]
	public async Task Same_named_local_of_a_non_handler_type_is_left_to_Razor()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "",
			wrapOpen: "var M = 1;",
			expectedCompilerErrorCode: "CS1503");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A same-named local declared after the binding, with an approved member of the same name, puts
	/// the binding before its own local's declaration point: C# already rejects that (CS0841, "cannot
	/// use local variable before it is declared"). Htmxor must add nothing (LR-555fbcf-S002/P001).
	/// </summary>
	[Fact]
	public async Task Same_named_local_declared_after_the_binding_is_left_to_Razor()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private void M() { }",
			wrapClose: "global::System.Action M = () => { };",
			expectedCompilerErrorCode: "CS0841");

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A local function declared in the Razor markup (<c>@{ void M() { } }</c>) is still a local
	/// declaration, the same family as a loop variable or a local: real Razor binds it, and with no
	/// component member of that name, Htmxor must report the generic "request-owned component" cause
	/// instead of silently accepting it (LR-555fbcf-S003/P002).
	/// </summary>
	[Fact]
	public async Task Local_function_with_the_handler_name_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "",
			wrapOpen: "void M() { }");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'M' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// A local function with the handler's name is in scope throughout its whole enclosing block, so
	/// it is what the binding actually resolves to even when an approved-but-differently-shaped
	/// component member shares the same name: the real generated dispatch calls the component member,
	/// while the markup binds the local function, so neither the local function's own shape nor the
	/// member's shape is the right cause — the generic "request-owned component" message is, the same
	/// as any other local declaration (LR-555fbcf-S003/P002).
	/// </summary>
	[Fact]
	public async Task Local_function_shadowing_an_async_void_member_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var diagnostics = await RunWithBuildRenderTreeAsync(
			handlerValue: "M",
			members: "private async void M(global::Htmxor.HtmxEventArgs a) " +
				"=> await global::System.Threading.Tasks.Task.Yield();",
			wrapOpen: "void M() { }");

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'M' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}
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
	/// Compiles a code-behind tree (<c>ReportComponent.razor.cs</c>) and the Razor-generated tree as
	/// two separate partial declarations of the same component, in that order - user sources before
	/// generator output, the order a real build uses. Only the Razor-generated partial carries
	/// <c>[RouteAttribute]</c> and the <c>: ComponentBase</c> base list, matching what Razor itself
	/// emits; the code-behind partial carries none of that, matching a real <c>.razor.cs</c> file.
	/// This is the seam for every finding about which tree production reads Razor's generated
	/// <c>Create</c> call from (LR-a6f7218-P001/S001): production must find that call only in the
	/// Razor-generated declaration, never in the code-behind's own <c>using</c> directives, parameters,
	/// or locals.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunWithCodeBehindAsync(
		string codeBehindUsings,
		string codeBehindMembers,
		string codeBehindExtra,
		string razorUsings,
		string razorMembers,
		string razorExtra,
		string handlerValue,
		string? expectedCompilerErrorCode = null)
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var codeBehindSource = $$"""
			{{codeBehindUsings}}
			namespace {{RootNamespace}}
			{
			{{codeBehindExtra}}
			public partial class ReportComponent
			{
				{{codeBehindMembers}}
			}
			}
			""";
		var razorSource = $$"""
			{{razorUsings}}
			namespace {{RootNamespace}}
			{
			{{razorExtra}}
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public partial class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				{{razorMembers}}
			}
			}
			""";
		var razorContent = "@page \"/reports/{Id:int}\"\n<button @onput=\"" + handlerValue + "\">Save</button>\n";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		return await RunActionAnalyzerAsync(
			new[]
			{
				(codeBehindSource, ComponentPath("ReportComponent.razor.cs")),
				(razorSource, RazorGeneratedPath("ReportComponent")),
			},
			razor,
			expectedCompilerErrorCode);
	}

	/// <summary>
	/// A single-tree fixture whose Razor binding value is the caller's own spelling (for example
	/// <c>this.M</c>), instead of the fixed <c>M</c> every other <see cref="RunForHandlerAsync"/>
	/// fixture uses. Its explicit bind call uses the same spelling, so the compile step proves real
	/// C#'s verdict for exactly the expression Razor emits and Htmxor binds.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunForThisQualifiedHandlerAsync(
		string handlerMember,
		string usings,
		string? expectedCompilerErrorCode = null)
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			{{usings}}
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				{{handlerMember}}

				private void Bind() => global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, this.M);
			}
			}
			""";
		var razorContent = "@page \"/reports/{Id:int}\"\n<button @onput=\"this.M\">Save</button>\n";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		return await RunActionAnalyzerAsync(source, razor, expectedCompilerErrorCode);
	}

	/// <summary>
	/// A fixture shaped exactly like real Razor's generated declaration: a <c>BuildRenderTree</c>
	/// override containing the binding at the attribute's own position, instead of a <c>Bind()</c>
	/// expression-bodied stand-in. <paramref name="wrapOpen"/>/<paramref name="wrapClose"/> nest that
	/// position inside a block, the way markup control flow (a <c>@foreach</c>, for example) becomes a
	/// nested C# block in the generated method — Razor binds the handler name at that exact nested
	/// position, not at the top of the method.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunWithBuildRenderTreeAsync(
		string handlerValue,
		string members,
		string wrapOpen = "",
		string wrapClose = "",
		string? expectedCompilerErrorCode = null)
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
				{
					{{wrapOpen}}
					__builder.OpenElement(0, "button");
					__builder.AddAttribute(1, "onput", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, {{handlerValue}}));
					__builder.CloseElement();
					{{wrapClose}}
				}

				{{members}}
			}
			}
			""";
		var razorContent = "@page \"/reports/{Id:int}\"\n<button @onput=\"" + handlerValue + "\">Save</button>\n";
		var razor = new SourceAdditionalText(componentPath, razorContent);

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
		=> await RunActionAnalyzerAsync(
			new[] { (source, RazorGeneratedPath("ReportComponent")) },
			razor,
			expectedCompilerErrorCode);

	/// <summary>
	/// The multi-tree overload: a real component compilation can carry more than one syntax tree for
	/// the same partial type (a code-behind file plus the Razor-generated declaration), each at its
	/// own path and in declaration order (user sources before generator output, matching a real
	/// build). Every fixture that needs to show which tree production reads Razor's generated
	/// <c>Create</c> call from (LR-a6f7218-P001/S001) uses this overload directly instead of
	/// <see cref="RunActionAnalyzerAsync(string, AdditionalText, string?)"/>.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunActionAnalyzerAsync(
		IReadOnlyList<(string Source, string FilePath)> sources,
		AdditionalText razor,
		string? expectedCompilerErrorCode = null)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.ActionHandlerShapeAnalyzer.Tests",
			sources.Select(pair => CSharpSyntaxTree.ParseText(pair.Source, parseOptions, pair.FilePath)),
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
