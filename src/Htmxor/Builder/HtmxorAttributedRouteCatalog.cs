using System.Reflection;
using Htmxor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Htmxor.Builder;

internal static class HtmxorAttributedRouteCatalog
{
	public static IReadOnlyList<HtmxorComponentRouteDescriptor> Build(
		Assembly applicationAssembly,
		IReadOnlyList<string> projectRootComponentTypeNames)
		=> Build(applicationAssembly, projectRootComponentTypeNames, []);

	public static IReadOnlyList<HtmxorComponentRouteDescriptor> Build(
		Assembly applicationAssembly,
		IReadOnlyList<string> projectRootComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(applicationAssembly);
		ArgumentNullException.ThrowIfNull(projectRootComponentTypeNames);
		ArgumentNullException.ThrowIfNull(generatedActions);

		var manifestTypeNames = ValidateManifest(projectRootComponentTypeNames);
		var routedTypes = GetRoutedTypes(applicationAssembly);
		ValidateRoutedTypesBelongToManifest(routedTypes, manifestTypeNames);

		foreach (var routedType in routedTypes)
		{
			ValidateComponentType(routedType.ComponentType);
		}

		var declarations = routedTypes
			.Select(routedType => ValidateDeclaration(routedType, generatedActions))
			.ToArray();

		return declarations
			.Select(CreateDescriptor)
			.ToArray();
	}

	private static HashSet<string> ValidateManifest(
		IReadOnlyList<string> projectRootComponentTypeNames)
	{
		var result = new HashSet<string>(StringComparer.Ordinal);
		string? previousTypeName = null;
		for (var index = 0; index < projectRootComponentTypeNames.Count; index++)
		{
			var typeName = projectRootComponentTypeNames[index];
			if (string.IsNullOrWhiteSpace(typeName))
			{
				throw new InvalidOperationException("The project-root component manifest contains a blank type name.");
			}

			if (previousTypeName is not null &&
				StringComparer.Ordinal.Compare(previousTypeName, typeName) >= 0)
			{
				throw new InvalidOperationException(
					"The project-root component manifest must contain unique type names in ordinal order.");
			}

			result.Add(typeName);
			previousTypeName = typeName;
		}

		return result;
	}

	private static RoutedType[] GetRoutedTypes(Assembly applicationAssembly)
		=> applicationAssembly.DefinedTypes
			.Select(static typeInfo => typeInfo.AsType())
			.Select(static type => new RoutedType(type, GetDeclaredRoutes(type)))
			.Where(static routedType => routedType.Routes.Length > 0)
			.OrderBy(static routedType => GetTypeName(routedType.ComponentType), StringComparer.Ordinal)
			.ToArray();

	private static CustomAttributeData[] GetDeclaredRoutes(Type type)
		=> CustomAttributeData.GetCustomAttributes(type)
			.Where(static attribute => attribute.AttributeType == typeof(HtmxRouteAttribute))
			.ToArray();

	private static void ValidateRoutedTypesBelongToManifest(
		IReadOnlyList<RoutedType> routedTypes,
		HashSet<string> manifestTypeNames)
	{
		var outsideManifest = routedTypes.FirstOrDefault(routedType =>
			!manifestTypeNames.Contains(GetTypeName(routedType.ComponentType)));
		if (outsideManifest is not null)
		{
			throw Unsupported(
				outsideManifest.ComponentType,
				"its HtmxRoute is declared outside the project-root component manifest");
		}
	}

	private static void ValidateComponentType(Type componentType)
	{
		if (!componentType.IsClass || componentType.IsAbstract || componentType.ContainsGenericParameters ||
			!typeof(IComponent).IsAssignableFrom(componentType))
		{
			throw Unsupported(
				componentType,
				"project-root components must be concrete, closed classes implementing IComponent");
		}
	}

	private static ValidatedDeclaration ValidateDeclaration(
		RoutedType routedType,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		var componentType = routedType.ComponentType;
		if (routedType.Routes.Length != 1)
		{
			throw Unsupported(componentType, "exactly one HtmxRoute must be declared");
		}

		if (GetHierarchyAttributes(componentType).Any(static attribute =>
			attribute.AttributeType == typeof(RouteAttribute)))
		{
			throw Unsupported(componentType, "normal component routes are not supported");
		}

		var route = ReadRoute(componentType, routedType.Routes[0]);
		var conflictingAction = route.ExplicitMethods is null
			? null
			: generatedActions.FirstOrDefault(action =>
				action.ComponentType == componentType &&
				!action.UsesStockRoute &&
				!route.ExplicitMethods.Contains(action.HttpMethod, StringComparer.OrdinalIgnoreCase));
		if (conflictingAction is not null)
		{
			throw Unsupported(
				componentType,
				$"explicit HtmxRoute.Methods is authoritative and does not allow the {conflictingAction.HttpMethod} binding");
		}

		var policy = ReadAuthorizationPolicy(componentType);
		return new ValidatedDeclaration(
			componentType,
			route.Template,
			policy,
			route.ExplicitMethods,
			route.CurrentUrl,
			route.Target,
			route.Targets);
	}

