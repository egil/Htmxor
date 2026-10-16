using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal static class HtmxorExplicitActionValidation
{
	private static readonly string[] SupportedMethods = { "POST", "PUT", "PATCH", "DELETE", "QUERY" };

	public static void AnalyzeLocal(SyntaxNodeAnalysisContext context, INamedTypeSymbol action, HtmxorRouteSymbols symbols)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		var attribute = (AttributeSyntax)context.Node;
		var declaration = attribute.Parent?.Parent;
		if (declaration is null ||
			context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor ||
			!SymbolEqualityComparer.Default.Equals(constructor.ContainingType, action) ||
			HasCompilerError(context.SemanticModel, declaration, attribute, context.CancellationToken))
		{
			return;
		}

		var handler = GetHandler(context.SemanticModel, declaration, context.CancellationToken);
		if (handler?.ContainingType is not { } component)
		{
			return;
		}

		var method = ReadMethod(context.SemanticModel, attribute, context.CancellationToken);
		foreach (var issue in GetLocalIssues(context.Compilation, component, handler, method, symbols))
		{
			context.ReportDiagnostic(issue.ToLocalDiagnostic(attribute.GetLocation()));
		}
	}

	public static void AnalyzeConflicts(CompilationAnalysisContext context)
	{
		var action = context.Compilation.GetTypeByMetadataName("Htmxor.HtmxActionAttribute");
		if (action is null)
		{
			return;
		}

		var bindings = RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken).ToArray();
		var forms = RazorCompiledForm.Find(context.Compilation, context.CancellationToken).ToArray();
		foreach (var component in GetTypes(context.Compilation.Assembly.GlobalNamespace))
		{
			AnalyzeOwnerConflicts(context, component, action, bindings, forms);
		}
	}

	private static IMethodSymbol? GetHandler(SemanticModel model, SyntaxNode declaration, System.Threading.CancellationToken token)
		=> declaration switch
		{
			MethodDeclarationSyntax method => model.GetDeclaredSymbol(method, token) as IMethodSymbol,
			OperatorDeclarationSyntax @operator => model.GetDeclaredSymbol(@operator, token) as IMethodSymbol,
			ConversionOperatorDeclarationSyntax conversion => model.GetDeclaredSymbol(conversion, token) as IMethodSymbol,
			AccessorDeclarationSyntax accessor => model.GetDeclaredSymbol(accessor, token) as IMethodSymbol,
			DestructorDeclarationSyntax destructor => model.GetDeclaredSymbol(destructor, token) as IMethodSymbol,
			EventFieldDeclarationSyntax field when field.Declaration.Variables.Count > 0 =>
				(model.GetDeclaredSymbol(field.Declaration.Variables[0], token) as IEventSymbol)?.AddMethod,
			LocalFunctionStatementSyntax local => model.GetDeclaredSymbol(local, token) as IMethodSymbol,
			LambdaExpressionSyntax lambda => (model.GetOperation(lambda, token) as IAnonymousFunctionOperation)?.Symbol,
			_ => null,
		};

	private static bool HasCompilerError(SemanticModel model, SyntaxNode declaration,
		AttributeSyntax attribute, System.Threading.CancellationToken token)
	{
		var headerEnd = declaration switch
		{
			MethodDeclarationSyntax method => method.ConstraintClauses.LastOrDefault()?.Span.End ?? method.ParameterList.Span.End,
			OperatorDeclarationSyntax @operator => @operator.ParameterList.Span.End,
			ConversionOperatorDeclarationSyntax conversion => conversion.ParameterList.Span.End,
			AccessorDeclarationSyntax accessor => accessor.Keyword.Span.End,
			DestructorDeclarationSyntax destructor => destructor.ParameterList.Span.End,
			EventFieldDeclarationSyntax field when field.Declaration.Variables.Count > 0 =>
				field.Declaration.Variables[0].Identifier.Span.End,
			LocalFunctionStatementSyntax local => local.ConstraintClauses.LastOrDefault()?.Span.End ?? local.ParameterList.Span.End,
			LambdaExpressionSyntax lambda => lambda.Body.SpanStart,
			_ => attribute.Span.End,
		};
		var header = TextSpan.FromBounds(attribute.SpanStart, Math.Max(attribute.Span.End, headerEnd));
		if (HasMalformedHeaderError(model, header, token))
		{
			return true;
		}

		return declaration is AccessorDeclarationSyntax { Parent.Parent: BasePropertyDeclarationSyntax owner } &&
			owner.AccessorList is { } accessors &&
			HasMalformedHeaderError(model, TextSpan.FromBounds(owner.SpanStart, accessors.OpenBraceToken.SpanStart), token);
	}

	// Roslyn can locate body-flow errors on method names and getter keywords.
	private static bool HasMalformedHeaderError(SemanticModel model, TextSpan header,
		System.Threading.CancellationToken token)
		=> model.GetDiagnostics(header, token).Any(static diagnostic =>
			diagnostic.Severity == DiagnosticSeverity.Error && !IsBodyOwnedCompilerError(diagnostic));

	private static bool IsBodyOwnedCompilerError(Diagnostic diagnostic)
		=> diagnostic.Id is "CS0161" or "CS0177" or "CS1624";

	private static string? ReadMethod(SemanticModel model, AttributeSyntax attribute,
		System.Threading.CancellationToken token)
	{
		var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
		var constant = argument is null ? default : model.GetConstantValue(argument.Expression, token);
		return constant.HasValue && constant.Value is string method
			? SupportedMethods.FirstOrDefault(allowed => string.Equals(method, allowed, StringComparison.OrdinalIgnoreCase))
			: null;
	}

	private static string? ReadMethod(AttributeData attribute)
		=> attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string method
			? SupportedMethods.FirstOrDefault(allowed => string.Equals(method, allowed, StringComparison.OrdinalIgnoreCase)) : null;

	private static IEnumerable<HtmxorDiagnosticIssue> GetLocalIssues(Compilation compilation,
		INamedTypeSymbol component, IMethodSymbol handler, string? method, HtmxorRouteSymbols symbols)
	{
		if (method is null)
		{
			yield return new(HtmxorDiagnosticCause.InvalidExplicitVerb,
				"HtmxAction method must be POST, PUT, PATCH, DELETE or QUERY");
		}

		var attributes = component.GetAttributes();
		var route = attributes.FirstOrDefault(attribute => Is(attribute, symbols.HtmxRoute));
		if ((route is null && !attributes.Any(attribute => Is(attribute, symbols.Route))) ||
			!component.AllInterfaces.Any(type => SymbolEqualityComparer.Default.Equals(type, symbols.Component)))
		{
			yield return new(HtmxorDiagnosticCause.ExplicitUnroutedOwner,
				"HtmxAction must be declared on the component type that owns its route, not an inherited or unrouted owner");
		}

		if (attributes.Any(attribute => Is(attribute, symbols.DisableHtmxDirectRouting)))
		{
			yield return new(HtmxorDiagnosticCause.NormalOnlyAction,
				"HtmxAction is not supported on a component marked with DisableHtmxDirectRouting; remove the action or marker");
		}

		foreach (var issue in HtmxorActionHandler.GetExplicitUnsupportedIssues(compilation, handler))
		{
			yield return issue;
		}

		if (method is not null && GetAllowListIssue(route, method) is { } allowListIssue)
		{
			yield return allowListIssue;
		}
	}

	private static HtmxorDiagnosticIssue? GetAllowListIssue(AttributeData? route, string method)
	{
		var methods = route?.NamedArguments.FirstOrDefault(static argument => argument.Key == "Methods");
		return methods is { Key: "Methods" } argument &&
			!HtmxorRoutedComponent.HasBindingError(argument.Value) &&
			(argument.Value.IsNull || argument.Value.Kind != TypedConstantKind.Array || !argument.Value.Values.Any(value =>
				value.Value is string allowed && string.Equals(allowed, method, StringComparison.OrdinalIgnoreCase)))
			? new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.ExplicitMethodsConflict,
				"explicit HtmxRoute.Methods does not allow the " + method + " HtmxAction; add the method or remove the action")
			: null;
	}

	private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol scope)
		=> scope.GetTypeMembers().SelectMany(GetTypes)
			.Concat(scope.GetNamespaceMembers().SelectMany(GetTypes));

	private static IEnumerable<INamedTypeSymbol> GetTypes(INamedTypeSymbol type)
		=> new[] { type }.Concat(type.GetTypeMembers().SelectMany(GetTypes));

	private static bool IsFieldEventAccessor(IMethodSymbol method, System.Threading.CancellationToken token)
		=> method.AssociatedSymbol is IEventSymbol @event &&
			@event.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(token).Parent?.Parent is EventFieldDeclarationSyntax);

	private static void AnalyzeOwnerConflicts(CompilationAnalysisContext context, INamedTypeSymbol component,
		INamedTypeSymbol action, RazorCompiledEventBinding[] bindings, RazorCompiledForm[] forms)
	{
		var declarations = component.GetMembers().OfType<IMethodSymbol>()
			.Where(method => !IsFieldEventAccessor(method, context.CancellationToken))
			.SelectMany(method => method.GetAttributes().Where(attribute => Is(attribute, action))
				.Select(attribute => (Handler: method, Attribute: attribute))).ToArray();
		foreach (var declaration in declarations)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			if (declaration.Attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is not AttributeSyntax syntax)
			{
				continue;
			}
			var model = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
			if (syntax.Parent?.Parent is not { } target || HasCompilerError(model, target, syntax, context.CancellationToken))
			{
				continue;
			}
			var method = ReadMethod(declaration.Attribute);
			if (method is null)
			{
				continue;
			}
			foreach (var issue in GetConflictIssues(component, declaration.Handler, method, declarations, bindings, forms))
			{
				context.ReportDiagnostic(issue.ToDiagnostic(syntax.GetLocation()));
			}
		}
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetConflictIssues(INamedTypeSymbol component, IMethodSymbol handler,
		string method, (IMethodSymbol Handler, AttributeData Attribute)[] declarations,
		RazorCompiledEventBinding[] bindings, RazorCompiledForm[] forms)
	{
		if (declarations.Any(declaration => ReadMethod(declaration.Attribute) == method &&
			!SymbolEqualityComparer.Default.Equals(declaration.Handler, handler)) ||
			bindings.Any(binding => binding.HttpMethod == method && binding.Handler is not null &&
				SymbolEqualityComparer.Default.Equals(binding.Component, component) &&
				!SymbolEqualityComparer.Default.Equals(binding.Handler, handler)))
		{
			yield return new(HtmxorDiagnosticCause.ConflictingHandlers,
				method + " declarations on one route owner must name the same handler; select one handler");
		}

		if (method == "POST" && forms.Any(form => form.IsNamed && SymbolEqualityComparer.Default.Equals(form.Component, component)))
		{
			yield return new(HtmxorDiagnosticCause.NamedFormPostConflict,
				"named forms and a POST HtmxAction cannot share one route owner; choose named forms or the action");
		}
	}

	private static bool Is(AttributeData attribute, INamedTypeSymbol? type)
		=> type is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, type);
}
