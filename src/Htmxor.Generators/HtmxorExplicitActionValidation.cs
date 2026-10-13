using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Htmxor.Generators;

internal static class HtmxorExplicitActionValidation
{
	private static readonly string[] SupportedMethods = { "POST", "PUT", "PATCH", "DELETE", "QUERY" };

	public static void Analyze(CompilationAnalysisContext context)
	{
		var action = context.Compilation.GetTypeByMetadataName("Htmxor.HtmxActionAttribute");
		var symbols = HtmxorRouteSymbols.Resolve(context.Compilation);
		if (action is null || symbols is null || context.Compilation.GetDiagnostics(context.CancellationToken)
			.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
		{
			return;
		}

		AnalyzeNonMemberDeclarations(context, action);
		var bindings = RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken).ToArray();
		var forms = RazorCompiledForm.Find(context.Compilation, context.CancellationToken).ToArray();
		foreach (var component in GetTypes(context.Compilation.Assembly.GlobalNamespace))
		{
			AnalyzeOwner(context, component, action, symbols, bindings, forms);
		}
	}

	private static void AnalyzeNonMemberDeclarations(CompilationAnalysisContext context, INamedTypeSymbol action)
	{
		foreach (var tree in context.Compilation.SyntaxTrees)
		{
			var model = context.Compilation.GetSemanticModel(tree);
			foreach (var attribute in tree.GetRoot(context.CancellationToken).DescendantNodes().OfType<AttributeSyntax>())
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				var target = attribute.Parent?.Parent;
				IMethodSymbol? handler = target switch
				{
					LocalFunctionStatementSyntax local => model.GetDeclaredSymbol(local, context.CancellationToken) as IMethodSymbol,
					LambdaExpressionSyntax lambda => (model.GetOperation(lambda, context.CancellationToken) as IAnonymousFunctionOperation)?.Symbol,
					_ => null,
				};
				if (handler is null || model.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor ||
					!SymbolEqualityComparer.Default.Equals(constructor.ContainingType, action))
				{
					continue;
				}

				context.ReportDiagnostic(Diagnostic.Create(HtmxorActionDeclarationAnalyzer.UnsupportedDeclaration,
					attribute.GetLocation(), HtmxorActionHandler.GetExplicitUnsupportedReason(context.Compilation, handler)));
			}
		}
	}

	private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol scope)
		=> scope.GetTypeMembers().SelectMany(GetTypes)
			.Concat(scope.GetNamespaceMembers().SelectMany(GetTypes));

	private static IEnumerable<INamedTypeSymbol> GetTypes(INamedTypeSymbol type)
		=> new[] { type }.Concat(type.GetTypeMembers().SelectMany(GetTypes));

	private static void AnalyzeOwner(CompilationAnalysisContext context, INamedTypeSymbol component,
		INamedTypeSymbol action, HtmxorRouteSymbols symbols, RazorCompiledEventBinding[] bindings, RazorCompiledForm[] forms)
	{
		var declarations = component.GetMembers().OfType<IMethodSymbol>()
			.SelectMany(method => method.GetAttributes().Where(attribute => Is(attribute, action))
				.Select(attribute => (Handler: method, Attribute: attribute))).ToArray();
		foreach (var declaration in declarations)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			var method = ReadMethod(declaration.Attribute);
			var reason = GetReason(context.Compilation, component, declaration.Handler, method, symbols)
				?? GetConflictReason(component, declaration.Handler, method!, declarations, bindings, forms);
			if (reason is not null && declaration.Attribute.ApplicationSyntaxReference is { } syntax)
			{
				context.ReportDiagnostic(Diagnostic.Create(HtmxorActionDeclarationAnalyzer.UnsupportedDeclaration,
					syntax.GetSyntax(context.CancellationToken).GetLocation(), reason));
			}
		}
	}

	private static string? ReadMethod(AttributeData attribute)
		=> attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string method
			? SupportedMethods.FirstOrDefault(allowed => string.Equals(method, allowed, StringComparison.OrdinalIgnoreCase)) : null;

	private static string? GetReason(Compilation compilation, INamedTypeSymbol component,
		IMethodSymbol handler, string? method, HtmxorRouteSymbols symbols)
	{
		if (method is not ("POST" or "PUT" or "PATCH" or "DELETE" or "QUERY"))
		{
			return "HtmxAction method must be POST, PUT, PATCH, DELETE or QUERY";
		}

		var attributes = component.GetAttributes();
		var route = attributes.FirstOrDefault(attribute => Is(attribute, symbols.HtmxRoute));
		if ((route is null && !attributes.Any(attribute => Is(attribute, symbols.Route))) ||
			!component.AllInterfaces.Any(type => SymbolEqualityComparer.Default.Equals(type, symbols.Component)))
		{
			return "HtmxAction must be declared on the component type that owns its route, not an inherited or unrouted owner";
		}

		if (attributes.Any(attribute => Is(attribute, symbols.DisableHtmxDirectRouting)))
		{
			return "HtmxAction is not supported on a component marked with DisableHtmxDirectRouting";
		}

		return HtmxorActionHandler.GetExplicitUnsupportedReason(compilation, handler) ?? GetAllowListReason(route, method);
	}

	private static string? GetAllowListReason(AttributeData? route, string method)
	{
		var methods = route?.NamedArguments.FirstOrDefault(static argument => argument.Key == "Methods");
		return methods is { Key: "Methods" } argument &&
			(argument.Value.IsNull || argument.Value.Kind != TypedConstantKind.Array || !argument.Value.Values.Any(value =>
				value.Value is string allowed && string.Equals(allowed, method, StringComparison.OrdinalIgnoreCase)))
			? "explicit HtmxRoute.Methods does not allow the " + method + " HtmxAction" : null;
	}

	private static string? GetConflictReason(INamedTypeSymbol component, IMethodSymbol handler, string method,
		(IMethodSymbol Handler, AttributeData Attribute)[] declarations, RazorCompiledEventBinding[] bindings, RazorCompiledForm[] forms)
	{
		if (declarations.Any(declaration => ReadMethod(declaration.Attribute) == method &&
			!SymbolEqualityComparer.Default.Equals(declaration.Handler, handler)) ||
			bindings.Any(binding => binding.HttpMethod == method && binding.Handler is not null &&
				SymbolEqualityComparer.Default.Equals(binding.Component, component) &&
				!SymbolEqualityComparer.Default.Equals(binding.Handler, handler)))
		{
			return method + " declarations on one route owner must name the same handler";
		}

		return method == "POST" && forms.Any(form => form.IsNamed && SymbolEqualityComparer.Default.Equals(form.Component, component))
			? "named forms and a POST HtmxAction cannot share one route owner" : null;
	}

	private static bool Is(AttributeData attribute, INamedTypeSymbol? type)
		=> type is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, type);
}
