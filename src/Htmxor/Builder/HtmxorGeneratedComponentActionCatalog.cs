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

		foreach (var action in generatedActions)
		{
			ValidateAction(
				action ?? throw new InvalidOperationException("A generated component action cannot be null."),
				applicationAssembly,
				projectRootComponentTypeNames);
		}

		var duplicate = generatedActions
			.GroupBy(static action => action.ComponentType)
			.SelectMany(static component => component.GroupBy(
				static action => action.HttpMethod,
				StringComparer.OrdinalIgnoreCase))
			.FirstOrDefault(group => group.Count() > 1);
		if (duplicate is not null)
		{
			var action = duplicate.First();
			throw new InvalidOperationException(
				$"Component '{action.ComponentType.FullName}' declares more than one " +
				$"generated {action.HttpMethod} action.");
		}
	}

	private static void ValidateAction(
		HtmxorGeneratedComponentAction action,
		Assembly applicationAssembly,
		IReadOnlyList<string> projectRootComponentTypeNames)
	{
		if (!IsSupportedUnsafeMethod(action.HttpMethod))
		{
			throw new InvalidOperationException(
				$"Generated component action '{action.HandlerIdentity}' uses unsupported method '{action.HttpMethod}'.");
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

		var stockRouteCount = action.ComponentType.CustomAttributes.Count(
			static attribute => attribute.AttributeType == typeof(RouteAttribute));
		var htmxRouteCount = action.ComponentType.CustomAttributes.Count(
			static attribute => attribute.AttributeType == typeof(HtmxRouteAttribute));
		var hasExpectedOwner = action.UsesStockRoute
			? stockRouteCount == 1 && htmxRouteCount == 0
			: stockRouteCount == 0 && htmxRouteCount == 1;
		if (!hasExpectedOwner)
		{
			var expectedOwner = action.UsesStockRoute
				? "exactly one compiled stock route and no HtmxRoute"
				: "exactly one HtmxRoute and no compiled stock route";
			throw new InvalidOperationException(
				$"Generated component action '{action.HandlerIdentity}' on component " +
				$"'{action.ComponentType.FullName}' requires {expectedOwner}; " +
				$"found {stockRouteCount} stock and {htmxRouteCount} HTMX-only routes.");
		}
	}

	private static bool IsSupportedUnsafeMethod(string method)
		=> HttpMethods.IsPost(method) ||
			HttpMethods.IsPut(method) ||
			HttpMethods.IsPatch(method) ||
			HttpMethods.IsDelete(method);

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
