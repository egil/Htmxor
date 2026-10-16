using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorActionDeclarationAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		HtmxorDiagnosticRules.ActionDiagnostics.AddRange(HtmxorDiagnosticRules.LocalActionDiagnostics);

	internal static string WithExplicitRemediation(string reason)
		=> "Use [HtmxAction] to explicitly declare a supported instance action instead of this binding; " + reason;

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static start =>
		{
			var action = start.Compilation.GetTypeByMetadataName("Htmxor.HtmxActionAttribute");
			var symbols = HtmxorRouteSymbols.Resolve(start.Compilation);
			if (action is not null && symbols is not null)
			{
				start.RegisterSyntaxNodeAction(
					context => HtmxorExplicitActionValidation.AnalyzeLocal(context, action, symbols),
					SyntaxKind.Attribute);
			}
			start.RegisterCompilationEndAction(AnalyzeCompilation);
		});
	}

	private static void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		HtmxorExplicitActionValidation.AnalyzeConflicts(context);
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
			if (declaration.UnsupportedIssue is not null)
			{
				continue;
			}

			var component = context.Compilation.Assembly.GetTypeByMetadataName(declaration.ComponentTypeName);
			var issue = GetUnsupportedIssue(
				context.Compilation,
				component,
				declaration,
				typeNames?.GetGeneratedPath(declaration.Path),
				symbols);
			if (issue is { } diagnosed)
			{
				var message = diagnosed.Cause == HtmxorDiagnosticCause.NormalOnlyAction
					? diagnosed.Reason : WithExplicitRemediation(diagnosed.Reason);
				context.ReportDiagnostic(diagnosed.WithReason(message)
					.ToDiagnostic(Location.Create(declaration.Path, declaration.Span, declaration.LineSpan)));
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

	private static HtmxorDiagnosticIssue? GetUnsupportedIssue(
		Compilation compilation,
		INamedTypeSymbol? component,
		HtmxorComponentActionDeclaration declaration,
		string? generatedPath,
		HtmxorRouteSymbols symbols)
	{
		var componentIssue = GetComponentUnsupportedIssue(
			compilation,
			component,
			declaration,
			generatedPath) ?? GetNormalOnlyUnsupportedIssue(component!, symbols);
		if (componentIssue is not null)
		{
			return componentIssue;
		}

		var resolvedComponent = component!;
		var stockRoutes = GetExactAttributes(resolvedComponent, symbols.Route!);
		var htmxRoutes = GetExactAttributes(resolvedComponent, symbols.HtmxRoute);
		return stockRoutes.Length == 0 && htmxRoutes.Length == 0
			? new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.UnroutedBinding,
				"the component owns no route, so the binding has no action to declare; add @page or HtmxRoute, or remove the binding")
			: GetRouteShapeUnsupportedIssue(declaration, stockRoutes, htmxRoutes);
	}

	private static HtmxorDiagnosticIssue? GetRouteShapeUnsupportedIssue(
		HtmxorComponentActionDeclaration declaration,
		ImmutableArray<AttributeData> stockRoutes,
		ImmutableArray<AttributeData> htmxRoutes)
	{
		if (declaration.UsesStockRoute)
		{
			return stockRoutes.Length == 1 && htmxRoutes.Length == 0
				? null
				: new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.LocalPageRouteMismatch,
					"a component with a local @page action must compile with exactly one stock route and no HtmxRoute");
		}

		if (stockRoutes.Length > 0)
		{
			return new(HtmxorDiagnosticCause.StockRouteWithoutLocalPage,
				"a component action without a local @page cannot use a compiled stock route");
		}

		var methods = htmxRoutes[0].NamedArguments
			.Where(static argument => string.Equals(argument.Key, "Methods", StringComparison.Ordinal))
			.Select(static argument => argument.Value)
			.ToImmutableArray();
		return methods.Length == 0 || ContainsMethod(methods[0], declaration.HttpMethod)
			? null
			: new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.ExplicitMethodsConflict,
				"explicit HtmxRoute.Methods is authoritative and does not allow the " +
				declaration.HttpMethod + " binding; add the method or remove the binding");
	}

	private static HtmxorDiagnosticIssue? GetNormalOnlyUnsupportedIssue(INamedTypeSymbol component, HtmxorRouteSymbols symbols)
		=> symbols.DisableHtmxDirectRouting is not null &&
			GetExactAttributes(component, symbols.DisableHtmxDirectRouting).Length > 0
			? new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.NormalOnlyAction,
				"an inferred binding on a component marked with DisableHtmxDirectRouting is not supported; remove the binding or marker")
			: null;

	private static ImmutableArray<AttributeData> GetExactAttributes(
		INamedTypeSymbol component,
		INamedTypeSymbol attributeType)
		=> component.GetAttributes()
			.Where(attribute => SymbolEqualityComparer.Default.Equals(
				attribute.AttributeClass,
				attributeType))
			.ToImmutableArray();

	private static HtmxorDiagnosticIssue? GetComponentUnsupportedIssue(
		Compilation compilation,
		INamedTypeSymbol? component,
		HtmxorComponentActionDeclaration declaration,
		string? generatedPath)
	{
		if (component is null ||
			!HtmxorRouteManifest.HasCompiledRazorDeclaration(component, new[] { generatedPath }))
		{
			return new(HtmxorDiagnosticCause.MismatchedRazorOwner,
				"the action owner must compile from the matching Razor component; " +
				"its type name is derived from the file's folder and @namespace");
		}

		return declaration.HandlerName is null || declaration.HandlerAccess is null
			? null
			: HtmxorActionHandler.GetUnsupportedIssue(compilation, component, declaration);
	}

	private static bool ContainsMethod(TypedConstant methods, string httpMethod)
		=> methods.Kind == TypedConstantKind.Array && methods.Values.Any(value =>
			value.Value is string method && string.Equals(method, httpMethod, StringComparison.OrdinalIgnoreCase));
}
