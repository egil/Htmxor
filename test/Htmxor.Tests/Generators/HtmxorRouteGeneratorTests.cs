using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorRouteGeneratorTests
{
	private const string RuntimeStubs = """
		using System.Reflection;
		using System.Collections.Generic;

		namespace Htmxor
		{
			[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
			internal sealed class HtmxRouteAttribute(string template) : global::System.Attribute
			{
				public string Template { get; } = template;

				public string[] Methods { get; set; } = [];
			}

			internal sealed class HtmxEventArgs;
		}

		namespace Microsoft.AspNetCore.Components
		{
			internal interface IComponent;

			internal abstract class ComponentBase : IComponent
			{
				protected virtual void BuildRenderTree(
					global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
				{
				}
			}

			internal readonly struct EventCallback
			{
				public static EventCallbackFactory Factory { get; } = new();
			}

			internal sealed class EventCallbackFactory
			{
				public object Create<T>(object receiver, global::System.Action<T> callback)
					=> callback;
			}
		}

		namespace Microsoft.AspNetCore.Components.Rendering
		{
			internal sealed class RenderTreeBuilder
			{
				public void AddAttribute(int sequence, string name, object value)
				{
				}
			}
		}

		namespace Htmxor.Builder
		{
			internal sealed class HtmxorGeneratedComponentAction;
		}

		namespace Microsoft.AspNetCore.Routing
		{
			internal sealed class RouteGroupBuilder;
		}

		namespace Microsoft.AspNetCore.Builder
		{
			internal sealed class RazorComponentsEndpointConventionBuilder;

			internal static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				internal static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
					this RazorComponentsEndpointConventionBuilder builder,
					Routing.RouteGroupBuilder endpoints,
					Assembly applicationAssembly,
					IReadOnlyList<string> projectRootComponentTypeNames,
					IReadOnlyList<Htmxor.Builder.HtmxorGeneratedComponentAction> generatedActions)
					=> builder;
			}
		}
		""";
	private const string AllCSharpComponent = """
		namespace Htmxor.Consumer;

		[global::Htmxor.HtmxRouteAttribute("/csharp/{Id:int}", Methods = ["GET"])]
		internal sealed class AllCSharpComponent : global::Microsoft.AspNetCore.Components.ComponentBase
		{
			protected override void BuildRenderTree(
				global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
			{
				builder.AddAttribute(0, "hx-delete", "/csharp/42");
				builder.AddAttribute(
					1,
					"ondelete",
					global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(
						this,
						Delete));
			}

			private void Delete(global::Htmxor.HtmxEventArgs _)
			{
			}
		}
		""";

	[Fact]
	public void All_CSharp_component_with_explicit_methods_is_in_generated_registration()
	{
		var run = RunGeneratorWithCSharpSource(
			AllCSharpComponent,
			"RazorControl.razor");

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Htmxor.Consumer.AllCSharpComponent\"",
			generatedSource,
			StringComparison.Ordinal);
	}

	[Fact]
	public void All_CSharp_component_without_methods_is_not_in_generated_registration()
	{
		var source = AllCSharpComponent.Replace(
			", Methods = [\"GET\"]",
			string.Empty,
			StringComparison.Ordinal);
		var run = RunGeneratorWithCSharpSource(source);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		Assert.Empty(result.GeneratedSources);
	}

	[Fact]
	public void Matching_Razor_code_behind_with_explicit_methods_emits_one_manifest_entry()
	{
		var run = RunGeneratorWithCSharpSourceAtPath(
			AllCSharpComponent,
			"AllCSharpComponent.razor.cs",
			"AllCSharpComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Equal(
			1,
			Count(generatedSource, "\"Htmxor.Consumer.AllCSharpComponent\""));
	}

	[Fact]
	public void Matching_Razor_code_behind_without_methods_is_not_in_generated_registration()
	{
		var source = AllCSharpComponent.Replace(
			", Methods = [\"GET\"]",
			string.Empty,
			StringComparison.Ordinal);
		var run = RunGeneratorWithCSharpSourceAtPath(
			source,
			"AllCSharpComponent.razor.cs",
			"AllCSharpComponent.razor",
			"RazorControl.razor");

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Htmxor.Consumer.RazorControl\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain(
			"\"Htmxor.Consumer.AllCSharpComponent\"",
			generatedSource,
			StringComparison.Ordinal);
	}

	[Fact]
	public void Equivalent_CSharp_route_candidate_is_unchanged_on_incremental_rerun()
	{
		var equivalentSource = AllCSharpComponent.Replace(
			"internal sealed class AllCSharpComponent",
			"internal sealed partial class AllCSharpComponent",
			StringComparison.Ordinal);
		var run = RunGeneratorIncrementally(AllCSharpComponent, equivalentSource);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var result = Assert.Single(run.RunResult.Results);
		var step = Assert.Single(result.TrackedSteps["CSharpRouteCandidates"]);
		var output = Assert.Single(step.Outputs);

		Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason);
	}

	[Fact]
	public void Manual_render_tree_intent_does_not_emit_an_action()
	{
		var run = RunGeneratorsWithCSharpSource(AllCSharpComponent);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var generatedSources = run.RunResult.Results
			.SelectMany(static result => result.GeneratedSources)
			.ToArray();
		var registration = Assert.Single(
			generatedSources,
			static source => source.HintName == "HtmxorGeneratedRouteRegistration.g.cs");

		Assert.Contains(
			"\"Htmxor.Consumer.AllCSharpComponent\"",
			registration.SourceText.ToString(),
			StringComparison.Ordinal);
		Assert.DoesNotContain(
			generatedSources,
			static source => source.HintName == "HtmxorGeneratedActions.g.cs");
	}

	[Fact]
	public void Project_root_paths_emit_one_sorted_runtime_manifest_without_reading_Razor_content()
	{
		var forward = RunGenerator(
			"ZetaComponent.razor",
			"Nested/NestedComponent.razor",
			"_Imports.razor",
			"AlphaComponent.razor");
		var reverse = RunGenerator(
			"AlphaComponent.razor",
			"_Imports.razor",
			"Nested/NestedComponent.razor",
			"ZetaComponent.razor");

		Assert.Empty(forward.DriverDiagnostics);
		var result = Assert.Single(forward.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();
		var reverseSource = Assert.Single(
			Assert.Single(reverse.RunResult.Results).GeneratedSources).SourceText.ToString();

		Assert.Equal(generatedSource, reverseSource);
		Assert.Equal(1, Count(generatedSource, "AddHtmxorAttributedComponentEndpoints("));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.AlphaComponent\""));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.ZetaComponent\""));
		AssertInOrder(
			generatedSource,
			"\"Htmxor.Consumer.AlphaComponent\"",
			"\"Htmxor.Consumer.ZetaComponent\"");
		Assert.Contains(
			"typeof(HtmxorGeneratedRouteRegistrationExtensions).Assembly",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains("ProjectRootComponentTypeNames", generatedSource, StringComparison.Ordinal);
		Assert.Contains("AddGeneratedActions(generatedActions)", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("NestedComponent", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("_Imports", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("HtmxRoute", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("Authorize", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("policy", generatedSource, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("MapHtmxorGeneratedComponentEndpoint", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("typeof(global::Htmxor.Consumer.AlphaComponent)", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain("typeof(global::Htmxor.Consumer.ZetaComponent)", generatedSource, StringComparison.Ordinal);
		Assert.Empty(forward.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	private static void AssertInOrder(string source, string first, string second)
	{
		var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
		var secondIndex = source.IndexOf(second, StringComparison.Ordinal);

		Assert.True(firstIndex >= 0 && firstIndex < secondIndex, source);
	}

	private static int Count(string source, string value)
		=> source.Split(value, StringSplitOptions.None).Length - 1;

	private static GeneratorRun RunGenerator(params string[] relativePaths)
		=> RunGeneratorCore(
			null,
			"AllCSharpComponent.cs",
			includeActionGenerator: false,
			relativePaths);

	private static GeneratorRun RunGeneratorWithCSharpSource(
		string csharpSource,
		params string[] relativePaths)
		=> RunGeneratorCore(
			csharpSource,
			"AllCSharpComponent.cs",
			includeActionGenerator: false,
			relativePaths);

	private static GeneratorRun RunGeneratorWithCSharpSourceAtPath(
		string csharpSource,
		string csharpRelativePath,
		params string[] relativePaths)
		=> RunGeneratorCore(
			csharpSource,
			csharpRelativePath,
			includeActionGenerator: false,
			relativePaths);

	private static GeneratorRun RunGeneratorsWithCSharpSource(string csharpSource)
		=> RunGeneratorCore(
			csharpSource,
			"AllCSharpComponent.cs",
			includeActionGenerator: true,
			Array.Empty<string>());

	private static GeneratorRun RunGeneratorIncrementally(
		string initialSource,
		string updatedSource)
	{
		var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-generator-probe"));
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var componentPath = Path.Combine(projectDirectory, "AllCSharpComponent.cs");
		var runtimeTree = CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions);
		var initialTree = CSharpSyntaxTree.ParseText(initialSource, parseOptions, componentPath);
		var compilation = CreateCompilation(runtimeTree, initialTree);
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new HtmxorRouteGenerator().AsSourceGenerator() },
			Array.Empty<AdditionalText>(),
			parseOptions,
			new TestAnalyzerConfigOptionsProvider(projectDirectory),
			new GeneratorDriverOptions(
				IncrementalGeneratorOutputKind.None,
				trackIncrementalGeneratorSteps: true));

		driver = driver.RunGenerators(compilation);
		var updatedTree = CSharpSyntaxTree.ParseText(updatedSource, parseOptions, componentPath);
		var updatedCompilation = compilation.ReplaceSyntaxTree(initialTree, updatedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(
			updatedCompilation,
			out var outputCompilation,
			out var driverDiagnostics);

		return new GeneratorRun(driver.GetRunResult(), outputCompilation, driverDiagnostics);
	}

	private static GeneratorRun RunGeneratorCore(
		string? csharpSource,
		string csharpRelativePath,
		bool includeActionGenerator,
		params string[] relativePaths)
	{
		var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-generator-probe"));
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var syntaxTrees = new List<SyntaxTree>
		{
			CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions),
		};
		if (csharpSource is not null)
		{
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(
				csharpSource,
				parseOptions,
				Path.Combine(projectDirectory, csharpRelativePath)));
		}

		var compilation = CreateCompilation(syntaxTrees.ToArray());
		var generators = includeActionGenerator
			? new[]
			{
				new HtmxorRouteGenerator().AsSourceGenerator(),
				new HtmxorActionGenerator().AsSourceGenerator(),
			}
			: new[] { new HtmxorRouteGenerator().AsSourceGenerator() };
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			generators,
			relativePaths
				.Select(relativePath => new ThrowingAdditionalText(
					Path.Combine(projectDirectory, relativePath)))
				.ToArray<AdditionalText>(),
			parseOptions,
			new TestAnalyzerConfigOptionsProvider(projectDirectory));

		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var driverDiagnostics);

		return new GeneratorRun(driver.GetRunResult(), outputCompilation, driverDiagnostics);
	}

	private static CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTrees)
		=> CSharpCompilation.Create(
			"Htmxor.Consumer.Tests",
			syntaxTrees,
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

	private sealed record GeneratorRun(
		GeneratorDriverRunResult RunResult,
		Compilation OutputCompilation,
		ImmutableArray<Diagnostic> DriverDiagnostics);

	private sealed class ThrowingAdditionalText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("The path-only generator must not read Razor content.");
	}

	private sealed class TestAnalyzerConfigOptionsProvider(string projectDirectory)
		: AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal));

		public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["build_property.RootNamespace"] = "Htmxor.Consumer",
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
