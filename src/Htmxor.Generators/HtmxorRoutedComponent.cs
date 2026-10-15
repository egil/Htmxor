using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Htmxor;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

internal sealed class HtmxorRoutedComponent
{
	private HtmxorRoutedComponent(
		INamedTypeSymbol type,
		AttributeData route)
	{
		Type = type;
		Route = route;
	}

	private INamedTypeSymbol Type { get; }

	// HtmxRouteAttribute disallows multiple use, so the C# compiler reports a second one as CS0579.
	private AttributeData Route { get; }

	public static HtmxorRoutedComponent? Find(INamedTypeSymbol type, HtmxorRouteSymbols symbols)
	{
		var route = GetExactAttributes(type, symbols.HtmxRoute).FirstOrDefault();
		return route is null ? null : new HtmxorRoutedComponent(type, route);
	}

	public IEnumerable<HtmxorDiagnosticIssue> GetLocalIssues(
		HtmxorRouteSymbols symbols,
		CancellationToken cancellationToken)
	{
		foreach (var reason in GetLocalOriginReasons(cancellationToken))
		{
			yield return new(HtmxorDiagnosticCause.RouteTargetOrOrigin, reason);
		}

		var componentReason = ValidateComponent(symbols);
		if (componentReason is not null)
		{
			yield return new(HtmxorDiagnosticCause.RouteTargetOrOrigin, componentReason);
		}

		foreach (var reason in GetRouteReasons())
		{
			yield return new(HtmxorDiagnosticCause.MalformedRouteMember, reason);
		}

		var normalRouteReason = ValidateNormalRoute(symbols);
		if (normalRouteReason is not null)
		{
			yield return new(HtmxorDiagnosticCause.RouteTargetOrOrigin, normalRouteReason);
		}
	}

	public Location GetLocation(CancellationToken cancellationToken)
		=> Route.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ??
			Type.Locations.FirstOrDefault() ??
			Location.None;

	public HtmxorDiagnosticIssue? GetManifestIssue(ILookup<string, string?> razorManifest)
		=> Type.ContainingType is null &&
			Route.ApplicationSyntaxReference is { } reference &&
			HtmxorRouteManifest.IsRazorGeneratedPath(reference.SyntaxTree.FilePath) &&
			!HtmxorRouteManifest.HasCompiledRazorDeclaration(Type, razorManifest[GetMetadataName()])
				? new(HtmxorDiagnosticCause.RouteTargetOrOrigin,
					"the HtmxRoute component must compile from the matching Razor component; " +
					"its type name is derived from the file's folder and @namespace")
				: null;

	private IEnumerable<string> GetLocalOriginReasons(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (Type.ContainingType is not null)
		{
			yield return "the HtmxRoute component must be a top-level class";
		}
		else if (!IsRazorRoute())
		{
			var csharpComponent = GetCSharpComponent();
			if (csharpComponent is null)
			{
				yield return "the HtmxRoute component must be a top-level class";
			}
			else if (ValidateCSharpRouteOrigin(csharpComponent) is { } reason)
			{
				yield return reason;
			}
		}

		if (ValidateRouteOrigin(cancellationToken) is { } originReason)
		{
			yield return originReason;
		}
	}

	private bool IsRazorRoute()
		=> Route.ApplicationSyntaxReference is { } reference &&
			HtmxorRouteManifest.IsRazorGeneratedPath(reference.SyntaxTree.FilePath);

	private CSharpRoutedComponent? GetCSharpComponent()
	{
		if (Type.ContainingType is not null)
		{
			return null;
		}

		var path = Route.ApplicationSyntaxReference?.SyntaxTree.FilePath;
		if (string.IsNullOrEmpty(path) || HtmxorRouteManifest.IsRazorGeneratedPath(path!))
		{
			return null;
		}

		return new CSharpRoutedComponent(
			GetMetadataName(),
			path!,
			Route.NamedArguments.Any(static argument =>
				string.Equals(argument.Key, "Methods", StringComparison.Ordinal)));
	}

	private string? ValidateCSharpRouteOrigin(CSharpRoutedComponent component)
	{
		if (!HtmxorRouteManifest.HasCompiledRazorDeclaration(Type))
		{
			return null;
		}

		return HtmxorRouteManifest.IsMatchingRazorCodeBehind(Type, component.Path)
			? null
			: "a C# HtmxRoute declaration on a Razor component must use the matching .razor.cs partial";
	}

	private string? ValidateComponent(HtmxorRouteSymbols symbols)
	{
		var isConcreteClass = Type.TypeKind == TypeKind.Class &&
			!Type.IsAbstract &&
			!Type.IsStatic &&
			Type.Arity == 0;
		var implementsComponent = symbols.Component is not null && Type.AllInterfaces.Any(
			implemented => SymbolEqualityComparer.Default.Equals(implemented, symbols.Component));

		return isConcreteClass && implementsComponent
			? null
			: "the HtmxRoute target must be a concrete, non-generic Blazor component";
	}

	private IEnumerable<string> GetRouteReasons()
	{
		if (!IsRazorRoute() && !Route.NamedArguments.Any(static argument =>
			string.Equals(argument.Key, "Methods", StringComparison.Ordinal)))
		{
			yield return "a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods";
		}

		if (Route.ConstructorArguments.Length == 1 &&
			!HasBindingError(Route.ConstructorArguments[0]) &&
			ValidateTemplate(Route) is { } templateReason)
		{
			yield return templateReason;
		}

		foreach (var argument in Route.NamedArguments)
		{
			// Binding failures belong to the compiler; unrelated bound members remain independently diagnosable.
			if (!HasBindingError(argument.Value) && ValidateRouteNamedArgument(argument) is { } reason)
			{
				yield return reason;
			}
		}
	}

