using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorStockFormRegressionTests(ITestOutputHelper output)
{
	private const string DirectoryPath = "/tmp/htmxor179-regression-probe";
	private const string Imports = "@using Microsoft.AspNetCore.Components\n@using Microsoft.AspNetCore.Components.Forms\n";

	[Fact]
	public async Task Uppercase_HTML_POST_is_an_unnamed_form_error_at_the_form()
	{
		var result = await CompileAsync("Uppercase");

		Assert.Empty(result.CompilerErrors);
		var diagnostic = Assert.Single(result.HtmxorDiagnostics);
		Assert.Equal("HTMXOR028", diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains("@formname", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal(DirectoryPath + "/Uppercase.razor", diagnostic.Location.GetMappedLineSpan().Path);
		Assert.Equal(1, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
		Assert.True(diagnostic.Location.SourceSpan.Length > 0);
	}

	[Fact]
	public async Task Compiler_invalid_FormName_keeps_CS1503_without_redundant_Htmxor_error()
	{
		var result = await CompileAsync("InvalidName");

		var error = Assert.Single(result.CompilerErrors);
		Assert.Equal("CS1503", error.Id);
		Assert.Equal(DirectoryPath + "/InvalidName.razor", error.Location.GetMappedLineSpan().Path);
		Assert.Equal(1, error.Location.GetMappedLineSpan().StartLinePosition.Line);
		Assert.Empty(result.HtmxorDiagnostics);
	}

	[Theory]
	[InlineData("Alias", true)]
	[InlineData("Custom", false)]
	[InlineData("Nested", true)]
	public async Task Compiled_stock_identity_decides_the_named_form_manifest(string fixture, bool stockForm)
	{
		var result = await CompileAsync(fixture);

		Assert.Empty(result.CompilerErrors);
		Assert.Empty(result.HtmxorDiagnostics);
		Assert.Equal(stockForm, ReadManifest(result.Output, fixture));
	}

	[Theory]
	[InlineData("PrecededByGet")]
	[InlineData("PrecededByCustom")]
	public async Task Unnamed_static_POST_reports_its_form_after_an_unrelated_declaration(string fixture)
	{
		var result = await CompileAsync(fixture);

		Assert.Empty(result.CompilerErrors);
		var diagnostic = Assert.Single(result.HtmxorDiagnostics);
		var span = diagnostic.Location.GetMappedLineSpan();
		output.WriteLine($"Diagnostic: {diagnostic}; severity={diagnostic.Severity}; sourceSpan={diagnostic.Location.SourceSpan}; mappedSpan={span}");
		output.WriteLine(ReadFixture(fixture + ".source.fixture"));
		Assert.Equal("HTMXOR028", diagnostic.Id);
		Assert.Contains("no usable action is available", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Contains("@formname", diagnostic.GetMessage(), StringComparison.Ordinal);
		Assert.Equal("/tmp/htmxor179-location-regression-probe/" + fixture + ".razor", span.Path);
		Assert.Equal(2, span.StartLinePosition.Line);
		Assert.Equal(0, span.StartLinePosition.Character);
		Assert.Equal(ReadFixture(fixture + ".source.fixture").IndexOf("<form method=\"post\"", StringComparison.Ordinal), diagnostic.Location.SourceSpan.Start);
		Assert.True(diagnostic.Location.SourceSpan.Length > 0);
	}

	private static bool ReadManifest(Compilation compilation, string fixture)
	{
		using var stream = new MemoryStream();
		var emitted = compilation.Emit(stream);
		Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
		stream.Position = 0;
		var context = new AssemblyLoadContext("stock-form-regression", isCollectible: true);
		try
		{
			var assembly = context.LoadFromStream(stream);
			var registration = assembly.GetType("Microsoft.AspNetCore.Builder.HtmxorGeneratedRouteRegistrationExtensions", throwOnError: true)!;
			var owners = (string[])registration.GetField("NamedFormComponentTypeNames", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
			return owners.Contains("FormRegressionProbe." + fixture, StringComparer.Ordinal);
		}
		finally
		{
			context.Unload();
		}
	}

	private static async Task<Result> CompileAsync(string fixture)
	{
		var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var trees = new[] { CSharpSyntaxTree.ParseText(ReadFixture(fixture + ".razor.fixture"), parse, "Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/" + fixture + "_razor.g.cs"), CSharpSyntaxTree.ParseText(ReadFixture("FakeForm.support.fixture"), parse) };
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
			.Concat(new[] { typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.Location, typeof(Microsoft.AspNetCore.Components.Forms.EditForm).Assembly.Location, typeof(HtmxRouteAttribute).Assembly.Location })
			.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path));
		var compilation = CSharpCompilation.Create("Htmxor.StockForm.Regression." + fixture, trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var directory = fixture.StartsWith("PrecededBy", StringComparison.Ordinal) ? "/tmp/htmxor179-location-regression-probe"
			: fixture == "Nested" ? "/tmp/htmxor179-nested-regression-probe" : DirectoryPath;
		var files = ImmutableArray.Create<AdditionalText>(new RazorFile(directory + "/" + fixture + ".razor", ReadFixture(fixture + ".source.fixture")), new RazorFile(directory + "/_Imports.razor", Imports));
		var options = new Options(directory);
		GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { new HtmxorRouteGenerator().AsSourceGenerator(), new HtmxorActionGenerator().AsSourceGenerator() }, files, parse, options);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var driverDiagnostics);
		Assert.Empty(driverDiagnostics.Where(diagnostic => diagnostic.Id == "CS8785"));
		var diagnostics = await output.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer(), new HtmxorActionDeclarationAnalyzer(), new HtmxorCompiledBindingAnalyzer()), new AnalyzerOptions(files, options)).GetAnalyzerDiagnosticsAsync();
		return new Result(output, output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToImmutableArray(), driver.GetRunResult().Diagnostics.Concat(diagnostics).DistinctBy(diagnostic => (diagnostic.Id, diagnostic.Location.SourceSpan, diagnostic.GetMessage())).ToImmutableArray());
	}

	// Authentic SDK 10.0.400 output and matching Razor inputs; render-tree calls are never synthesized by this test.
	private static string ReadFixture(string name)
	{
		using var stream = typeof(HtmxorStockFormRegressionTests).Assembly.GetManifestResourceStream("Htmxor.Generators.StockFormFixtures." + name)!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private sealed record Result(Compilation Output, ImmutableArray<Diagnostic> CompilerErrors, ImmutableArray<Diagnostic> HtmxorDiagnostics);

	private sealed class RazorFile(string filePath, string content) : AdditionalText
	{
		public override string Path => filePath;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
	}

	private sealed class Options(string projectDirectory) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(projectDirectory);
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
	}

	private sealed class Values(string projectDirectory) : AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
		{
			value = key == "build_property.RootNamespace" ? "FormRegressionProbe" : projectDirectory;
			return key is "build_property.RootNamespace" or "build_property.MSBuildProjectDirectory";
		}
	}

}
