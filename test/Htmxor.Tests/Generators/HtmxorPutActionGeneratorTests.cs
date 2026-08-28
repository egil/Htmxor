using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorPutActionGeneratorTests
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
				string handlerIdentity);
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
			}
		}
		""";
	private static readonly string ProjectDirectory = Path.GetFullPath(
		Path.Combine(Path.GetTempPath(), "htmxor-put-generator-tests"));

	[Fact]
	public void Simple_project_root_method_group_emits_one_shared_action_and_compiles_with_route_manifest()
	{
		var report = new RazorInput(
			"ReportComponent.razor",
			"""
			<button hx-put="/reports/41?source=queue" @onput="PutReport">Save</button>
			""");

		var run = RunGenerators(report);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		var routeSource = GetGeneratedSource(run, "HtmxorGeneratedRouteRegistration.g.cs");
		var actionSource = GetGeneratedSource(run, "HtmxorGeneratedPutAction.g.cs");
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
	public void Hx_put_without_onput_emits_only_the_route_manifest()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			<button hx-put="/reports/41?source=queue">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		Assert.Contains(
			run.RunResult.Results.SelectMany(static result => result.GeneratedSources),
			static source => source.HintName == "HtmxorGeneratedRouteRegistration.g.cs");
		Assert.DoesNotContain(
			run.RunResult.Results.SelectMany(static result => result.GeneratedSources),
			static source => source.HintName == "HtmxorGeneratedPutAction.g.cs");
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Onput_text_outside_a_markup_attribute_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@* <button @onput="CommentHandler">Comment</button> *@
			<!-- <button @onput="HtmlCommentHandler">Comment</button> -->
			<button title="@onput=&quot;TitleHandler&quot;">No action</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoPutSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_an_attribute_value_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			<div title='prefix @onput="PutReport" suffix'>No action</div>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoPutSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Nonbinding_onput_inside_a_code_string_does_not_emit_an_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			""""
			@code {
				private const string Sample = """<button @onput="PutReport">""";
			}
			""""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoPutSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Stock_page_onput_does_not_emit_an_htmx_only_action()
	{
		var run = RunGenerators(new RazorInput(
			"ReportComponent.razor",
			"""
			@page "/reports/{ReportId:int}"
			<button hx-put="/reports/41" @onput="PutReport">Save</button>
			"""));

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.RunResult.Diagnostics);
		AssertNoPutSource(run);
		Assert.Empty(CompilationErrors(run.OutputCompilation));
	}

	[Fact]
	public void Dynamic_onput_fails_closed_at_the_external_attribute_location()
	{
		const string content = """
			<button
				hx-put="/reports/41"
				@onput="@(() => PutReport(default!))">Save</button>
			""";
		var input = new RazorInput("ReportComponent.razor", content);
		var run = RunGenerators(input);

		var diagnostic = Assert.Single(run.RunResult.Diagnostics);
		AssertUnsupportedDiagnostic(diagnostic, input, content.IndexOf("@onput", StringComparison.Ordinal));
		Assert.Contains("simple method-group", diagnostic.GetMessage(), StringComparison.Ordinal);
		AssertNoPutSource(run);
	}

	[Fact]
	public void Multiple_onput_declarations_fail_closed_in_stable_external_path_order()
	{
		var alpha = new RazorInput(
			"AlphaComponent.razor",
			"<button @onput=\"PutAlpha\">Alpha</button>");
		var zeta = new RazorInput(
			"ZetaComponent.razor",
			"<button @onput=\"PutZeta\">Zeta</button>");

		var reverse = RunGenerators(zeta, alpha);
		var forward = RunGenerators(alpha, zeta);
		var reverseDiagnostics = reverse.RunResult.Diagnostics;
		var forwardDiagnostics = forward.RunResult.Diagnostics;

		Assert.Equal(2, reverseDiagnostics.Length);
		Assert.Equal(
			new[] { alpha.FullPath, zeta.FullPath },
			reverseDiagnostics.Select(static diagnostic => diagnostic.Location.GetLineSpan().Path));
		Assert.Equal(
			forwardDiagnostics.Select(static diagnostic => diagnostic.GetMessage()),
			reverseDiagnostics.Select(static diagnostic => diagnostic.GetMessage()));
		Assert.Equal(
			forwardDiagnostics.Select(static diagnostic => diagnostic.Location.GetLineSpan().Path),
			reverseDiagnostics.Select(static diagnostic => diagnostic.Location.GetLineSpan().Path));
		Assert.All(reverseDiagnostics, diagnostic =>
		{
			Assert.Equal("HTMXOR002", diagnostic.Id);
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			Assert.Equal(LocationKind.ExternalFile, diagnostic.Location.Kind);
			Assert.Contains("exactly one", diagnostic.GetMessage(), StringComparison.Ordinal);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		});
		AssertNoPutSource(reverse);
		AssertNoPutSource(forward);
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

	private static void AssertNoPutSource(GeneratorRun run)
		=> Assert.DoesNotContain(
			run.RunResult.Results.SelectMany(static result => result.GeneratedSources),
			static source => source.HintName == "HtmxorGeneratedPutAction.g.cs");

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
				new HtmxorPutActionGenerator().AsSourceGenerator(),
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
