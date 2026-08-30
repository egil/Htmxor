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
		Assert.Contains(
			"the action owner must compile from the matching project-root Razor component",
			diagnostic.GetMessage(),
			StringComparison.Ordinal);
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

	[Fact]
	public async Task Custom_authorization_metadata_alongside_standard_policy_fails_closed()
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

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("exactly one effective authorization", diagnostic.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Custom_anonymous_metadata_fails_closed()
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

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("anonymous", diagnostic.GetMessage(), StringComparison.Ordinal);
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
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"], Target = \"result\")]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]",
		"Target")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\", Roles = \"admin\")]",
		"Roles")]
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\", AuthenticationSchemes = \"scheme\")]",
		"AuthenticationSchemes")]
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
	[InlineData(
		"[global::Htmxor.HtmxRouteAttribute(\"/items/{Id:int}\", Methods = [\"GET\"])]",
		"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.read\")]\n[global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]",
		"anonymous")]
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

	[Fact]
	public async Task Exact_route_outside_project_root_fails_closed_while_lookalike_is_ignored()
	{
		var nestedPath = ComponentPath(Path.Combine("Nested", "NestedComponent.razor"));
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
			new[] { lookalikePath, nestedPath });

		var diagnostic = Assert.Single(diagnostics);
		Assert.Contains("project-root", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(nestedPath, diagnostic.Location.GetMappedLineSpan().Path);
	}

	[Fact]
	public async Task More_than_two_supported_components_reports_every_declaration()
	{
		var paths = new[]
		{
			ComponentPath("AlphaComponent.razor"),
			ComponentPath("BetaComponent.razor"),
			ComponentPath("GammaComponent.razor"),
		};
		var sources = paths.Select((path, index) => ComponentSource(
			Path.GetFileNameWithoutExtension(path),
			path,
			$"[global::Htmxor.HtmxRouteAttribute(\"/items/{{Id{index}:int}}\", Methods = [\"GET\"])]",
			$"[global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute(\"items.{index}.read\")]"));

		var diagnostics = await RunAnalyzerAsync(sources, paths);

		Assert.Equal(3, diagnostics.Length);
		Assert.All(diagnostics, diagnostic =>
			Assert.Contains("at most two", diagnostic.GetMessage(), StringComparison.Ordinal));
		Assert.Equal(paths, diagnostics.Select(diagnostic => diagnostic.Location.GetMappedLineSpan().Path));
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
			}
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("handler 'DeleteReport' must be an instance method", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
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
			}
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("handler 'DeleteReport' must be an instance method", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
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
			public sealed class ReportComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			}
			""";
		var razor = new SourceAdditionalText(
			componentPath,
			"""
			@page "/reports/{Id:int}"
			<button @ondelete="DeleteReport">Delete</button>
			""");

		var diagnostics = await RunActionAnalyzerAsync(source, razor);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("handler 'DeleteReport' must be an instance method", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
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

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR002", diagnostic.Id);
		Assert.Contains("handler 'DeleteReport' must be an instance method", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Equal(componentPath, diagnostic.Location.GetLineSpan().Path);
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

	private static string ComponentPath(string relativePath)
		=> Path.Combine(ProjectDirectory, relativePath);

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

	private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
		IEnumerable<string> sources,
		IEnumerable<string> razorPaths,
		IEnumerable<string>? sourcePaths = null)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var additionalPaths = razorPaths.ToArray();
		var paths = sourcePaths?.ToArray();
		var trees = sources
			.Select((source, index) => CSharpSyntaxTree.ParseText(
				source,
				parseOptions,
				paths is null
					? RazorGeneratedPath(Path.GetFileNameWithoutExtension(additionalPaths[index]))
					: paths[index]))
			.ToImmutableArray();
		var compilation = CSharpCompilation.Create(
			"Htmxor.Analyzer.Tests",
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var additionalFiles = additionalPaths
			.Select(static path => new ThrowingAdditionalText(path))
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

	private static async Task<ImmutableArray<Diagnostic>> RunActionAnalyzerAsync(
		string source,
		AdditionalText razor,
		bool includeRouteAnalyzer = false,
		string? sourcePath = null)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var compilation = CSharpCompilation.Create(
			"Htmxor.ActionAnalyzer.Tests",
			new[] { CSharpSyntaxTree.ParseText(
				source,
				parseOptions,
				sourcePath ?? RazorGeneratedPath("ReportComponent")) },
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var analyzerOptions = new AnalyzerOptions(
			ImmutableArray.Create(razor),
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

	private sealed class ThrowingAdditionalText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("The analyzer must not read Razor content.");
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
