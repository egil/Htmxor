using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

// Resolves an inferred binding's handler from compiled symbols. The accepted shapes are void M(), void M(HtmxEventArgs),
// Task M() and Task M(HtmxEventArgs) on an accessible instance method. A shape the Razor compiler already rejects at the
// binding (no bindable overload, or more than one) gets no reason here, so Htmxor never duplicates Razor's error.
internal static class HtmxorActionHandler
{
	public static string? GetUnsupportedReason(Compilation compilation, INamedTypeSymbol component, string handlerName)
	{
		var accessible = FindMembers(component, handlerName)
			.Where(member => compilation.IsSymbolAccessibleWithin(member, component))
			.ToList();
		if (accessible.Count == 0)
		{
			// Razor reports a missing or inaccessible member itself, unless a static method imported with
			// `using static` is what the binding resolves to.
			return HasImportableStaticMethod(compilation, component, handlerName)
				? Reason(handlerName, "must be an instance method on the request-owned component")
				: null;
		}

		return accessible.All(static member => member is IMethodSymbol)
			? GetMethodReason(compilation, handlerName, accessible.Cast<IMethodSymbol>().ToList())
			: Reason(handlerName, "is not a method");
	}

	private static string? GetMethodReason(Compilation compilation, string handlerName, IReadOnlyList<IMethodSymbol> declared)
	{
		var eventArgs = compilation.GetTypeByMetadataName("Htmxor.HtmxEventArgs");
		var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
		var methods = WithoutOverridden(declared);
		var bindable = methods.Where(method => IsRazorBindable(compilation, method, eventArgs, task)).ToList();
		if (bindable.Count != 1)
		{
			return null;
		}

		return methods.Count > 1
			? Reason(handlerName, "is overloaded; give the handler a unique name")
			: GetShapeCause(bindable[0], eventArgs, task) is { } cause ? Reason(handlerName, cause) : null;
	}

	private static string? GetShapeCause(IMethodSymbol method, INamedTypeSymbol? eventArgs, INamedTypeSymbol? task)
	{
		if (method.IsStatic)
		{
			return "is static; make it an instance method";
		}

		if (method.IsAsync && method.ReturnsVoid)
		{
			return "is async void; return Task instead";
		}

		if (!method.ReturnsVoid && !SymbolEqualityComparer.Default.Equals(method.ReturnType, task))
		{
			return "returns a value; return void or Task";
		}

		return method.Parameters.Length == 0
			? null
			: GetParameterCause(method.Parameters[0], eventArgs);
	}

	private static string? GetParameterCause(IParameterSymbol parameter, INamedTypeSymbol? eventArgs)
	{
		if (parameter.IsOptional)
		{
			return "has an optional parameter; make it required";
		}

		return SymbolEqualityComparer.Default.Equals(parameter.Type, eventArgs)
			? null
			: "has a parameter that is not HtmxEventArgs; its parameter must be HtmxEventArgs";
	}

	// The shapes EventCallbackFactory.Create<HtmxEventArgs> accepts as a method group, which is what Razor compiles.
	private static bool IsRazorBindable(
		Compilation compilation,
		IMethodSymbol method,
		INamedTypeSymbol? eventArgs,
		INamedTypeSymbol? task)
		=> !method.IsGenericMethod &&
			ReturnsBindable(compilation, method, task) &&
			AcceptsBindable(compilation, method.Parameters, eventArgs);

	private static bool ReturnsBindable(Compilation compilation, IMethodSymbol method, INamedTypeSymbol? task)
		=> method.ReturnsVoid ||
			(task is not null && method.ReturnType.IsReferenceType && compilation.HasImplicitConversion(method.ReturnType, task));

	private static bool AcceptsBindable(
		Compilation compilation,
		ImmutableArray<IParameterSymbol> parameters,
		INamedTypeSymbol? eventArgs)
		=> parameters.Length == 0 ||
			(parameters.Length == 1 &&
				parameters[0].RefKind == RefKind.None &&
				parameters[0].Type.IsReferenceType &&
				eventArgs is not null &&
				compilation.HasImplicitConversion(eventArgs, parameters[0].Type));

	private static bool HasImportableStaticMethod(Compilation compilation, INamedTypeSymbol component, string handlerName)
		=> compilation.GetSymbolsWithName(handlerName, SymbolFilter.Member)
			.OfType<IMethodSymbol>()
			.Any(method => method.IsStatic && compilation.IsSymbolAccessibleWithin(method, component));

	private static List<ISymbol> FindMembers(INamedTypeSymbol component, string handlerName)
	{
		var members = new List<ISymbol>();
		for (var current = component; current is not null; current = current.BaseType)
		{
			members.AddRange(current.GetMembers(handlerName));
		}

		return members;
	}

	private static List<IMethodSymbol> WithoutOverridden(IReadOnlyList<IMethodSymbol> methods)
	{
		var overridden = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
		foreach (var method in methods)
		{
			for (var current = method.OverriddenMethod; current is not null; current = current.OverriddenMethod)
			{
				overridden.Add(current);
			}
		}

		return methods.Where(method => !overridden.Contains(method)).ToList();
	}

	private static string Reason(string handlerName, string cause)
		=> "handler '" + handlerName + "' " + cause;
}
