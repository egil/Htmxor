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
/// other shape that real Razor itself compiles fails with its own cause-specific HTMXOR002. A shape
/// real Razor itself rejects (<c>ValueTask</c>, wrong parameter type, two or more parameters,
/// generic, <c>ref</c>/<c>in</c>, ambiguous overloads) gets no Htmxor diagnostic at all.
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

	/// <summary>
	/// A shape real Razor itself rejects at the binding gets no Htmxor diagnostic at all: the owner's
	/// amended scope decision (#308) is that Htmxor adds nothing for a case the Razor compiler
	/// already decided. Each row's fixture carries <see cref="BindMethod"/>, the real Razor-generated
	/// call, so real C# overload resolution produces the asserted compiler-error code (confirmed by
	/// the owner's real Htmxor.TestApp probe at `main` `bd14996`) instead of this suite assuming it.
	/// One row per rejection branch that sits beside a diagnosed shape: <c>ValueTask</c> and a
	/// non-<c>int</c>-return (CS1503, beside the diagnosed "returns a value" row), a non-
	/// <c>HtmxEventArgs</c>-compatible parameter (CS1503, beside the diagnosed <c>EventArgs</c>/
	/// <c>object</c> rows), two parameters (CS1503), a generic method (CS1503), a <c>ref</c> or
	/// <c>in</c> parameter (CS1503), and an ambiguous overload pair (CS0121, beside the diagnosed
	/// resolvable overloads).
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
	/// A lambda or closure, a method call, and a conditional are the #307 value-grammar causes; the
	/// handler-shape causes pinned by #308 are a disjoint set, so this list deliberately only needs to
	/// rule out other handler-shape causes, not the value-grammar ones (a value-grammar cause can
	/// never fire here: every fixture below binds a plain method-group identifier).
	///
	/// This is the one owner of the cause-fragment list; <see cref="HtmxorRouteDeclarationAnalyzerTests"/>
	/// references it directly instead of keeping its own copy, so a cause rename or addition cannot
	/// silently weaken one file's absence check while the other is updated.
	/// </summary>
	internal static readonly string[] HandlerShapeCauseFragments =
	{
		"returns a value", "async void", "parameter must be HtmxEventArgs", "optional parameter",
		"static", "overloaded", "not a method", "not accessible",
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
