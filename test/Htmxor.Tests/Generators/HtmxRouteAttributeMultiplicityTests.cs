using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators.Tests;

/// <summary>
/// <c>HtmxRouteAttribute</c> allows one use per class, so the C# compiler alone reports a second
/// <c>[HtmxRoute]</c> as CS0579 and Htmxor's analyzer adds nothing. This uses its own compilation
/// because <c>RunAnalyzerAsync</c> rejects fixtures that have compiler errors.
/// </summary>
public sealed class HtmxRouteAttributeMultiplicityTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Second_HtmxRoute_attribute_is_compiler_error_CS0579_with_no_added_diagnostic(bool local)
	{
		const string source = """
			namespace Htmxor.Consumer;

			[global::Htmxor.HtmxRouteAttribute("/items/{Id:int}", Methods = ["GET"])]
			[global::Htmxor.HtmxRouteAttribute("/other/{Id:int}", Methods = ["GET"])]
			public sealed class ItemComponent : global::Microsoft.AspNetCore.Components.ComponentBase;
			""";
		var compilation = CSharpCompilation.Create(
			"Htmxor.Multiplicity.Tests",
			new[] { CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default, "ItemComponent.cs") },
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var compilerDiagnostics = compilation.GetDiagnostics();
		var cs0579 = compilerDiagnostics.Where(static d => d.Id == "CS0579").ToImmutableArray();

		var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
		var driver = compilation
			.WithAnalyzers(
				ImmutableArray.Create<DiagnosticAnalyzer>(new HtmxorRouteDeclarationAnalyzer()),
				analyzerOptions);
		var analyzerDiagnostics = local
			? await driver.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(compilation.SyntaxTrees.Single()), null, CancellationToken.None)
			: await driver.GetAnalyzerDiagnosticsAsync();

		Assert.True(
			cs0579.Length == 1,
			"Expected exactly one CS0579 for the duplicate HtmxRoute attribute, found: " +
			string.Join(", ", compilerDiagnostics.Select(d => d.Id)));
		Assert.Empty(analyzerDiagnostics);
	}

	private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

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
}
