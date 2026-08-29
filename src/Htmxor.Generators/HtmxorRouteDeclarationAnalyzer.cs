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

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(UnsupportedDeclaration);

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

		var manifest = ProjectRootComponentManifest
			.GetTypeNames(context.Options.AdditionalFiles, context.Options.AnalyzerConfigOptionsProvider)
			.ToImmutableHashSet(StringComparer.Ordinal);
		var components = HtmxorRoutedComponent.FindAll(context.Compilation.Assembly, symbols);

		foreach (var component in components)
		{
			var reason = component.GetUnsupportedReason(
				symbols,
				manifest,
				context.Options.AnalyzerConfigOptionsProvider,
				components.Length,
				context.CancellationToken);
			if (reason is not null)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					UnsupportedDeclaration,
					component.GetLocation(context.CancellationToken),
					reason));
			}
		}
	}
}
