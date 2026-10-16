using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
		var ownerHeader = declaration is AccessorDeclarationSyntax { Parent.Parent: BasePropertyDeclarationSyntax owner } &&
			owner.AccessorList is { } accessors
			? TextSpan.FromBounds(owner.SpanStart, accessors.OpenBraceToken.SpanStart)
			: (TextSpan?)null;
		var errors = GetHeaderErrors(model, header, ownerHeader, token);
		if (errors.Length == 0)
		{
			return false;
		}

		var replacement = ReplaceBody(declaration);
		if (replacement is null)
		{
			return true;
		}

		token.ThrowIfCancellationRequested();
		var tree = declaration.SyntaxTree;
		var root = tree.GetRoot(token);
		var newRoot = root.ReplaceNode(declaration, replacement);
		var newTree = tree.WithRootAndOptions(newRoot, tree.Options);
		var newModel = model.Compilation.ReplaceSyntaxTree(tree, newTree).GetSemanticModel(newTree);
		// Body-flow errors can point at header tokens; only errors that survive a terminating body are declaration-owned.
		var remaining = GetHeaderErrors(newModel, header, ownerHeader, token);
		return errors.Any(original => remaining.Any(current =>
			current.Id == original.Id && current.Location.SourceSpan == original.Location.SourceSpan));
	}

	private static Diagnostic[] GetHeaderErrors(SemanticModel model, TextSpan header, TextSpan? ownerHeader,
		System.Threading.CancellationToken token)
		=> (ownerHeader is { } owner
			? model.GetDiagnostics(header, token).Concat(model.GetDiagnostics(owner, token))
			: model.GetDiagnostics(header, token))
			.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

	private static SyntaxNode? ReplaceBody(SyntaxNode declaration)
	{
		var throwExpression = SyntaxFactory.ThrowExpression(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
		var throwBlock = SyntaxFactory.Block(SyntaxFactory.ThrowStatement(
			SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
		return declaration switch
		{
			MethodDeclarationSyntax { Body: not null } method => method.WithBody(throwBlock),
			MethodDeclarationSyntax { ExpressionBody: not null } method =>
				method.WithExpressionBody(method.ExpressionBody.WithExpression(throwExpression)),
			OperatorDeclarationSyntax { Body: not null } @operator => @operator.WithBody(throwBlock),
			OperatorDeclarationSyntax { ExpressionBody: not null } @operator =>
				@operator.WithExpressionBody(@operator.ExpressionBody.WithExpression(throwExpression)),
			ConversionOperatorDeclarationSyntax { Body: not null } conversion => conversion.WithBody(throwBlock),
			ConversionOperatorDeclarationSyntax { ExpressionBody: not null } conversion =>
				conversion.WithExpressionBody(conversion.ExpressionBody.WithExpression(throwExpression)),
			AccessorDeclarationSyntax { Body: not null } accessor => accessor.WithBody(throwBlock),
			AccessorDeclarationSyntax { ExpressionBody: not null } accessor =>
				accessor.WithExpressionBody(accessor.ExpressionBody.WithExpression(throwExpression)),
			DestructorDeclarationSyntax { Body: not null } destructor => destructor.WithBody(throwBlock),
			DestructorDeclarationSyntax { ExpressionBody: not null } destructor =>
				destructor.WithExpressionBody(destructor.ExpressionBody.WithExpression(throwExpression)),
			LocalFunctionStatementSyntax { Body: not null } local => local.WithBody(throwBlock),
			LocalFunctionStatementSyntax { ExpressionBody: not null } local =>
				local.WithExpressionBody(local.ExpressionBody.WithExpression(throwExpression)),
			LambdaExpressionSyntax { Body: BlockSyntax } lambda => lambda.WithBody(throwBlock),
			LambdaExpressionSyntax lambda => lambda.WithBody(throwExpression),
			_ => null,
		};
	}

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
