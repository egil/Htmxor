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
			public sealed class RouteGroupBuilder;
		}

		namespace Microsoft.AspNetCore.Builder
		{
			public sealed class RazorComponentsEndpointConventionBuilder;

			public static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				public static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
					this RazorComponentsEndpointConventionBuilder builder,
					Routing.RouteGroupBuilder endpoints,
					Assembly applicationAssembly,
					IReadOnlyList<string> projectRootComponentTypeNames,
					IReadOnlyList<Htmxor.Builder.HtmxorGeneratedComponentAction> generatedActions)
					=> builder;
			}
		}

		namespace Microsoft.AspNetCore.Components
		{
			public interface IComponent
			{
				Task SetParametersAsync(ParameterView parameters);
			}

			public readonly struct ParameterView;

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
				bool usesStockRoute);
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
	public void Binding_after_prior_self_closing_component_markup_fails_closed()
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
