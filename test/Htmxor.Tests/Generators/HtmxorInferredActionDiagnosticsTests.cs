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
	[InlineData("Shape", "013,018")]
	[InlineData("Normal", "013,018,034")]
	[InlineData("Allowlist", "013,018,033")]
	[InlineData("Parse", "013,015,018")]
	[InlineData("Conflict", "013,013,018,031,031")]
	[InlineData("Generic", "012,013,018")]
	[InlineData("Parameter", "013,021,024")]
	[InlineData("Dynamic", "013,024,025")]
	[InlineData("Overload", "011,013")]
	[InlineData("Unrouted", "013,014,018")]
	[InlineData("TaskValue", "013,019")]
	[InlineData("EventArgs", "013,020")]
	[InlineData("LocalPage", "013,018,038")]
	[InlineData("StockRoute", "013,018,039")]
	[InlineData("Field", "007")]
	[InlineData("Property", "008")]
	[InlineData("Delegate", "041")]
	[InlineData("Imported", "040")]
	[InlineData("GetLambda", "006,015")]
	[InlineData("Misplaced", "006,015,036")]
	[InlineData("Cross", "013,015,018,042")]
	public async Task Real_Razor_reports_every_independent_cause_once(string fixture, string expected)
	{
		var result = await AnalyzeAsync(fixture);

		Assert.Equal(Ids(expected), result.Complete.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		Assert.All(result.Complete, diagnostic => AssertLocation(diagnostic, fixture));
		AssertConflictMultiplicity(result.Complete, fixture);
	}

	[Theory]
	[InlineData("Shape", "013,018")]
	[InlineData("Normal", "013,018,034")]
	[InlineData("Allowlist", "013,018,033")]
	[InlineData("Parse", "013,018")]
	[InlineData("Conflict", "013,013,018")]
	[InlineData("Generic", "012,013,018")]
	[InlineData("Parameter", "013,021,024")]
	[InlineData("Dynamic", "013,024,025")]
	[InlineData("Overload", "011,013")]
	[InlineData("Unrouted", "013,014,018")]
	[InlineData("TaskValue", "013,019")]
	[InlineData("EventArgs", "013,020")]
	[InlineData("LocalPage", "013,018,038")]
	[InlineData("StockRoute", "013,018,039")]
	[InlineData("Field", "007")]
	[InlineData("Property", "008")]
	[InlineData("Delegate", "041")]
	[InlineData("Imported", "040")]
	public async Task Semantic_feedback_contains_all_local_causes_without_aggregate_cascades(string fixture, string expected)
	{
		var result = await AnalyzeAsync(fixture);

		Assert.Equal(Ids(expected), result.Local.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		AssertConflictMultiplicity(result.Local, fixture);
		Assert.All(result.Local, diagnostic =>
		{
			AssertLocation(diagnostic, fixture);
			Assert.NotNull(diagnostic.Location.SourceTree);
			Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	[Fact]
	public async Task Compiled_binding_missing_from_scanned_declarations_remains_an_aggregate_mismatch()
	{
		var result = await AnalyzeAsync("Valid", omitBinding: true);

		Assert.Equal(new[] { "HTMXOR042" }, result.Complete.Select(diagnostic => diagnostic.Id));
		Assert.Empty(result.Local);
	}

	[Fact]
	public async Task Known_normal_only_binding_is_local_even_when_scanner_input_is_stale()
	{
		var result = await AnalyzeAsync("Normal", omitBinding: true);

		Assert.Equal(Ids("013,018,034"), result.Complete.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		Assert.Equal(Ids("013,018,034"), result.Local.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData("Lambda", "006")]
	[InlineData("Factory", "009")]
	[InlineData("Computed", "010")]
	[InlineData("GetLambda", "006,015")]
	[InlineData("Field", "007")]
	[InlineData("Delegate", "041")]
	public async Task Recoverable_compiled_only_binding_exposes_its_local_cause(string fixture, string expected)
	{
		var result = await AnalyzeAsync(fixture, omitBinding: true);

		Assert.Equal(Ids(expected), result.Complete.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		Assert.Equal(Ids(expected), result.Local.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
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

	private static IEnumerable<string> Ids(string suffixes) => suffixes.Split(',').Select(suffix => "HTMXOR" + suffix).Order(StringComparer.Ordinal);

	private static void AssertConflictMultiplicity(ImmutableArray<Diagnostic> diagnostics, string fixture)
	{
		if (fixture != "Conflict")
		{
			return;
		}
		Assert.Equal(new[] { 1, 2 }, diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR013")
			.Select(diagnostic => diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line).Order());
		Assert.Equal(1, Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR018")).Location.GetMappedLineSpan().StartLinePosition.Line);
	}

	private static void AssertLocation(Diagnostic diagnostic, string fixture)
	{
		var content = Read(fixture, "razor");
		var mapped = diagnostic.Location.GetMappedLineSpan();
		Assert.Equal(Directory + "/" + fixture + ".razor", mapped.Path);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
		var line = SourceText.From(content).Lines[mapped.StartLinePosition.Line].ToString();
		var direct = diagnostic.Id is "HTMXOR006" or "HTMXOR015" or "HTMXOR031" or "HTMXOR036";
		var column = direct ? line.IndexOf("@on", StringComparison.Ordinal) : line.IndexOf("=\"", line.IndexOf("@on", StringComparison.Ordinal), StringComparison.Ordinal) + 2;
		Assert.Equal(column, mapped.StartLinePosition.Character);
		Assert.Equal(direct ? 6 : line.Contains("Other", StringComparison.Ordinal) ? 5 : fixture == "Imported" ? 7 : 1,
			mapped.EndLinePosition.Character - mapped.StartLinePosition.Character);
	}

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
