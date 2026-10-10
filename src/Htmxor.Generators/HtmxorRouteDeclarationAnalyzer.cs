using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorRouteDeclarationAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor UnsupportedDeclaration = new(
		"HTMXOR001",
		"Unsupported HTMX-only route declaration",
		"Unsupported HTMX-only route declaration: {0}",
		"Htmxor.Generators",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[]
		{
			WellKnownDiagnosticTags.NotConfigurable,
			WellKnownDiagnosticTags.CompilationEnd,
		});

	private static readonly DiagnosticDescriptor UnsupportedNormalOnlyDeclaration = new(
		"HTMXOR003",
		"Unsupported normal-only route declaration",
		"Unsupported normal-only route declaration: {0}",
		"Htmxor.Generators",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[]
		{
			WellKnownDiagnosticTags.NotConfigurable,
			WellKnownDiagnosticTags.CompilationEnd,
		});

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(UnsupportedDeclaration, UnsupportedNormalOnlyDeclaration);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterCompilationAction(AnalyzeCompilation);
	}

	private static void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		var symbols = HtmxorRouteSymbols.Resolve(context.Compilation);
		if (symbols is null)
		{
			return;
		}

		var manifest = GetRazorManifest(context);
		var components = HtmxorRoutedComponent.FindAll(context.Compilation.Assembly, symbols);

		foreach (var component in components)
		{
			var reason = component.GetUnsupportedReason(
				symbols,
				manifest,
				context.CancellationToken);
			if (reason is not null)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					UnsupportedDeclaration,
					component.GetLocation(context.CancellationToken),
					reason));
			}
		}

		if (symbols.DisableHtmxDirectRouting is null)
		{
			return;
		}

		foreach (var declaration in HtmxorNormalOnlyDeclaration.FindAll(
			context.Compilation.Assembly,
			symbols.DisableHtmxDirectRouting))
		{
			var reason = declaration.GetUnsupportedReason(symbols, context.CancellationToken);
			if (reason is not null)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					UnsupportedNormalOnlyDeclaration,
					declaration.GetLocation(context.CancellationToken),
					reason));
			}
		}
	}

	// Each derived Razor component type name with the generated hint path of the file that claims it.
	private static ILookup<string, string?> GetRazorManifest(CompilationAnalysisContext context)
	{
		var typeNames = RazorComponentTypeNames.Create(
			context.Options.AnalyzerConfigOptionsProvider,
			context.Options.AdditionalFiles,
			context.CancellationToken);
		return context.Options.AdditionalFiles
			.Select(file => (
				TypeName: typeNames?.GetTypeName(file, context.CancellationToken),
				GeneratedPath: typeNames?.GetGeneratedPath(file.Path)))
			.Where(static entry => entry.TypeName is not null)
			.ToLookup(static entry => entry.TypeName!, static entry => entry.GeneratedPath, StringComparer.Ordinal);
	}
}
