using System.Reflection;
using System.Text.RegularExpressions;
using Htmxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Htmxor.Builder;

internal static class HtmxorAttributedRouteCatalog
{
	public static IReadOnlyList<HtmxorComponentRouteDescriptor> Build(
		Assembly applicationAssembly,
		IReadOnlyList<string> routedComponentTypeNames)
		=> Build(applicationAssembly, routedComponentTypeNames, []);

	public static IReadOnlyList<HtmxorComponentRouteDescriptor> Build(
		Assembly applicationAssembly,
		IReadOnlyList<string> routedComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
		=> Build(applicationAssembly, routedComponentTypeNames, generatedActions, []);

	public static IReadOnlyList<HtmxorComponentRouteDescriptor> Build(
		Assembly applicationAssembly,
		IReadOnlyList<string> routedComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions,
		IReadOnlyList<string> namedFormComponentTypeNames)
		=> Build(applicationAssembly, routedComponentTypeNames, generatedActions, namedFormComponentTypeNames,
			HtmxorExplicitComponentActionCatalog.Build(applicationAssembly, routedComponentTypeNames, namedFormComponentTypeNames));

	internal static IReadOnlyList<HtmxorComponentRouteDescriptor> Build(
		Assembly applicationAssembly,
		IReadOnlyList<string> routedComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions,
		IReadOnlyList<string> namedFormComponentTypeNames,
		IReadOnlyList<HtmxorComponentActionDescriptor> explicitActions)
	{
		ArgumentNullException.ThrowIfNull(applicationAssembly);
		ArgumentNullException.ThrowIfNull(routedComponentTypeNames);
		ArgumentNullException.ThrowIfNull(generatedActions);
		ArgumentNullException.ThrowIfNull(namedFormComponentTypeNames);

		HtmxorNamedFormCatalog.Validate(routedComponentTypeNames, namedFormComponentTypeNames, generatedActions);
		var manifestTypeNames = ValidateManifest(routedComponentTypeNames);
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
			.Select(declaration => CreateDescriptor(declaration, namedFormComponentTypeNames, explicitActions))
			.ToArray();
	}

	private static HashSet<string> ValidateManifest(
		IReadOnlyList<string> routedComponentTypeNames)
	{
		var result = new HashSet<string>(StringComparer.Ordinal);
		string? previousTypeName = null;
		for (var index = 0; index < routedComponentTypeNames.Count; index++)
		{
			var typeName = routedComponentTypeNames[index];
			if (string.IsNullOrWhiteSpace(typeName))
			{
				throw new InvalidOperationException("The generated component manifest contains a blank type name.");
			}

			if (previousTypeName is not null &&
				StringComparer.Ordinal.Compare(previousTypeName, typeName) >= 0)
			{
				throw new InvalidOperationException(
					"The generated component manifest must contain unique type names in ordinal order.");
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
				"its HtmxRoute is declared outside the generated component manifest");
		}
	}

	private static void ValidateComponentType(Type componentType)
	{
		if (!componentType.IsClass || componentType.IsAbstract || componentType.ContainsGenericParameters ||
			!typeof(IComponent).IsAssignableFrom(componentType))
		{
			throw Unsupported(
				componentType,
				HtmxorRejectionReasons.RouteTarget);
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

		if (CustomAttributeData.GetCustomAttributes(componentType).Any(static attribute =>
			attribute.AttributeType == typeof(DisableHtmxDirectRoutingAttribute)))
		{
			throw Unsupported(componentType, HtmxorRejectionReasons.NormalOnlyRoute);
		}

		if (GetHierarchyAttributes(componentType).Any(static attribute =>
			attribute.AttributeType == typeof(RouteAttribute)))
		{
			throw Unsupported(componentType, HtmxorRejectionReasons.StockRoute);
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
				HtmxorRejectionReasons.InferredMethods(conflictingAction.HttpMethod));
		}

		return new ValidatedDeclaration(
			componentType,
			route.Template,
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
			throw Unsupported(componentType, HtmxorRejectionReasons.BlankTemplate);
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

		if (!HtmxorRouteTemplateContract.HasLeadingSlash(template))
		{
			throw Unsupported(
				componentType,
				$"HtmxRoute template '{template}' is invalid: {HtmxorRouteTemplateContract.MissingLeadingSlashReason.TrimEnd('.')}");
		}

		try
		{
			RoutePatternFactory.Parse(template);
		}
		catch (RoutePatternException exception)
		{
			throw Unsupported(componentType, RejectedTemplate(template, exception.Message), exception);
		}

		return new ValidatedRoute(template, explicitMethods, currentUrl, target, targets);
	}

	// Stock resolves inline constraints only when endpoint routing builds its matcher for the first request. Resolving
	// them here through the same stock factory fails the application at startup with stock's reason instead.
	public static void ResolveConstraints(
		IReadOnlyList<HtmxorComponentRouteDescriptor> descriptors,
		ParameterPolicyFactory parameterPolicyFactory)
	{
		ArgumentNullException.ThrowIfNull(descriptors);
		ArgumentNullException.ThrowIfNull(parameterPolicyFactory);
		foreach (var descriptor in descriptors)
		{
			var pattern = RoutePatternFactory.Parse(descriptor.NormalizedRoute);
			foreach (var parameter in pattern.Parameters)
			{
				foreach (var policy in parameter.ParameterPolicies)
				{
					ResolveConstraint(descriptor, parameterPolicyFactory, parameter, policy);
				}
			}
		}
	}

	private static void ResolveConstraint(
		HtmxorComponentRouteDescriptor descriptor,
		ParameterPolicyFactory parameterPolicyFactory,
		RoutePatternParameterPart parameter,
		RoutePatternParameterPolicyReference policy)
	{
		try
		{
			var resolvedPolicy = parameterPolicyFactory.Create(parameter, policy);
			if (resolvedPolicy is OptionalRouteConstraint optional)
			{
				resolvedPolicy = optional.InnerConstraint;
			}

			// Stock compiles a regex constraint's pattern on first match; reading Constraint compiles it now.
			if (resolvedPolicy is RegexRouteConstraint regex)
			{
				_ = regex.Constraint;
			}
		}
		// An unknown constraint throws InvalidOperationException, a constraint that cannot be constructed throws
		// RouteCreationException, and an invalid regex pattern throws RegexParseException.
		catch (Exception exception) when (exception is InvalidOperationException or RouteCreationException or RegexParseException)
		{
			throw Unsupported(
				descriptor.ComponentType,
				RejectedTemplate(descriptor.NormalizedRoute, exception.Message),
				exception);
		}
	}

	private static string RejectedTemplate(string template, string stockReason)
		=> $"stock routing rejects the HtmxRoute template '{template}': {stockReason.TrimEnd('.')}";

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
					argument.MemberName == nameof(HtmxRouteAttribute.CurrentUrl)
						? HtmxorRejectionReasons.CurrentUrl : HtmxorRejectionReasons.Target);
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
				HtmxorRejectionReasons.Targets);
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
				HtmxorRejectionReasons.RouteMethods);
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
				HtmxorRejectionReasons.RouteMethods);
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

	private static CustomAttributeData[] GetHierarchyAttributes(Type componentType)
	{
		var attributes = new List<CustomAttributeData>();
		for (var current = componentType; current is not null; current = current.BaseType)
		{
			attributes.AddRange(CustomAttributeData.GetCustomAttributes(current));
		}

		return attributes.ToArray();
	}

	private static HtmxorComponentRouteDescriptor CreateDescriptor(ValidatedDeclaration declaration, IReadOnlyList<string> namedFormComponentTypeNames,
		IReadOnlyList<HtmxorComponentActionDescriptor> explicitActions)
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

		var hasNamedForms = namedFormComponentTypeNames.Contains(GetTypeName(declaration.ComponentType), StringComparer.Ordinal);
		if (hasNamedForms && declaration.ExplicitMethods is not null &&
			!expectedMethods.Contains(HttpMethods.Post, StringComparer.OrdinalIgnoreCase))
		{
			throw Unsupported(declaration.ComponentType, HtmxorRejectionReasons.FormMethods);
		}

		var effectiveMethods = hasNamedForms && declaration.ExplicitMethods is null
			? new[] { HttpMethods.Get, HttpMethods.Post }
			: expectedMethods;
		if (declaration.ExplicitMethods is null)
		{
			effectiveMethods = effectiveMethods.Concat(explicitActions.Where(action => action.ComponentType == declaration.ComponentType)
				.Select(static action => action.HttpMethod)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		}
		return new HtmxorComponentRouteDescriptor(
			declaration.ComponentType,
			declaration.Route,
			metadata,
			effectiveMethods.ToArray());
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
		string[]? ExplicitMethods,
		string? CurrentUrl,
		string? Target,
		string[]? Targets);
}
