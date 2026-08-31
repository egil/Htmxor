using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

internal sealed class HtmxorRoutedComponent
{
	private HtmxorRoutedComponent(
		INamedTypeSymbol type,
		ImmutableArray<AttributeData> routes)
	{
		Type = type;
		Routes = routes;
	}

	private INamedTypeSymbol Type { get; }

	private ImmutableArray<AttributeData> Routes { get; }

	public static ImmutableArray<HtmxorRoutedComponent> FindAll(
		IAssemblySymbol assembly,
		HtmxorRouteSymbols symbols)
		=> GetTypes(assembly.GlobalNamespace)
			.Select(type => new HtmxorRoutedComponent(
				type,
				GetExactAttributes(type, symbols.HtmxRoute)))
			.Where(static component => !component.Routes.IsDefaultOrEmpty)
			.OrderBy(static component => component.GetMetadataName(), StringComparer.Ordinal)
			.ToImmutableArray();

	public string? GetUnsupportedReason(
		HtmxorRouteSymbols symbols,
		ImmutableHashSet<string> manifest,
		AnalyzerConfigOptionsProvider optionsProvider,
		CancellationToken cancellationToken)
		=> ValidateManifest(manifest, optionsProvider) ??
			ValidateComponent(symbols) ??
			ValidateRoute() ??
			ValidateRouteOrigin(cancellationToken) ??
			ValidateNormalRoute(symbols) ??
			ValidateAuthorization(symbols);

	public Location GetLocation(CancellationToken cancellationToken)
	{
		var attributeLocation = Routes
			.Select(route => route.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation())
			.Where(static location => location is not null)
			.OrderBy(static location => location!.GetMappedLineSpan().Path, StringComparer.Ordinal)
			.ThenBy(static location => location!.SourceSpan.Start)
			.FirstOrDefault();

		return attributeLocation ?? Type.Locations.FirstOrDefault() ?? Location.None;
	}

	private string? ValidateManifest(
		ImmutableHashSet<string> manifest,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		if (Routes.All(static route =>
			route.ApplicationSyntaxReference is { } reference &&
			HtmxorRouteManifest.IsRazorGeneratedPath(reference.SyntaxTree.FilePath)))
		{
			return manifest.Contains(GetMetadataName())
				? null
				: "the HtmxRoute component must be a project-root Razor component";
		}

		var csharpComponent = GetCSharpComponent();
		if (csharpComponent is null)
		{
			return "the HtmxRoute component must be a project-root Razor component";
		}

		if (!HtmxorRouteManifest.IsProjectRoot(csharpComponent, optionsProvider))
		{
			return "the HtmxRoute component must be a project-root Razor component";
		}

		var csharpRouteOriginReason = ValidateCSharpRouteOrigin(csharpComponent);
		if (csharpRouteOriginReason is not null)
		{
			return csharpRouteOriginReason;
		}

		return Routes.Length == 1 && !csharpComponent.HasExplicitMethods
			? "a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods"
			: null;
	}

	private CSharpRoutedComponent? GetCSharpComponent()
	{
		if (Type.ContainingType is not null)
		{
			return null;
		}

		var path = Routes
			.Select(static route => route.ApplicationSyntaxReference?.SyntaxTree.FilePath)
			.FirstOrDefault(static candidate =>
				!string.IsNullOrEmpty(candidate) &&
				!HtmxorRouteManifest.IsRazorGeneratedPath(candidate!));
		if (path is null)
		{
			return null;
		}

		return new CSharpRoutedComponent(
			GetMetadataName(),
			Type.ContainingNamespace.ToDisplayString(),
			path,
			Routes.Length == 1 && Routes[0].NamedArguments.Any(static argument =>
				string.Equals(argument.Key, "Methods", StringComparison.Ordinal)));
	}

