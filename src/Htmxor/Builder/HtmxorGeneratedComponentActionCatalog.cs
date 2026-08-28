using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Builder;

internal static class HtmxorGeneratedComponentActionCatalog
{
	public static void Validate(
		Assembly applicationAssembly,
		IReadOnlyList<string> projectRootComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(applicationAssembly);
		ArgumentNullException.ThrowIfNull(projectRootComponentTypeNames);
		ArgumentNullException.ThrowIfNull(generatedActions);
		if (generatedActions.Count == 0)
		{
			return;
		}

		if (generatedActions.Count != 1)
		{
			throw new InvalidOperationException("Htmxor supports exactly one generated component action.");
		}

		var action = generatedActions[0]
			?? throw new InvalidOperationException("The generated component action cannot be null.");
		if (!HttpMethods.IsPut(action.HttpMethod))
		{
			throw new InvalidOperationException("Htmxor supports only a generated PUT action.");
		}

		var componentTypeName = action.ComponentType.FullName;
		if (!string.Equals(
			action.ComponentType.Assembly.FullName,
			applicationAssembly.FullName,
			StringComparison.Ordinal) ||
			componentTypeName is null ||
			!projectRootComponentTypeNames.Contains(componentTypeName, StringComparer.Ordinal))
		{
			throw new InvalidOperationException(
				$"Generated component action '{action.HandlerIdentity}' does not belong to the project-root component manifest.");
		}

		var compiledStockRouteCount = action.ComponentType.CustomAttributes.Count(
			static attribute => attribute.AttributeType == typeof(RouteAttribute));
		if (compiledStockRouteCount != 1)
		{
			throw new InvalidOperationException(
				$"Generated component action '{action.HandlerIdentity}' on component " +
				$"'{action.ComponentType.FullName}' requires exactly one compiled stock route; " +
				$"found {compiledStockRouteCount}.");
		}
	}

	public static IReadOnlyList<HtmxorComponentActionDescriptor> Bind(
		Type componentType,
		string normalizedRoute,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(componentType);
		ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRoute);
		ArgumentNullException.ThrowIfNull(generatedActions);
		return generatedActions
			.Where(action => action.ComponentType == componentType)
			.Select(action => new HtmxorComponentActionDescriptor(
				action.ComponentType,
				normalizedRoute,
				action.HttpMethod,
				action.HandlerIdentity,
				action))
			.ToArray();
	}
}
