using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorRouteDeclarationAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor UnsupportedNormalOnlyDeclaration = new(
		"HTMXOR003",
		"Unsupported normal-only route declaration",
		"Unsupported normal-only route declaration: {0}",
		"Htmxor.Generators",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: HtmxorDiagnosticRules.HelpLinkBase + "#htmxor003",
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		HtmxorDiagnosticRules.RouteDiagnostics.Add(UnsupportedNormalOnlyDeclaration).Add(HtmxorDiagnosticRules.ManifestOrigin);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static start =>
		{
			var symbols = HtmxorRouteSymbols.Resolve(start.Compilation);
			if (symbols is null)
			{
				return;
			}

			start.RegisterSymbolAction(context => AnalyzeSymbol(context, symbols), SymbolKind.NamedType);
			start.RegisterCompilationEndAction(context => AnalyzeManifest(context, symbols));
		});
	}

	private static void AnalyzeSymbol(SymbolAnalysisContext context, HtmxorRouteSymbols symbols)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		var type = (INamedTypeSymbol)context.Symbol;
		var component = HtmxorRoutedComponent.Find(type, symbols);
		if (component is not null)
		{
			foreach (var issue in component.GetLocalIssues(symbols, context.CancellationToken))
			{
				context.ReportDiagnostic(issue.ToDiagnostic(component.GetLocation(context.CancellationToken)));
			}
		}

		AnalyzeNormalOnly(context, type, symbols);
	}

	private static void AnalyzeNormalOnly(
		SymbolAnalysisContext context,
		INamedTypeSymbol type,
		HtmxorRouteSymbols symbols)
	{
		if (symbols.DisableHtmxDirectRouting is null)
		{
			return;
		}

		foreach (var declaration in HtmxorNormalOnlyDeclaration.Find(type, symbols.DisableHtmxDirectRouting))
		{
			foreach (var reason in declaration.GetUnsupportedReasons(symbols, context.CancellationToken))
			{
				context.ReportDiagnostic(Diagnostic.Create(
					UnsupportedNormalOnlyDeclaration,
					declaration.GetLocation(context.CancellationToken),
					reason));
			}
		}
	}

	private static void AnalyzeManifest(CompilationAnalysisContext context, HtmxorRouteSymbols symbols)
	{
		var manifest = GetRazorManifest(context);
		foreach (var type in HtmxorRoutedComponent.GetTypes(context.Compilation.Assembly.GlobalNamespace))
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			var component = HtmxorRoutedComponent.Find(type, symbols);
			if (component?.GetManifestIssue(manifest) is { } issue)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					HtmxorDiagnosticRules.ManifestOrigin,
					component.GetLocation(context.CancellationToken),
					issue.Reason));
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
