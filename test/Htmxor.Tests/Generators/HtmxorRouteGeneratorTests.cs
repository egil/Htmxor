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
			internal interface IEndpointRouteBuilder;
		}

		namespace Microsoft.AspNetCore.Components.Endpoints.Infrastructure
		{
			internal static class ComponentEndpointConventionBuilderHelper
			{
				internal static global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder GetEndpointRouteBuilder(
					global::Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder builder)
					=> throw new global::System.NotImplementedException();
			}
		}

		namespace Microsoft.AspNetCore.Builder
		{
			internal sealed class RazorComponentsEndpointConventionBuilder;

			internal static class HtmxorComponentEndpointRouteBuilderExtensions
			{
				internal static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
					this RazorComponentsEndpointConventionBuilder builder,
					Routing.IEndpointRouteBuilder endpoints,
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
				builder.AddAttribute(1, "hx-query", "/csharp/42");
				builder.AddAttribute(
					2,
					"ondelete",
					global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(
						this,
						Delete));
				builder.AddAttribute(
					3,
					"onquery",
					global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(
						this,
						Query));
			}

			private void Delete(global::Htmxor.HtmxEventArgs _)
			{
			}

			private void Query(global::Htmxor.HtmxEventArgs _)
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
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains("AddHtmxorEndpoints(", generatedSource, StringComparison.Ordinal);
		Assert.DoesNotContain(
			"Htmxor.Consumer.AllCSharpComponent",
			generatedSource,
			StringComparison.Ordinal);
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
	public void All_CSharp_component_in_arbitrary_file_remains_registered_with_same_named_Razor_input()
	{
		var run = RunGeneratorWithCSharpSourceAtPath(
			AllCSharpComponent,
			"Widgets.cs",
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
	public void Razor_generated_omitted_methods_candidate_does_not_suppress_Razor_manifest()
	{
		const string source = """
			namespace Htmxor.Consumer;

			[global::Htmxor.HtmxRouteAttribute("/razor")]
			internal sealed class RazorControl : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";
		var generatedPath = Path.Combine(
			"obj",
			"generated",
			"Microsoft.CodeAnalysis.Razor.Compiler",
			"Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator",
			"RazorControl_razor.g.cs");
		var run = RunGeneratorWithCSharpSourceAtPath(
			source,
			generatedPath,
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
	public void Application_without_HtmxRoute_declarations_emits_empty_registration_manifest()
	{
		var run = RunGenerator();

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"AddHtmxorEndpoints(",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains(
			"ComponentEndpointConventionBuilderHelper.GetEndpointRouteBuilder(builder)",
			generatedSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain("Htmxor.Consumer.", generatedSource, StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// The manifest is one deterministic, ordinally sorted set of type names, independent of
	/// input order.
	/// </summary>
	[Fact]
	public void Project_root_paths_emit_one_sorted_runtime_manifest_in_ordinal_order()
	{
		const string plainComponent = "<p>Plain</p>\n";
		var forward = RunGeneratorWithRazorContent(
			("ZetaComponent.razor", plainComponent),
			("DeltaComponent.razor", plainComponent),
			("_Imports.razor", "@using System\n"),
			("AlphaComponent.razor", plainComponent),
			("GammaComponent.razor", plainComponent));
		var reverse = RunGeneratorWithRazorContent(
			("GammaComponent.razor", plainComponent),
			("AlphaComponent.razor", plainComponent),
			("_Imports.razor", "@using System\n"),
			("DeltaComponent.razor", plainComponent),
			("ZetaComponent.razor", plainComponent));

		Assert.Empty(forward.DriverDiagnostics);
		var result = Assert.Single(forward.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();
		var reverseSource = Assert.Single(
			Assert.Single(reverse.RunResult.Results).GeneratedSources).SourceText.ToString();

		Assert.Equal(generatedSource, reverseSource);
		Assert.Equal(1, Count(generatedSource, "AddHtmxorAttributedComponentEndpoints("));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.AlphaComponent\""));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.DeltaComponent\""));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.GammaComponent\""));
		Assert.Equal(1, Count(generatedSource, "\"Htmxor.Consumer.ZetaComponent\""));
		AssertInOrder(
			generatedSource,
			"\"Htmxor.Consumer.AlphaComponent\"",
			"\"Htmxor.Consumer.DeltaComponent\"",
			"\"Htmxor.Consumer.GammaComponent\"",
			"\"Htmxor.Consumer.ZetaComponent\"");
		Assert.Contains(
			"typeof(HtmxorGeneratedRouteRegistrationExtensions).Assembly",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Contains("AddGeneratedActions(generatedActions)", generatedSource, StringComparison.Ordinal);
		Assert.Contains(
			"ComponentEndpointConventionBuilderHelper.GetEndpointRouteBuilder(builder)",
			generatedSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain("RouteGroupBuilder", generatedSource, StringComparison.Ordinal);
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

	/// <summary>
	/// The generated manifest's identifier no longer says "project root".
	/// </summary>
	[Fact]
	public void Generated_manifest_identifier_does_not_say_project_root()
	{
		var run = RunGenerator("AlphaComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.DoesNotContain("ProjectRoot", generatedSource, StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// A component's <c>@namespace</c> directive is honoured even when the rest of the file would
	/// break a broader parse.
	/// </summary>
	[Fact]
	public void Namespace_directive_is_honoured_even_when_the_rest_of_the_file_is_unparseable()
	{
		const string garbageWithOverride =
			"@namespace Totally.Override\n" +
			"<p>@{ unbalanced {{{ ]]] ((( not valid Razor or C# at all\n";
		var run = RunGeneratorWithRazorContent(
			("GarbageOverrideComponent.razor", garbageWithOverride));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Equal(
			1,
			Count(generatedSource, "\"Totally.Override.GarbageOverrideComponent\""));
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// An <c>@namespace</c> directive declared directly in the component's own file is used
	/// verbatim, with no folder suffix.
	/// </summary>
	[Fact]
	public void InFile_namespace_override_names_the_manifest_entry_verbatim()
	{
		var run = RunGeneratorWithRazorContent(
			("Components/Pages/OverrideComponent.razor", "@namespace Totally.Different\n<p>Hi</p>\n"));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Totally.Different.OverrideComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// An ancestor <c>_Imports.razor</c>'s <c>@namespace</c> composes with the relative folder
	/// path from that file down to the component, exactly like the default-namespace convention
	/// does for <c>RootNamespace</c>. This row goes two folder levels deep to exercise
	/// multi-segment composition, not just one.
	/// </summary>
	[Fact]
	public void Ancestor_Imports_namespace_composes_with_the_relative_folder_to_the_component()
	{
		var run = RunGeneratorWithRazorContent(
			("Components/ImportsNs/_Imports.razor", "@namespace Probe.ImportsNs\n"),
			("Components/ImportsNs/Deep/Deeper/ImportsComponent.razor", "<p>Hi</p>\n"));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Probe.ImportsNs.Deep.Deeper.ImportsComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// The nearest ancestor <c>_Imports.razor</c> that declares <c>@namespace</c> wins over a
	/// farther one that also declares it.
	/// </summary>
	[Fact]
	public void Nearer_ancestor_Imports_namespace_wins_over_a_farther_one()
	{
		var run = RunGeneratorWithRazorContent(
			("_Imports.razor", "@namespace Root.Override\n"),
			("Nearer/_Imports.razor", "@namespace Nearer.Override\n"),
			("Nearer/Deep/PrecedenceComponent.razor", "<p>Hi</p>\n"));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Nearer.Override.Deep.PrecedenceComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain("Root.Override", generatedSource, StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// An ancestor <c>_Imports.razor</c> with no <c>@namespace</c> directive (only <c>@using</c>
	/// lines, the stock Blazor Web App template shape for <c>Components/_Imports.razor</c>) does
	/// not stop the walk: the nearest ancestor that actually declares <c>@namespace</c> wins,
	/// composed with the relative folder from <em>that</em> file down to the component.
	/// </summary>
	[Fact]
	public void Nearer_Imports_without_a_namespace_directive_is_skipped_for_a_farther_one_that_declares_it()
	{
		var run = RunGeneratorWithRazorContent(
			("Gap/_Imports.razor", "@namespace Gap.Far\n"),
			("Gap/Mid/_Imports.razor", "@using System\n"),
			("Gap/Mid/Leaf/GapComponent.razor", "<p>Hi</p>\n"));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Gap.Far.Mid.Leaf.GapComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// An <c>@namespace</c> directive is honoured even when it is not the file's first line.
	/// </summary>
	[Fact]
	public void Namespace_directive_is_honoured_when_it_is_not_the_files_first_line()
	{
		var run = RunGeneratorWithRazorContent(
			("LateComponent.razor", "@using System\n@namespace Late.Ns\n<p>Hi</p>\n"));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Late.Ns.LateComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// A hyphenated folder segment, which is not a valid C# identifier, gets the same
	/// underscore-joined identifier form in the default namespace that the real Razor SDK gives
	/// it.
	/// </summary>
	[Fact]
	public void Sanitized_folder_segment_gets_its_Razor_identifier_form_in_the_manifest()
	{
		var run = RunGenerator("Components/My-Feature/HyphenComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Htmxor.Consumer.Components.My_Feature.HyphenComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// A folder segment starting with a digit, which is not a valid C# identifier, gets an
	/// underscore prefix in the default namespace -- but the real Razor SDK does not sanitize the
	/// folder name itself anywhere else, so the generator's path-matching must key off the raw
	/// folder name while the manifest entry uses the sanitized namespace segment.
	/// </summary>
	[Fact]
	public void Folder_segment_starting_with_a_digit_gets_an_underscore_prefix_in_the_manifest()
	{
		var run = RunGenerator("Components/1st/DigitComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Htmxor.Consumer.Components._1st.DigitComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// An in-file <c>@namespace</c> wins over an ancestor <c>_Imports.razor</c>'s value, used
	/// verbatim.
	/// </summary>
	[Fact]
	public void InFile_namespace_wins_over_an_ancestor_Imports_namespace()
	{
		var run = RunGeneratorWithRazorContent(
			("_Imports.razor", "@namespace Ignored.Value\n"),
			("InFileWins/OverrideComponent.razor", "@namespace InFile.Override\n<p>Hi</p>\n"));

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"InFile.Override.OverrideComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.DoesNotContain("Ignored.Value", generatedSource, StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// A Razor component below the project directory gets the default namespace the Razor SDK
	/// assigns it: <c>RootNamespace</c> plus the dotted relative folder path.
	/// </summary>
	[Fact]
	public void One_level_nested_folder_Razor_component_gets_the_default_dotted_subfolder_namespace_in_the_manifest()
	{
		var run = RunGenerator("Components/Pages/AlphaComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Htmxor.Consumer.Components.Pages.AlphaComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Fact]
	public void Two_level_nested_folder_Razor_component_gets_the_default_dotted_subfolder_namespace_in_the_manifest()
	{
		var run = RunGenerator("Components/Admin/Reports/NestedComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Htmxor.Consumer.Components.Admin.Reports.NestedComponent\"",
			generatedSource,
			StringComparison.Ordinal);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// Issue #285 cross-wiring: two Razor files sharing a leaf name but sitting in different
	/// folders (so the real Razor SDK gives them different default namespaces) must each get
	/// their own distinct manifest entry; neither name may be dropped or merged into the other.
	/// </summary>
	[Fact]
	public void Same_named_Razor_components_in_different_folders_each_get_their_own_manifest_entry()
	{
		var run = RunGenerator(
			"Areas/Alpha/ReportComponent.razor",
			"Areas/Beta/ReportComponent.razor");

		Assert.Empty(run.DriverDiagnostics);
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Equal(
			1,
			Count(generatedSource, "\"Htmxor.Consumer.Areas.Alpha.ReportComponent\""));
		Assert.Equal(
			1,
			Count(generatedSource, "\"Htmxor.Consumer.Areas.Beta.ReportComponent\""));
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	/// <summary>
	/// An all-C# component is a real compiled symbol, so its namespace never needs path guessing.
	/// </summary>
	[Fact]
	public void All_CSharp_component_outside_the_root_namespace_is_in_generated_registration()
	{
		var source = AllCSharpComponent.Replace(
			"namespace Htmxor.Consumer;",
			"namespace Other.Namespace;",
			StringComparison.Ordinal);
		var run = RunGeneratorWithCSharpSource(source);

		Assert.Empty(run.DriverDiagnostics);
		Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(
			diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var result = Assert.Single(run.RunResult.Results);
		Assert.Empty(result.Diagnostics);
		var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();

		Assert.Contains(
			"\"Other.Namespace.AllCSharpComponent\"",
			generatedSource,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// Issue #285: a <c>.razor.cs</c> partial carrying the attribute is also a real compiled
	/// symbol; moving its sibling <c>.razor</c> file below the project directory must not remove
	/// it from the manifest.
	/// </summary>
	[Fact]
	public void Matching_Razor_code_behind_outside_project_root_emits_one_manifest_entry()
	{
		var run = RunGeneratorWithCSharpSourceAtPath(
			AllCSharpComponent,
			"Components/Pages/AllCSharpComponent.razor.cs",
			"Components/Pages/AllCSharpComponent.razor");

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

	private static void AssertInOrder(string source, params string[] values)
	{
		var indexes = values
			.Select(value => source.IndexOf(value, StringComparison.Ordinal))
			.ToArray();

		Assert.True(indexes.All(index => index >= 0), source);
		Assert.True(indexes.SequenceEqual(indexes.OrderBy(index => index)), source);
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
				.Select(relativePath => (AdditionalText)new TextAdditionalText(
					Path.Combine(projectDirectory, relativePath),
					"<p></p>\n"))
				.ToArray(),
			parseOptions,
			new TestAnalyzerConfigOptionsProvider(projectDirectory));

		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var driverDiagnostics);

		return new GeneratorRun(driver.GetRunResult(), outputCompilation, driverDiagnostics);
	}

	/// <summary>
	/// The manifest generator reads the <c>@namespace</c> directive from a component's own file
	/// and from its ancestor <c>_Imports.razor</c> files; this runner supplies each file's exact
	/// content so a row can exercise that directly.
	/// </summary>
	private static GeneratorRun RunGeneratorWithRazorContent(
		params (string RelativePath, string Content)[] files)
	{
		var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-generator-probe"));
		var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CreateCompilation(CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions));
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new HtmxorRouteGenerator().AsSourceGenerator() },
			files
				.Select(file => (AdditionalText)new TextAdditionalText(
					Path.Combine(projectDirectory, file.RelativePath),
					file.Content))
				.ToArray(),
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
