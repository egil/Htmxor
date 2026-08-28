using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Htmxor.Generators;

internal sealed class RouteDeclaration
{
	private const string HtmxRouteMetadataName = "Htmxor.HtmxRouteAttribute";
	private const string AuthorizeMetadataName = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";

	private RouteDeclaration(
		string path,
		string componentName,
		string route,
		string policy,
		bool isSupported,
		Location location)
	{
		Path = path;
		ComponentName = componentName;
		Route = route;
		Policy = policy;
		IsSupported = isSupported;
		Location = location;
	}

	public string Path { get; }

	public string ComponentName { get; }

	public string Route { get; }

	public string Policy { get; }

	public bool IsSupported { get; }

	public Location Location { get; }

	public static ImmutableArray<RouteDeclaration> Bind(
		ImmutableArray<RazorSourceFile> files,
		Compilation compilation,
		string rootNamespace,
		string projectDirectory)
	{
		var parseOptions = GetParseOptions(compilation);
		var documents = files
			.Select(file => RazorDirectiveDocument.Parse(file, parseOptions))
			.OrderBy(static document => document.File.Path, StringComparer.Ordinal)
			.ToImmutableArray();
		var declarations = ImmutableArray.CreateBuilder<RouteDeclaration>();

		foreach (var component in documents.Where(static document => !document.IsImports))
		{
			var imports = FindApplicableImports(documents, component, projectDirectory);
			var declaration = BindComponent(
				component,
				imports,
				compilation,
				parseOptions,
				rootNamespace,
				projectDirectory);
			if (declaration is not null)
			{
				declarations.Add(declaration);
			}
		}

		return declarations.ToImmutable();
	}

	private static RouteDeclaration? BindComponent(
		RazorDirectiveDocument component,
		ImmutableArray<RazorDirectiveDocument> imports,
		Compilation compilation,
		CSharpParseOptions parseOptions,
		string rootNamespace,
		string projectDirectory)
	{
		var binding = ComponentAttributeBinding.Bind(
			component,
			imports,
			compilation,
			parseOptions,
			rootNamespace);
		var routes = binding.FindAttributes(HtmxRouteMetadataName);
		if (routes.IsDefaultOrEmpty && !HasUnresolvedRouteCandidate(component, imports, binding))
		{
			return null;
		}

		var authorizations = binding.FindAttributes(AuthorizeMetadataName);
		var hasRoute = TryReadRoute(routes, out var route);
		var hasPolicy = TryReadPolicy(authorizations, out var policy);
		var isSupported = hasRoute &&
			hasPolicy &&
			!binding.HasErrors &&
			HasSupportedImports(imports, projectDirectory, rootNamespace) &&
			IsSupportedDocument(component, projectDirectory, rootNamespace);

		return new RouteDeclaration(
			component.File.Path,
			component.ComponentName,
			route,
			policy,
			isSupported,
			FindLocation(routes, component, imports));
	}

	private static ImmutableArray<RazorDirectiveDocument> FindApplicableImports(
		ImmutableArray<RazorDirectiveDocument> documents,
		RazorDirectiveDocument component,
		string projectDirectory)
		=> documents
			.Where(document => document.IsImports &&
				IsAtOrUnderDirectory(document.File.Path, projectDirectory) &&
				IsAtOrUnderDirectory(component.File.Path, GetDirectory(document.File.Path)))
			.OrderBy(static document => GetDirectory(document.File.Path).Length)
			.ThenBy(static document => document.File.Path, StringComparer.Ordinal)
			.ToImmutableArray();

	private static bool HasUnresolvedRouteCandidate(
		RazorDirectiveDocument component,
		ImmutableArray<RazorDirectiveDocument> imports,
		ComponentAttributeBinding binding)
	{
		var documents = imports.Add(component);
		var hasRouteText = documents.Any(static document => document.HasHtmxRouteAttributeText);
		var hasMalformedRouteText = documents.Any(static document =>
			document.HasMalformedAttribute && document.HasHtmxRouteAttributeText);
		var hasRouteUsing = documents.Any(static document => document.HasHtmxRouteUsingText);
		var hasUnboundAlias = hasRouteUsing &&
			(documents.Any(static document => document.HasMalformedAttribute) ||
			(binding.HasErrors && documents.Any(static document => !document.Attributes.IsDefaultOrEmpty)));

		return hasMalformedRouteText || hasUnboundAlias || (binding.HasErrors && hasRouteText);
	}

	private static bool TryReadRoute(
		ImmutableArray<BoundComponentAttribute> routes,
		out string route)
	{
		route = string.Empty;
		if (routes.Length != 1)
		{
			return false;
		}

		var data = routes[0].Data;
		if (!TryReadSingleString(data.ConstructorArguments, out route) ||
			data.NamedArguments.Length != 1 ||
			!string.Equals(data.NamedArguments[0].Key, "Methods", StringComparison.Ordinal))
		{
			return false;
		}

		return HasOnlyGet(data.NamedArguments[0].Value) && HasConstrainedParameter(route);
	}

