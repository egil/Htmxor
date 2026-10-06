using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorActionGeneratorTests
{
	private const string RootNamespace = "Htmxor.Consumer";
	private const string RuntimeStubs = """
		using System;
		using System.Collections.Generic;
		using System.Reflection;
		using System.Threading.Tasks;

		namespace Microsoft.AspNetCore.Routing
		{
			public interface IEndpointRouteBuilder;
		}

		namespace Microsoft.AspNetCore.Components.Endpoints.Infrastructure
		{
			public static class ComponentEndpointConventionBuilderHelper
			{
				public static global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder GetEndpointRouteBuilder(
					global::Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder builder)
					=> throw new global::System.NotImplementedException();
			}
		}

		namespace Microsoft.AspNetCore.Builder
		{
			public sealed class RazorComponentsEndpointConventionBuilder;

			public static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				public static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
					this RazorComponentsEndpointConventionBuilder builder,
					Routing.IEndpointRouteBuilder endpoints,
					Assembly applicationAssembly,
					IReadOnlyList<string> projectRootComponentTypeNames,
					IReadOnlyList<Htmxor.Builder.HtmxorGeneratedComponentAction> generatedActions)
					=> builder;
			}
		}

		namespace Microsoft.AspNetCore.Components
		{
			[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
			public sealed class RouteAttribute(string template) : Attribute;

			public interface IComponent
			{
				Task SetParametersAsync(ParameterView parameters);
			}

			public readonly struct ParameterView;

			public class ComponentBase : IComponent
			{
				public virtual Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
			}

			[AttributeUsage(AttributeTargets.Property)]
			public sealed class InjectAttribute : Attribute;

			public static class EventCallback
			{
				public static EventCallbackFactory Factory { get; } = new();
			}

			public sealed class EventCallbackFactory
			{
				public EventCallback<T> Create<T>(object receiver, Func<T, Task> callback) => new(callback);
			}

			public readonly struct EventCallback<T>(Func<T, Task> callback)
			{
				public Task InvokeAsync(T value) => callback(value);
			}
		}

		namespace Htmxor.Builder
		{
			public sealed class HtmxorGeneratedComponentAction(
				Type componentType,
				string httpMethod,
				string handlerIdentity,
				bool usesStockRoute,
				Type? routeProcessorType = null);
		}

		namespace Htmxor.Endpoints
		{
			public interface IHtmxorGeneratedComponentActionRequest
			{
				bool TryConsume(Builder.HtmxorGeneratedComponentAction action);
			}
		}

		namespace Htmxor.Http
		{
			public sealed class HtmxContext;
		}

		namespace Htmxor
		{
			public sealed class HtmxEventArgs(Http.HtmxContext context) : EventArgs;
		}

		namespace Htmxor.Consumer
		{
			public partial class ReportComponent
			{
				public Task SetParametersAsync(
					Microsoft.AspNetCore.Components.ParameterView parameters) => Task.CompletedTask;

				private Task PutReport(Htmxor.HtmxEventArgs args) => Task.CompletedTask;

				private Task PostReport(Htmxor.HtmxEventArgs args) => Task.CompletedTask;

				private Task PatchReport(Htmxor.HtmxEventArgs args) => Task.CompletedTask;

				private Task DeleteReport(Htmxor.HtmxEventArgs args) => Task.CompletedTask;

				private Task QueryReport(Htmxor.HtmxEventArgs args) => Task.CompletedTask;
			}
		}
		""";
	private static readonly string ProjectDirectory = Path.GetFullPath(
		Path.Combine(Path.GetTempPath(), "htmxor-action-generator-tests"));

	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"", "button", "@onpost", "POST", "PostReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "button", "@onput", "PUT", "PutReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "InputText", "@onpatch", "PATCH", "PatchReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "InputText", "@ondelete", "DELETE", "DeleteReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "form", "@onquery", "QUERY", "QueryReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "button", "@onpost", "POST", "PostReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "button", "@onput", "PUT", "PutReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "InputText", "@onpatch", "PATCH", "PatchReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "InputText", "@ondelete", "DELETE", "DeleteReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "form", "@onquery", "QUERY", "QueryReport")]
	public void Route_owner_and_component_binding_emit_one_compiling_action(
		string routeDeclaration,
		string tagName,
		string binding,
		string httpMethod,
		string handlerName)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<{tagName} {binding}="{handlerName}" />
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = Assert.Single(
			run.RunResult.Results
				.SelectMany(static result => result.GeneratedSources)
				.Where(static source => source.HintName != "HtmxorGeneratedRouteRegistration.g.cs"))
			.SourceText
			.ToString();
		Assert.Contains($"\"{httpMethod}\"", actionSource, StringComparison.Ordinal);
		Assert.Contains($"this, {handlerName}", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"", "button", "@onpost", "POST", "PostReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "button", "@onput", "PUT", "PutReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "InputText", "@onpatch", "PATCH", "PatchReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "InputText", "@ondelete", "DELETE", "DeleteReport")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "form", "@onquery", "QUERY", "QueryReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "button", "@onpost", "POST", "PostReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "button", "@onput", "PUT", "PutReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "InputText", "@onpatch", "PATCH", "PatchReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "InputText", "@ondelete", "DELETE", "DeleteReport")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "form", "@onquery", "QUERY", "QueryReport")]
	public void Static_id_target_order_preserves_generated_action_and_allow_list(
		string routeDeclaration,
		string tagName,
		string binding,
		string httpMethod,
		string handlerName)
	{
		var bindingFirst = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<{tagName} {binding}="{handlerName}" hx-target="#selector" />
			"""));
		var targetFirst = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<{tagName} hx-target="#selector" {binding}="{handlerName}" />
			"""));

		Assert.Empty(bindingFirst.DriverDiagnostics);
		Assert.Empty(bindingFirst.RunResult.Diagnostics);
		Assert.Empty(targetFirst.DriverDiagnostics);
		Assert.Empty(targetFirst.RunResult.Diagnostics);
		var bindingFirstAction = GetGeneratedSource(bindingFirst, "HtmxorGeneratedActions.g.cs");
		var targetFirstAction = GetGeneratedSource(targetFirst, "HtmxorGeneratedActions.g.cs");
		Assert.Contains($"\"{httpMethod}\"", targetFirstAction, StringComparison.Ordinal);
		Assert.Contains($"this, {handlerName}", targetFirstAction, StringComparison.Ordinal);
		Assert.Equal(bindingFirstAction, targetFirstAction);
		Assert.Equal(
			GetGeneratedSource(bindingFirst, "HtmxorGeneratedRouteRegistration.g.cs"),
			GetGeneratedSource(targetFirst, "HtmxorGeneratedRouteRegistration.g.cs"));
		Assert.Empty(CompilationErrors(bindingFirst.OutputCompilation));
		Assert.Empty(CompilationErrors(targetFirst.OutputCompilation));
	}

	[Fact]
	public void Component_tag_binding_after_bind_attribute_emits_a_compiling_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}")]
			<InputText @bind-Value="InputValue" @onpatch="PatchReport" />
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PatchReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Stock_page_html_binding_after_prior_markup_emits_a_compiling_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<p>Review the report before saving.</p>
			<button @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Stock_page_binding_after_prior_self_closing_markup_emits_a_compiling_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<hr />
			<button @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Binding_after_prior_self_closing_component_markup_emits_a_compiling_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<InputText @bind-Value="InputValue" />
			<button @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A double-quoted binding generates its action from anywhere in the route owner's own
	/// markup (#306), for both a <c>@page</c> owner and an omitted-<c>Methods</c>
	/// <c>HtmxRoute</c> owner.
	/// </summary>
	public static IEnumerable<object[]> BindingAnywherePositionCases()
	{
		(string RouteDeclaration, bool UsesStockRoute)[] routeOwners =
		{
			("@page \"/reports/{ReportId:int}\"", true),
			("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", false),
		};

		(string Position, string Template)[] positions =
		{
			("control_flow", """
				%ROUTE%
				@foreach (var item in Items)
				{
					<button @onput="PutReport">Save</button>
				}
				"""),
			("control_flow_switch", """
				%ROUTE%
				@switch (Selection)
				{
					case "a":
						<button @onput="PutReport">Save</button>
						break;
				}
				"""),
			("child_content", """
				%ROUTE%
				<SomeWrapper>
					<button @onput="PutReport">Save</button>
				</SomeWrapper>
				"""),
			("htmx_fragment", """
				%ROUTE%
				<HtmxFragment Name="save">
					<button @onput="PutReport">Save</button>
				</HtmxFragment>
				"""),
			("after_component_markup", """
				%ROUTE%
				<SomeWidget>
					<span>Widget content</span>
				</SomeWidget>
				<button @onput="PutReport">Save</button>
				"""),
			("after_code", """
				%ROUTE%
				@code {
					private Task Placeholder() => Task.CompletedTask;
				}
				<button @onput="PutReport">Save</button>
				"""),
			("after_inherits", """
				%ROUTE%
				@inherits ReportComponentBase
				<button @onput="PutReport">Save</button>
				"""),
			("after_layout", """
				%ROUTE%
				@layout ReportLayout
				<button @onput="PutReport">Save</button>
				"""),
			("after_arbitrary_earlier_attribute", """
				%ROUTE%
				<button hx-vals='{"foo":"bar"}' @onput="PutReport">Save</button>
				"""),
			("after_attribute_with_embedded_angle_bracket", """
				%ROUTE%
				<button onclick="@(() => Count++)" @onput="PutReport">Save</button>
				"""),
			("before_route_declaration", """
				<button @onput="PutReport">Save</button>
				%ROUTE%
				"""),
			("before_code", """
				%ROUTE%
				<button @onput="PutReport">Save</button>
				@code {
					private Task Placeholder() => Task.CompletedTask;
				}
				"""),
			("before_inherits", """
				%ROUTE%
				<button @onput="PutReport">Save</button>
				@inherits ReportComponentBase
				"""),
			("before_layout", """
				%ROUTE%
				<button @onput="PutReport">Save</button>
				@layout ReportLayout
				"""),
			("line_comment_before_binding_in_control_flow", """
				%ROUTE%
				@foreach (var item in Items)
				{
					// A per-row comment.
					<button @onput="PutReport">Save</button>
				}
				"""),
			("block_comment_before_binding_in_control_flow", """
				%ROUTE%
				@foreach (var item in Items)
				{
					/* A per-row comment. */
					<button @onput="PutReport">Save</button>
				}
				"""),
			("razor_comment_before_binding_in_control_flow", """
				%ROUTE%
				@foreach (var item in Items)
				{
					@* Don't render archived rows twice. *@
					<button @onput="PutReport">Save</button>
				}
				"""),
			("html_comment_before_binding_in_control_flow", """
				%ROUTE%
				@foreach (var item in Items)
				{
					<!-- A per-row comment. -->
					<button @onput="PutReport">Save</button>
				}
				"""),
		};

		foreach (var owner in routeOwners)
		{
			foreach (var position in positions)
			{
				yield return new object[]
				{
					position.Position,
					position.Template.Replace("%ROUTE%", owner.RouteDeclaration, StringComparison.Ordinal),
					owner.UsesStockRoute,
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(BindingAnywherePositionCases))]
	public void Binding_emits_its_action_from_anywhere_in_the_route_owners_markup(
		string position,
		string content,
		bool usesStockRoute)
	{
		var run = RunGenerators(new RazorInput("ReportComponent.razor", content));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		AssertChosenOwner(actionSource, "Htmxor.Consumer.ReportComponent.PUT.PutReport", usesStockRoute);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A binding placed after an earlier binding of another method must not silently replace it:
	/// both actions, each tied to its own HTTP method, must be generated (#306).
	/// </summary>
	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"", true)]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", false)]
	public void Binding_after_an_earlier_binding_of_another_method_emits_both_actions(
		string routeDeclaration,
		bool usesStockRoute)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<button @onpost="PostReport">Create</button>
			<button @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("\"POST\"", actionSource, StringComparison.Ordinal);
		Assert.Contains("this, PostReport", actionSource, StringComparison.Ordinal);
		Assert.Contains("\"PUT\"", actionSource, StringComparison.Ordinal);
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Equal(2, CountOccurrences(actionSource, "actions.Add("));
		AssertChosenOwner(actionSource, "Htmxor.Consumer.ReportComponent.POST.PostReport", usesStockRoute);
		AssertChosenOwner(actionSource, "Htmxor.Consumer.ReportComponent.PUT.PutReport", usesStockRoute);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A binding commented out with a Razor comment must never be mistaken for a real
	/// declaration, and must never suppress the real binding that follows it inside the same
	/// control-flow body (#306).
	/// </summary>
	[Fact]
	public void Binding_after_a_Razor_commented_out_binding_of_another_handler_emits_only_the_real_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			@foreach (var item in Items)
			{
				@* <button @onput="Old"> *@
				<button @onput="PutReport">Save</button>
			}
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.DoesNotContain("Old", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A binding commented out with a Razor comment inside a start tag must never be mistaken for
	/// a real declaration, even though the element also carries an unrelated attribute (#306).
	/// </summary>
	[Fact]
	public void Binding_commented_out_inside_a_start_tag_emits_no_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button hx-delete="/x" @* @ondelete="DeleteReport" *@>x</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A binding commented out with a Razor comment inside a start tag must not count toward the
	/// at-most-one rule for a real binding of the same kind on the same element (#306).
	/// </summary>
	[Fact]
	public void Binding_after_a_Razor_commented_out_binding_in_the_same_start_tag_emits_only_the_real_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button @* @onpatch="Old" *@ @onpatch="PatchReport">x</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PatchReport", actionSource, StringComparison.Ordinal);
		Assert.DoesNotContain("Old", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A Razor comment is never a binding and never desyncs the scan, wherever it appears in the
	/// route owner's own file (#306): directly after an implicit expression or plain text in
	/// markup, inside a statement header, or inside a markup expression wrapping a template. Each
	/// row asserts exactly the expected real actions, with no diagnostics; the comment itself
	/// never contributes an action or a diagnostic. Inside a quoted attribute value, "@* ... *@"
	/// is never a comment: real Razor runs it to its next "*@" marker as literal text, across any
	/// quotes in between, so the attribute value cannot end until that marker, and a real binding
	/// name or attribute caught inside the span is never live. A comment whose body starts with
	/// the same character as its own opener ("@*@..." or "/*/...") must still run to its real
	/// closing marker, not the one that overlaps the opener itself, wherever it appears: at top
	/// level, after an expression, inside a control-flow body, or as a C# comment in a markup
	/// "@{ }" block.
	/// </summary>
	public static IEnumerable<object[]> RazorCommentNeverBindsOrDesyncsCases() =>
		new (string Scenario, string Content, string[] ExpectedHandlers)[]
		{
			("comment_after_implicit_expression_in_markup", """
				@page "/reports/{ReportId:int}"
				<td>@Name@* <b @ondelete="DeleteReport">x</b> *@</td>
				""",
				Array.Empty<string>()),
			("comment_after_plain_text_at_top_level", """
				@page "/reports/{ReportId:int}"
				Note@* <b @ondelete="DeleteReport">x</b> *@
				""",
				Array.Empty<string>()),
			("comment_with_an_apostrophe_in_a_statement_header", """
				@page "/reports/{ReportId:int}"
				@foreach (var x in Items @* don't page yet *@)
				{
					<button @onput="PutReport">x</button>
				}
				""",
				new[] { "PutReport" }),
			("commented_out_template_inside_a_markup_expression", """
				@page "/reports/{ReportId:int}"
				<div>@(Wrap(@* @<b @onput="PutReport">x</b> *@ null))</div>
				@code {
					private RenderFragment Wrap(RenderFragment? f) => f ?? (__builder => { });
				}
				""",
				Array.Empty<string>()),
			("comment_on_its_own_line_inside_control_flow", """
				@page "/reports/{ReportId:int}"
				@foreach (var item in Items)
				{
					@* Don't render twice. *@
					<button @onput="PutReport">Save</button>
				}
				""",
				new[] { "PutReport" }),
			("comment_in_the_same_start_tag_as_the_real_binding", """
				@page "/reports/{ReportId:int}"
				<button @* @ondelete="Old" *@ @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("comment_syntax_in_a_quoted_attribute_value_runs_to_its_closing_marker", """
				@page "/reports/{ReportId:int}"
				<button title="@* x" @ondelete="DeleteReport" data-note="y *@" @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("comment_whose_body_starts_with_at_closes_at_its_opener_at_top_level", """
				@page "/reports/{ReportId:int}"
				@*@if (Show) { <b @ondelete="DeleteReport">x</b> }*@
				<button @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("comment_whose_body_starts_with_at_closes_at_its_opener_after_an_expression", """
				@page "/reports/{ReportId:int}"
				<td>@Name@*@if (Show) { <b @ondelete="DeleteReport">x</b> }*@</td>
				<button @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("comment_whose_body_starts_with_at_closes_at_its_opener_inside_an_if_body", """
				@page "/reports/{ReportId:int}"
				@if (Show)
				{
					@*@if (Nested) { <b @ondelete="DeleteReport">x</b> }*@
					<button @onput="PutReport">x</button>
				}
				""",
				new[] { "PutReport" }),
			("code_comment_whose_body_starts_with_a_slash_closes_at_its_opener", """
				@page "/reports/{ReportId:int}"
				@{
					/*/ oops */
					<button @onput="PutReport">x</button>
				}
				""",
				new[] { "PutReport" }),
		}.Select(static scenario => new object[] { scenario.Scenario, scenario.Content, scenario.ExpectedHandlers });

	[Theory]
	[MemberData(nameof(RazorCommentNeverBindsOrDesyncsCases))]
	public void Razor_comment_is_never_a_binding_and_never_desyncs_the_scan(
		string scenario,
		string content,
		string[] expectedHandlers)
	{
		var run = RunGenerators(new RazorInput("ReportComponent.razor", content));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		if (expectedHandlers.Length == 0)
		{
			AssertNoActionSource(run);
			Assert.Empty(CompilationErrors(run.OutputCompilation));
			return;
		}

		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Equal(expectedHandlers.Length, CountOccurrences(actionSource, "actions.Add("));
		Assert.All(expectedHandlers, handler =>
			Assert.Contains($"this, {handler}", actionSource, StringComparison.Ordinal));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Dynamic_onput_inside_control_flow_fails_closed_at_its_own_span()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@foreach (var item in Items)
			{
				<button @onput="@(() => PutReport(default!))">Save</button>
			}
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		Assert.Contains("simple method-group", diagnostic.GetMessage(), StringComparison.Ordinal);
		AssertNoActionSource(run);
	}

	[Fact]
	public void Two_onput_bindings_inside_control_flow_trigger_the_at_most_one_rule()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			@foreach (var item in Items)
			{
				<button @onput="PutReport">Save</button>
				<button @onput="PutReport">Save again</button>
			}
			"""));

		Assert.Equal(2, run.RunResult.Diagnostics.Length);
		Assert.All(run.RunResult.Diagnostics, diagnostic =>
		{
			Assert.Equal("HTMXOR002", diagnostic.Id);
			Assert.Contains("at most one", diagnostic.GetMessage(), StringComparison.Ordinal);
		});
		AssertNoActionSource(run);
	}

	/// <summary>
	/// The owner decided fail closed for these positions
	/// (https://github.com/egil/Htmxor/issues/306#issuecomment-6021458497): a binding inside a
	/// Razor template (<c>@&lt;tag&gt;</c>), whether the template sits in <c>@code</c> or in a
	/// markup <c>@{ }</c> block, or inside markup rendered by an <c>@code</c> helper method, must
	/// fail the build with HTMXOR002 at its own span and never generate an action.
	/// </summary>
	public static IEnumerable<object[]> TemplateOrCodeMarkupBindingCases() =>
		new (string Scenario, string Content)[]
		{
			("razor_template_inside_code", """
				@page "/reports/{ReportId:int}"
				@code {
					RenderFragment F => @<button @onput="PutReport">x</button>;
				}
				"""),
			("razor_template_inside_markup_block", """
				@page "/reports/{ReportId:int}"
				@{
					RenderFragment f = @<button @onput="PutReport">x</button>;
				}
				"""),
			("markup_inside_code_helper_method", """
				@page "/reports/{ReportId:int}"
				@code {
					void RenderRow(RenderTreeBuilder __builder)
					{
						<button @onput="PutReport">x</button>
					}
				}
				"""),
			("razor_template_inside_markup_expression_call", """
				@page "/reports/{ReportId:int}"
				<div>@(Wrap(@<button @onput="PutReport">x</button>))</div>
				@code {
					private RenderFragment Wrap(RenderFragment f) => f;
				}
				"""),
			("razor_template_inside_markup_method_call", """
				@page "/reports/{ReportId:int}"
				<div>@Wrap(@<button @onput="PutReport">x</button>)</div>
				@code {
					private RenderFragment Wrap(RenderFragment f) => f;
				}
				"""),
		}.Select(static scenario => new object[] { scenario.Scenario, scenario.Content });

	[Theory]
	[MemberData(nameof(TemplateOrCodeMarkupBindingCases))]
	public void Binding_in_a_Razor_template_or_code_markup_fails_closed_at_its_own_span(
		string scenario,
		string content)
	{
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		Assert.EndsWith(
			"@onput in a Razor template or @code markup is not supported; " +
			"put the binding in the component's own markup",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A generator must never throw while the document is mid-edit, whatever construct was left
	/// open at the end of the buffer: an open tag, an unterminated C# string, an unterminated
	/// char literal, or an unterminated string inside an <c>@attribute</c> directive's argument
	/// list (#306).
	/// </summary>
	[Theory]
	[InlineData("@page \"/x\"\n@if (Show) { <div ")]
	[InlineData("@page \"/x\"\n@if (Show) { var s = \"abc\\")]
	[InlineData("@page \"/x\"\n@if (Show) { var c = '")]
	[InlineData("@page \"/x\"\n@attribute [Foo(\"abc\\")]
	public void Truncated_control_flow_markup_does_not_throw_the_generator(string content)
	{
		var run = RunGenerators(new RazorInput("ReportComponent.razor", content));

		AssertNoGeneratorException(run);
	}

	/// <summary>
	/// A generator must never throw while the document is mid-edit, for example when a tag,
	/// quote, string escape or char literal is still open at the end of the buffer (#306). This
	/// sweeps every eighth prefix of a component shaped like
	/// <c>samples/HtmxorExamples/ArchiveTogglePage.razor</c> (multi-line quoted attributes, a
	/// control-flow body, a nested component and an <c>@if</c>/<c>else</c> markup split) so the
	/// scan index is exercised stopping at hundreds of realistic mid-token positions, without
	/// sweeping every single character.
	/// </summary>
	[Fact]
	public void Every_eighth_prefix_of_a_representative_component_does_not_throw_the_generator()
	{
		const string source = """
			@inherits ConditionalComponentBase
			@page "/examples/archive-toggle/{Id:guid?}"
			@code {
				private IEnumerable<Contact> data = Enumerable.Empty<Contact>();

				[Parameter]
				public Guid Id { get; set; } = Guid.Empty;

				protected override void OnInitialized()
				{
					data = Id == Guid.Empty
						? Contacts.Data.Values.Take(30)
						: Contacts.Data.Values.Where(c => c.Id == Id);
				}

				private void ToggleArchive()
				{
					if (!Contacts.Data.TryGetValue(Id, out var contact))
					{
						throw new Microsoft.AspNetCore.Http.BadHttpRequestException(
							"A known contact id is required.");
					}

					contact.Archived = !contact.Archived;
				}
			}
			<PageTitle>Htmxor - Examples - Contact Archived Toggle</PageTitle>
			<AntiforgeryToken />
			<h1>Contact Archived Toggle</h1>
			<p>This is a Htmxor version of the <a href="https://htmx.org/essays/template-fragments/" title="Template Fragments article">contact archive toggle</a> example in the Template Fragments article.</p>
			<table class="table">
				<thead>
					<tr>
						<th>First Name</th>
						<th>Last Name</th>
						<th>Email</th>
						<th>Status</th>
					</tr>
				</thead>
				<tbody>
					@foreach (var contact in data)
					{
						<tr>
							<td>@contact.FirstName</td>
							<td>@contact.LastName</td>
							<td>@contact.Email</td>
							<td>
								<HtmxFragment>
									<button hx-patch="/examples/archive-toggle/@contact.Id"
											hx-swap="outerHTML"
											@onpatch="ToggleArchive">
										@if (contact.Archived)
										{
											<text>Unarchive</text>
										}
										else
										{
											<text>Archive</text>
										}
									</button>
								</HtmxFragment>
							</td>
						</tr>
					}
				</tbody>
			</table>
			""";

		for (var length = 0; length <= source.Length; length += 8)
		{
			var run = RunGenerators(new RazorInput("ReportComponent.razor", source[..length]));
			Assert.True(
				run.DriverDiagnostics.All(static diagnostic => diagnostic.Id != "CS8785"),
				$"Prefix length {length} made the generator throw: " +
				string.Join("; ", run.DriverDiagnostics.Where(static diagnostic => diagnostic.Id == "CS8785")));
		}
	}

	/// <summary>
	/// Binding-like text inside a C# lexical region of the route owner's own file — a comment,
	/// string, char literal, or the C# surrounding a control-flow body or an <c>@{ }</c> block —
	/// must not emit an action (#306). A char literal containing a brace is included because
	/// misreading it would desynchronize the scanner's brace-depth tracking and let the comment
	/// below it leak through as a real action.
	/// </summary>
	public static IEnumerable<object[]> NonbindingCSharpLexicalCases() =>
		new (string Scenario, string Content)[]
		{
			("code_line_comment", """
				@page "/reports/{ReportId:int}"
				@code {
					// <button @onput="PutReport">Save</button>
				}
				"""),
			("code_block_comment", """
				@page "/reports/{ReportId:int}"
				@code {
					/* <button @onput="PutReport">Save</button> */
				}
				"""),
			("code_verbatim_string", """
				@page "/reports/{ReportId:int}"
				@code {
					private const string Sample = @"<button @onput=""PutReport"">";
				}
				"""),
			("code_escaped_string", """
				@page "/reports/{ReportId:int}"
				@code {
					private const string Sample = "<button @onput=\"PutReport\">";
				}
				"""),
			("foreach_line_comment", """
				@page "/reports/{ReportId:int}"
				@foreach (var item in Items)
				{
					// <button @onput="PutReport">Save</button>
				}
				"""),
			("block_escaped_string", """
				@page "/reports/{ReportId:int}"
				@{
					var sample = "<button @onput=\"PutReport\">";
				}
				"""),
			("code_char_literal_with_brace", """
				@page "/reports/{ReportId:int}"
				@code {
					private const char Brace = '}';
					// <button @onput="PutReport">Save</button>
				}
				"""),
		}.Select(static scenario => new object[] { scenario.Scenario, scenario.Content });

	[Theory]
	[MemberData(nameof(NonbindingCSharpLexicalCases))]
	public void Binding_like_text_inside_a_Csharp_lexical_region_does_not_emit_an_action(
		string scenario,
		string content)
	{
		var run = RunGenerators(new RazorInput("ReportComponent.razor", content));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Omitted_methods_component_binding_after_prior_markup_emits_a_compiling_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}")]
			<p>Review the report before saving.</p>
			<InputText @bind-Value="InputValue" @onpatch="PatchReport" />
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PatchReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Multiple_unsafe_bindings_on_one_html_tag_emit_distinct_compiling_actions()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button hx-put="/reports/41" hx-delete="/reports/41" @onput="PutReport" @ondelete="DeleteReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Contains("this, DeleteReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Simple_stock_page_method_group_emits_one_shared_action_and_compiles_with_route_manifest()
	{
		var report = new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button hx-put="/reports/41?source=queue" @onput="PutReport">Save</button>
			""");

		var run = RunGenerators(report);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var routeSource = GetGeneratedSource(run, "HtmxorGeneratedRouteRegistration.g.cs");
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("AddGeneratedActions(generatedActions)", routeSource, StringComparison.Ordinal);
		Assert.Contains(
			"actions.Add(global::Htmxor.Consumer.ReportComponent.__HtmxorGeneratedPutAction)",
			actionSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"TryConsume(__HtmxorGeneratedPutAction)",
			actionSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"\"Htmxor.Consumer.ReportComponent.PUT.PutReport\"",
			actionSource,
			StringComparison.Ordinal);
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.DoesNotContain("/reports/41", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Page_directive_like_text_inside_later_code_comment_does_not_suppress_supported_action()
	{
		var report = new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button hx-put="/reports/41?source=queue" @onput="PutReport">Save</button>

			@code {
				/*
				@page "/not-a-directive"
				*/
			}
			""");

		var run = RunGenerators(report);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"", "hx-post")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "hx-put")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "hx-patch")]
	[InlineData("@page \"/reports/{ReportId:int}\"", "hx-delete")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "hx-post")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "hx-put")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "hx-patch")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", "hx-delete")]
	public void Client_unsafe_attribute_without_a_binding_emits_only_the_route_manifest(
		string routeDeclaration,
		string clientAttribute)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<button {clientAttribute}="/reports/41?source=queue">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		Assert.Contains(
			run.RunResult.Results.SelectMany(static result => result.GeneratedSources),
			static source => source.HintName == "HtmxorGeneratedRouteRegistration.g.cs");
		Assert.DoesNotContain(
			run.RunResult.Results.SelectMany(static result => result.GeneratedSources),
			static source => source.HintName == "HtmxorGeneratedActions.g.cs");
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]")]
	public void Htmx_four_action_and_method_do_not_grant_a_server_action(string routeDeclaration)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<button hx-action="/reports/41" hx-method="PUT">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]")]
	public void Htmx_four_query_does_not_grant_a_query_server_action(string routeDeclaration)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<button hx-query="/reports/41">Query</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]")]
	public void Static_id_target_without_a_binding_does_not_grant_a_server_action(string routeDeclaration)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<button hx-target="#selector">No action</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Onput_text_outside_a_markup_attribute_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button title="@onput=&quot;TitleHandler&quot;">No action</button>
			@* <button @onput="CommentHandler">Comment</button> *@
			<!-- <button @onput="HtmlCommentHandler">Comment</button> -->
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_an_attribute_value_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<div title='prefix @onput="PutReport" suffix'>No action</div>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_a_raw_string_attribute_value_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			""""
			@page "/reports/{ReportId:int}"
			<div title="@(""" @onput="PutReport" """)">No action</div>
			""""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_an_interpolated_attribute_expression_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<div title="@($"{/* @onput="PutReport" */ 1}")">No action</div>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_a_code_string_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			""""
			@page "/reports/{ReportId:int}"
			@code {
				private const string Sample = """<button @onput="PutReport">""";
			}
			""""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_an_attribute_raw_string_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			""""
			@page "/reports/{ReportId:int}"
			@attribute [System.ComponentModel.Description("""<button @onput="PutReport">""")]
			<div>No action</div>
			""""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_a_multiline_attribute_comment_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			@attribute [System.Obsolete(/*]
			<button @onput="PutReport">
			*/ "message")]
			<div>No action</div>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_script_text_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<script>const sample = '<button @onput="PutReport">';</script>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_ondelete_inside_multiline_script_text_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<script>
				<button @ondelete="DeleteReport">
			</script>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_ondelete_after_self_closing_script_syntax_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<script />
				<button @ondelete="DeleteReport">
			</script>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_ondelete_after_apparent_plaintext_pair_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<plaintext></plaintext>
			<button @ondelete="DeleteReport">
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_ondelete_after_uppercase_apparent_plaintext_pair_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<PLAINTEXT></PLAINTEXT>
			<button @ondelete="DeleteReport">
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_ondelete_after_misleading_script_slash_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<script>ignored/ >
				<button @ondelete="DeleteReport">
			</script>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_ondelete_after_nested_raw_text_suffix_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<div><script></div>
				<button @ondelete="DeleteReport">
			</script></div>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Stock_page_onput_emits_an_action_without_copying_route_text()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button hx-put="/reports/41" @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.DoesNotContain("/reports/{ReportId:int}", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Omitted_methods_htmx_only_action_emits_stock_route_processor()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@attribute [Htmxor.HtmxRoute("/reports/{ReportId:int}")]
			<button @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains(
			"[global::Microsoft.AspNetCore.Components.RouteAttribute(\"/reports/{ReportId:int}\")]",
			actionSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"typeof(__HtmxorRouteProcessor)",
			actionSource,
			StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Dynamic_onput_fails_closed_at_the_external_attribute_location()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button
				hx-put="/reports/41"
				@onput="@(() => PutReport(default!))">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		Assert.Contains("simple method-group", diagnostic.GetMessage(), StringComparison.Ordinal);
		AssertNoActionSource(run);
	}

	private static void AssertUnsupportedDiagnostic(
		Diagnostic diagnostic,
		RazorInput input,
		int expectedStart)
	{
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(LocationKind.ExternalFile, diagnostic.Location.Kind);
		Assert.Equal(input.FullPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(new TextSpan(expectedStart, "@onput".Length), diagnostic.Location.SourceSpan);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	private static void AssertNoActionSource(GeneratorRun run)
		=> Assert.DoesNotContain(
			run.RunResult.Results.SelectMany(static result => result.GeneratedSources),
			static source => source.HintName == "HtmxorGeneratedActions.g.cs");

	/// <summary>
	/// The protected behavior for incomplete, mid-edit Razor text is that the generator does not
	/// throw (CS8785), not that it reports no diagnostics at all: a recognized-but-unsupported
	/// binding still correctly reports its own HTMXOR002, truncated or not.
	/// </summary>
	private static void AssertNoGeneratorException(GeneratorRun run)
		=> Assert.DoesNotContain(run.DriverDiagnostics, static diagnostic => diagnostic.Id == "CS8785");

	/// <summary>
	/// Confirms which route owner the scanner attributed to a generated action: a <c>@page</c>
	/// owner emits a <c>true</c> stock-route flag and no custom route processor; an omitted-
	/// <c>Methods</c> <c>HtmxRoute</c> owner emits a <c>false</c> flag plus the generated
	/// <c>RouteAttribute</c> and route-processor type. A whole-file owner reader that attributes
	/// the wrong kind would still emit the action text alone, so this must be checked separately.
	/// </summary>
	private static void AssertChosenOwner(string actionSource, string handlerIdentity, bool usesStockRoute)
	{
		var marker = $"\"{handlerIdentity}\",";
		var markerIndex = actionSource.IndexOf(marker, StringComparison.Ordinal);
		Assert.True(markerIndex >= 0, $"Handler identity '{handlerIdentity}' was not found in the generated source.");
		var afterMarker = actionSource[(markerIndex + marker.Length)..].TrimStart();

		if (usesStockRoute)
		{
			Assert.StartsWith("true,", afterMarker, StringComparison.Ordinal);
			Assert.DoesNotContain("RouteAttribute(", actionSource, StringComparison.Ordinal);
		}
		else
		{
			Assert.StartsWith("false,", afterMarker, StringComparison.Ordinal);
			Assert.Contains(
				"[global::Microsoft.AspNetCore.Components.RouteAttribute(\"/reports/{ReportId:int}\")]",
				actionSource,
				StringComparison.Ordinal);
			Assert.Contains("typeof(__HtmxorRouteProcessor)", actionSource, StringComparison.Ordinal);
		}
	}

	private static int CountOccurrences(string text, string value)
	{
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += value.Length;
		}

		return count;
	}

	private static string GetGeneratedSource(GeneratorRun run, string hintName)
		=> Assert.Single(
			run.RunResult.Results
				.SelectMany(static result => result.GeneratedSources)
				.Where(source => source.HintName == hintName))
			.SourceText
			.ToString();

	private static IEnumerable<Diagnostic> CompilationErrors(Compilation compilation)
		=> compilation.GetDiagnostics().Where(
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

	private static GeneratorRun RunGenerators(params RazorInput[] inputs)
	{
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(
			LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.PutGenerator.Tests",
			new[] { CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions) },
			CreateReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new ISourceGenerator[]
			{
				new HtmxorRouteGenerator().AsSourceGenerator(),
				new HtmxorActionGenerator().AsSourceGenerator(),
			},
			inputs.Select(static input => (AdditionalText)new TextAdditionalText(
				input.FullPath,
				input.Content)).ToArray(),
			parseOptions,
			new TestAnalyzerConfigOptionsProvider(ProjectDirectory));

		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var driverDiagnostics);

		return new GeneratorRun(driver.GetRunResult(), outputCompilation, driverDiagnostics);
	}

	private static ImmutableArray<MetadataReference> CreateReferences()
	{
		var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
			.Split(Path.PathSeparator) ?? Array.Empty<string>();

		return platformAssemblies
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();
	}

	private sealed record GeneratorRun(
		GeneratorDriverRunResult RunResult,
		Compilation OutputCompilation,
		ImmutableArray<Diagnostic> DriverDiagnostics);

	private sealed record RazorInput(string RelativePath, string Content)
	{
		public string FullPath { get; } = Path.Combine(ProjectDirectory, RelativePath);
	}

	private sealed class TextAdditionalText(string path, string content) : AdditionalText
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
