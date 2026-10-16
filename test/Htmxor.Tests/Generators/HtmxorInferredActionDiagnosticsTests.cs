using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorInferredActionDiagnosticsTests
{
	private const string Directory = "/tmp/issue338-razor-capture";
	private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
		.Concat([typeof(ComponentBase).Assembly.Location, typeof(HtmxRouteAttribute).Assembly.Location])
		.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

	[Theory]
	[InlineData("Shape", "013:1:8:14,018:1:8:14")]
	[InlineData("Normal", "013:2:8:14,018:2:8:14,034:2:8:14")]
	[InlineData("Allowlist", "013:1:8:14,018:1:8:14,033:1:8:14")]
	[InlineData("Parse", "013:1:8:14,015:1:8:14,018:1:8:14")]
	[InlineData("Conflict", "013:1:8:14,013:2:8:14,018:1:8:14,031:1:8:14,031:2:8:14")]
	[InlineData("Generic", "012:1:8:14,013:1:8:14,018:1:8:14")]
	[InlineData("Parameter", "013:1:8:14,021:1:8:14,024:1:8:14")]
	[InlineData("Dynamic", "013:1:8:14,024:1:8:14,025:1:8:14")]
	[InlineData("Overload", "011:1:8:14,013:1:8:14")]
	[InlineData("Unrouted", "013:1:8:14,014:1:8:14,018:1:8:14")]
	[InlineData("TaskValue", "013:1:8:14,019:1:8:14")]
	[InlineData("EventArgs", "013:1:8:14,020:1:8:14")]
	[InlineData("LocalPage", "013:2:8:14,018:2:8:14,038:2:8:14")]
	[InlineData("StockRoute", "013:1:8:14,018:1:8:14,039:1:8:14")]
	[InlineData("Field", "007:1:8:14")]
	[InlineData("Property", "008:1:8:14")]
	[InlineData("Delegate", "041:1:8:14")]
	[InlineData("Imported", "040:2:8:14")]
	[InlineData("GetLambda", "006:1:8:14,015:1:8:14")]
	[InlineData("Misplaced", "006:1:38:44,015:1:38:44,036:1:38:44")]
	[InlineData("Cross", "013:1:8:14,015:1:8:14,018:1:8:14,042:2:17:22")]
	public async Task Real_Razor_reports_every_independent_cause_once(string fixture, string expected)
	{
		var result = await AnalyzeAsync(fixture);

		AssertDiagnostics(fixture, expected, result.Complete);
	}

	[Theory]
	[InlineData("Shape", "013:1:8:14,018:1:8:14")]
	[InlineData("Normal", "013:2:8:14,018:2:8:14,034:2:8:14")]
	[InlineData("Allowlist", "013:1:8:14,018:1:8:14,033:1:8:14")]
	[InlineData("Parse", "013:1:8:14,018:1:8:14")]
	[InlineData("Conflict", "013:1:8:14,013:2:8:14,018:1:8:14")]
	[InlineData("Generic", "012:1:8:14,013:1:8:14,018:1:8:14")]
	[InlineData("Parameter", "013:1:8:14,021:1:8:14,024:1:8:14")]
	[InlineData("Dynamic", "013:1:8:14,024:1:8:14,025:1:8:14")]
	[InlineData("Overload", "011:1:8:14,013:1:8:14")]
	[InlineData("Unrouted", "013:1:8:14,014:1:8:14,018:1:8:14")]
	[InlineData("TaskValue", "013:1:8:14,019:1:8:14")]
	[InlineData("EventArgs", "013:1:8:14,020:1:8:14")]
	[InlineData("LocalPage", "013:2:8:14,018:2:8:14,038:2:8:14")]
	[InlineData("StockRoute", "013:1:8:14,018:1:8:14,039:1:8:14")]
	[InlineData("Field", "007:1:8:14")]
	[InlineData("Property", "008:1:8:14")]
	[InlineData("Delegate", "041:1:8:14")]
	[InlineData("Imported", "040:2:8:14")]
	[InlineData("Cross", "013:1:8:14,018:1:8:14")]
	public async Task Semantic_feedback_contains_all_local_causes_without_aggregate_cascades(string fixture, string expected)
	{
		var result = await AnalyzeAsync(fixture);

		Assert.DoesNotContain(result.Local, diagnostic => diagnostic.Id == "HTMXOR042");
		AssertDiagnostics(fixture, expected, result.Local);
		Assert.All(result.Local, diagnostic =>
		{
			Assert.NotNull(diagnostic.Location.SourceTree);
			Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	[Fact]
	public async Task Compiled_binding_missing_from_scanned_declarations_remains_an_aggregate_mismatch()
	{
		var result = await AnalyzeAsync("Valid", omitBinding: true);

		AssertDiagnostics("Valid", "042:1:16:17", result.Complete);
		Assert.Empty(result.Local);
	}

	[Fact]
	public async Task Known_normal_only_binding_is_local_even_when_scanner_input_is_stale()
	{
		var result = await AnalyzeAsync("Normal", omitBinding: true);

		AssertDiagnostics("Normal", "013:2:16:17,018:2:16:17,034:2:16:17", result.Complete);
		AssertDiagnostics("Normal", "013:2:16:17,018:2:16:17,034:2:16:17", result.Local);
	}

	[Theory]
	[InlineData("Lambda", "006:1:16:25")]
	[InlineData("Factory", "009:1:16:22")]
	[InlineData("Computed", "010:1:16:34")]
	[InlineData("GetLambda", "006:1:16:25,015:1:16:25")]
	[InlineData("Field", "007:1:16:17")]
	[InlineData("Delegate", "041:1:16:17")]
	public async Task Recoverable_compiled_only_binding_exposes_its_local_cause(string fixture, string expected)
	{
		var result = await AnalyzeAsync(fixture, omitBinding: true);

		AssertDiagnostics(fixture, expected, result.Complete);
		AssertDiagnostics(fixture, expected, result.Local);
		Assert.All(result.Local, diagnostic => Assert.NotNull(diagnostic.Location.SourceTree));
	}

	[Fact]
	public async Task Valid_binding_keeps_its_generated_action_without_diagnostics()
	{
		var result = await AnalyzeAsync("Valid");

		Assert.Empty(result.Complete);
		Assert.Empty(result.Local);
		Assert.Contains("\"PUT\"", result.Generated, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Compiler_owned_unbindable_callback_gets_no_guessed_Htmxor_shape_diagnostic()
	{
		var result = await AnalyzeAsync("Compiler", compilerError: "CS1503");

		Assert.Empty(result.Complete);
		Assert.Empty(result.Local);
	}

	private static void AssertDiagnostics(string fixture, string specification, ImmutableArray<Diagnostic> diagnostics)
	{
		var expected = specification.Split(',').Select(item => ExpectedLocation(fixture, item));
		var actual = diagnostics.Select(diagnostic =>
		{
			var mapped = diagnostic.Location.GetMappedLineSpan();
			return new DiagnosticLocation(diagnostic.Id, mapped.Path, mapped.StartLinePosition.Line,
				mapped.StartLinePosition.Character, mapped.EndLinePosition.Line, mapped.EndLinePosition.Character);
		});

		Assert.Equal(OrderLocations(expected), OrderLocations(actual));
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		});
	}

	private static DiagnosticLocation ExpectedLocation(string fixture, string specification)
	{
		var values = specification.Split(':');
		var line = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
		return new DiagnosticLocation("HTMXOR" + values[0], Directory + "/" + fixture + ".razor", line,
			int.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture), line,
			int.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture));
	}

	private static IOrderedEnumerable<DiagnosticLocation> OrderLocations(IEnumerable<DiagnosticLocation> locations)
		=> locations.OrderBy(location => location.Id, StringComparer.Ordinal).ThenBy(location => location.Path, StringComparer.Ordinal)
			.ThenBy(location => location.StartLine).ThenBy(location => location.StartColumn)
			.ThenBy(location => location.EndLine).ThenBy(location => location.EndColumn);

	private sealed record DiagnosticLocation(string Id, string Path, int StartLine, int StartColumn, int EndLine, int EndColumn);

	private static async Task<Result> AnalyzeAsync(string fixture, bool omitBinding = false, string? compilerError = null)
	{
		var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var tree = CSharpSyntaxTree.ParseText(Read(fixture, "generated"), parse,
			"Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/" + fixture + "_razor.g.cs");
		var declaration = CSharpSyntaxTree.ParseText(Read(fixture, "declaration"), parse,
			"Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/" + fixture + "_razor.decl.g.cs");
		var compilation = CSharpCompilation.Create("InferredFeedback", [tree, declaration], References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Equal(compilerError is null ? [] : new[] { compilerError }, errors.Select(diagnostic => diagnostic.Id));
		var content = Read(fixture, "razor");
		var razor = new File(Directory + "/" + fixture + ".razor", omitBinding ? "@page \"/actions\"\n" : content);
		var imports = new File(Directory + "/_Imports.razor", "@using Microsoft.AspNetCore.Components\n@using Htmxor\n@using Htmxor.Components\n");
		var options = new Options();
		GeneratorDriver generator = CSharpGeneratorDriver.Create([new HtmxorRouteGenerator().AsSourceGenerator(), new HtmxorActionGenerator().AsSourceGenerator()], [razor, imports], parse, options);
		generator = generator.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
		Assert.DoesNotContain(generatorDiagnostics, diagnostic => diagnostic.Id == "CS8785");
		var analyzers = output.WithAnalyzers([new HtmxorActionDeclarationAnalyzer(), new HtmxorCompiledBindingAnalyzer()], new AnalyzerOptions([razor, imports], options));
		var complete = generator.GetRunResult().Diagnostics.AddRange(await analyzers.GetAnalyzerDiagnosticsAsync());
		var local = await analyzers.GetAnalyzerSemanticDiagnosticsAsync(output.GetSemanticModel(tree), null, CancellationToken.None);
		return new Result(complete, local, string.Join("\n", generator.GetRunResult().GeneratedTrees.Select(generated => generated.GetText().ToString())));
	}

	private static string Read(string fixture, string part)
	{
		using var stream = typeof(HtmxorInferredActionDiagnosticsTests).Assembly.GetManifestResourceStream("Htmxor.Generators.InferredActionFixtures." + fixture + "." + part + ".fixture")!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private sealed record Result(ImmutableArray<Diagnostic> Complete, ImmutableArray<Diagnostic> Local, string Generated);
	private sealed class File(string path, string content) : AdditionalText
	{
		public override string Path => path;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
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
			value = key == "build_property.RootNamespace" ? "Consumer" : Directory;
			return key is "build_property.RootNamespace" or "build_property.MSBuildProjectDirectory";
		}
	}
}
