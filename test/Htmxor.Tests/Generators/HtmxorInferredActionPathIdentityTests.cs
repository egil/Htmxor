using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorInferredActionDiagnosticsTests
{
	private const string PathCaseDirectory = "/tmp/issue338-stale-case";
	private const string LowerPath = PathCaseDirectory + "/Components/admin/Foo.razor";
	private const string StalePath = PathCaseDirectory + "/Components/Admin/Foo.razor";
	private const string MessagePrefix = "no usable action is available: Use [HtmxAction] to explicitly declare a supported instance action instead of this binding; ";

	[CaseSensitiveSourcePathsTheory]
	[InlineData("matching-only", false)]
	[InlineData("matching-only", true)]
	[InlineData("stale-first", false)]
	[InlineData("stale-first", true)]
	[InlineData("stale-last", false)]
	[InlineData("stale-last", true)]
	public async Task Case_distinct_stale_scanner_input_cannot_hide_the_real_components_causes(string input, bool local)
	{
		var result = await AnalyzePathCaseAsync(input);

		AssertPathCaseDiagnostics(input, local, local ? result.Local : result.Complete);
	}

	private static void AssertPathCaseDiagnostics(string input, bool local, ImmutableArray<Diagnostic> diagnostics)
	{
		var expected = new List<PathCaseDiagnostic>
		{
			new("HTMXOR013", MessagePrefix + "handler 'M' is static; make it an instance method and declare [HtmxAction]", LowerPath, 1, 16, 1, 17),
			new("HTMXOR018", MessagePrefix + "handler 'M' is async void; return Task instead", LowerPath, 1, 16, 1, 17),
			new("HTMXOR013", MessagePrefix + "handler 'Other' is static; make it an instance method and declare [HtmxAction]", LowerPath, 2, 16, 2, 21),
		};
		if (!local)
		{
			const string conflict = MessagePrefix + "@onput binds different handlers 'M' and 'Other'; bind one handler per HTTP method";
			expected.Add(new("HTMXOR031", conflict, LowerPath, 1, 8, 1, 14));
			expected.Add(new("HTMXOR031", conflict, LowerPath, 2, 8, 2, 14));
			if (input != "matching-only")
			{
				expected.Add(new("HTMXOR037", MessagePrefix + "the action owner must compile from the matching Razor component; its type name is derived from the file's folder and @namespace", StalePath, 1, 8, 1, 14));
			}
		}
		var actual = diagnostics.Select(diagnostic =>
		{
			var mapped = diagnostic.Location.GetMappedLineSpan();
			return new PathCaseDiagnostic(diagnostic.Id, diagnostic.GetMessage(), mapped.Path,
				mapped.StartLinePosition.Line, mapped.StartLinePosition.Character, mapped.EndLinePosition.Line, mapped.EndLinePosition.Character);
		});

		Assert.Equal(OrderPathCaseDiagnostics(expected), OrderPathCaseDiagnostics(actual));
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
			Assert.Equal(diagnostic.Id is "HTMXOR031" or "HTMXOR037",
				diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.CompilationEnd));
			Assert.Equal(diagnostic.Id is "HTMXOR013" or "HTMXOR018", diagnostic.Location.SourceTree is not null);
		});
	}

	private static IOrderedEnumerable<PathCaseDiagnostic> OrderPathCaseDiagnostics(IEnumerable<PathCaseDiagnostic> diagnostics)
		=> diagnostics.OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
			.ThenBy(diagnostic => diagnostic.StartLine).ThenBy(diagnostic => diagnostic.StartColumn)
			.ThenBy(diagnostic => diagnostic.EndLine).ThenBy(diagnostic => diagnostic.EndColumn).ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal);

	private sealed record PathCaseDiagnostic(string Id, string Message, string Path, int StartLine, int StartColumn, int EndLine, int EndColumn);

	private static async Task<Result> AnalyzePathCaseAsync(string input)
	{
		const string generatedDirectory = "Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/Components/admin/";
		var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var tree = CSharpSyntaxTree.ParseText(Read("PathCaseLower", "generated"), parse, generatedDirectory + "Foo_razor.g.cs");
		var declaration = CSharpSyntaxTree.ParseText(Read("PathCaseLower", "declaration"), parse, generatedDirectory + "Foo_razor.decl.g.cs");
		var compilation = CSharpCompilation.Create("PathCaseFeedback", [tree, declaration], References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		Assert.NotNull(compilation.GetTypeByMetadataName("Consumer.Components.admin.Foo"));
		Assert.Null(compilation.GetTypeByMetadataName("Consumer.Components.Admin.Foo"));
		AssertPathCaseBinding(compilation.GetSemanticModel(tree), tree);

		var matching = new File(LowerPath, Read("PathCaseLower", "razor"));
		var stale = new File(StalePath, Read("PathCaseStale", "razor"));
		var imports = new File(PathCaseDirectory + "/_Imports.razor", "@using Microsoft.AspNetCore.Components\n@using Htmxor\n@using Htmxor.Components\n");
		ImmutableArray<AdditionalText> files = input switch
		{
			"matching-only" => [matching, imports],
			"stale-first" => [stale, matching, imports],
			"stale-last" => [matching, stale, imports],
			_ => throw new ArgumentOutOfRangeException(nameof(input)),
		};
		var options = new Options("Consumer", PathCaseDirectory);
		GeneratorDriver generator = CSharpGeneratorDriver.Create([new HtmxorRouteGenerator().AsSourceGenerator(), new HtmxorActionGenerator().AsSourceGenerator()], files, parse, options);
		generator = generator.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
		Assert.DoesNotContain(generatorDiagnostics, diagnostic => diagnostic.Id == "CS8785");
		Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		Assert.Empty(generator.GetRunResult().Results[1].GeneratedSources);
		Assert.Null(output.GetTypeByMetadataName("Consumer.Components.Admin.Foo"));
		var analyzers = output.WithAnalyzers([new HtmxorActionDeclarationAnalyzer(), new HtmxorCompiledBindingAnalyzer()], new AnalyzerOptions(files, options));
		return new Result(generator.GetRunResult().Diagnostics.AddRange(await analyzers.GetAnalyzerDiagnosticsAsync()),
			await analyzers.GetAnalyzerSemanticDiagnosticsAsync(output.GetSemanticModel(tree), null, CancellationToken.None), string.Empty);
	}

	private static void AssertPathCaseBinding(SemanticModel model, SyntaxTree tree)
	{
		var calls = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
			.Where(call => model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "Create", ContainingType.Name: "EventCallbackFactory" }).ToArray();
		Assert.Equal(2, calls.Length);
		var call = calls[0];
		var factory = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(call).Symbol);
		Assert.Equal("Microsoft.AspNetCore.Components.EventCallbackFactory", factory.ContainingType.ToDisplayString());
		Assert.Equal("Htmxor.HtmxEventArgs", Assert.Single(factory.TypeArguments).ToDisplayString());
		Assert.Equal(CandidateReason.None, model.GetSymbolInfo(call).CandidateReason);
		Assert.Equal(LineVisibility.Hidden, tree.GetLineVisibility(call.SpanStart));
		var method = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(call.ArgumentList.Arguments[1].Expression).Symbol);
		Assert.Equal("M", method.Name);
		Assert.Equal("Consumer.Components.admin.Foo", method.ContainingType.ToDisplayString());
		Assert.True(method.IsStatic && method.IsAsync && method.ReturnsVoid);
		var mapped = call.ArgumentList.Arguments[1].Expression.GetLocation().GetMappedLineSpan();
		Assert.Equal(LowerPath, mapped.Path);
		Assert.Equal(new LinePosition(1, 16), mapped.StartLinePosition);
		Assert.Equal(new LinePosition(1, 17), mapped.EndLinePosition);
		var staleLine = SourceText.From(Read("PathCaseStale", "razor")).Lines[1].ToString();
		var start = staleLine.IndexOf("@onput", StringComparison.Ordinal);
		var end = staleLine.IndexOf("\">", StringComparison.Ordinal) + 1;
		Assert.InRange(mapped.StartLinePosition.Character, start, end);
		Assert.InRange(mapped.EndLinePosition.Character, start, end);
	}

	[AttributeUsage(AttributeTargets.Method)]
	private sealed class CaseSensitiveSourcePathsTheoryAttribute : TheoryAttribute
	{
		public CaseSensitiveSourcePathsTheoryAttribute()
		{
			if (OperatingSystem.IsWindows())
			{
				Skip = "Case-distinct authored paths require the existing non-Windows source-identity policy.";
			}
		}
	}
}
