using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorStockFormCompilerTests
{
	private const string ProjectDirectory = "/htmxor-stock-form-tests";
	private const string RazorPath = ProjectDirectory + "/FormOwner.razor";
	private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

	[Theory]
	[InlineData("Unnamed", "<EditForm Model=\"model\" method=\"post\" />", "FormName", "HTMXOR027")]
	[InlineData("PlainUnnamed", "<form method=\"post\"></form>", "@formname", "HTMXOR028")]
	public async Task Own_unnamed_post_form_on_HtmxRoute_fails_at_the_form(string fixture, string markup, string cause, string expectedId)
	{
		var diagnostics = await CompileAsync(fixture, markup);

		AssertFormDiagnostic(diagnostics, cause, expectedId);
	}

	[Theory]
	[InlineData("Literal", "<EditForm Model=\"model\" FormName=\"save\" method=\"post\" />")]
	[InlineData("Expression", "<EditForm Model=\"model\" FormName=\"@(name)\" method=\"post\" />")]
	[InlineData("Plain", "<form method=\"post\" @formname=\"save\"></form>")]
	[InlineData("PlainExpression", "<form method=\"post\" @formname=\"@(name)\"></form>")]
	[InlineData("PlainNamedGet", "<form @formname=\"save\"></form>")]
	public async Task Explicit_GET_allowlist_rejects_each_own_named_form(string fixture, string markup)
	{
		var diagnostics = await CompileAsync(fixture, markup, explicitGet: true);

		AssertFormDiagnostic(diagnostics, "Methods", "HTMXOR033");
		Assert.Contains("POST", Assert.Single(diagnostics).GetMessage(), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("Literal", "<EditForm Model=\"model\" FormName=\"save\" method=\"post\" />")]
	[InlineData("PlainExpression", "<form method=\"post\" @formname=\"@(name)\"></form>")]
	public async Task Compiled_named_form_omitted_by_inference_fails_the_final_compilation_gate(string fixture, string markup)
	{
		var diagnostics = await CompileAsync(fixture, markup, omitFormFromAdditionalFiles: true);

		AssertFormDiagnostic(diagnostics, "POST", "HTMXOR043");
		Assert.Contains("form", Assert.Single(diagnostics).GetMessage(), StringComparison.OrdinalIgnoreCase);
		AssertOmittedFormRemediation(Assert.Single(diagnostics));
	}

	[Fact]
	public async Task Compiled_literal_named_form_with_no_Razor_mapping_still_fails_completeness()
	{
		var diagnostics = await CompileAsync("Plain", "<form method=\"post\" @formname=\"save\"></form>", omitFormFromAdditionalFiles: true);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR043", diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Contains("POST", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains("form", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.EndsWith("FormOwner_razor.g.cs", diagnostic.Location.GetMappedLineSpan().Path, StringComparison.Ordinal);
		Assert.True(diagnostic.Location.SourceSpan.Length > 0);
		AssertOmittedFormRemediation(diagnostic);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Named_form_and_onpost_on_one_owner_fail_for_both_route_kinds(bool stockPage)
	{
		var diagnostics = await CompileAsync("Literal", "<EditForm Model=\"model\" FormName=\"save\" method=\"post\" />", stockPage: stockPage, postAction: true);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal("HTMXOR032", diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains("POST", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Contains("form", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(RazorPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.InRange(diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line, 1, 2);
	}

	[Fact]
	public async Task Compiled_binding_omitted_by_inference_reports_the_binding_consistency_cause()
	{
		var diagnostics = await CompileAsync("Conflict", "<EditForm Model=\"model\" FormName=\"save\" method=\"post\" />",
			postAction: true, omitFormFromAdditionalFiles: true);

		Assert.Equal(2, diagnostics.Length);
		var binding = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.GetMessage().Contains("matching generated action", StringComparison.Ordinal)));
		Assert.Equal("HTMXOR042", binding.Id);
		Assert.Equal(DiagnosticSeverity.Error, binding.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, binding.Descriptor.CustomTags);
		Assert.Contains("no usable action is available", binding.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Contains("matching generated action", binding.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(RazorPath, binding.Location.GetMappedLineSpan().Path);
		Assert.Equal(2, binding.Location.GetMappedLineSpan().StartLinePosition.Line);
		Assert.True(binding.Location.SourceSpan.Length > 0);
		var conflict = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "HTMXOR032"));
		Assert.Equal(DiagnosticSeverity.Error, conflict.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, conflict.Descriptor.CustomTags);
		Assert.Contains("no usable action is available", conflict.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Contains("named forms and a POST action cannot share one route owner", conflict.GetMessage(), StringComparison.Ordinal);
		Assert.Contains("use named forms or a POST action", conflict.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(RazorPath, conflict.Location.GetMappedLineSpan().Path);
		Assert.InRange(conflict.Location.GetMappedLineSpan().StartLinePosition.Line, 1, 2);
		Assert.True(conflict.Location.SourceSpan.Length > 0);
	}

	[Theory]
	[InlineData("Unnamed", "<EditForm Model=\"model\" method=\"post\" />")]
	[InlineData("PlainUnnamed", "<form method=\"post\"></form>")]
	[InlineData("Literal", "<EditForm Model=\"model\" FormName=\"save\" method=\"post\" />")]
	public async Task Stock_page_keeps_its_form_declarations_without_Htmxor_diagnostics(string fixture, string markup)
	{
		var diagnostics = await CompileAsync(fixture, markup, stockPage: true);

		Assert.Empty(diagnostics);
	}

	private static void AssertOmittedFormRemediation(Diagnostic diagnostic)
	{
		var message = diagnostic.GetMessage();
		Assert.True(message.Contains("move the form into the route owner's own Razor markup", StringComparison.Ordinal), message);
		Assert.True(message.Contains("or replace the form with a supported explicit POST action", StringComparison.Ordinal), message);
		Assert.DoesNotContain("use a supported named POST form", message, StringComparison.Ordinal);
		Assert.DoesNotContain("add FormName", message, StringComparison.Ordinal);
	}

	private static void AssertFormDiagnostic(ImmutableArray<Diagnostic> diagnostics, string cause, string expectedId)
	{
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(expectedId, diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		Assert.Contains(cause, diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(RazorPath, diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Equal(1, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
		Assert.True(diagnostic.Location.SourceSpan.Length > 0);
	}

	private static async Task<ImmutableArray<Diagnostic>> CompileAsync(string fixture, string markup, bool stockPage = false, bool explicitGet = false, bool postAction = false, bool omitFormFromAdditionalFiles = false)
	{
		if (postAction) fixture = "Conflict";
		var route = stockPage ? "@page \"/forms\"" : "@attribute [Htmxor.HtmxRoute(\"/forms\"" + (explicitGet ? ", Methods = new[] { \"GET\" }" : "") + ")]";
		var members = postAction ? "private object model = new(); private void Save() { }" : "private object model = new(); private string name = \"save\";";
		var content = route + "\n" + markup + (postAction ? "\n<button @onpost=\"Save\">Save</button>" : "") + "\n@code { " + members + " }\n";
		var source = ReadFixture(fixture).Replace("namespace probe", "namespace Htmxor.Consumer", StringComparison.Ordinal)
			.Replace("class " + fixture, "class FormOwner", StringComparison.Ordinal)
			.Replace("/tmp/htmxor179-razor-probe/" + fixture + ".razor", RazorPath, StringComparison.Ordinal);
		source = System.Text.RegularExpressions.Regex.Replace(source, @"#line \((\d+),(\d+)\)-\((\d+),(\d+)\) ""/htmxor-stock-form-tests/FormOwner.razor""", match =>
			$"#line ({int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) + 1},{match.Groups[2].Value})-({int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture) + 1},{match.Groups[4].Value}) \"{RazorPath}\"");
		var attribute = stockPage ? "[global::Microsoft.AspNetCore.Components.RouteAttribute(\"/forms\")]" : "[global::Htmxor.HtmxRouteAttribute(\"/forms\"" + (explicitGet ? ", Methods = new[] { \"GET\" }" : "") + ")]";
		source = source.Replace("public partial class", attribute + "\n    public partial class", StringComparison.Ordinal);
		var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var tree = CSharpSyntaxTree.ParseText(source, parse, "Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/FormOwner_razor.g.cs");
		var compilation = CSharpCompilation.Create("Htmxor.StockForm.Tests", new[] { tree }, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		Assert.Empty(errors);
		// A stale scanner input must not hide a form present in the real compiled Razor tree.
		var razor = new RazorFile(omitFormFromAdditionalFiles ? route + "\n" : content);
		var options = new Options();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { new HtmxorRouteGenerator().AsSourceGenerator(), new HtmxorActionGenerator().AsSourceGenerator() }, new AdditionalText[] { razor, new ImportsFile() }, parse, options);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var driverDiagnostics);
		Assert.Empty(driverDiagnostics.Where(diagnostic => diagnostic.Id == "CS8785"));
		Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var analyzerDiagnostics = await output.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer(), new HtmxorActionDeclarationAnalyzer(), new HtmxorCompiledBindingAnalyzer()), new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(razor, new ImportsFile()), options)).GetAnalyzerDiagnosticsAsync();
		return driver.GetRunResult().Diagnostics.Concat(analyzerDiagnostics).DistinctBy(diagnostic => (diagnostic.Id, diagnostic.Location.SourceSpan, diagnostic.GetMessage())).ToImmutableArray();
	}

	// These fixtures are unedited output from Razor SDK 10.0.400; the test relocates only owner identity and source paths.
	private static string ReadFixture(string name)
	{
		var assembly = typeof(HtmxorStockFormCompilerTests).Assembly;
		using var stream = assembly.GetManifestResourceStream("Htmxor.Generators.StockFormFixtures." + name + ".razor.fixture")!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private static ImmutableArray<MetadataReference> CreateReferences()
		=> ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
			.Concat(new[] { typeof(ComponentBase).Assembly.Location, typeof(Microsoft.AspNetCore.Components.Forms.EditForm).Assembly.Location, typeof(HtmxRouteAttribute).Assembly.Location })
			.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

	private sealed class RazorFile(string content) : AdditionalText
	{
		public override string Path => RazorPath;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
	}

	private sealed class ImportsFile : AdditionalText
	{
		public override string Path => ProjectDirectory + "/_Imports.razor";
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From("@using Microsoft.AspNetCore.Components\n@using Microsoft.AspNetCore.Components.Forms\n");
	}

	private sealed class Options : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new Values();
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
	}

	private sealed class Values : AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
		{
			value = key == "build_property.RootNamespace" ? "Htmxor.Consumer" : ProjectDirectory;
			return key is "build_property.RootNamespace" or "build_property.MSBuildProjectDirectory";
		}
	}
}