	private static bool TryReadPolicy(
		ImmutableArray<BoundComponentAttribute> authorizations,
		out string policy)
	{
		policy = string.Empty;
		if (authorizations.Length != 1)
		{
			return false;
		}

		var data = authorizations[0].Data;
		if (data.ConstructorArguments.Length > 1 || data.NamedArguments.Length > 1)
		{
			return false;
		}

		if (data.ConstructorArguments.Length == 1 &&
			data.ConstructorArguments[0].Value is not string constructorPolicy)
		{
			return false;
		}

		policy = data.ConstructorArguments.Length == 1
			? (string)data.ConstructorArguments[0].Value!
			: string.Empty;
		if (data.NamedArguments.Length == 1)
		{
			var named = data.NamedArguments[0];
			if (!string.Equals(named.Key, "Policy", StringComparison.Ordinal) ||
				named.Value.Value is not string namedPolicy)
			{
				return false;
			}

			policy = namedPolicy;
		}

		return !string.IsNullOrWhiteSpace(policy);
	}

	private static bool TryReadSingleString(
		ImmutableArray<TypedConstant> arguments,
		out string value)
	{
		value = string.Empty;
		if (arguments.Length != 1 || arguments[0].Value is not string resolved)
		{
			return false;
		}

		value = resolved;
		return !string.IsNullOrWhiteSpace(value);
	}

	private static bool HasOnlyGet(TypedConstant methods)
		=> methods.Kind == TypedConstantKind.Array &&
		methods.Values.Length == 1 &&
		methods.Values[0].Value is string method &&
		string.Equals(method, "GET", StringComparison.Ordinal);

	private static bool IsSupportedDocument(
		RazorDirectiveDocument component,
		string projectDirectory,
		string rootNamespace)
		=> IsInDirectory(component.File.Path, projectDirectory) &&
		HasSupportedDirectives(component, rootNamespace);

	private static bool HasSupportedImports(
		ImmutableArray<RazorDirectiveDocument> imports,
		string projectDirectory,
		string rootNamespace)
		=> imports.Length <= 1 &&
		imports.All(import =>
			IsInDirectory(import.File.Path, projectDirectory) &&
			HasSupportedDirectives(import, rootNamespace));

	private static bool HasSupportedDirectives(
		RazorDirectiveDocument document,
		string rootNamespace)
		=> !document.HasPage &&
		!document.HasMalformedDirective &&
		(document.DeclaredNamespace is null ||
		string.Equals(document.DeclaredNamespace, rootNamespace, StringComparison.Ordinal));

	private static bool IsInDirectory(string path, string directory)
		=> string.Equals(
			GetDirectory(path),
			directory,
			StringComparison.OrdinalIgnoreCase);

	private static bool IsAtOrUnderDirectory(string path, string directory)
	{
		var current = GetDirectory(path);
		while (current.Length > 0)
		{
			if (string.Equals(current, directory, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			current = GetDirectory(current);
		}

		return false;
	}

	private static string GetDirectory(string path)
		=> System.IO.Path.GetDirectoryName(path) ?? string.Empty;

	private static Location FindLocation(
		ImmutableArray<BoundComponentAttribute> routes,
		RazorDirectiveDocument component,
		ImmutableArray<RazorDirectiveDocument> imports)
	{
		if (!routes.IsDefaultOrEmpty && routes[0].Location != Location.None)
		{
			return routes[0].Location;
		}

		var documents = imports.Add(component);
		var routeTextLocation = documents
			.Select(static document => document.HtmxRouteTextLocation)
			.FirstOrDefault(static location => location != Location.None) ?? Location.None;
		if (routeTextLocation != Location.None)
		{
			return routeTextLocation;
		}

		var attributeLocation = documents
			.Select(static document => document.AttributeFallbackLocation)
			.FirstOrDefault(static location => location != Location.None) ?? Location.None;
		return attributeLocation != Location.None
			? attributeLocation
			: component.FallbackLocation;
	}

	private static CSharpParseOptions GetParseOptions(Compilation compilation)
		=> compilation.SyntaxTrees
			.Select(static tree => tree.Options)
			.OfType<CSharpParseOptions>()
			.FirstOrDefault() ?? CSharpParseOptions.Default;

	private static bool HasConstrainedParameter(string route)
	{
		var openingBrace = route.IndexOf('{');
		var constraint = route.IndexOf(':', openingBrace + 1);
		var closingBrace = route.IndexOf('}', constraint + 1);

		return openingBrace >= 0 && constraint > openingBrace && closingBrace > constraint;
	}
}
