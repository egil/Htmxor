using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorRouteDeclarationAnalyzerTests
{
	private const string RootNamespace = "Htmxor.Consumer";
	private static readonly string ProjectDirectory = Path.GetFullPath(
		Path.Combine(Path.GetTempPath(), "htmxor-analyzer-tests"));
	private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

	[Fact]
	public async Task All_CSharp_component_with_explicit_methods_is_supported()
	{
		var componentPath = ComponentPath("AllCSharpComponent.cs");
		var source = $$"""
			namespace {{RootNamespace}};

			[global::Htmxor.HtmxRouteAttribute("/csharp/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("csharp.read")]
			public sealed class AllCSharpComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			Array.Empty<string>(),
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task All_CSharp_component_in_arbitrary_file_is_supported_despite_unrelated_Razor_type()
	{
		var componentPath = ComponentPath("Widgets.cs");
		var razorPath = ComponentPath("AllCSharpComponent.razor");
		var generatedSource = """
			namespace Other;

			public partial class AllCSharpComponent :
				global::Microsoft.AspNetCore.Components.ComponentBase
			{
			}
			""";
		var source = $$"""
			namespace {{RootNamespace}};

			[global::Htmxor.HtmxRouteAttribute("/csharp/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("csharp.read")]
			public sealed class AllCSharpComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { generatedSource, source },
			new[] { razorPath },
			new[] { RazorGeneratedPath("AllCSharpComponent"), componentPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Matching_Razor_code_behind_with_explicit_methods_is_supported()
	{
		var componentPath = ComponentPath("CodeBehindComponent.razor.cs");
		var razorPath = ComponentPath("CodeBehindComponent.razor");
		var generatedSource = $$"""
			namespace {{RootNamespace}};

			public partial class CodeBehindComponent :
				global::Microsoft.AspNetCore.Components.ComponentBase
			{
			}
			""";
		var codeBehindSource = $$"""
			namespace {{RootNamespace}};

			[global::Htmxor.HtmxRouteAttribute("/code-behind/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("code-behind.read")]
			public sealed partial class CodeBehindComponent;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { generatedSource, codeBehindSource },
			new[] { razorPath },
			new[] { RazorGeneratedPath("CodeBehindComponent"), componentPath });

		Assert.Empty(diagnostics);
	}

	[Theory]
	[InlineData("Other.cs")]
	[InlineData("Other.razor.cs")]
	public async Task Explicit_route_in_nonmatching_CSharp_partial_reports_nonconfigurable_error(
		string relativePath)
	{
		var componentPath = ComponentPath(relativePath);
		var razorPath = ComponentPath("CodeBehindComponent.razor");
		var generatedSource = $$"""
			namespace {{RootNamespace}};

			public partial class CodeBehindComponent :
				global::Microsoft.AspNetCore.Components.ComponentBase
			{
			}
			""";
		var csharpSource = $$"""
			namespace {{RootNamespace}};

			[global::Htmxor.HtmxRouteAttribute("/code-behind/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("code-behind.read")]
			public sealed partial class CodeBehindComponent;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { generatedSource, csharpSource },
			new[] { razorPath },
			new[] { RazorGeneratedPath("CodeBehindComponent"), componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported HTMX-only route declaration: " +
			"a C# HtmxRoute declaration on a Razor component must use the matching .razor.cs partial",
			diagnostic.GetMessage());
		Assert.Equal(componentPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task All_CSharp_component_without_methods_reports_nonconfigurable_error()
	{
		var componentPath = ComponentPath("AllCSharpComponent.cs");
		var source = $$"""
			namespace {{RootNamespace}};

			[global::Htmxor.HtmxRouteAttribute("/csharp/{Id:int}")]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("csharp.read")]
			public sealed class AllCSharpComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			Array.Empty<string>(),
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported HTMX-only route declaration: " +
			"a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods",
			diagnostic.GetMessage());
		Assert.Equal(componentPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task Matching_Razor_code_behind_without_methods_reports_nonconfigurable_error()
	{
		var componentPath = ComponentPath("CodeBehindComponent.razor.cs");
		var source = $$"""
			namespace {{RootNamespace}};

			[global::Htmxor.HtmxRouteAttribute("/code-behind/{Id:int}")]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("code-behind.read")]
			public sealed partial class CodeBehindComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { ComponentPath("CodeBehindComponent.razor") },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported HTMX-only route declaration: " +
			"a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods",
			diagnostic.GetMessage());
		Assert.Equal(componentPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task CSharp_line_mapping_cannot_waive_the_methods_requirement()
	{
		var componentPath = ComponentPath("AllCSharpComponent.cs");
		var razorPath = ComponentPath("AllCSharpComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}};

			#line 12 "{{EscapePath(razorPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/csharp/{Id:int}")]
			#line default
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("csharp.read")]
			public sealed class AllCSharpComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { razorPath },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(
			"Unsupported HTMX-only route declaration: " +
			"a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods",
			diagnostic.GetMessage());
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task Same_named_Razor_binding_cannot_attach_to_all_CSharp_component()
	{
		var componentPath = ComponentPath("ReportComponent.cs");
		var razorPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Htmxor.HtmxRouteAttribute("/reports/{Id:int}", Methods = ["GET", "DELETE"])]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task DeleteReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		var razor = new SourceAdditionalText(
			razorPath,
			"""
			<button @ondelete="DeleteReport">Delete</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(
			source,
			razor,
			sourcePath: componentPath);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		// Issue #285: the "project-root" eligibility gate is gone, but a same-named Razor
		// file still must not cross-wire onto an unrelated compiled type that merely shares its
		// guessed name; this remains a fail-closed mismatch regardless of wording.
		Assert.Contains(
			"the action owner must compile from the matching",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
		Assert.DoesNotContain("project-root", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(razorPath, diagnostic.Location.GetLineSpan().Path);
	}

	[Fact]
	public async Task Unsupported_second_declaration_reports_one_mapped_nonconfigurable_diagnostic()
	{
		var reportPath = ComponentPath("ReportComponent.razor");
		var summaryPath = ComponentPath("SummaryComponent.razor");
		var report = ComponentSource(
			"ReportComponent",
			reportPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/reports/{ReportId:int}\", Methods = [\"GET\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"reports.read\")]");
		var summary = ComponentSource(
			"SummaryComponent",
			summaryPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/summaries/{SummaryId:int}\", Methods = [\"TRACE\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"summaries.read\")]");

		var diagnostics = await RunAnalyzerAsync(
			new[] { report, summary },
			new[] { reportPath, summaryPath });
		var reversedDiagnostics = await RunAnalyzerAsync(
			new[] { summary, report },
			new[] { summaryPath, reportPath });

		var diagnostic = Assert.Single(diagnostics);
		var reversedDiagnostic = Assert.Single(reversedDiagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains("GET", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(summaryPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(diagnostic.GetMessage(), reversedDiagnostic.GetMessage());
		Assert.Equal(
			diagnostic.Location.GetMappedLineSpan().Path,
			reversedDiagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Compiler_bound_aliases_constants_and_array_forms_are_supported()
	{
		var reportPath = ComponentPath("ReportComponent.razor");
		var summaryPath = ComponentPath("SummaryComponent.razor");
		var report = BoundComponentSource(
			"ReportComponent",
			reportPath,
			"/reports/{ReportId:int}",
			"reports.read",
			"new[] { GetMethod }",
			usePolicyConstructor: false);
		var summary = BoundComponentSource(
			"SummaryComponent",
			summaryPath,
			"/summaries/{SummaryId:int}",
			"summaries.read",
			"[GetMethod]",
			usePolicyConstructor: true);

		var diagnostics = await RunAnalyzerAsync(
			new[] { summary, report },
			new[] { summaryPath, reportPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A second authorization declaration -- here a custom type that is itself an
	/// <c>IAuthorizeData</c> -- combines with the standard policy exactly as stock <c>@page</c>
	/// combines multiple authorization attributes.
	/// </summary>
	[Fact]
	public async Task Custom_authorization_metadata_alongside_standard_policy_combines_without_diagnostics()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace CustomSecurity
			{
			public sealed class ExtraAuthorizationAttribute :
				global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
			}

			namespace {{RootNamespace}}
			{
			#line 30 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("items.read")]
			[global::CustomSecurity.ExtraAuthorizationAttribute]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A custom <c>IAllowAnonymous</c> type allows anonymous access exactly as stock does when any
	/// <c>IAllowAnonymous</c> metadata is present.
	/// </summary>
	[Fact]
	public async Task Custom_anonymous_metadata_allows_anonymous_without_diagnostics()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace CustomSecurity
			{
			[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = true)]
			public sealed class ExtraAnonymousAttribute :
				global::System.Attribute,
				global::Microsoft.AspNetCore.Authorization.IAllowAnonymous;
			}

			namespace {{RootNamespace}}
			{
			#line 30 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("items.read")]
			[global::CustomSecurity.ExtraAnonymousAttribute]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Stock <c>@page</c> honors any <c>IAuthorizeData</c>, not only the standard <c>Authorize</c>
	/// attribute: a lone custom type that implements <c>IAuthorizeData</c> directly, without
	/// deriving from <c>AuthorizeAttribute</c>, is accepted as the component's sole authorization
	/// declaration. This is stock metadata read by the stock authorization middleware, not a
	/// custom <c>IAuthorizationHandler</c> or requirement.
	/// </summary>
	[Fact]
	public async Task Custom_IAuthorizeData_only_metadata_reports_no_diagnostics()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace CustomSecurity
			{
			public sealed class CustomAuthorizeDataAttribute :
				global::System.Attribute,
				global::Microsoft.AspNetCore.Authorization.IAuthorizeData
			{
				public string? Policy { get; set; }
				public string? Roles { get; set; }
				public string? AuthenticationSchemes { get; set; }
			}
			}

			namespace {{RootNamespace}}
			{
			#line 30 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			[global::CustomSecurity.CustomAuthorizeDataAttribute(Policy = "items.read")]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Route_representation_filters_are_supported_for_compiled_declarations()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"], CurrentUrl = \"/orders\", Target = \"section#result\", Targets = [\"section#result\", \"div#fallback\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Null_targets_are_supported_for_compiled_declarations()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"], Targets = null!)]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	[Theory]
	[InlineData("CurrentUrl = \"ftp://example.test/orders\"", "CurrentUrl")]
	[InlineData("Target = \"section#\"", "Target")]
	[InlineData("Targets = [\"section\", \" \"]", "Targets")]
	public async Task Invalid_route_representation_values_report_nonconfigurable_error(
		string namedArgument,
		string memberName)
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			$"[global::Htmxor.HtmxRouteAttribute(\"/items/{{Id:int}}\", Methods = [\"GET\"], {namedArgument})]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(
			$"HtmxRoute named argument '{memberName}'",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Theory]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"TRACE\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"GET, POST, PUT, PATCH, DELETE, and QUERY")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]\n[global::Htmxor.HtmxRouteAttribute(\"/other/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"exactly one HtmxRoute")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id=foo:bar}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}/{broken\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"constrained")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]\n[global::Microsoft.AspNetCore.Components.RouteAttribute(\"/normal/{Id:int}\")]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"normal Blazor route")]
	public async Task Unsupported_bound_metadata_fails_closed(
		string routeAttribute,
		string authorizationAttribute,
		string expectedReason)
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			routeAttribute,
			authorizationAttribute);

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains(expectedReason, diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	/// <summary>
	/// <c>HtmxRoute</c> authorization metadata is accepted exactly as on a stock <c>@page</c>: any
	/// stock authorization metadata, or none, and the application's <c>FallbackPolicy</c> covers
	/// the no-metadata case as it does for any other endpoint.
	/// </summary>
	[Theory]
	[InlineData("", "no authorization metadata")]
	[InlineData(
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute]",
		"bare [Authorize]")]
	[InlineData(
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(Policy = \"items.read\")]",
		"[Authorize(Policy = ...)]")]
	[InlineData(
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(Roles = \"admin\")]",
		"[Authorize(Roles = ...)]")]
	[InlineData(
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(AuthenticationSchemes = \"custom-scheme\")]",
		"[Authorize(AuthenticationSchemes = ...)]")]
	[InlineData(
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(Roles = \"admin\")]\n[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(Policy = \"items.read\")]",
		"two [Authorize]")]
	[InlineData(
		"[global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]",
		"[AllowAnonymous]")]
	public async Task Supported_authorization_metadata_shapes_report_no_diagnostics(
		string authorizationAttribute,
		string shape)
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
			authorizationAttribute);

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.True(diagnostics.IsEmpty, $"Shape '{shape}' must not report a diagnostic: {string.Join(", ", diagnostics.Select(d => d.GetMessage()))}");
	}

	/// <summary>
	/// An <c>[Authorize(Policy = ...)]</c> declared on a base component is effective for a derived
	/// <c>HtmxRoute</c> component, regardless of which type in the hierarchy declares it.
	/// </summary>
	[Fact]
	public async Task Authorize_inherited_from_a_base_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("items.read")]
			public abstract class ItemComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase;

			#line 20 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			#line default
			public sealed class ItemComponent : ItemComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Combining <c>[Authorize]</c> with <c>[AllowAnonymous]</c> matches stock: any
	/// <c>IAllowAnonymous</c> metadata allows anonymous access regardless of any
	/// <c>IAuthorizeData</c> also present.
	/// </summary>
	[Fact]
	public async Task Authorize_combined_with_AllowAnonymous_reports_no_diagnostics()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]\n[global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Route_target_that_is_not_a_component_fails_closed()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = ComponentSource(
			"ItemComponent",
			componentPath,
			"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
			"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
			"global::System.Object");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("Blazor component", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	/// <summary>
	/// A nested-folder <c>HtmxRoute</c> component's tree has the real Razor-generated path for a
	/// component below the project directory: the generator mirrors the component's relative
	/// folder under its own output directory, not the flat <c>&lt;name&gt;_razor.g.cs</c> shape a
	/// project-root file gets. A lookalike attribute type from an unrelated namespace must stay
	/// ignored either way.
	/// </summary>
	[Fact]
	public async Task Nested_folder_HtmxRoute_component_is_discovered_through_its_real_mirrored_generated_path_while_lookalike_is_ignored()
	{
		var nestedPath = ComponentPath(Path.Combine("Nested", "NestedComponent.razor"));
		var nestedGeneratedPath = RazorGeneratedPath(Path.Combine("Nested", "NestedComponent"));
		var lookalikePath = ComponentPath("LookalikeComponent.razor");
		var nested = $$"""
			namespace {{RootNamespace}}.Nested
			{
			#line 12 "{{EscapePath(nestedPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/nested/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("nested.read")]
			#line default
			public sealed class NestedComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";
		var lookalike = $$"""
			namespace Lookalike
			{
			[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
			public sealed class HtmxRouteAttribute(string template) : global::System.Attribute;
			}

			namespace {{RootNamespace}}
			{
			#line 40 "{{EscapePath(lookalikePath)}}"
			[global::Lookalike.HtmxRouteAttribute("/lookalike/{Id:int}")]
			#line default
			public sealed class LookalikeComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { lookalike, nested },
			new[] { lookalikePath, nestedPath },
			new[] { RazorGeneratedPath("LookalikeComponent"), nestedGeneratedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Guards <c>HtmxorRouteManifest.HasCompiledRazorDeclaration</c>'s generated-paths overload
	/// against cross-wiring: two real <c>.razor</c> files with the same leaf name, in different
	/// folders, whose <em>derived</em> names collide on the exact same string. <c>Components/A/X.razor</c>
	/// is the real, attributed component, compiled to a namespace its own evidence never guesses.
	/// <c>Components/B/X.razor</c> is an unrelated, non-routed file whose evidence hides an
	/// <c>@namespace</c> line inside a Razor comment -- real Razor ignores a commented-out directive
	/// like this (confirmed empirically: an SDK build of this exact shape compiles to the ordinary
	/// folder-default namespace instead), but the narrow, comment-unaware scan this analyzer uses to
	/// avoid reading full Razor content does not, so it wrongly guesses A's real namespace for B.
	/// That makes the manifest lookup group both files' generated paths under one key: only the
	/// mirrored-folder comparison, not the shared leaf file name, can tell A's own declaration apart
	/// from B's. A must fail closed rather than being confirmed through B's generated path.
	/// </summary>
	[Fact]
	public async Task Colliding_guess_from_a_different_real_file_fails_closed_instead_of_cross_wiring()
	{
		var pathA = ComponentPath(Path.Combine("Components", "A", "X.razor"));
		var pathB = ComponentPath(Path.Combine("Components", "B", "X.razor"));
		var generatedPathA = RazorGeneratedPath(Path.Combine("Components", "A", "X"));
		var source = $$"""
			namespace Confusable
			{
			#line 1 "{{EscapePath(pathA)}}"
			[global::Htmxor.HtmxRouteAttribute("/confusable/{Id:int}", Methods = ["GET"])]
			#line default
			public sealed class X : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[]
			{
				(pathA, PlainRazorContent),
				(pathB, "@*\n@namespace Confusable\n*@\n<p>Hi</p>\n"),
			},
			new[] { generatedPathA });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Contains(
			"the HtmxRoute component must compile from the matching Razor component",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
		Assert.Equal(pathA, diagnostic.Location.GetMappedLineSpan().Path);
	}

	/// <summary>
	/// Issue #285's own named placement: a <c>Components/Pages</c> component (one folder below
	/// the project directory, default namespace) must be discovered exactly like a project-root
	/// file, using the component's real one-level mirrored generated path.
	/// </summary>
	[Fact]
	public async Task HtmxRoute_component_in_Components_Pages_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("Components", "Pages", "AlphaComponent.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Components", "Pages", "AlphaComponent"));
		var source = $$"""
			namespace {{RootNamespace}}.Components.Pages
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/alpha/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("alpha.read")]
			#line default
			public sealed class AlphaComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath },
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// An all-C# component is a real compiled symbol regardless of namespace: no path guessing is
	/// ever involved.
	/// </summary>
	[Fact]
	public async Task All_CSharp_HtmxRoute_component_outside_the_root_namespace_reports_no_diagnostics()
	{
		var componentPath = ComponentPath("OutsideComponent.cs");
		var source = """
			namespace Other.Namespace
			{
			[global::Htmxor.HtmxRouteAttribute("/outside/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("outside.read")]
			public sealed class OutsideComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			Array.Empty<string>(),
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A <c>.razor.cs</c> partial carrying the attribute is also a real compiled symbol; its
	/// sibling <c>.razor</c> file may live anywhere below the project directory.
	/// </summary>
	[Fact]
	public async Task Razor_code_behind_HtmxRoute_declaration_outside_project_root_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("Components", "Pages", "CodeBehindComponent.razor.cs"));
		var razorPath = ComponentPath(Path.Combine("Components", "Pages", "CodeBehindComponent.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Components", "Pages", "CodeBehindComponent"));
		var generatedSource = $$"""
			namespace {{RootNamespace}}.Components.Pages;

			public partial class CodeBehindComponent :
				global::Microsoft.AspNetCore.Components.ComponentBase
			{
			}
			""";
		var codeBehindSource = $$"""
			namespace {{RootNamespace}}.Components.Pages;

			[global::Htmxor.HtmxRouteAttribute("/code-behind/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("code-behind.read")]
			public sealed partial class CodeBehindComponent;
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { generatedSource, codeBehindSource },
			new[] { razorPath },
			new[] { generatedPath, componentPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// An <c>@namespace</c> override declared directly in the component's own file: the compiled
	/// namespace is exactly the override, with no folder suffix, and the analyzer confirms it
	/// against the component's own real content.
	/// </summary>
	[Fact]
	public async Task InFile_namespace_override_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("Components", "Pages", "OverrideComponent.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Components", "Pages", "OverrideComponent"));
		var source = $$"""
			namespace Totally.Different
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/override/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("override.read")]
			#line default
			public sealed class OverrideComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { (componentPath, "@namespace Totally.Different\n<p>Hi</p>\n") },
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// An ancestor <c>_Imports.razor</c>'s <c>@namespace</c> composed with a two-level-deep
	/// relative folder: the analyzer confirms the compiled namespace against the component's own
	/// plain content plus the ancestor file's real <c>@namespace</c> content.
	/// </summary>
	[Fact]
	public async Task Ancestor_Imports_namespace_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(
			Path.Combine("Components", "ImportsNs", "Deep", "Deeper", "ImportsComponent.razor"));
		var importsPath = ComponentPath(Path.Combine("Components", "ImportsNs", "_Imports.razor"));
		var generatedPath = RazorGeneratedPath(
			Path.Combine("Components", "ImportsNs", "Deep", "Deeper", "ImportsComponent"));
		var source = $$"""
			namespace Probe.ImportsNs.Deep.Deeper
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/imports-ns/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("imports-ns.read")]
			#line default
			public sealed class ImportsComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[]
			{
				(componentPath, PlainRazorContent),
				(importsPath, "@namespace Probe.ImportsNs\n"),
			},
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The nearest ancestor <c>_Imports.razor</c> wins over a farther one: the analyzer confirms
	/// the compiled namespace against both ancestor files' real content.
	/// </summary>
	[Fact]
	public async Task Nearer_ancestor_Imports_namespace_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(
			Path.Combine("Nearer", "Deep", "PrecedenceComponent.razor"));
		var rootImportsPath = ComponentPath("_Imports.razor");
		var nearerImportsPath = ComponentPath(Path.Combine("Nearer", "_Imports.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Nearer", "Deep", "PrecedenceComponent"));
		var source = $$"""
			namespace Nearer.Override.Deep
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/nearer/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("nearer.read")]
			#line default
			public sealed class PrecedenceComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[]
			{
				(componentPath, PlainRazorContent),
				(rootImportsPath, "@namespace Root.Override\n"),
				(nearerImportsPath, "@namespace Nearer.Override\n"),
			},
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// An in-file <c>@namespace</c> wins over an ancestor <c>_Imports.razor</c>'s value: the
	/// analyzer confirms the compiled namespace against the component's own override content,
	/// ignoring the ancestor's different value.
	/// </summary>
	[Fact]
	public async Task InFile_namespace_wins_over_ancestor_Imports_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("InFileWins", "OverrideComponent.razor"));
		var importsPath = ComponentPath("_Imports.razor");
		var generatedPath = RazorGeneratedPath(Path.Combine("InFileWins", "OverrideComponent"));
		var source = $$"""
			namespace InFile.Override
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/infile-wins/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("infile-wins.read")]
			#line default
			public sealed class OverrideComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[]
			{
				(componentPath, "@namespace InFile.Override\n<p>Hi</p>\n"),
				(importsPath, "@namespace Ignored.Value\n"),
			},
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A derived name that diverges from the real compiled declaration -- for any reason,
	/// including a scan miss on a shape this contract does not cover -- must fail closed, never
	/// silently registering the wrong type or cross-wiring onto an unrelated one. Here the
	/// component's real content has no <c>@namespace</c> and no ancestor <c>_Imports.razor</c>
	/// declares one either, so the correct derivation is the default convention
	/// (<c>Htmxor.Consumer.Components.Pages.OverrideComponent</c>), while the compiled
	/// declaration sits in an unrelated namespace.
	/// </summary>
	[Fact]
	public async Task Namespace_override_divorced_from_the_path_guess_fails_closed_instead_of_cross_wiring()
	{
		var componentPath = ComponentPath(Path.Combine("Components", "Pages", "OverrideComponent.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Components", "Pages", "OverrideComponent"));
		var source = $$"""
			namespace Totally.Different
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/override/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("override.read")]
			#line default
			public sealed class OverrideComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { (componentPath, PlainRazorContent) },
			new[] { generatedPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR001", diagnostic.Id);
		Assert.Equal(componentPath, diagnostic.Location.GetMappedLineSpan().Path);
	}

	/// <summary>
	/// An ancestor <c>_Imports.razor</c> with no <c>@namespace</c> directive (only <c>@using</c>
	/// lines) does not stop the walk: the nearest ancestor that actually declares <c>@namespace</c>
	/// wins, composed with the relative folder from that file down to the component.
	/// </summary>
	[Fact]
	public async Task Farther_ancestor_Imports_namespace_wins_when_the_nearer_one_only_has_using_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("Gap", "Mid", "Leaf", "GapComponent.razor"));
		var farImportsPath = ComponentPath(Path.Combine("Gap", "_Imports.razor"));
		var midImportsPath = ComponentPath(Path.Combine("Gap", "Mid", "_Imports.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Gap", "Mid", "Leaf", "GapComponent"));
		var source = $$"""
			namespace Gap.Far.Mid.Leaf
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/gap/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("gap.read")]
			#line default
			public sealed class GapComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[]
			{
				(componentPath, PlainRazorContent),
				(farImportsPath, "@namespace Gap.Far\n"),
				(midImportsPath, "@using System\n"),
			},
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// An <c>@namespace</c> directive is honoured even when it is not the file's first line.
	/// </summary>
	[Fact]
	public async Task Namespace_directive_not_on_the_first_line_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath("LateComponent.razor");
		var generatedPath = RazorGeneratedPath("LateComponent");
		var source = $$"""
			namespace Late.Ns
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/late-ns/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("late-ns.read")]
			#line default
			public sealed class LateComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { (componentPath, "@using System\n@namespace Late.Ns\n<p>Hi</p>\n") },
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A folder segment that is not a valid C# identifier (a hyphen) gets the same sanitized
	/// identifier form in both the compiled namespace and the generator's mirrored path that the
	/// real Razor SDK gives it.
	/// </summary>
	[Fact]
	public async Task Sanitized_folder_segment_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("Components", "My-Feature", "HyphenComponent.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Components", "My_Feature", "HyphenComponent"));
		var source = $$"""
			namespace {{RootNamespace}}.Components.My_Feature
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/hyphen/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("hyphen.read")]
			#line default
			public sealed class HyphenComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { (componentPath, PlainRazorContent) },
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A folder segment starting with a digit, which is not a valid C# identifier, diverges from
	/// the hyphen case above: the real Razor SDK sanitizes only the compiled namespace
	/// (<c>_1st</c>) and keeps the raw folder name (<c>1st</c>) in its own mirrored generated
	/// path. The analyzer must confirm the sanitized namespace against that raw-path declaration
	/// rather than expect both to sanitize together.
	/// </summary>
	[Fact]
	public async Task Leading_digit_folder_segment_component_reports_no_diagnostics()
	{
		var componentPath = ComponentPath(Path.Combine("Components", "1st", "DigitComponent.razor"));
		var generatedPath = RazorGeneratedPath(Path.Combine("Components", "1st", "DigitComponent"));
		var source = $$"""
			namespace {{RootNamespace}}.Components._1st
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/digit/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("digit.read")]
			#line default
			public sealed class DigitComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { (componentPath, PlainRazorContent) },
			new[] { generatedPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// Issue #285 acceptance criterion: "the two 'project-root' messages are gone." Both
	/// HTMXOR001 and HTMXOR002 still fail closed on a mismatch (proven above and in
	/// <see cref="Same_named_Razor_binding_cannot_attach_to_all_CSharp_component"/>); neither may
	/// explain that failure by naming "project-root" as the eligibility rule.
	/// </summary>
	[Fact]
	public async Task Project_root_wording_is_gone_from_both_fail_closed_diagnostics()
	{
		var overridePath = ComponentPath(Path.Combine("Components", "Pages", "WordingComponent.razor"));
		var overrideGeneratedPath = RazorGeneratedPath(Path.Combine("Components", "Pages", "WordingComponent"));
		var overrideSource = $$"""
			namespace Totally.Different.Wording
			{
			#line 1 "{{EscapePath(overridePath)}}"
			[global::Htmxor.HtmxRouteAttribute("/wording/{Id:int}", Methods = ["GET"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("wording.read")]
			#line default
			public sealed class WordingComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";
		var routeDiagnostics = await RunAnalyzerAsync(
			new[] { overrideSource },
			new[] { overridePath },
			new[] { overrideGeneratedPath });

		var actionComponentPath = ComponentPath("WordingActionComponent.cs");
		var actionRazorPath = ComponentPath("WordingActionComponent.razor");
		var actionSource = $$"""
			namespace {{RootNamespace}}
			{
			[global::Htmxor.HtmxRouteAttribute("/wording-action/{Id:int}", Methods = ["GET", "DELETE"])]
			public sealed class WordingActionComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task DeleteWording(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		var actionRazor = new SourceAdditionalText(
			actionRazorPath,
			"""
			<button @ondelete="DeleteWording">Delete</button>
			""");
		var actionDiagnostics = await RunActionAnalyzerAsync(
			actionSource,
			actionRazor,
			sourcePath: actionComponentPath);

		Assert.NotEmpty(routeDiagnostics);
		Assert.NotEmpty(actionDiagnostics);
		Assert.All(
			routeDiagnostics.Concat(actionDiagnostics),
			diagnostic => Assert.DoesNotContain(
				"project-root",
				diagnostic.GetMessage(),
				StringComparison.Ordinal));
	}

	[Fact]
	public async Task Arbitrary_supported_component_count_reports_no_diagnostics()
	{
		var paths = Enumerable.Range(0, 8)
			.Select(index => ComponentPath($"Component{index}.razor"))
			.ToArray();
		var sources = paths.Select((path, index) => ComponentSource(
			Path.GetFileNameWithoutExtension(path),
			path,
			$"[global::Htmxor.HtmxRouteAttribute(\"/items/{{Id{index}:int}}\", Methods = [\"GET\"])]",
			$"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.{index}.read\")]"));

		var diagnostics = await RunAnalyzerAsync(sources, paths);

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Htmx_route_originating_from_imports_fails_closed()
	{
		var componentPath = ComponentPath("ItemComponent.razor");
		var importsPath = ComponentPath("_Imports.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(importsPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}")]
			#line 20 "{{EscapePath(componentPath)}}"
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("items.read")]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath, importsPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("HtmxRoute declarations from _Imports.razor are not supported", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(importsPath, diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Htmx_route_mapped_to_a_backslash_separated_Imports_path_fails_closed()
	{
		// Mapped paths may use either separator, whatever the host OS. On a Unix host this
		// backslash path catches a check built on Path.GetFileName, which splits only on '/'.
		var componentPath = ComponentPath("BackslashImportsComponent.razor");
		var importsPath = "C:\\Proj\\_Imports.razor";
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(importsPath)}}"
			[global::Htmxor.HtmxRouteAttribute("/backslash-imports/{Id:int}")]
			#line 20 "{{EscapePath(componentPath)}}"
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("backslash.read")]
			#line default
			public sealed class BackslashImportsComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath, importsPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("HtmxRoute declarations from _Imports.razor are not supported", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(importsPath, diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task Htmx_route_on_a_component_file_merely_ending_in_Imports_razor_reports_no_diagnostics()
	{
		// "Admin_Imports.razor" ends with "_Imports.razor" but is an ordinary component file, not
		// the special _Imports.razor, so its HtmxRoute is the component's own declaration.
		var adminImportsPath = ComponentPath("Admin_Imports.razor");
		var source = ComponentSource(
			Path.GetFileNameWithoutExtension(adminImportsPath),
			adminImportsPath,
			"""[global::Htmxor.HtmxRouteAttribute("/admin/{Id:int}", Methods = ["GET"])]""",
			"""[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("admin.read")]""");

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { adminImportsPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_declared_in_Imports_reports_HTMXOR003()
	{
		// Cause (a): the marker declared in _Imports.razor instead of the routed type's own
		// declaration, mirroring the existing HtmxRoute-from-_Imports rule above.
		var componentPath = ComponentPath("ItemComponent.razor");
		var importsPath = ComponentPath("_Imports.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(importsPath)}}"
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			#line 20 "{{EscapePath(componentPath)}}"
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/items/{Id:int}")]
			#line default
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath, importsPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR003", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported normal-only route declaration: " +
			"DisableHtmxDirectRouting declarations from _Imports.razor are not supported",
			diagnostic.GetMessage());
		var mappedSpan = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal(importsPath, mappedSpan.Path);
		Assert.Equal(new LinePosition(0, 1), mappedSpan.StartLinePosition);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_on_a_component_file_merely_ending_in_Imports_razor_is_not_cause_a()
	{
		// A component-local declaration in a file whose name merely ends with "_Imports.razor" (for
		// example "Admin_Imports.razor") is not the special _Imports.razor; the marker and the stock
		// Route both belong to this routed type's own declaration, which is exactly what cause (a)
		// must not flag. Only a file actually named _Imports.razor is cause (a).
		var adminImportsPath = ComponentPath("Admin_Imports.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(adminImportsPath)}}"
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/admin")]
			#line default
			public sealed class AdminImports : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { adminImportsPath });

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_on_an_abstract_type_without_a_local_stock_route_reports_HTMXOR003()
	{
		// Cause (b): the marker on an abstract type that has no local stock route of its own.
		var componentPath = ComponentPath("ItemComponentBase.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			public abstract class ItemComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase;
			#line default
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR003", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported normal-only route declaration: " +
			"DisableHtmxDirectRouting requires a local stock @page or Route declaration",
			diagnostic.GetMessage());
		var mappedSpan = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal(componentPath, mappedSpan.Path);
		Assert.Equal(new LinePosition(0, 1), mappedSpan.StartLinePosition);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_on_a_concrete_base_of_a_routed_component_reports_HTMXOR003_at_the_base()
	{
		// Cause (b), the inheritance edge: a concrete base class carries the marker, and its derived
		// component declares its own stock route. A check that treats the marker as effective through
		// inheritance would stay silent here. Only the marker's own line maps to basePath; both type
		// identifiers stay on the generated path, so a diagnostic anywhere but the base's marker
		// attribute fails the path and position assertions below.
		var basePath = ComponentPath("ItemComponentBase.razor");
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(basePath)}}"
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			#line default
			public class ItemComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase;
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/items/{Id:int}")]
			#line default
			public sealed class ItemComponent : ItemComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { basePath, componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR003", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported normal-only route declaration: " +
			"DisableHtmxDirectRouting requires a local stock @page or Route declaration",
			diagnostic.GetMessage());
		var mappedSpan = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal(basePath, mappedSpan.Path);
		Assert.Equal(new LinePosition(0, 1), mappedSpan.StartLinePosition);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_on_a_non_component_type_reports_HTMXOR003()
	{
		// Cause (b): a non-component type, which likewise has no local stock route. Its real source
		// is a .cs file (a .razor file always compiles to a component), so this fixture compiles
		// directly at componentPath instead of simulating a Razor-generated path.
		var componentPath = ComponentPath("MarkedPlainClass.cs");
		const string source = """
			namespace Htmxor.Consumer;

			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			public sealed class MarkedPlainClass;
			""";
		var expectedPosition = SourceText.From(source).Lines.GetLinePosition(
			source.IndexOf("global::Htmxor.DisableHtmxDirectRoutingAttribute", StringComparison.Ordinal));

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			Array.Empty<string>(),
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR003", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported normal-only route declaration: " +
			"DisableHtmxDirectRouting requires a local stock @page or Route declaration",
			diagnostic.GetMessage());
		var mappedSpan = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal(componentPath, mappedSpan.Path);
		Assert.Equal(expectedPosition, mappedSpan.StartLinePosition);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_combined_with_HtmxRoute_reports_HTMXOR003()
	{
		// Cause (c): the marker combined with HtmxRoute on the same type. Only the marker's own line
		// maps to componentPath; HtmxRoute and the class declaration stay on the unmapped generated
		// path, so reporting at either of those fails both the path and the position assertion below.
		// HtmxorAttributedRouteCatalogTests pins that the runtime counterpart (ValidateDeclaration)
		// must also throw for the same combination.
		var componentPath = ComponentPath("MarkedReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Htmxor.HtmxRouteAttribute("/reports/{Id:int}", Methods = ["GET"])]
			#line 1 "{{EscapePath(componentPath)}}"
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			#line default
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("reports.read")]
			public sealed class MarkedReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR003", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported normal-only route declaration: " +
			"DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type",
			diagnostic.GetMessage());
		var mappedSpan = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal(componentPath, mappedSpan.Path);
		Assert.Equal(new LinePosition(0, 1), mappedSpan.StartLinePosition);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	[Fact]
	public async Task DisableHtmxDirectRouting_with_HtmxLayout_builds_without_HTMXOR003()
	{
		// #170 "Interactions": HtmxLayout on a marked component is allowed and has no effect.
		var componentPath = ComponentPath("ItemComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/items/{Id:int}")]
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			[global::Htmxor.HtmxLayoutAttribute(typeof(ItemLayout))]
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;

			public sealed class ItemLayout : global::Microsoft.AspNetCore.Components.LayoutComponentBase;
			}
			""";

		var diagnostics = await RunAnalyzerAsync(
			new[] { source },
			new[] { componentPath });

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// A binding in a component that owns no route at all (no local <c>@page</c> and no
	/// <c>HtmxRoute</c>) gets its own "owns no route" cause (#307), not the generic
	/// exactly-one-HtmxRoute message that also covers more than one declared HtmxRoute.
	/// </summary>
	[Fact]
	public async Task Binding_in_a_component_that_owns_no_route_fails_closed_for_an_action()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task PutReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		const string razorContent = """
			<button @onput="PutReport">Save</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("owns no route", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@onput", StringComparison.Ordinal), "@onput".Length),
			diagnostic.Location.SourceSpan);
	}

	[Fact]
	public async Task Stock_route_without_a_local_page_declaration_fails_closed_for_an_action()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var importsPath = ComponentPath("_Imports.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			#line 1 "{{EscapePath(importsPath)}}"
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			#line default
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task PutReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			<button @onput="PutReport">Save</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("without a local @page cannot use a compiled stock route", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
	}

	/// <summary>
	/// A component with two <c>@page</c> directives and a binding must fail the build through
	/// HTMXOR002, never silently drop the binding (#306).
	/// </summary>
	[Fact]
	public async Task Binding_in_a_component_with_two_page_directives_fails_closed_for_an_action()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}/alt")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task PutReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@page "/reports/{Id:int}"
			@page "/reports/{Id:int}/alt"
			<button @onput="PutReport">Save</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains(
			"exactly one stock route and no HtmxRoute",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The analyzer must never throw while the document is mid-edit (#306): the same truncated,
	/// mid-token Razor text that must not crash the generator must not crash the analyzer either,
	/// since both run <c>HtmxorComponentActionDeclaration.ParseAll</c> over every keystroke in an
	/// IDE.
	/// </summary>
	[Fact]
	public async Task Truncated_control_flow_markup_does_not_throw_the_analyzer()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"@page \"/x\"\n@if (Show) { <div ");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "AD0001");
	}

	[Fact]
	public async Task Static_handler_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private static global::System.Threading.Tasks.Task DeleteReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;

				{{BindMethod("DeleteReport")}}
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		// A static method gets its own "static" cause (#308), not the generic
		// instance-method message shared by every other unsupported handler.
		HtmxorActionHandlerShapeAnalyzerTests.AssertHandlerCauseSpecificMessage(diagnostic, "static");
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@ondelete", StringComparison.Ordinal), "@ondelete".Length),
			diagnostic.Location.SourceSpan);
	}

	[Fact]
	public async Task Static_delegate_handler_member_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private static readonly global::System.Func<global::Htmxor.HtmxEventArgs, global::System.Threading.Tasks.Task> DeleteReport =
					_ => global::System.Threading.Tasks.Task.CompletedTask;

				{{BindMethod("DeleteReport")}}
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		// A static delegate field is a non-method member, the same "not a method" cause (#308) as
		// an EventCallback field or an Action property, regardless of it also being static.
		HtmxorActionHandlerShapeAnalyzerTests.AssertHandlerCauseSpecificMessage(diagnostic, "not a method");
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@ondelete", StringComparison.Ordinal), "@ondelete".Length),
			diagnostic.Location.SourceSpan);
	}

	[Fact]
	public async Task Imported_static_handler_outside_component_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			global using static ExternalHandlers;

			public static class ExternalHandlers
			{
				public static global::System.Threading.Tasks.Task DeleteReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}

			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				{{BindMethod("DeleteReport")}}
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		// No member named DeleteReport exists anywhere on the component's own type hierarchy. Real
		// Razor still binds the call: a real Htmxor.TestApp probe at this HEAD confirms C# simple-name
		// lookup finds the `global using static` import's public DeleteReport and compiles cleanly.
		// The imported method is not a member of the component at all, so Htmxor's compiled-symbol
		// resolution never considers it "present" there; the owner's decision (#308, "Decision and
		// correction") is that this stays "absent", not "the wrong shape", keeping the generic
		// instance-method message instead of one of the eight shape causes.
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'DeleteReport' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HtmxorActionHandlerShapeAnalyzerTests.HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}

		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@ondelete", StringComparison.Ordinal), "@ondelete".Length),
			diagnostic.Location.SourceSpan);
	}

	[Fact]
	public async Task Imported_static_handler_with_inaccessible_base_collision_is_rejected_as_a_nonconfigurable_action_declaration()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			global using static ExternalHandlers;

			public static class ExternalHandlers
			{
				public static global::System.Threading.Tasks.Task DeleteReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}

			namespace {{RootNamespace}}
			{
			public abstract class ReportComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task DeleteReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}

			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : ReportComponentBase
			{
				{{BindMethod("DeleteReport")}}
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		// The private base member cannot rescue this binding by itself, but it does not need to:
		// C# simple-name lookup skips the inaccessible private base DeleteReport and continues to the
		// `global using static` import's public DeleteReport, which real Razor binds instead (a real
		// Htmxor.TestApp probe at this HEAD confirms the page compiles cleanly). The owner's
		// correction (#308, "Decision and correction") is that both imported-static cases - with or
		// without a colliding inaccessible base member - keep the same generic instance-method
		// message, because the bound handler is still not a member of the component.
		var message = diagnostic.GetMessage();
		Assert.Contains("handler 'DeleteReport' must be an instance method", message, StringComparison.Ordinal);
		foreach (var fragment in HtmxorActionHandlerShapeAnalyzerTests.HandlerShapeCauseFragments)
		{
			Assert.DoesNotContain(fragment, message, StringComparison.Ordinal);
		}

		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@ondelete", StringComparison.Ordinal), "@ondelete".Length),
			diagnostic.Location.SourceSpan);
	}

	[Fact]
	public async Task Accessible_base_instance_handler_is_supported()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			public abstract class ReportComponentBase : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				protected global::System.Threading.Tasks.Task DeleteReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}

			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			public sealed class ReportComponent : ReportComponentBase;
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		Assert.Empty(diagnostics);
	}

	[Fact]
	public async Task Binding_outside_explicit_htmx_route_methods_is_nonconfigurable()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Htmxor.HtmxRouteAttribute("/reports/{Id:int}", Methods = ["GET"])]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task QueryReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@attribute [Htmxor.HtmxRoute("/reports/{Id:int}", Methods = ["GET"])]
			<form @onquery="QueryReport"></form>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("explicit HtmxRoute.Methods is authoritative", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
	}

	/// <summary>
	/// Two agreeing bindings outside an explicit <c>HtmxRoute.Methods</c> allow-list each fail with
	/// the explicit-Methods HTMXOR002 at their own span (#309 test-contract remediation,
	/// LR-4628ce4-P002): the multiplicity collapse this slice adds never widens what an explicit
	/// <c>Methods</c> list already excludes, so agreement between the two bindings' handler names
	/// does not let either one through. The real <see cref="HtmxorActionDeclarationAnalyzer"/> runs
	/// here, the same as the single-binding row above, so this exercises the analyzer half of the
	/// #309 contract that the generator-only test suite never reaches.
	/// </summary>
	[Fact]
	public async Task Two_agreeing_bindings_outside_explicit_htmx_route_methods_each_fail_at_their_own_span()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Htmxor.HtmxRouteAttribute("/reports/{Id:int}", Methods = ["GET"])]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task QueryReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		const string razorContent = """
			@attribute [Htmxor.HtmxRoute("/reports/{Id:int}", Methods = ["GET"])]
			<form @onquery="QueryReport"></form>
			<form @onquery="QueryReport"></form>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		Assert.Equal(2, diagnostics.Length);
		var firstSpan = razorContent.IndexOf("@onquery", StringComparison.Ordinal);
		var secondSpan = razorContent.IndexOf("@onquery", firstSpan + 1, StringComparison.Ordinal);
		var bySpan = diagnostics.ToDictionary(static d => d.Location.SourceSpan.Start);
		foreach (var span in new[] { firstSpan, secondSpan })
		{
			var diagnostic = bySpan[span];
			Assert.Equal("HTMXOR002", diagnostic.Id);
			Assert.Contains("explicit HtmxRoute.Methods is authoritative", diagnostic.GetMessage(), StringComparison.Ordinal);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
			Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
			Assert.Equal(new TextSpan(span, "@onquery".Length), diagnostic.Location.SourceSpan);
		}
	}

	[Fact]
	public async Task Binding_inside_explicit_htmx_route_methods_is_supported()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Htmxor.HtmxRouteAttribute("/reports/{Id:int}", Methods = ["GET", "QUERY"])]
			[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute("reports.write")]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task QueryReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@attribute [Htmxor.HtmxRoute("/reports/{Id:int}", Methods = ["GET", "QUERY"])]
			<form @onquery="QueryReport"></form>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor, includeRouteAnalyzer: true);

		Assert.Empty(diagnostics);
	}

	/// <summary>
	/// The action analyzer resolves a binding's owner by the guessed name
	/// (<c>RazorComponentTypeNames.GetTypeName</c>) and looks that name up with
	/// <c>GetTypeByMetadataName</c>, so a wrong guess can resolve to someone else's real, compiled
	/// component. <c>Components/A/X.razor</c> hides an <c>@namespace Collide</c> line inside a Razor
	/// comment -- real Razor ignores a commented-out directive like this (confirmed empirically: an
	/// SDK build of this exact shape compiles to the ordinary folder-default namespace instead), but
	/// the narrow, comment-unaware scan this analyzer uses to avoid reading full Razor content does
	/// not, so A's binding resolves to <c>Collide.X</c> -- which is <c>Components/B/X.razor</c>'s
	/// real, unrelated compiled component, not A's own (different) compiled declaration. The
	/// generated-path check must refuse to let A's binding attach to B's component just because the
	/// two files share a leaf name: A fails closed, and B's own, identically-shaped binding -- which
	/// resolves to the very same component by the very same guess, but at its own real generated
	/// path -- stays accepted.
	/// </summary>
	[Fact]
	public async Task Action_binding_whose_guessed_owner_collides_with_a_different_real_component_fails_closed()
	{
		var pathA = ComponentPath(Path.Combine("Components", "A", "X.razor"));
		var pathB = ComponentPath(Path.Combine("Components", "B", "X.razor"));
		var generatedPathA = RazorGeneratedPath(Path.Combine("Components", "A", "X"));
		var generatedPathB = RazorGeneratedPath(Path.Combine("Components", "B", "X"));
		var sourceA = $$"""
			namespace {{RootNamespace}}.Components.A
			{
			#line 1 "{{EscapePath(pathA)}}"
			public sealed class X : global::Microsoft.AspNetCore.Components.ComponentBase;
			#line default
			}
			""";
		var sourceB = $$"""
			namespace Collide
			{
			#line 1 "{{EscapePath(pathB)}}"
			[global::Htmxor.HtmxRouteAttribute("/b/{Id:int}", Methods = ["GET", "PUT"])]
			#line default
			public sealed class X : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task PutIt(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		const string razorContentA = """
			@*
			@namespace Collide
			*@
			@attribute [Htmxor.HtmxRoute("/a/{Id:int}", Methods = ["GET", "PUT"])]
			<button @onput="PutIt">Save</button>
			""";
		const string razorContentB = """
			@namespace Collide
			@attribute [Htmxor.HtmxRoute("/b/{Id:int}", Methods = ["GET", "PUT"])]
			<button @onput="PutIt">Save</button>
			""";

		var diagnostics = await RunActionAnalyzerAsync(
			new[] { sourceA, sourceB },
			new[] { generatedPathA, generatedPathB },
			new AdditionalText[]
			{
				new SourceAdditionalText(pathA, razorContentA),
				new SourceAdditionalText(pathB, razorContentB),
			});

		// Exactly one diagnostic total: A fails closed and B is fully accepted, not just A.
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains(
			"the action owner must compile from the matching Razor component",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
		Assert.Equal(pathA, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContentA.IndexOf("@onput", StringComparison.Ordinal), "@onput".Length),
			diagnostic.Location.SourceSpan);
	}

	[Fact]
	public async Task Inferred_binding_on_a_DisableHtmxDirectRouting_marked_component_is_a_build_error()
	{
		// #172 point 5.4: any action on a marked component is a build error, reported through the
		// existing HTMXOR002 with a cause-specific message, at the binding's own location.
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task PutReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @onput="PutReport">Save</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal(
			"Unsupported component action declaration: " +
			"an inferred binding on a component marked with DisableHtmxDirectRouting is not supported",
			diagnostic.GetMessage());
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
		Assert.Equal(
			new TextSpan(razorContent.IndexOf("@onput", StringComparison.Ordinal), "@onput".Length),
			diagnostic.Location.SourceSpan);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
	}

	/// <summary>
	/// Two agreeing bindings on a <c>DisableHtmxDirectRouting</c>-marked component each fail with
	/// that HTMXOR002 at their own span (#309 test-contract remediation, LR-4628ce4-P002): #172
	/// point 5.4's "any action on a marked component is a build error" still applies to every
	/// agreeing occurrence, not just the first.
	/// </summary>
	[Fact]
	public async Task Two_agreeing_bindings_on_a_DisableHtmxDirectRouting_marked_component_each_fail_at_their_own_span()
	{
		var componentPath = ComponentPath("ReportComponent.razor");
		var source = $$"""
			namespace {{RootNamespace}}
			{
			[global::Microsoft.AspNetCore.Components.RouteAttribute("/reports/{Id:int}")]
			[global::Htmxor.DisableHtmxDirectRoutingAttribute]
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private global::System.Threading.Tasks.Task PutReport(global::Htmxor.HtmxEventArgs args)
					=> global::System.Threading.Tasks.Task.CompletedTask;
			}
			}
			""";
		const string razorContent = """
			@page "/reports/{Id:int}"
			<button @onput="PutReport">Save</button>
			<button @onput="PutReport">Save again</button>
			""";
		var razor = new SourceAdditionalText(componentPath, razorContent);

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		Assert.Equal(2, diagnostics.Length);
		var firstSpan = razorContent.IndexOf("@onput", StringComparison.Ordinal);
		var secondSpan = razorContent.IndexOf("@onput", firstSpan + 1, StringComparison.Ordinal);
		var bySpan = diagnostics.ToDictionary(static d => d.Location.SourceSpan.Start);
		foreach (var span in new[] { firstSpan, secondSpan })
		{
			var diagnostic = bySpan[span];
			Assert.Equal("HTMXOR002", diagnostic.Id);
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			Assert.Equal(
				"Unsupported component action declaration: " +
				"an inferred binding on a component marked with DisableHtmxDirectRouting is not supported",
				diagnostic.GetMessage());
			Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
			Assert.Equal(new TextSpan(span, "@onput".Length), diagnostic.Location.SourceSpan);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		}
	}

	private static string ComponentPath(string relativePath)
		=> Path.Combine(ProjectDirectory, relativePath);

	/// <summary>
	/// The exact call real Razor generates for an <c>@onX="handlerName"</c> binding: production finds
	/// this call directly in the Razor-generated declaration's own syntax and reads it with the real
	/// semantic model, so a fixture with no such call present gives production nothing to read and it
	/// can never diagnose it. Every fixture below that relies on a cause-specific or absence diagnostic
	/// must declare one, the same fidelity <c>HtmxorActionHandlerShapeAnalyzerTests.BindMethod</c>
	/// already gives its own fixtures.
	/// </summary>
	private static string BindMethod(string handlerName)
		=> "private void Bind() => " +
			"global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, " +
			handlerName + ");";

	private static string ComponentSource(
		string componentName,
		string mappedPath,
		string routeAttribute,
		string authorizationAttribute,
		string baseType = "global::Microsoft.AspNetCore.Components.ComponentBase")
		=> $$"""
			namespace {{RootNamespace}}
			{
			#line 20 "{{EscapePath(mappedPath)}}"
			{{routeAttribute}}
			{{authorizationAttribute}}
			#line default
			public sealed class {{componentName}} : {{baseType}};
			}
			""";

	private static string BoundComponentSource(
		string componentName,
		string mappedPath,
		string route,
		string policy,
		string methodsExpression,
		bool usePolicyConstructor)
	{
		var authorization = usePolicyConstructor
			? "[AuthAlias(\"constructor.policy\", Policy = RequiredPolicy)]"
			: "[AuthAlias(Policy = RequiredPolicy)]";
		return $$"""
			using RouteAlias = global::Htmxor.HtmxRouteAttribute;
			using AuthAlias = global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

			namespace {{RootNamespace}}
			{
			#line 73 "{{EscapePath(mappedPath)}}"
			[RouteAlias(RouteTemplate, Methods = {{methodsExpression}})]
			{{authorization}}
			#line default
			public sealed class {{componentName}} : global::Microsoft.AspNetCore.Components.ComponentBase
			{
				private const string RouteTemplate = "{{route}}";
				private const string GetMethod = "GET";
				private const string RequiredPolicy = "{{policy}}";
			}
			}
			""";
	}

	private static string EscapePath(string path) => path.Replace("\"", "\\\"");

	/// <summary>
	/// Plain, directive-free markup for a <c>.razor</c> fixture whose content is irrelevant to
	/// the row: no <c>@namespace</c> for the analyzer's own namespace scan to find.
	/// </summary>
	private const string PlainRazorContent = "<p></p>\n";

	private static Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
		IEnumerable<string> sources,
		IEnumerable<string> razorPaths,
		IEnumerable<string>? sourcePaths = null)
		=> RunAnalyzerAsync(
			sources,
			razorPaths.Select(static path => (path, PlainRazorContent)),
			sourcePaths);

	/// <summary>
	/// The analyzer reads each <c>.razor</c> additional file's content (its own and any ancestor
	/// <c>_Imports.razor</c>) to derive a namespace, which it then confirms against the compiled
	/// declaration; this overload supplies that content explicitly.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
		IEnumerable<string> sources,
		IEnumerable<(string Path, string Content)> razorFiles,
		IEnumerable<string>? sourcePaths = null)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var additionalFileEntries = razorFiles.ToArray();
		var paths = sourcePaths?.ToArray();
		var trees = sources
			.Select((source, index) => CSharpSyntaxTree.ParseText(
				source,
				parseOptions,
				paths is null
					? RazorGeneratedPath(Path.GetFileNameWithoutExtension(additionalFileEntries[index].Path))
					: paths[index]))
			.ToImmutableArray();
		var compilation = CSharpCompilation.Create(
			"Htmxor.Analyzer.Tests",
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var additionalFiles = additionalFileEntries
			.Select(static file => new SourceAdditionalText(file.Path, file.Content))
			.ToImmutableArray<AdditionalText>();
		var analyzerOptions = new AnalyzerOptions(
			additionalFiles,
			new TestAnalyzerConfigOptionsProvider(ProjectDirectory));

		var diagnostics = await compilation
			.WithAnalyzers(
				ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer()),
				analyzerOptions)
			.GetAnalyzerDiagnosticsAsync();

		return diagnostics
			.OrderBy(diagnostic => diagnostic.Location.GetMappedLineSpan().Path, StringComparer.Ordinal)
			.ThenBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
			.ToImmutableArray();
	}

	private static Task<ImmutableArray<Diagnostic>> RunActionAnalyzerAsync(
		string source,
		AdditionalText razor,
		bool includeRouteAnalyzer = false,
		string? sourcePath = null)
		=> RunActionAnalyzerAsync(
			new[] { source },
			new[] { sourcePath ?? RazorGeneratedPath("ReportComponent") },
			new[] { razor },
			includeRouteAnalyzer);

	/// <summary>
	/// A multi-file overload: each compiled source gets its own generated-path identity (the thing
	/// <c>HtmxorRouteManifest.HasCompiledRazorDeclaration</c> compares against), and every razor
	/// additional file is visible to the action analyzer's own per-file scan, so a binding in one
	/// file can be checked against a component resolved from a different file's guessed name.
	/// </summary>
	private static async Task<ImmutableArray<Diagnostic>> RunActionAnalyzerAsync(
		IEnumerable<string> sources,
		IEnumerable<string> sourcePaths,
		IEnumerable<AdditionalText> razorFiles,
		bool includeRouteAnalyzer = false)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var paths = sourcePaths.ToArray();
		var trees = sources
			.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, paths[index]))
			.ToImmutableArray();
		var compilation = CSharpCompilation.Create(
			"Htmxor.ActionAnalyzer.Tests",
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var analyzerOptions = new AnalyzerOptions(
			razorFiles.ToImmutableArray(),
			new TestAnalyzerConfigOptionsProvider(ProjectDirectory));

		var analyzers = includeRouteAnalyzer
			? ImmutableArray.Create<DiagnosticAnalyzer>(
				new HtmxorRouteDeclarationAnalyzer(),
				new HtmxorActionDeclarationAnalyzer())
			: ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorActionDeclarationAnalyzer());

		return await compilation
			.WithAnalyzers(
				analyzers,
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
			typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location,
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
