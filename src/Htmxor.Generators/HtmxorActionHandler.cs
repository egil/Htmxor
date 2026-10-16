using System;
using System.Collections.Generic;
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

	public static IEnumerable<HtmxorDiagnosticIssue> GetExplicitUnsupportedIssues(Compilation compilation, IMethodSymbol method)
	{
		if (method.MethodKind != MethodKind.Ordinary)
		{
			yield return Issue(HtmxorDiagnosticCause.NonordinaryExplicitAction, method.Name,
				"is not an ordinary callback method; declare an ordinary instance method returning void or Task");
			yield break;
		}

		if (method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>().Count() > 1)
		{
			yield return Issue(HtmxorDiagnosticCause.ExplicitOverload, method.Name,
				"is overloaded; give the handler a unique name");
		}

		foreach (var issue in GetShapeIssues(compilation, method, inferred: false))
		{
			yield return issue;
		}
	}

	public static HtmxorDiagnosticIssue? GetUnsupportedIssue(
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
			IMethodSymbol { MethodKind: MethodKind.LocalFunction } => Issue(HtmxorDiagnosticCause.NonmemberHandler, handlerName, NotAComponentMember),
			IMethodSymbol method => GetMethodReason(compilation, component, handlerName, method, model.GetMemberGroup(argument).Length),
			IParameterSymbol parameter when IsEventCallback(parameter.Type) => Issue(HtmxorDiagnosticCause.InferredEventCallbackParameter, handlerName, "is an EventCallback parameter, not a method; use [HtmxAction] on a component instance method"),
			IPropertySymbol property when IsEventCallback(property.Type) && IsComponentParameter(property) => Issue(HtmxorDiagnosticCause.InferredEventCallbackParameter, handlerName, "is an EventCallback parameter, not a method; use [HtmxAction] on a component instance method"),
			IFieldSymbol field when IsEventCallback(field.Type) => Issue(HtmxorDiagnosticCause.InferredEventCallbackField, handlerName, "is an EventCallback field, not a method; use [HtmxAction] on a component instance method"),
			ILocalSymbol or IParameterSymbol or IRangeVariableSymbol => Issue(HtmxorDiagnosticCause.NonmemberHandler, handlerName, NotAComponentMember + "; use [HtmxAction]"),
			null => null,
			_ => Issue(HtmxorDiagnosticCause.NonmethodHandler, handlerName, "is not a method; use [HtmxAction] on a component instance method"),
		};
	}

	private static bool IsComponentParameter(IPropertySymbol property)
	{
		foreach (var attribute in property.GetAttributes())
		{
			for (var type = attribute.AttributeClass; type is not null; type = type.BaseType)
			{
				if (type.ToDisplayString() is "Microsoft.AspNetCore.Components.ParameterAttribute" or
					"Microsoft.AspNetCore.Components.CascadingParameterAttributeBase")
				{
					return true;
				}
			}
		}

		return false;
	}

	private static HtmxorDiagnosticIssue? GetMethodReason(
		Compilation compilation,
		INamedTypeSymbol component,
		string handlerName,
		IMethodSymbol method,
		int groupSize)
	{
		// Covers `using static` imports and both extension forms, whose containing type is not the component's.
		if (!IsInComponentChain(method.ContainingType, component))
		{
			return Issue(HtmxorDiagnosticCause.NonmemberHandler, handlerName, NotAComponentMember + "; use [HtmxAction]");
		}

		if (groupSize > 1)
		{
			return Issue(HtmxorDiagnosticCause.InferredOverload, handlerName,
				"is overloaded; give the handler a unique instance method name and declare [HtmxAction]");
		}

		return GetShapeIssues(compilation, method, inferred: true)
			.Select(static issue => (HtmxorDiagnosticIssue?)issue).FirstOrDefault();
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetShapeIssues(
		Compilation compilation, IMethodSymbol method, bool inferred)
	{
		if (method.IsStatic)
		{
			yield return Issue(HtmxorDiagnosticCause.StaticHandler, method.Name,
				"is static; make it an instance method" + (inferred ? " and declare [HtmxAction]" : string.Empty));
		}

		if (method.IsGenericMethod)
		{
			yield return Issue(inferred ? HtmxorDiagnosticCause.InferredGeneric : HtmxorDiagnosticCause.ExplicitGeneric,
				method.Name, "is generic; make it non-generic" + (inferred ? " and declare [HtmxAction]" : string.Empty));
		}

		foreach (var issue in GetReturnIssues(compilation, method))
		{
			yield return issue;
		}

		foreach (var issue in GetParameterIssues(compilation, method))
		{
			yield return issue;
		}
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetReturnIssues(Compilation compilation, IMethodSymbol method)
	{
		if (method.IsAsync && method.ReturnsVoid)
		{
			yield return Issue(HtmxorDiagnosticCause.AsyncVoid, method.Name, "is async void; return Task instead");
		}

		var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
		if (!method.ReturnsVoid && !SymbolEqualityComparer.Default.Equals(method.ReturnType, task))
		{
			yield return Issue(GetReturnCause(method.ReturnType), method.Name, "returns a value; return void or Task");
		}
	}

	private static HtmxorDiagnosticCause GetReturnCause(ITypeSymbol returnType)
	{
		if (returnType is not INamedTypeSymbol named)
		{
			return HtmxorDiagnosticCause.OtherValueReturn;
		}

		return named.ConstructedFrom.ToDisplayString() switch
		{
			"System.Threading.Tasks.ValueTask" or "System.Threading.Tasks.ValueTask<TResult>" => HtmxorDiagnosticCause.ValueTaskReturn,
			"System.Threading.Tasks.Task<TResult>" => HtmxorDiagnosticCause.TaskOfTReturn,
			_ => HtmxorDiagnosticCause.OtherValueReturn,
		};
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetParameterIssues(Compilation compilation, IMethodSymbol method)
	{
		if (method.Parameters.Length > 1)
		{
			yield return Issue(HtmxorDiagnosticCause.OtherUnsupportedParameter, method.Name,
				"has unsupported parameters; use zero parameters or one required HtmxEventArgs parameter");
			yield break;
		}
		if (method.Parameters.Length == 0)
		{
			yield break;
		}

		var parameter = method.Parameters[0];
		if (parameter.RefKind != RefKind.None)
		{
			yield return Issue(HtmxorDiagnosticCause.RefOrOutParameter, method.Name,
				"has unsupported parameters; use one by-value HtmxEventArgs parameter");
		}
		if (parameter.IsOptional)
		{
			yield return Issue(HtmxorDiagnosticCause.OptionalParameter, method.Name, "has an optional parameter; make it required");
		}

		var eventArgs = compilation.GetTypeByMetadataName("Htmxor.HtmxEventArgs");
		if (!SymbolEqualityComparer.Default.Equals(parameter.Type, eventArgs))
		{
			var cause = parameter.Type.ToDisplayString() switch
			{
				"System.EventArgs" => HtmxorDiagnosticCause.EventArgsParameter,
				"object" => HtmxorDiagnosticCause.ObjectParameter,
				_ => HtmxorDiagnosticCause.OtherUnsupportedParameter,
			};
			yield return Issue(cause, method.Name,
				"has a parameter that is not HtmxEventArgs; its parameter must be HtmxEventArgs");
		}
	}

	// The Create<HtmxEventArgs> call Razor generated for this binding, in the component's Razor-generated declaration.
	// Razor maps the handler argument to the binding's position, so when one handler is bound more than once the nearest
	// mapped argument at or after the binding attribute, on its line or a later one, is the binding's own. A tree without
	// #line mapping falls back to the first matching call.
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

	private static bool IsEventCallback(ITypeSymbol type)
		=> type is INamedTypeSymbol named &&
			named.ConstructedFrom.ToDisplayString() is
				"Microsoft.AspNetCore.Components.EventCallback" or
				"Microsoft.AspNetCore.Components.EventCallback<TValue>";

	private static HtmxorDiagnosticIssue Issue(HtmxorDiagnosticCause cause, string handlerName, string reason)
		=> new(cause, "handler '" + handlerName + "' " + reason);
}