	private static ValidatedRoute ReadRoute(Type componentType, CustomAttributeData attribute)
	{
		if (attribute.ConstructorArguments.Count != 1 ||
			attribute.ConstructorArguments[0].Value is not string template ||
			string.IsNullOrWhiteSpace(template))
		{
			throw Unsupported(componentType, "the HtmxRoute template must be a non-blank string");
		}

		string[]? explicitMethods = null;
		string? currentUrl = null;
		string? target = null;
		string[]? targets = null;
		foreach (var namedArgument in attribute.NamedArguments)
		{
			switch (namedArgument.MemberName)
			{
				case nameof(HtmxRouteAttribute.Methods):
					explicitMethods = ReadExplicitMethods(componentType, namedArgument.TypedValue);
					break;
				case nameof(HtmxRouteAttribute.CurrentUrl):
					currentUrl = ReadOptionalString(componentType, namedArgument);
					break;
				case nameof(HtmxRouteAttribute.Target):
					target = ReadOptionalString(componentType, namedArgument);
					break;
				case nameof(HtmxRouteAttribute.Targets):
					targets = ReadTargets(componentType, namedArgument.TypedValue);
					break;
				default:
					throw Unsupported(
						componentType,
						$"HtmxRoute named argument '{namedArgument.MemberName}' is not supported");
			}
		}

		if (!HtmxorRouteTemplateContract.IsSupported(template))
		{
			throw Unsupported(
				componentType,
				"the HtmxRoute template must use supported literal segments and constrained route parameters");
		}

		try
		{
			RoutePatternFactory.Parse(template);
		}
		catch (RoutePatternException exception)
		{
			throw Unsupported(componentType, "the HtmxRoute template is not a valid route pattern", exception);
		}

		return new ValidatedRoute(template, explicitMethods, currentUrl, target, targets);
	}

	private static string? ReadOptionalString(
		Type componentType,
		CustomAttributeNamedArgument argument)
	{
		if (argument.TypedValue.Value is null)
		{
			return null;
		}

		if (argument.TypedValue.Value is string value)
		{
			var isValid = argument.MemberName switch
			{
				nameof(HtmxRouteAttribute.CurrentUrl) => HtmxRouteRepresentationContract.IsValidCurrentUrl(value),
				nameof(HtmxRouteAttribute.Target) => HtmxRouteRepresentationContract.IsValidOptionalTarget(value),
				_ => false,
			};
			if (!isValid)
			{
				throw Unsupported(
					componentType,
					$"HtmxRoute named argument '{argument.MemberName}' must be a valid representation value");
			}

			return value;
		}

		throw Unsupported(
			componentType,
			$"HtmxRoute named argument '{argument.MemberName}' must be a string or null");
	}

	private static string[] ReadTargets(
		Type componentType,
		CustomAttributeTypedArgument targetsArgument)
	{
		if (targetsArgument.Value is null)
		{
			return [];
		}

		if (targetsArgument.Value is not IEnumerable<CustomAttributeTypedArgument> targets)
		{
			throw Unsupported(
				componentType,
				"HtmxRoute named argument 'Targets' must be a string array");
		}

		var values = targets
			.Select(static argument => argument.Value as string)
			.ToArray();
		if (values.Any(static value => !HtmxRouteRepresentationContract.IsValidTarget(value)))
		{
			throw Unsupported(
				componentType,
				"HtmxRoute named argument 'Targets' must be a string array");
		}

		return values!;
	}

	private static string[] ReadExplicitMethods(
		Type componentType,
		CustomAttributeTypedArgument methodsArgument)
	{
		if (methodsArgument.Value is not IEnumerable<CustomAttributeTypedArgument> methods)
		{
			throw Unsupported(
				componentType,
				"explicit HtmxRoute.Methods must be a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY");
		}

		var values = methods
			.Select(static argument => argument.Value as string)
			.ToArray();
		if (values.Length == 0 ||
			values.Any(static method => method is null || !TryNormalizeMethod(method, out _)) ||
			values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length)
		{
			throw Unsupported(
				componentType,
				"explicit HtmxRoute.Methods must be a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY");
		}

		return values
			.Select(static method =>
			{
				TryNormalizeMethod(method!, out var normalized);
				return normalized!;
			})
			.ToArray();
	}

	private static bool TryNormalizeMethod(string method, out string? normalized)
	{
		normalized = HttpMethods.IsGet(method) ? HttpMethods.Get
			: HttpMethods.IsPost(method) ? HttpMethods.Post
			: HttpMethods.IsPut(method) ? HttpMethods.Put
			: HttpMethods.IsPatch(method) ? HttpMethods.Patch
			: HttpMethods.IsDelete(method) ? HttpMethods.Delete
			: string.Equals(method, "QUERY", StringComparison.OrdinalIgnoreCase) ? "QUERY"
			: null;
		return normalized is not null;
	}

