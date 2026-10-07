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

			// Mirrors the four delegate-typed Create<TValue> overloads of the real factory
			// (Microsoft.AspNetCore.Components 10.0.11, verified by reflection), which has six
			// overloads in total. The two EventCallback-typed overloads are omitted: a method group
			// never converts to them, so a handler of any of the four approved shapes binds through
			// plain C# overload resolution exactly as it would against the real framework.
			public sealed class EventCallbackFactory
			{
				public EventCallback<T> Create<T>(object receiver, Action callback) =>
					new(_ => { callback(); return Task.CompletedTask; });

				public EventCallback<T> Create<T>(object receiver, Action<T> callback) =>
					new(value => { callback(value); return Task.CompletedTask; });

				public EventCallback<T> Create<T>(object receiver, Func<Task> callback) =>
					new(_ => callback());

				public EventCallback<T> Create<T>(object receiver, Func<T, Task> callback) => new(callback);
			}

			public readonly struct EventCallback<T>(Func<T, Task> callback)
			{
				public Task InvokeAsync(T value) => callback(value);

				public Task InvokeAsync() => callback(default!);
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

				private Task GemÆndring(Htmxor.HtmxEventArgs args) => Task.CompletedTask;

				// The four approved handler signatures (#308), under names distinct from the
				// Task<HtmxEventArgs> handlers above.
				private void VoidNoParamHandler() { }

				private void VoidEventArgsParamHandler(Htmxor.HtmxEventArgs args) { }

				private Task TaskNoParamHandler() => Task.CompletedTask;
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

	/// <summary>
	/// Two more spellings are approved and generate the same action as the existing quoted
	/// spelling (#307, covered by <see cref="Route_owner_and_component_binding_emit_one_compiling_action"/>):
	/// unquoted <c>@onX=M</c> and <c>@onX="@M"</c>. This holds for both a <c>@page</c> owner and an
	/// omitted-<c>Methods</c> <c>HtmxRoute</c> owner, and for all five binding kinds.
	///
	/// A parenthesized method group, <c>@onX="@(M)"</c>, and an unquoted explicit expression,
	/// <c>@onX=@M</c>, are also accepted: both compile to the same method group as the spellings
	/// above and take the same classification path, so one representative row each is enough.
	///
	/// Any bare method-group name is an approved spelling (#307): the expression, after the Razor
	/// <c>@</c> and any parentheses, is a simple identifier or <c>this.Identifier</c>, whatever the
	/// quoting. So a single-quoted value (<c>@onX='M'</c>), a qualified <c>this.M</c>, a bare
	/// parenthesized group without an explicit <c>@</c> expression (<c>@onX="(M)"</c>), and a handler
	/// name containing a non-ASCII identifier letter each get one representative row.
	/// </summary>
	public static IEnumerable<object[]> ApprovedSpellingCases()
	{
		(string RouteDeclaration, bool UsesStockRoute)[] routeOwners =
		{
			("@page \"/reports/{ReportId:int}\"", true),
			("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", false),
		};

		(string TagName, string Binding, string HttpMethod, string HandlerName)[] bindings =
		{
			("button", "@onpost", "POST", "PostReport"),
			("button", "@onput", "PUT", "PutReport"),
			("InputText", "@onpatch", "PATCH", "PatchReport"),
			("InputText", "@ondelete", "DELETE", "DeleteReport"),
			("form", "@onquery", "QUERY", "QueryReport"),
		};

		(string Spelling, Func<string, string, string> Attribute)[] spellings =
		{
			("unquoted", static (binding, handler) => $"{binding}={handler}"),
			("at_quoted", static (binding, handler) => $"{binding}=\"@{handler}\""),
		};

		foreach (var owner in routeOwners)
		{
			foreach (var binding in bindings)
			{
				foreach (var spelling in spellings)
				{
					yield return new object[]
					{
						spelling.Spelling,
						owner.RouteDeclaration,
						owner.UsesStockRoute,
						binding.TagName,
						spelling.Attribute(binding.Binding, binding.HandlerName),
						binding.HttpMethod,
						binding.HandlerName,
					};
				}
			}
		}

		yield return new object[]
		{
			"paren_quoted",
			"@page \"/reports/{ReportId:int}\"",
			true,
			"button",
			"@onput=\"@(PutReport)\"",
			"PUT",
			"PutReport",
		};
		yield return new object[]
		{
			"at_unquoted",
			"@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]",
			false,
			"button",
			"@onput=@PutReport",
			"PUT",
			"PutReport",
		};
		yield return new object[]
		{
			"single_quoted",
			"@page \"/reports/{ReportId:int}\"",
			true,
			"button",
			"@onput='PutReport'",
			"PUT",
			"PutReport",
		};
		yield return new object[]
		{
			"this_qualified",
			"@page \"/reports/{ReportId:int}\"",
			true,
			"button",
			"@onput=\"this.PutReport\"",
			"PUT",
			"PutReport",
		};
		yield return new object[]
		{
			"paren_without_at",
			"@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]",
			false,
			"button",
			"@onput=\"(PutReport)\"",
			"PUT",
			"PutReport",
		};
		yield return new object[]
		{
			"non_ascii_handler",
			"@page \"/reports/{ReportId:int}\"",
			true,
			"button",
			"@onpost=\"GemÆndring\"",
			"POST",
			"GemÆndring",
		};
	}

	[Theory]
	[MemberData(nameof(ApprovedSpellingCases))]
	public void Approved_spelling_generates_its_action_for_every_binding_kind_and_route_owner(
		string spelling,
		string routeDeclaration,
		bool usesStockRoute,
		string tagName,
		string attribute,
		string httpMethod,
		string handlerName)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<{tagName} {attribute} />
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains($"\"{httpMethod}\"", actionSource, StringComparison.Ordinal);
		Assert.Contains($"this, {handlerName}", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		AssertChosenOwner(actionSource, $"Htmxor.Consumer.ReportComponent.{httpMethod}.{handlerName}", usesStockRoute);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// Each of the three approved handler signatures not already covered by
	/// <see cref="Route_owner_and_component_binding_emit_one_compiling_action"/>'s
	/// <c>Task M(HtmxEventArgs)</c> rows (#308) reaches its callback: it generates its action with no
	/// diagnostics, and the whole pipeline compiles. <c>Create&lt;HtmxEventArgs&gt;</c> has one real
	/// overload per shape (verified by reflection against Microsoft.AspNetCore.Components), so
	/// ordinary C# overload resolution binds a <c>void</c> or parameterless handler exactly as it
	/// binds the existing shape. This holds for both a <c>@page</c> owner and an omitted-<c>Methods</c>
	/// <c>HtmxRoute</c> owner (#308 AC1), the same two owners
	/// <see cref="Route_owner_and_component_binding_emit_one_compiling_action"/>'s rows use, and
	/// <see cref="AssertChosenOwner"/> confirms each row picked the right one.
	/// </summary>
	public static IEnumerable<object[]> ApprovedHandlerSignatureCases()
	{
		(string Shape, string HandlerName)[] signatures =
		{
			("void_no_param", "VoidNoParamHandler"),
			("void_event_args_param", "VoidEventArgsParamHandler"),
			("task_no_param", "TaskNoParamHandler"),
		};

		(string RouteDeclaration, bool UsesStockRoute)[] routeOwners =
		{
			("@page \"/reports/{ReportId:int}\"", true),
			("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", false),
		};

		foreach (var signature in signatures)
		{
			foreach (var owner in routeOwners)
			{
				yield return new object[]
				{
					signature.Shape,
					signature.HandlerName,
					owner.RouteDeclaration,
					owner.UsesStockRoute,
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(ApprovedHandlerSignatureCases))]
	public void Approved_handler_signature_emits_a_compiling_action(
		string shape,
		string handlerName,
		string routeDeclaration,
		bool usesStockRoute)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			<button @onput="{handlerName}">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains($"this, {handlerName}", actionSource, StringComparison.Ordinal);
		AssertChosenOwner(actionSource, $"Htmxor.Consumer.ReportComponent.PUT.{handlerName}", usesStockRoute);
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
	/// the same character as its own opener ("@*@..." or "/*/...") must still run past that
	/// overlapping position to its real closing marker, wherever it appears: at top level, after
	/// an expression, inside a control-flow body, in a start tag, or as a C# comment in a markup
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
			("comment_whose_body_starts_with_at_runs_past_its_opener_at_top_level", """
				@page "/reports/{ReportId:int}"
				@*@if (Show) { <b @ondelete="DeleteReport">x</b> }*@
				<button @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("comment_whose_body_starts_with_at_runs_past_its_opener_after_an_expression", """
				@page "/reports/{ReportId:int}"
				<td>@Name@*@if (Show) { <b @ondelete="DeleteReport">x</b> }*@</td>
				<button @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("comment_whose_body_starts_with_at_runs_past_its_opener_inside_an_if_body", """
				@page "/reports/{ReportId:int}"
				@if (Show)
				{
					@*@if (Nested) { <b @ondelete="DeleteReport">x</b> }*@
					<button @onput="PutReport">x</button>
				}
				""",
				new[] { "PutReport" }),
			("comment_whose_body_starts_with_at_in_a_start_tag_runs_past_its_opener", """
				@page "/reports/{ReportId:int}"
				<button @*@title="x" @ondelete="DeleteReport" *@ @onput="PutReport">x</button>
				""",
				new[] { "PutReport" }),
			("code_comment_whose_body_starts_with_a_slash_runs_past_its_opener", """
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
		AssertCauseSpecificMessage(diagnostic, "lambda or closure");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A lambda with a parameter is the same lambda cause as a parameterless lambda (#307), so it
	/// gets the same "lambda or closure" message, not the generic method-group message.
	/// </summary>
	[Fact]
	public void Lambda_binding_with_a_parameter_fails_closed_with_the_lambda_or_closure_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button @onput="@(e => PutReport(e))">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "lambda or closure");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A lambda that captures a local, such as a <c>@foreach</c> loop variable, is a closure. Its
	/// cause shares the "lambda or closure" message with a plain lambda (#307).
	/// </summary>
	[Fact]
	public void Closure_capturing_the_loop_variable_fails_closed_with_the_lambda_or_closure_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@foreach (var item in Items)
			{
				<button @onput="@(e => PutReport(item))">Save</button>
			}
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "lambda or closure");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A method call, whether an implicit expression or wrapped in an explicit one, fails closed
	/// with its own "method call" cause (#307), distinct from a lambda or a computed expression.
	/// The call target returns a delegate (<c>Func&lt;HtmxEventArgs, Task&gt;</c>): a call to a
	/// <c>Task</c>-returning handler, such as <c>PutReport()</c>, is not valid Razor for an
	/// <c>@onX</c> attribute and would not compile in a real component.
	/// </summary>
	[Theory]
	[InlineData("@MakeHandler()")]
	[InlineData("@(MakeHandler())")]
	public void Method_call_binding_fails_closed_with_the_method_call_message(string value)
	{
		var content = """
			@page "/reports/{ReportId:int}"
			@code {
				private Func<HtmxEventArgs, Task> MakeHandler() => PutReport;
			}
			<button @onput="%VALUE%">Save</button>
			""".Replace("%VALUE%", value, StringComparison.Ordinal);
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "method call");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A conditional expression is a computed expression (#307): neither a simple method-group name
	/// nor a lambda nor a plain call, so it gets its own "computed expression" cause.
	/// </summary>
	[Fact]
	public void Conditional_binding_fails_closed_with_the_computed_expression_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@code {
				private bool flag;
			}
			<button @onput="@(flag ? PutReport : PostReport)">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "computed expression");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// GET is implicit for every htmx request, so <c>@onget</c> is not an action binding. Htmxor
	/// still registers <c>onget</c> as a Razor event handler (<c>src/Htmxor/EventHandlers.cs</c>), so
	/// the binding compiles and must fail closed with its own cause instead of being silently
	/// accepted (#307).
	/// </summary>
	[Fact]
	public void Onget_binding_fails_closed_with_the_get_is_implicit_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button @onget="PostReport">Load</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onget", StringComparison.Ordinal));
		Assert.Contains("GET is implicit", diagnostic.GetMessage(), StringComparison.Ordinal);
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A switch expression that picks between two method groups is the switch form of the
	/// conditional this slice already reports as "a computed expression" (#307): it is neither a
	/// lambda nor a plain call, even though its arms are written with <c>=&gt;</c>.
	/// </summary>
	[Fact]
	public void Switch_expression_binding_fails_closed_with_the_computed_expression_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@code {
				private int Mode;
			}
			<button @onput="@(Mode switch { 0 => PutReport, _ => PostReport })">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "computed expression");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// Combining two delegate-returning calls with <c>+</c> is a computed expression (#307), not a
	/// method call: the value as a whole is a binary expression, even though each operand is itself
	/// a call.
	/// </summary>
	[Fact]
	public void Delegate_combination_binding_fails_closed_with_the_computed_expression_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@code {
				private Func<HtmxEventArgs, Task> MakeA(int x) => PutReport;
				private Func<HtmxEventArgs, Task> MakeB(int x) => PostReport;
			}
			<button @onput="@(MakeA(1) + MakeB(2))">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "computed expression");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A conditional whose branches are themselves delegate-returning calls is still a computed
	/// expression (#307), not a method call: the value as a whole is the conditional, even though it
	/// ends with a call's closing parenthesis.
	/// </summary>
	[Fact]
	public void Conditional_of_method_calls_binding_fails_closed_with_the_computed_expression_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@code {
				private bool IsAdmin() => true;
				private Func<HtmxEventArgs, Task> MakePut() => PutReport;
				private Func<HtmxEventArgs, Task> MakePost() => PostReport;
			}
			<button @onput="@(IsAdmin() ? MakePut() : MakePost())">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "computed expression");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A method call is still a method call when the callee's name happens to start with the word
	/// "delegate" (#307): the cause is decided by what the expression is, not by a substring of its
	/// spelling.
	/// </summary>
	[Fact]
	public void Lowercase_delegate_prefixed_method_call_binding_fails_closed_with_the_method_call_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			@code {
				private Func<HtmxEventArgs, Task> delegateFactory() => PutReport;
			}
			<button @onput="@(delegateFactory())">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "method call");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// An anonymous method is the same lambda cause as a lambda expression (#307), so it gets the
	/// same "lambda or closure" message.
	/// </summary>
	[Fact]
	public void Anonymous_method_binding_fails_closed_with_the_lambda_or_closure_message()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button @onput="@(delegate (HtmxEventArgs e) { return PutReport(e); })">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "lambda or closure");
		AssertNoActionSource(run);
	}

	/// <summary>
	/// A binding with no value at all (<c>@onX</c>, with no <c>=</c>) or an empty quoted value
	/// (<c>@onX=""</c>) is not a binding (#307, owner decision): real Razor builds both with no
	/// errors and drops the attribute, so Htmxor must report nothing and generate nothing rather than
	/// add a diagnostic for a case Razor already decided. <c>@onX="@"</c> (a bare Razor transition
	/// with nothing after it) is not covered here: Razor itself rejects that page (RZ1005), so there
	/// is no case to classify.
	/// </summary>
	[Theory]
	[InlineData("missing_value", "<button @onput>Save</button>")]
	[InlineData("empty_value", "<button @onput=\"\">Save</button>")]
	public void Empty_or_missing_binding_value_is_not_a_binding(
		string scenario,
		string elementMarkup)
	{
		var content = """
			@page "/reports/{ReportId:int}"
			%ELEMENT%
			""".Replace("%ELEMENT%", elementMarkup, StringComparison.Ordinal);
		var run = RunGenerators(new RazorInput("ReportComponent.razor", content));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// Two agreeing bindings nested in the same control-flow body collapse into exactly one
	/// generated action, one registration and one dispatch (#309): the removed "at most one" rule
	/// used to reject this exact pairing outright, even though both bindings name the same handler.
	/// </summary>
	[Fact]
	public void Two_agreeing_onput_bindings_inside_control_flow_collapse_into_one_action()
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

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Equal(1, CountOccurrences(actionSource, "TryConsume("));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// Two or more bindings that name the same handler for one method collapse into exactly one
	/// generated action, one registration and one dispatch (#309), across mixed approved spellings
	/// (#307: <c>M</c>, <c>this.M</c>, <c>@(M)</c>, <c>@M</c>), separate and nested positions, and
	/// both route-owner shapes.
	/// </summary>
	public static IEnumerable<object[]> AgreeingBindingCases()
	{
		(string RouteDeclaration, bool UsesStockRoute)[] routeOwners =
		{
			("@page \"/reports/{ReportId:int}\"", true),
			("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]", false),
		};

		(string Scenario, string Markup)[] scenarios =
		{
			("two_separate_elements_same_spelling", """
				<button @onput="PutReport">A</button>
				<button @onput="PutReport">B</button>
				"""),
			("two_separate_elements_this_qualified_spelling", """
				<button @onput="PutReport">A</button>
				<button @onput="this.PutReport">B</button>
				"""),
			("two_separate_elements_at_expression_spellings", """
				<button @onput="@(PutReport)">A</button>
				<button @onput="@PutReport">B</button>
				"""),
			("earlier_top_level_and_later_nested_element", """
				<button @onput="PutReport">A</button>
				@foreach (var item in Items)
				{
					<button @onput="PutReport">B</button>
				}
				"""),
			("three_agreeing_bindings_mixed_spellings_and_positions", """
				<button @onput="PutReport">A</button>
				@foreach (var item in Items)
				{
					<button @onput="this.PutReport">B</button>
				}
				<button @onput="@(PutReport)">C</button>
				"""),
		};

		foreach (var owner in routeOwners)
		{
			foreach (var scenario in scenarios)
			{
				yield return new object[]
				{
					scenario.Scenario,
					owner.RouteDeclaration,
					owner.UsesStockRoute,
					scenario.Markup,
				};
			}
		}
	}

	[Theory]
	[MemberData(nameof(AgreeingBindingCases))]
	public void Agreeing_bindings_for_one_method_collapse_into_one_action(
		string scenario,
		string routeDeclaration,
		bool usesStockRoute,
		string markup)
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			$"""
			{routeDeclaration}
			{markup}
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Equal(1, CountOccurrences(actionSource, "TryConsume("));
		AssertChosenOwner(actionSource, "Htmxor.Consumer.ReportComponent.PUT.PutReport", usesStockRoute);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// Different handlers bound to one method fail the build: each conflicting binding gets its own
	/// HTMXOR002 at its own span, with a message that names both competing handlers, and no action is
	/// generated for that method (#309). This replaces the removed "at most one" rule, which rejected
	/// every multiplicity identically whether or not the bindings agreed.
	/// </summary>
	[Theory]
	[InlineData("@page \"/reports/{ReportId:int}\"")]
	[InlineData("@attribute [Htmxor.HtmxRoute(\"/reports/{ReportId:int}\")]")]
	public void Different_handlers_for_one_method_fail_with_a_conflict_diagnostic_at_each_binding(
		string routeDeclaration)
	{
		var content = $"""
			{routeDeclaration}
			<button @onput="PutReport">A</button>
			<button @onput="PostReport">B</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		Assert.Equal(2, run.RunResult.Diagnostics.Length);
		var firstSpan = content.IndexOf("@onput", StringComparison.Ordinal);
		var secondSpan = content.IndexOf("@onput", firstSpan + 1, StringComparison.Ordinal);
		var bySpan = run.RunResult.Diagnostics.ToDictionary(static d => d.Location.SourceSpan.Start);
		AssertConflictDiagnostic(bySpan[firstSpan], input, firstSpan, "PutReport", "PostReport");
		AssertConflictDiagnostic(bySpan[secondSpan], input, secondSpan, "PutReport", "PostReport");
		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A conflicting method's failure is scoped to that method: another method on the same component
	/// still generates its own action (#309), rather than one bad method suppressing every action the
	/// whole component would otherwise generate.
	/// </summary>
	[Fact]
	public void Conflicting_bindings_fail_closed_while_another_method_on_the_same_component_still_generates()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button @onput="PutReport">A</button>
			<button @onput="PostReport">B</button>
			<button @onpost="PostReport">C</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		Assert.Equal(2, run.RunResult.Diagnostics.Length);
		var firstSpan = content.IndexOf("@onput", StringComparison.Ordinal);
		var secondSpan = content.IndexOf("@onput", firstSpan + 1, StringComparison.Ordinal);
		var bySpan = run.RunResult.Diagnostics.ToDictionary(static d => d.Location.SourceSpan.Start);
		AssertConflictDiagnostic(bySpan[firstSpan], input, firstSpan, "PutReport", "PostReport");
		AssertConflictDiagnostic(bySpan[secondSpan], input, secondSpan, "PutReport", "PostReport");

		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("\"POST\"", actionSource, StringComparison.Ordinal);
		Assert.Contains("this, PostReport", actionSource, StringComparison.Ordinal);
		Assert.DoesNotContain("\"PUT\"", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Equal(1, CountOccurrences(actionSource, "TryConsume("));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// Three bindings for one method, two agreeing and one different, make every one of them
	/// conflicting (#309): agreement is evaluated across the whole method, not pairwise, so the two
	/// bindings that agree with each other do not get a pass just because they outnumber the third.
	/// </summary>
	[Fact]
	public void Three_bindings_two_agreeing_and_one_different_all_fail_with_the_conflict_diagnostic()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button @onput="PutReport">A</button>
			<button @onput="PutReport">B</button>
			<button @onput="PostReport">C</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		Assert.Equal(3, run.RunResult.Diagnostics.Length);
		var firstSpan = content.IndexOf("@onput", StringComparison.Ordinal);
		var secondSpan = content.IndexOf("@onput", firstSpan + 1, StringComparison.Ordinal);
		var thirdSpan = content.IndexOf("@onput", secondSpan + 1, StringComparison.Ordinal);
		var bySpan = run.RunResult.Diagnostics.ToDictionary(static d => d.Location.SourceSpan.Start);
		foreach (var span in new[] { firstSpan, secondSpan, thirdSpan })
		{
			AssertConflictDiagnostic(bySpan[span], input, span, "PutReport", "PostReport");
		}

		AssertNoActionSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A binding that names no handler keeps its own #307 cause even next to a binding that does name
	/// one (#309): a lambda is not a "different handler" for conflict purposes, so it must never
	/// trigger the conflict message, and the valid sibling binding still generates its action normally.
	/// </summary>
	[Fact]
	public void Lambda_binding_next_to_a_valid_binding_keeps_its_own_cause_and_does_not_conflict()
	{
		const string content = """
			@page "/reports/{ReportId:int}"
			<button @onput="@(() => PutReport(default!))">A</button>
			<button @onput="PutReport">B</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		AssertCauseSpecificMessage(diagnostic, "lambda or closure");
		Assert.DoesNotContain("binds both", diagnostic.GetMessage(), StringComparison.Ordinal);

		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Equal(1, CountOccurrences(actionSource, "TryConsume("));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// Characterization (#309 AC4): one handler bound to several methods still generates one action
	/// per method, unchanged by this slice's per-method multiplicity rule, because each binding kind
	/// remains its own declaration group. This must already hold at this slice's starting revision.
	/// </summary>
	[Fact]
	public void One_handler_bound_to_several_methods_still_generates_one_action_per_method()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button @onput="PutReport" @onpatch="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Contains("\"PUT\"", actionSource, StringComparison.Ordinal);
		Assert.Contains("\"PATCH\"", actionSource, StringComparison.Ordinal);
		Assert.Equal(2, CountOccurrences(actionSource, "this, PutReport"));
		Assert.Equal(2, CountOccurrences(actionSource, "actions.Add("));
		Assert.Equal(2, CountOccurrences(actionSource, "TryConsume("));
		Assert.Empty(CompilationErrors(run.OutputCompilation));
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

	/// <summary>
	/// A script's raw text ends only at its own real closing tag, never at a tag name that merely
	/// starts with the same letters, such as "&lt;/scripture&gt;" inside a string. A real binding
	/// after the script must still be found: a closing-tag lookalike must not end the script early
	/// and strand a dangling quote that swallows the rest of the file.
	/// </summary>
	[Fact]
	public void Binding_after_a_script_containing_a_closing_tag_lookalike_emits_its_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<script>var t = "</scripture><div title='";</script>
			<button @onput="PutReport">x</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	/// <summary>
	/// A script's closing tag name also ends at trailing whitespace before the ">", not only at
	/// the ">" itself: "&lt;/script &gt;" is a valid closing tag in HTML and in Razor. A real
	/// binding after it must still be found.
	/// </summary>
	[Fact]
	public void Binding_after_a_script_whose_closing_tag_has_trailing_whitespace_emits_its_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<script>var t = 1;</script >
			<button @onput="PutReport">x</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedActions.g.cs");
		Assert.Equal(1, CountOccurrences(actionSource, "actions.Add("));
		Assert.Contains("this, PutReport", actionSource, StringComparison.Ordinal);
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
		AssertCauseSpecificMessage(diagnostic, "lambda or closure");
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

	/// <summary>
	/// A lambda or closure, a method call, and a conditional (computed expression) are three
	/// distinct value-grammar causes (#307) that must never share a message: a catch-all message
	/// naming every cause would make every value-error test pass regardless of which cause actually
	/// fired. Asserting the expected fragment's presence and the other two fragments' absence rules
	/// that out.
	/// </summary>
	private static readonly string[] ValueGrammarCauseFragments =
		{ "lambda or closure", "method call", "computed expression" };

	private static void AssertCauseSpecificMessage(Diagnostic diagnostic, string expectedFragment)
	{
		var message = diagnostic.GetMessage();
		Assert.Contains(expectedFragment, message, StringComparison.Ordinal);
		foreach (var otherFragment in ValueGrammarCauseFragments)
		{
			if (!string.Equals(otherFragment, expectedFragment, StringComparison.Ordinal))
			{
				Assert.DoesNotContain(otherFragment, message, StringComparison.Ordinal);
			}
		}
	}

	/// <summary>
	/// Shared assertion for the #309 conflict diagnostic: the ID, severity, non-configurable tag and
	/// span every HTMXOR002 already carries (<see cref="AssertUnsupportedDiagnostic"/>), plus the
	/// exact proposed message naming both competing handlers and the now-removed "at most one"
	/// fragment's absence.
	/// </summary>
	private static void AssertConflictDiagnostic(
		Diagnostic diagnostic,
		RazorInput input,
		int expectedStart,
		string first,
		string second)
	{
		AssertUnsupportedDiagnostic(diagnostic, input, expectedStart);
		var message = diagnostic.GetMessage();
		Assert.DoesNotContain("at most one", message, StringComparison.Ordinal);
		Assert.EndsWith(
			$"@onput binds both '{first}' and '{second}'; exactly one handler is supported per method",
			message,
			StringComparison.Ordinal);
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
