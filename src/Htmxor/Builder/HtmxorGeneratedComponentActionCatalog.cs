using Microsoft.AspNetCore.Http;

namespace Htmxor.Builder;

internal static class HtmxorGeneratedComponentActionCatalog
{
	public static IReadOnlyList<HtmxorComponentActionDescriptor> Bind(
		IReadOnlyList<HtmxorComponentGetRouteDescriptor> routes,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(routes);
		ArgumentNullException.ThrowIfNull(generatedActions);
		if (generatedActions.Count == 0)
		{
			return [];
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

		var route = routes.SingleOrDefault(route => route.ComponentType == action.ComponentType)
			?? throw new InvalidOperationException(
				$"Generated component action '{action.HandlerIdentity}' does not belong to a supported HTMX-only route.");
		return
		[
			new HtmxorComponentActionDescriptor(
				action.ComponentType,
				route.NormalizedRoute,
				HttpMethods.Put,
				action.HandlerIdentity,
				action),
		];
	}
}
