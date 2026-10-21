using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Htmxor.Generators;

internal sealed class RazorCompiledEventBinding
{
	private RazorCompiledEventBinding(
		SemanticModel semanticModel,
		INamedTypeSymbol component,
		string attributeName,
		IMethodSymbol? handler,
		ExpressionSyntax expression,
		bool isBound)
	{
		SemanticModel = semanticModel;
		Component = component;
		AttributeName = attributeName;
		Handler = handler;
		Expression = expression;
		IsBound = isBound;
	}

	public SemanticModel SemanticModel { get; }

	public INamedTypeSymbol Component { get; }

	public string AttributeName { get; }

	public string HttpMethod => AttributeName.Substring(2).ToUpperInvariant();

	public IMethodSymbol? Handler { get; }

	public ExpressionSyntax Expression { get; }

	public bool IsBound { get; }

	public Location Location => Expression.GetLocation();

	public static IEnumerable<RazorCompiledEventBinding> Find(
		Compilation compilation,
		CancellationToken cancellationToken,
		bool includeUnbound = false)
	{
		foreach (var tree in compilation.SyntaxTrees.Where(static tree => HtmxorRouteManifest.IsRazorGeneratedPath(tree.FilePath)))
		{
			var model = compilation.GetSemanticModel(tree);
			foreach (var invocation in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (TryRead(model, invocation, cancellationToken, includeUnbound) is { } binding)
				{
					yield return binding;
				}
			}
		}
	}

	public static RazorCompiledEventBinding? TryRead(
		SemanticModel model,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken,
		bool includeUnbound = false)
	{
		if (!IsHtmxCallback(model, invocation, cancellationToken, includeUnbound, out var isBound))
		{
			return null;
		}

		// Razor hides its callback scaffolding while mapping the handler back to markup.
		// This also covers member templates without claiming handwritten C# builder calls.
		if (invocation.SyntaxTree.GetLineVisibility(invocation.SpanStart, cancellationToken) != LineVisibility.Hidden ||
			FindComponent(model, invocation, cancellationToken) is not { } component)
		{
			return null;
		}

		var attributeName = FindAttributeName(model, invocation, cancellationToken);
		var handler = invocation.ArgumentList.Arguments[1].Expression;
		if (!IsHtmxorEvent(attributeName))
		{
			return null;
		}

		isBound = isBound && !model.GetDiagnostics(invocation.Span, cancellationToken)
			.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		if (!isBound && !includeUnbound)
		{
			return null;
		}

		return new RazorCompiledEventBinding(
			model,
			component,
			attributeName!,
			isBound ? ReadHandler(model, component, handler, cancellationToken) : null,
			handler,
			isBound);
	}

	private static INamedTypeSymbol? FindComponent(
		SemanticModel model,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken)
	{
		foreach (var declaration in invocation.Ancestors().OfType<ClassDeclarationSyntax>())
		{
			if (model.GetDeclaredSymbol(declaration, cancellationToken) is INamedTypeSymbol component &&
				HtmxorRouteManifest.HasCompiledRazorDeclaration(component))
			{
				return component;
			}
		}

		return null;
	}

	private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
	{
		while (expression is ParenthesizedExpressionSyntax parenthesized)
		{
			expression = parenthesized.Expression;
		}

		return expression;
	}

	private static IMethodSymbol? ReadHandler(
		SemanticModel model,
		INamedTypeSymbol component,
		ExpressionSyntax expression,
		CancellationToken cancellationToken)
	{
		var handler = Unwrap(expression);
		if (!SymbolEqualityComparer.Default.Equals(
			model.GetEnclosingSymbol(handler.SpanStart, cancellationToken)?.ContainingType,
			component))
		{
			return null;
		}

		return handler is IdentifierNameSyntax or
			MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax }
			? model.GetSymbolInfo(handler, cancellationToken).Symbol as IMethodSymbol
			: null;
	}

	private static bool IsHtmxCallback(
		SemanticModel model,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken,
		bool includeUnbound,
		out bool isBound)
	{
		isBound = false;
		if (invocation.ArgumentList.Arguments.Count != 2 ||
			invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Create" })
		{
			return false;
		}
		var symbol = model.GetSymbolInfo(invocation, cancellationToken);
		isBound = IsCallbackFactory(symbol.Symbol);
		return isBound || includeUnbound && symbol.CandidateReason == CandidateReason.OverloadResolutionFailure &&
			!symbol.CandidateSymbols.IsEmpty && symbol.CandidateSymbols.All(IsCallbackFactory);
	}

	private static bool IsCallbackFactory(ISymbol? symbol)
		=> symbol is IMethodSymbol method &&
			method.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Components.EventCallbackFactory" &&
			method.TypeArguments.Length == 1 &&
			method.TypeArguments[0].ToDisplayString() == "Htmxor.HtmxEventArgs";

	private static string? FindAttributeName(
		SemanticModel model,
		InvocationExpressionSyntax callback,
		CancellationToken cancellationToken)
	{
		foreach (var invocation in callback.Ancestors().OfType<InvocationExpressionSyntax>())
		{
			if (ReadBuilderAttributeName(model, invocation, cancellationToken) is { } attributeName)
			{
				return attributeName;
			}

			if (model.GetOperation(invocation, cancellationToken) is IInvocationOperation call &&
				call.Arguments.FirstOrDefault(argument => argument.Syntax.Span.Contains(callback.Span)) is { Parameter: { } parameter })
			{
				var forwardedName = FindForwardedAttributeName(model.Compilation, call.TargetMethod, parameter, cancellationToken);
				if (forwardedName is not null)
				{
					return forwardedName;
				}
			}
		}

		return null;
	}

	// Generic component inference forwards the callback through a generated helper parameter.
	// Match that parameter semantically to the builder attribute, rather than treating argument order as a contract.
	private static string? FindForwardedAttributeName(
		Compilation compilation,
		IMethodSymbol method,
		IParameterSymbol parameter,
		CancellationToken cancellationToken)
	{
		foreach (var reference in method.OriginalDefinition.DeclaringSyntaxReferences
			.Where(static reference => HtmxorRouteManifest.IsRazorGeneratedPath(reference.SyntaxTree.FilePath)))
		{
			var model = compilation.GetSemanticModel(reference.SyntaxTree);
			foreach (var invocation in reference.GetSyntax(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				var attributeName = ReadBuilderAttributeName(model, invocation, cancellationToken);
				if (attributeName is not null &&
					SymbolEqualityComparer.Default.Equals(
						model.GetSymbolInfo(invocation.ArgumentList.Arguments[2].Expression, cancellationToken).Symbol,
						parameter.OriginalDefinition))
				{
					return attributeName;
				}
			}
		}

		return null;
	}

	private static string? ReadBuilderAttributeName(
		SemanticModel model,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken)
	{
		if (invocation.ArgumentList.Arguments.Count < 3 ||
			model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method ||
			method.Name is not ("AddAttribute" or "AddComponentParameter") ||
			method.ContainingType.ToDisplayString() != "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder")
		{
			return null;
		}

		return model.GetConstantValue(invocation.ArgumentList.Arguments[1].Expression, cancellationToken).Value as string;
	}

	private static bool IsHtmxorEvent(string? name)
		=> name is "onpost" or "onput" or "onpatch" or "ondelete" or "onquery" or "onget";
}
