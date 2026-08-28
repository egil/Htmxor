using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorActionDeclarationAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor UnsupportedDeclaration = new(
		"HTMXOR002",
		"Unsupported component action declaration",
		"Unsupported component action declaration: {0}",
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
		if (symbols is null || symbols.Route is null)
		{
			return;
		}

		foreach (var declaration in GetDeclarations(context))
		{
			if (declaration.UnsupportedReason is not null)
			{
				continue;
			}

			var reason = GetUnsupportedReason(
				context.Compilation.Assembly.GetTypeByMetadataName(declaration.ComponentTypeName),
				declaration,
				symbols);
			if (reason is not null)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					UnsupportedDeclaration,
					Location.Create(declaration.Path, declaration.Span, declaration.LineSpan),
					reason));
			}
		}
	}

	private static IEnumerable<HtmxorComponentActionDeclaration> GetDeclarations(
		CompilationAnalysisContext context)
		=> context.Options.AdditionalFiles
			.Where(static file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
			.SelectMany(file => HtmxorComponentActionDeclaration.ParseAll(
				file,
				ProjectRootComponentManifest.GetTypeName(
					file,
					context.Options.AnalyzerConfigOptionsProvider),
				context.CancellationToken));

	private static string? GetUnsupportedReason(
		INamedTypeSymbol? component,
		HtmxorComponentActionDeclaration declaration,
		HtmxorRouteSymbols symbols)
	{
		if (component is null)
		{
			return "the action owner must compile as a project-root Razor component";
		}

		var stockRoutes = GetExactAttributes(component, symbols.Route!);
		var htmxRoutes = GetExactAttributes(component, symbols.HtmxRoute);
		if (declaration.UsesStockRoute)
		{
			return stockRoutes.Length == 1 && htmxRoutes.Length == 0
				? null
				: "a component with a local @page action must compile with exactly one stock route and no HtmxRoute";
		}

		if (stockRoutes.Length > 0)
		{
			return "a component action without a local @page cannot use a compiled stock route";
		}

		if (htmxRoutes.Length != 1)
		{
			return "a component action without a local @page must compile with exactly one HtmxRoute";
		}

		var methods = htmxRoutes[0].NamedArguments
			.Where(static argument => string.Equals(argument.Key, "Methods", StringComparison.Ordinal))
			.Select(static argument => argument.Value)
			.ToImmutableArray();
		return methods.Length == 0 || ContainsMethod(methods[0], declaration.HttpMethod)
			? null
			: "explicit HtmxRoute.Methods is authoritative and does not allow the " +
				declaration.HttpMethod + " binding";
	}

	private static ImmutableArray<AttributeData> GetExactAttributes(
		INamedTypeSymbol component,
		INamedTypeSymbol attributeType)
		=> component.GetAttributes()
			.Where(attribute => SymbolEqualityComparer.Default.Equals(
				attribute.AttributeClass,
				attributeType))
			.ToImmutableArray();

	private static bool ContainsMethod(TypedConstant methods, string httpMethod)
		=> methods.Kind == TypedConstantKind.Array && methods.Values.Any(value =>
			value.Value is string method && string.Equals(method, httpMethod, StringComparison.OrdinalIgnoreCase));
}