	private static string ReadAuthorizationPolicy(Type componentType)
	{
		var attributes = GetHierarchyAttributes(componentType);
		if (attributes.Any(static attribute =>
			typeof(IAllowAnonymous).IsAssignableFrom(attribute.AttributeType)))
		{
			throw Unsupported(componentType, "AllowAnonymous is not supported");
		}

		var authorization = attributes
			.Where(static attribute => typeof(IAuthorizeData).IsAssignableFrom(attribute.AttributeType))
			.ToArray();
		if (authorization.Length != 1 || authorization[0].AttributeType != typeof(AuthorizeAttribute))
		{
			throw Unsupported(componentType, "exactly one standard Authorize policy must be effective");
		}

		var attribute = authorization[0];
		if (attribute.ConstructorArguments.Count > 1)
		{
			throw Unsupported(componentType, "the Authorize declaration is not supported");
		}

		var policy = attribute.ConstructorArguments.Count == 1
			? attribute.ConstructorArguments[0].Value as string
			: null;
		foreach (var argument in attribute.NamedArguments)
		{
			if (!string.Equals(argument.MemberName, nameof(AuthorizeAttribute.Policy), StringComparison.Ordinal) ||
				argument.TypedValue.Value is not string namedPolicy)
			{
				throw Unsupported(
					componentType,
					"Authorize must contain only one policy without roles or authentication schemes");
			}

			policy = namedPolicy;
		}

		if (string.IsNullOrWhiteSpace(policy))
		{
			throw Unsupported(componentType, "Authorize must declare a non-blank policy");
		}

		return policy;
	}

	private static CustomAttributeData[] GetHierarchyAttributes(Type componentType)
	{
		var attributes = new List<CustomAttributeData>();
		for (var current = componentType; current is not null; current = current.BaseType)
		{
			attributes.AddRange(CustomAttributeData.GetCustomAttributes(current));
		}

		return attributes.ToArray();
	}

	private static HtmxorComponentRouteDescriptor CreateDescriptor(ValidatedDeclaration declaration)
	{
		object[] metadata;
		try
		{
			metadata = declaration.ComponentType.GetCustomAttributes(inherit: true).ToArray();
		}
		catch (Exception exception)
		{
			throw Unsupported(
				declaration.ComponentType,
				"its effective component metadata could not be constructed",
				exception);
		}

		var route = metadata.OfType<HtmxRouteAttribute>().SingleOrDefault();
		var expectedMethods = declaration.ExplicitMethods ?? [HtmxRouteAttribute.ImplicitHttpMethod];
		var expectedTargets = declaration.Targets ?? [];
		var actualTargets = route?.Targets ?? [];
		if (route is null ||
			!string.Equals(route.Template, declaration.Route, StringComparison.Ordinal) ||
			route.Methods is null ||
			!route.Methods.SequenceEqual(expectedMethods, StringComparer.OrdinalIgnoreCase) ||
			!string.Equals(route.CurrentUrl, declaration.CurrentUrl, StringComparison.Ordinal) ||
			!string.Equals(route.Target, declaration.Target, StringComparison.Ordinal) ||
			!actualTargets.SequenceEqual(expectedTargets, StringComparer.Ordinal))
		{
			throw Unsupported(
				declaration.ComponentType,
				"its constructed HtmxRoute metadata does not match the validated declaration");
		}

		if (!metadata.OfType<AuthorizeAttribute>().Any(authorize =>
			string.Equals(authorize.Policy, declaration.Policy, StringComparison.Ordinal)))
		{
			throw Unsupported(
				declaration.ComponentType,
				"its constructed Authorize metadata does not match the validated declaration");
		}

		return new HtmxorComponentRouteDescriptor(
			declaration.ComponentType,
			declaration.Route,
			metadata,
			expectedMethods.ToArray());
	}

	private static InvalidOperationException Unsupported(Type componentType, string reason)
		=> new($"Component '{GetTypeName(componentType)}' is not a supported HTMX-only route declaration: {reason}.");

	private static InvalidOperationException Unsupported(
		Type componentType,
		string reason,
		Exception innerException)
		=> new(
			$"Component '{GetTypeName(componentType)}' is not a supported HTMX-only route declaration: {reason}.",
			innerException);

	private static string GetTypeName(Type type) => type.FullName ?? type.Name;

	private sealed record RoutedType(Type ComponentType, CustomAttributeData[] Routes);

	private sealed record ValidatedRoute(
		string Template,
		string[]? ExplicitMethods,
		string? CurrentUrl,
		string? Target,
		string[]? Targets);

	private sealed record ValidatedDeclaration(
		Type ComponentType,
		string Route,
		string Policy,
		string[]? ExplicitMethods,
		string? CurrentUrl,
		string? Target,
		string[]? Targets);
}
