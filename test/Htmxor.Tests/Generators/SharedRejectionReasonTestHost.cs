using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

internal static class SharedRejectionReasonTestHost
{
	private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
		.Concat([typeof(ComponentBase).Assembly.Location, typeof(HtmxRouteAttribute).Assembly.Location])
		.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

	public static CSharpCompilation Compile(params SyntaxTree[] trees)
	{
		var compilation = CSharpCompilation.Create("SharedReasons" + Guid.NewGuid().ToString("N"), trees, References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		return compilation;
	}

	public static SyntaxTree Parse(string source, string path = "Owner.cs")
		=> CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), path);

	public static (Assembly Assembly, Type Owner) Emit(CSharpCompilation compilation, string name = "Consumer.Owner")
	{
		using var stream = new MemoryStream();
		var result = compilation.Emit(stream);
		Assert.True(result.Success, string.Join("\n", result.Diagnostics));
		var assembly = Assembly.Load(stream.ToArray());
		return (assembly, assembly.GetType(name, throwOnError: true)!);
	}

	public static async Task<ImmutableArray<Diagnostic>> Analyze(CSharpCompilation compilation, params DiagnosticAnalyzer[] analyzers)
		=> await compilation.WithAnalyzers(analyzers.ToImmutableArray()).GetAnalyzerDiagnosticsAsync();

	public static void AssertDiagnostic(Diagnostic diagnostic, string expected)
	{
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		var message = diagnostic.GetMessage();
		var reason = message[(message.IndexOf(": ", StringComparison.Ordinal) + 2)..];
		const string advice = "Use [HtmxAction] to explicitly declare a supported instance action instead of this binding; ";
		if (reason.StartsWith(advice, StringComparison.Ordinal)) reason = reason[advice.Length..];
		if (reason.StartsWith("handler '", StringComparison.Ordinal)) reason = reason[(reason.IndexOf("' ", 9, StringComparison.Ordinal) + 2)..];
		Assert.Equal(expected, reason.TrimEnd('.'));
	}

	public static void AssertException(InvalidOperationException exception, string expected)
	{
		var reason = exception.Message[(exception.Message.IndexOf(": ", StringComparison.Ordinal) + 2)..];
		if (reason.StartsWith("handler '", StringComparison.Ordinal)) reason = reason[(reason.IndexOf("' ", 9, StringComparison.Ordinal) + 2)..];
		Assert.Equal(expected, reason.TrimEnd('.'));
	}

	public static async Task<RazorResult> Razor(string family, string fixture, string directory)
	{
		var generated = "Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/";
		var compilation = Compile(Parse(Read(family, fixture, "generated"), generated + fixture + "_razor.g.cs"),
			Parse(Read(family, fixture, "declaration"), generated + fixture + "_razor.decl.g.cs"));
		var imports = family == "FormDiagnostic" ? Read(family, "_Imports", "razor") : "@using Microsoft.AspNetCore.Components\n@using Htmxor\n@using Htmxor.Components\n";
		ImmutableArray<AdditionalText> files = [new File(directory + "/" + fixture + ".razor", Read(family, fixture, "razor")), new File(directory + "/_Imports.razor", imports)];
		var options = new Options(directory);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new HtmxorRouteGenerator().AsSourceGenerator(), new HtmxorActionGenerator().AsSourceGenerator()], files,
			(CSharpParseOptions)compilation.SyntaxTrees.First().Options, options);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var errors);
		Assert.DoesNotContain(errors, diagnostic => diagnostic.Id == "CS8785");
		Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var diagnostics = await output.WithAnalyzers([new HtmxorRouteDeclarationAnalyzer(), new HtmxorActionDeclarationAnalyzer(), new HtmxorCompiledBindingAnalyzer()], new AnalyzerOptions(files, options)).GetAnalyzerDiagnosticsAsync();
		return new RazorResult(compilation, driver.GetRunResult().Diagnostics.AddRange(diagnostics));
	}

	private static string Read(string family, string fixture, string part)
	{
		using var stream = typeof(SharedRejectionReasonTestHost).Assembly.GetManifestResourceStream("Htmxor.Generators." + family + "Fixtures." + fixture + "." + part + ".fixture")!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	internal sealed record RazorResult(CSharpCompilation Compilation, ImmutableArray<Diagnostic> Diagnostics);
	private sealed class File(string path, string content) : AdditionalText
	{
		public override string Path => path;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
	}
	private sealed class Options(string directory) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(directory);
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
	}
	private sealed class Values(string directory) : AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
		{
			value = key == "build_property.RootNamespace" ? "Consumer" : directory;
			return key is "build_property.RootNamespace" or "build_property.MSBuildProjectDirectory";
		}
	}
}
