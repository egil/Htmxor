using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

// Resolves an inferred binding's handler from the call Razor itself generated for it,
// EventCallback.Factory.Create<HtmxEventArgs>(this, M), with the compilation's semantic model. Scope, overloads, hiding,
// locals and `using static` imports are therefore exactly the compiler's. When that call does not compile, Razor reports
// the error itself and Htmxor adds nothing. Otherwise the bound handler must be one of void M(), void M(HtmxEventArgs),
// Task M() or Task M(HtmxEventArgs) on an instance method of the component or a base type.
internal static class HtmxorActionHandler
{
	private const string NotAComponentMember = "must be an instance method on the request-owned component";

	public static string? GetUnsupportedReason(
		Compilation compilation,
		INamedTypeSymbol component,
		HtmxorComponentActionDeclaration declaration)
	{
		var invocation = FindGeneratedCreateCall(component, declaration);
		if (invocation is null)
		{
			return null;
		}

		var model = compilation.GetSemanticModel(invocation.SyntaxTree);
		if (HasError(model, invocation))
		{
			return null;
		}

		var argument = HandlerArgument(invocation);

		var handlerName = declaration.HandlerName!;
		return model.GetSymbolInfo(argument).Symbol switch
		{
			IMethodSymbol { MethodKind: MethodKind.LocalFunction } => Reason(handlerName, NotAComponentMember),
			IMethodSymbol method => GetMethodReason(compilation, component, handlerName, method, model.GetMemberGroup(argument).Length),
			ILocalSymbol or IParameterSymbol or IRangeVariableSymbol => Reason(handlerName, NotAComponentMember),
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
			return Reason(handlerName, NotAComponentMember);
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

	// The Create<HtmxEventArgs> call Razor generated for this binding, in the component's Razor-generated declaration.
	// Razor maps the handler argument to the binding's position, which picks the call when one handler is bound more
	// than once; the nearest mapped argument after the binding attribute on its line is the binding's own.
	private static InvocationExpressionSyntax? FindGeneratedCreateCall(
		INamedTypeSymbol component,
		HtmxorComponentActionDeclaration declaration)
	{
		var candidates = component.DeclaringSyntaxReferences
			.Where(static reference => HtmxorRouteManifest.IsRazorGeneratedPath(reference.SyntaxTree.FilePath))
			.SelectMany(static reference => reference.GetSyntax().DescendantNodes().OfType<InvocationExpressionSyntax>())
			.Where(invocation => IsHtmxEventCallbackCreate(invocation) &&
				NamesHandler(HandlerArgument(invocation), declaration.HandlerAccess))
			.ToList();
		return candidates
			.Select(invocation => (Invocation: invocation, Position: MappedPositionAfterBinding(HandlerArgument(invocation), declaration)))
			.Where(static candidate => candidate.Position is not null)
			.OrderBy(static candidate => candidate.Position!.Value.Line)
			.ThenBy(static candidate => candidate.Position!.Value.Character)
			.Select(static candidate => candidate.Invocation)
			.FirstOrDefault() ?? candidates.FirstOrDefault();
	}

	// Compares structurally, so whitespace Razor keeps from the markup (`this . M`) still matches.
	private static bool NamesHandler(ExpressionSyntax argument, string? handlerAccess)
		=> argument switch
		{
			IdentifierNameSyntax name => handlerAccess == name.Identifier.ValueText,
			MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name }
				=> handlerAccess == "this." + name.Identifier.ValueText,
			_ => false,
		};

	// The handler expression without the parentheses Razor keeps from spellings such as "(M)".
	private static ExpressionSyntax HandlerArgument(InvocationExpressionSyntax invocation)
	{
		var argument = invocation.ArgumentList.Arguments[1].Expression;
		while (argument is ParenthesizedExpressionSyntax parenthesized)
		{
			argument = parenthesized.Expression;
		}

		return argument;
	}

	private static bool IsHtmxEventCallbackCreate(InvocationExpressionSyntax invocation)
		=> invocation.ArgumentList.Arguments.Count == 2 &&
			invocation.Expression is MemberAccessExpressionSyntax
			{
				Name: GenericNameSyntax { Identifier.ValueText: "Create" } create,
				Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Factory" },
			} &&
			create.TypeArgumentList.Arguments.Count == 1 &&
			create.TypeArgumentList.Arguments[0].ToString().EndsWith("HtmxEventArgs", StringComparison.Ordinal);

	// Where Razor maps the argument in the .razor file, when that is at or after the binding attribute (the value may
	// follow on a later line); otherwise null.
	private static LinePosition? MappedPositionAfterBinding(
		ExpressionSyntax argument,
		HtmxorComponentActionDeclaration declaration)
	{
		var mapped = argument.SyntaxTree.GetMappedLineSpan(argument.Span);
		var afterBinding = mapped.HasMappedPath &&
			string.Equals(mapped.Path, declaration.Path, StringComparison.OrdinalIgnoreCase) &&
			mapped.StartLinePosition >= declaration.LineSpan.Start;
		return afterBinding ? mapped.StartLinePosition : null;
	}

	private static bool HasError(SemanticModel model, SyntaxNode node)
		=> model.GetDiagnostics(node.Span).Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

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