	private static bool HasBindingError(TypedConstant value)
		=> value.Kind == TypedConstantKind.Error ||
			(value.Kind == TypedConstantKind.Array && !value.IsNull && value.Values.Any(HasBindingError));

	private static string? ValidateTemplate(AttributeData route)
	{
		if (route.ConstructorArguments.Length != 1 ||
			route.ConstructorArguments[0].Value is not string template ||
			string.IsNullOrWhiteSpace(template))
		{
			return "HtmxRoute must resolve one nonblank constant route template";
		}

		// Stock ASP0017 reports template grammar errors and startup resolves constraints; neither checks the leading '/'.
		return HtmxorRouteTemplateContract.HasLeadingSlash(template)
			? null
			: "HtmxRoute template '" + template + "' is invalid: " + HtmxorRouteTemplateContract.MissingLeadingSlashReason;
	}

	private static string? ValidateRouteNamedArgument(KeyValuePair<string, TypedConstant> argument)
		=> argument.Key switch
		{
			"Methods" => HasSupportedExplicitMethods(argument.Value)
				? null
				: "explicit HtmxRoute Methods must resolve to a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY",
			"CurrentUrl" => HasSupportedOptionalString(argument.Value) &&
				HtmxRouteRepresentationContract.IsValidCurrentUrl(argument.Value.Value as string)
				? null
				: "HtmxRoute named argument 'CurrentUrl' must resolve to a valid relative or absolute HTTP(S) URI or null",
			"Target" => HasSupportedOptionalString(argument.Value) &&
				HtmxRouteRepresentationContract.IsValidOptionalTarget(argument.Value.Value as string)
				? null
				: "HtmxRoute named argument 'Target' must resolve to a valid element identity or null or whitespace",
			"Targets" => HasSupportedTargets(argument.Value) && HasSupportedTargetValues(argument.Value)
				? null
				: "HtmxRoute named argument 'Targets' must resolve to a constant array of valid element identities",
			_ => "HtmxRoute named argument '" + argument.Key + "' is not supported",
		};

	private string? ValidateRouteOrigin(CancellationToken cancellationToken)
	{
		var path = Route.ApplicationSyntaxReference?
			.GetSyntax(cancellationToken)
			.GetLocation()
			.GetMappedLineSpan()
			.Path;
		return RazorImportsFile.Matches(path)
			? "HtmxRoute declarations from _Imports.razor are not supported; move HtmxRoute onto the route-owning component"
			: null;
	}

	private string? ValidateNormalRoute(HtmxorRouteSymbols symbols)
	{
		if (symbols.Route is null)
		{
			return "the Blazor RouteAttribute symbol could not be resolved";
		}

		return GetHierarchyAttributes(Type).Any(attribute =>
			SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.Route))
			? "an HTMX-only component cannot also declare a normal Blazor route"
			: null;
	}

	private string GetMetadataName() => GetMetadataName(Type);

	private static string GetMetadataName(INamedTypeSymbol type)
	{
		if (type.ContainingType is not null)
		{
			return GetMetadataName(type.ContainingType) + "+" + type.MetadataName;
		}

		var namespaceName = type.ContainingNamespace.ToDisplayString();
		return string.IsNullOrEmpty(namespaceName)
			? type.MetadataName
			: namespaceName + "." + type.MetadataName;
	}

	private static ImmutableArray<AttributeData> GetExactAttributes(
		INamedTypeSymbol type,
		INamedTypeSymbol attributeType)
		=> type.GetAttributes()
			.Where(attribute => SymbolEqualityComparer.Default.Equals(
				attribute.AttributeClass,
				attributeType))
			.ToImmutableArray();

	private static IEnumerable<AttributeData> GetHierarchyAttributes(INamedTypeSymbol type)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			foreach (var attribute in current.GetAttributes())
			{
				yield return attribute;
			}
		}
	}

	internal static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol @namespace)
		=> @namespace.GetTypeMembers()
			.SelectMany(GetTypeAndNestedTypes)
			.Concat(@namespace.GetNamespaceMembers().SelectMany(GetTypes));

	private static IEnumerable<INamedTypeSymbol> GetTypeAndNestedTypes(INamedTypeSymbol type)
		=> new[] { type }.Concat(type.GetTypeMembers().SelectMany(GetTypeAndNestedTypes));

	private static bool HasSupportedExplicitMethods(TypedConstant methods)
	{
		if (methods.IsNull || methods.Kind != TypedConstantKind.Array || methods.Values.Length == 0)
		{
			return false;
		}

		var uniqueMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		return methods.Values.All(value =>
			value.Value is string method &&
			IsSupportedMethod(method) &&
			uniqueMethods.Add(method));
	}

	private static bool HasSupportedOptionalString(TypedConstant value)
		=> value.IsNull || value.Value is string;

	private static bool HasSupportedTargets(TypedConstant targets)
		=> targets.IsNull ||
			(targets.Kind == TypedConstantKind.Array &&
				targets.Values.All(static value => value.Value is string));

	private static bool HasSupportedTargetValues(TypedConstant targets)
		=> targets.IsNull ||
			targets.Values.All(static value =>
			value.Value is string target && HtmxRouteRepresentationContract.IsValidTarget(target));

	private static bool IsSupportedMethod(string method)
		=> string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "QUERY", StringComparison.OrdinalIgnoreCase);

}