	private string? ValidateCSharpRouteOrigin(CSharpRoutedComponent component)
	{
		if (Routes.Length != 1 ||
			!HtmxorRouteManifest.HasCompiledRazorDeclaration(Type))
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

	private string? ValidateRoute()
	{
		if (Routes.Length != 1)
		{
			return "each component must declare exactly one HtmxRoute attribute";
		}

		var route = Routes[0];
		return ValidateTemplate(route) ?? ValidateRouteNamedArguments(route);
	}

	private static string? ValidateTemplate(AttributeData route)
	{
		if (route.ConstructorArguments.Length != 1 ||
			route.ConstructorArguments[0].Value is not string template ||
			string.IsNullOrWhiteSpace(template))
		{
			return "HtmxRoute must resolve one nonblank constant route template";
		}

		return HtmxorRouteTemplateContract.IsSupported(template)
			? null
			: "HtmxRoute must use supported literal segments and constrained route parameters";
	}

	private static string? ValidateRouteNamedArguments(AttributeData route)
	{
		var unsupportedArgument = route.NamedArguments
			.Select(static argument => argument.Key)
			.Where(static name => !string.Equals(name, "Methods", StringComparison.Ordinal))
			.OrderBy(static name => name, StringComparer.Ordinal)
			.FirstOrDefault();
		if (unsupportedArgument is not null)
		{
			return "HtmxRoute named argument '" + unsupportedArgument + "' is not supported";
		}

		var methods = route.NamedArguments
			.Where(static argument => string.Equals(argument.Key, "Methods", StringComparison.Ordinal))
			.Select(static argument => argument.Value)
			.ToImmutableArray();
		if (methods.Length == 0)
		{
			return null;
		}

		return methods.Length == 1 && HasSupportedExplicitMethods(methods[0])
			? null
			: "explicit HtmxRoute Methods must resolve to a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY";
	}

	private string? ValidateRouteOrigin(CancellationToken cancellationToken)
	{
		var path = Routes[0].ApplicationSyntaxReference?
			.GetSyntax(cancellationToken)
			.GetLocation()
			.GetMappedLineSpan()
			.Path;
		return path is not null && path.EndsWith("_Imports.razor", StringComparison.OrdinalIgnoreCase)
			? "HtmxRoute declarations from _Imports.razor are not supported"
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

	private string? ValidateAuthorization(HtmxorRouteSymbols symbols)
	{
		if (symbols.Authorize is null ||
			symbols.AuthorizeData is null ||
			symbols.AllowAnonymous is null)
		{
			return "the ASP.NET Core authorization symbols could not be resolved";
		}

		var attributes = GetHierarchyAttributes(Type).ToImmutableArray();
		if (attributes.Any(attribute => Implements(attribute, symbols.AllowAnonymous)))
		{
			return "an HTMX-only component cannot allow anonymous access";
		}

		var authorizations = attributes
			.Where(attribute => Implements(attribute, symbols.AuthorizeData))
			.ToImmutableArray();
		if (authorizations.Length != 1)
		{
			return "each component must have exactly one effective authorization declaration";
		}

		return SymbolEqualityComparer.Default.Equals(
			authorizations[0].AttributeClass,
			symbols.Authorize)
			? ValidatePolicy(authorizations[0])
			: "the effective authorization declaration must be the standard Authorize attribute";
	}

	private static string? ValidatePolicy(AttributeData authorization)
	{
		var unsupportedArgument = authorization.NamedArguments
			.Select(static argument => argument.Key)
			.Where(static name => !string.Equals(name, "Policy", StringComparison.Ordinal))
			.OrderBy(static name => name, StringComparer.Ordinal)
			.FirstOrDefault();
		if (unsupportedArgument is not null)
		{
			return "Authorize named argument '" + unsupportedArgument + "' is not supported";
		}

		if (authorization.ConstructorArguments.Length > 1)
		{
			return "Authorize must declare one policy through its constructor or Policy property";
		}

		var policy = GetEffectivePolicy(authorization);
		return !string.IsNullOrWhiteSpace(policy)
			? null
			: "Authorize must resolve one nonblank policy through its constructor or Policy property";
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

	private static bool Implements(AttributeData attribute, INamedTypeSymbol interfaceType)
		=> attribute.AttributeClass?.AllInterfaces.Any(implemented =>
			SymbolEqualityComparer.Default.Equals(implemented, interfaceType)) == true;

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

	private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol @namespace)
		=> @namespace.GetTypeMembers()
			.SelectMany(GetTypeAndNestedTypes)
			.Concat(@namespace.GetNamespaceMembers().SelectMany(GetTypes));

	private static IEnumerable<INamedTypeSymbol> GetTypeAndNestedTypes(INamedTypeSymbol type)
		=> new[] { type }.Concat(type.GetTypeMembers().SelectMany(GetTypeAndNestedTypes));

	private static string? GetEffectivePolicy(AttributeData authorization)
	{
		var namedPolicy = authorization.NamedArguments
			.Where(static argument => string.Equals(argument.Key, "Policy", StringComparison.Ordinal))
			.Select(static argument => argument.Value.Value as string)
			.FirstOrDefault();
		return authorization.NamedArguments.Any(static argument =>
			string.Equals(argument.Key, "Policy", StringComparison.Ordinal))
			? namedPolicy
			: authorization.ConstructorArguments.Length == 1
				? authorization.ConstructorArguments[0].Value as string
				: null;
	}

	private static bool HasSupportedExplicitMethods(TypedConstant methods)
	{
		if (methods.Kind != TypedConstantKind.Array || methods.Values.Length == 0)
		{
			return false;
		}

		var uniqueMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		return methods.Values.All(value =>
			value.Value is string method &&
			IsSupportedMethod(method) &&
			uniqueMethods.Add(method));
	}

	private static bool IsSupportedMethod(string method)
		=> string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(method, "QUERY", StringComparison.OrdinalIgnoreCase);

}
