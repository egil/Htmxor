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
	internal static readonly DiagnosticDescriptor UnsupportedDeclaration = new(
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

		var typeNames = RazorComponentTypeNames.Create(
			context.Options.AnalyzerConfigOptionsProvider,
			context.Options.AdditionalFiles,
			context.CancellationToken);
		foreach (var declaration in GetDeclarations(context, typeNames))
		{
			if (declaration.UnsupportedReason is not null)
			{
				continue;
			}

			var reason = GetUnsupportedReason(
				context.Compilation,
				context.Compilation.Assembly.GetTypeByMetadataName(declaration.ComponentTypeName),
				declaration,
				typeNames?.GetGeneratedPath(declaration.Path),
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
		CompilationAnalysisContext context,
		RazorComponentTypeNames? typeNames)
		=> context.Options.AdditionalFiles
			.Where(static file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
			.SelectMany(file => HtmxorComponentActionDeclaration.ParseAll(
				file,
				typeNames?.GetTypeName(file, context.CancellationToken),
				context.CancellationToken));

	private static string? GetUnsupportedReason(
		Compilation compilation,
		INamedTypeSymbol? component,
		HtmxorComponentActionDeclaration declaration,
		string? generatedPath,
		HtmxorRouteSymbols symbols)
	{
		var componentReason = GetComponentUnsupportedReason(
			compilation,
			component,
			declaration,
			generatedPath) ?? GetNormalOnlyUnsupportedReason(component!, symbols);
		if (componentReason is not null)
		{
			return componentReason;
		}

		var resolvedComponent = component!;
		var stockRoutes = GetExactAttributes(resolvedComponent, symbols.Route!);
		var htmxRoutes = GetExactAttributes(resolvedComponent, symbols.HtmxRoute);
		return stockRoutes.Length == 0 && htmxRoutes.Length == 0
			? "the component owns no route, so the binding has no action to declare; add @page or HtmxRoute, or remove the binding"
			: GetRouteShapeUnsupportedReason(declaration, stockRoutes, htmxRoutes);
	}

	private static string? GetRouteShapeUnsupportedReason(
		HtmxorComponentActionDeclaration declaration,
		ImmutableArray<AttributeData> stockRoutes,
		ImmutableArray<AttributeData> htmxRoutes)
	{
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

	private static string? GetNormalOnlyUnsupportedReason(INamedTypeSymbol component, HtmxorRouteSymbols symbols)
		=> symbols.DisableHtmxDirectRouting is not null &&
			GetExactAttributes(component, symbols.DisableHtmxDirectRouting).Length > 0
			? "an inferred binding on a component marked with DisableHtmxDirectRouting is not supported"
			: null;

	private static ImmutableArray<AttributeData> GetExactAttributes(
		INamedTypeSymbol component,
		INamedTypeSymbol attributeType)
		=> component.GetAttributes()
			.Where(attribute => SymbolEqualityComparer.Default.Equals(
				attribute.AttributeClass,
				attributeType))
			.ToImmutableArray();

	private static string? GetComponentUnsupportedReason(
		Compilation compilation,
		INamedTypeSymbol? component,
		HtmxorComponentActionDeclaration declaration,
		string? generatedPath)
	{
		if (component is null ||
			!HtmxorRouteManifest.HasCompiledRazorDeclaration(component, new[] { generatedPath }))
		{
			return "the action owner must compile from the matching Razor component; " +
				"its type name is derived from the file's folder and @namespace";
		}

		return declaration.HandlerName is null || declaration.HandlerAccess is null
			? null
			: HtmxorActionHandler.GetUnsupportedReason(compilation, component, declaration);
	}

	private static bool ContainsMethod(TypedConstant methods, string httpMethod)
		=> methods.Kind == TypedConstantKind.Array && methods.Values.Any(value =>
			value.Value is string method && string.Equals(method, httpMethod, StringComparison.OrdinalIgnoreCase));
}
