using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Htmxor.Generators;

// Resolves an inferred binding's handler the way Razor does: by binding EventCallback.Factory.Create<HtmxEventArgs>(this, M)
// inside the component, so overload resolution, hiding and `using static` imports are the compiler's own. When that binding
// fails, Razor reports the error itself and Htmxor adds nothing. Otherwise the bound handler must be one of void M(),
// void M(HtmxEventArgs), Task M() or Task M(HtmxEventArgs) on an instance method of the component or a base type.
internal static class HtmxorActionHandler
{
	public static string? GetUnsupportedReason(
		Compilation compilation,
		INamedTypeSymbol component,
		string handlerName,
		string handlerAccess)
	{
		var anchor = FindRazorAnchor(component);
		if (anchor is not null && handlerAccess == handlerName && DeclaresName(anchor, handlerName))
		{
			// Razor binds the name where the attribute sits, so a @foreach variable, lambda parameter or local with that name
			// is what it binds to, not a component member.
			return Reason(handlerName, "must be an instance method on the request-owned component");
		}

		var binding = anchor is null ? null : Bind(compilation, anchor, handlerAccess);
		if (binding is null)
		{
			return null;
		}

		var (symbol, group) = binding.Value;
		return symbol switch
		{
			IMethodSymbol method when !HasDelegateCompatibleParameters(compilation, method) => null,
			IMethodSymbol method => GetMethodReason(compilation, component, handlerName, method, group),
			null => null,
			_ => Reason(handlerName, "is not a method"),
		};
	}

	private static string? GetMethodReason(
		Compilation compilation,
		INamedTypeSymbol component,
		string handlerName,
		IMethodSymbol method,
		int groupSize)
	{
		if (method.IsStatic && !IsInComponentChain(method.ContainingType, component))
		{
			return Reason(handlerName, "must be an instance method on the request-owned component");
		}

		if (groupSize > 1)
		{
			return Reason(handlerName, "is overloaded; give the handler a unique name");
		}

		return GetShapeCause(compilation, method) is { } cause ? Reason(handlerName, cause) : null;
	}

	private static string? GetShapeCause(Compilation compilation, IMethodSymbol method)
	{
		if (method.IsStatic)
		{
			return "is static; make it an instance method";
		}

		if (method.IsGenericMethod)
		{
			return "is generic; make it non-generic";
		}

		if (method.IsAsync && method.ReturnsVoid)
		{
			return "is async void; return Task instead";
		}

		var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
		if (!method.ReturnsVoid && !SymbolEqualityComparer.Default.Equals(method.ReturnType, task))
		{
			return "returns a value; return void or Task";
		}

		return method.Parameters.Length == 0
			? null
			: GetParameterCause(compilation, method.Parameters[0]);
	}

	private static string? GetParameterCause(Compilation compilation, IParameterSymbol parameter)
	{
		if (parameter.IsOptional)
		{
			return "has an optional parameter; make it required";
		}

		var eventArgs = compilation.GetTypeByMetadataName("Htmxor.HtmxEventArgs");
		return SymbolEqualityComparer.Default.Equals(parameter.Type, eventArgs)
			? null
			: "has a parameter that is not HtmxEventArgs; its parameter must be HtmxEventArgs";
	}

	// Returns the symbol the handler argument binds to and the size of its method group, or null when the binding fails,
	// which Razor reports itself.
	private static (ISymbol? Symbol, int GroupSize)? Bind(
		Compilation compilation,
		MethodDeclarationSyntax method,
		string handlerAccess)
	{
		var invocation = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression(
			"global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Htmxor.HtmxEventArgs>(this, " +
			handlerAccess + ")");
		var speculative = Speculate(compilation, method, invocation);
		if (speculative is null)
		{
			return null;
		}

		var bound = (InvocationExpressionSyntax)speculative.SyntaxTree.GetRoot()
			.DescendantNodesAndSelf()
			.First(static node => node is InvocationExpressionSyntax);
		var argument = bound.ArgumentList.Arguments[1].Expression;
		return speculative.GetSymbolInfo(bound).Symbol is null || !speculative.GetConversion(argument).Exists
			? null
			: (speculative.GetSymbolInfo(argument).Symbol, speculative.GetMemberGroup(argument).Length);
	}

	// Binds the invocation inside an instance method of the component, where `this` and the component's members are in scope.
	private static SemanticModel? Speculate(
		Compilation compilation,
		MethodDeclarationSyntax method,
		InvocationExpressionSyntax invocation)
	{
		var model = compilation.GetSemanticModel(method.SyntaxTree);
		SemanticModel? speculative;
		var bound = method.Body is { } body
			? model.TryGetSpeculativeSemanticModel(
				body.SpanStart + 1,
				SyntaxFactory.ExpressionStatement(invocation),
				out speculative)
			: model.TryGetSpeculativeSemanticModel(
				method.ExpressionBody!.Expression.SpanStart,
				SyntaxFactory.ArrowExpressionClause(invocation),
				out speculative);
		return bound ? speculative : null;
	}

	// Razor binds the handler inside BuildRenderTree in its generated declaration, with that file's usings. Only direct
	// members of that declaration are considered, so a code-behind or a nested type never supplies the scope.
	private static MethodDeclarationSyntax? FindRazorAnchor(INamedTypeSymbol component)
	{
		var candidates = component.DeclaringSyntaxReferences
			.Where(static reference => HtmxorRouteManifest.IsRazorGeneratedPath(reference.SyntaxTree.FilePath))
			.Select(static reference => reference.GetSyntax())
			.OfType<TypeDeclarationSyntax>()
			.SelectMany(static declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
			.Where(static method =>
				!method.Modifiers.Any(SyntaxKind.StaticKeyword) &&
				(method.Body is not null || method.ExpressionBody is not null))
			.ToList();
		return candidates.FirstOrDefault(static method => method.Identifier.ValueText == "BuildRenderTree") ??
			candidates.FirstOrDefault();
	}

	private static bool DeclaresName(MethodDeclarationSyntax anchor, string name)
		=> anchor.DescendantNodes().Any(node => node switch
		{
			VariableDeclaratorSyntax variable => variable.Identifier.ValueText == name,
			ForEachStatementSyntax loop => loop.Identifier.ValueText == name,
			ParameterSyntax parameter => parameter.Identifier.ValueText == name,
			SingleVariableDesignationSyntax designation => designation.Identifier.ValueText == name,
			_ => false,
		});

	// Overload resolution can pick Create through a user-defined parameter conversion, but the method-group conversion
	// then fails (CS0123): a delegate parameter only converts by identity or implicit reference.
	private static bool HasDelegateCompatibleParameters(Compilation compilation, IMethodSymbol method)
	{
		var eventArgs = compilation.GetTypeByMetadataName("Htmxor.HtmxEventArgs");
		if (method.Parameters.Length == 0 || eventArgs is null)
		{
			return true;
		}

		var conversion = compilation.ClassifyCommonConversion(eventArgs, method.Parameters[0].Type);
		return conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference);
	}

	private static bool IsInComponentChain(INamedTypeSymbol? type, INamedTypeSymbol component)
	{
		for (var current = component; current is not null; current = current.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, type?.OriginalDefinition))
			{
				return true;
			}
		}

		return false;
	}

	private static string Reason(string handlerName, string cause)
		=> "handler '" + handlerName + "' " + cause;
}
