using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Htmxor.Generators;

internal static class HtmxorCompiledAction
{
	public static bool Matches(
		Compilation compilation,
		RazorCompiledEventBinding binding,
		INamedTypeSymbol actionType,
		CancellationToken cancellationToken)
	{
		if (!IsComponentHandler(binding))
		{
			return false;
		}

		var identity = binding.Component.ToDisplayString() + "." + binding.HttpMethod + "." + binding.Handler!.Name;
		return binding.Component.GetMembers().OfType<IPropertySymbol>()
			.Where(property => SymbolEqualityComparer.Default.Equals(property.Type, actionType))
			.SelectMany(static property => property.DeclaringSyntaxReferences)
			.Where(static reference => IsGeneratedActionPath(reference.SyntaxTree.FilePath))
			.Any(reference => MatchesAction(compilation, reference, binding, identity, cancellationToken));
	}

	private static bool MatchesAction(
		Compilation compilation,
		SyntaxReference reference,
		RazorCompiledEventBinding binding,
		string identity,
		CancellationToken cancellationToken)
	{
		if (reference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax property ||
			property is not
			{
				Initializer.Value: BaseObjectCreationExpressionSyntax { ArgumentList: { Arguments.Count: >= 3 } arguments },
			} || arguments.Arguments[0].Expression is not TypeOfExpressionSyntax owner)
		{
			return false;
		}

		var model = compilation.GetSemanticModel(reference.SyntaxTree);
		return SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(owner.Type, cancellationToken).Type, binding.Component) &&
			Equals(model.GetConstantValue(arguments.Arguments[1].Expression, cancellationToken).Value, binding.HttpMethod) &&
			Equals(model.GetConstantValue(arguments.Arguments[2].Expression, cancellationToken).Value, identity) &&
			MatchesDispatch(model, property, binding.Handler!, cancellationToken);
	}

	private static bool MatchesDispatch(
		SemanticModel model,
		PropertyDeclarationSyntax property,
		IMethodSymbol handler,
		CancellationToken cancellationToken)
	{
		var propertySymbol = model.GetDeclaredSymbol(property, cancellationToken);
		return property.Ancestors().OfType<ClassDeclarationSyntax>().First().Members
			.OfType<MethodDeclarationSyntax>()
			.Where(static method => method.Identifier.ValueText == "SetParametersAsync")
			.SelectMany(static method => method.DescendantNodes().OfType<IfStatementSyntax>())
			.Where(statement => ConsumesAction(model, statement, propertySymbol, cancellationToken))
			.SelectMany(static statement => statement.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
			.Any(invocation => IsHandlerCall(model, invocation, handler, cancellationToken));
	}

	private static bool ConsumesAction(
		SemanticModel model,
		IfStatementSyntax statement,
		ISymbol? property,
		CancellationToken cancellationToken)
		=> statement.Condition is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation &&
			model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol { Name: "TryConsume" } &&
			SymbolEqualityComparer.Default.Equals(
				model.GetSymbolInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Symbol,
				property);

	private static bool IsHandlerCall(
		SemanticModel model,
		InvocationExpressionSyntax invocation,
		IMethodSymbol handler,
		CancellationToken cancellationToken)
		=> invocation.ArgumentList.Arguments.Count == 2 &&
			model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol
			{
				Name: "Create",
				ContainingType.Name: "EventCallbackFactory",
			} &&
			SymbolEqualityComparer.Default.Equals(
				model.GetSymbolInfo(invocation.ArgumentList.Arguments[1].Expression, cancellationToken).Symbol,
				handler);

	private static bool IsGeneratedActionPath(string path)
		=> path.Replace('\\', '/').EndsWith(
			"Htmxor.Generators.HtmxorActionGenerator/HtmxorGeneratedActions.g.cs",
			StringComparison.Ordinal);

	private static bool IsComponentHandler(RazorCompiledEventBinding binding)
	{
		if (binding.Handler is not { IsStatic: false, IsGenericMethod: false, MethodKind: MethodKind.Ordinary } handler)
		{
			return false;
		}

		for (var type = binding.Component; type is not null; type = type.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, handler.ContainingType.OriginalDefinition))
			{
				return true;
			}
		}

		return false;
	}
}
