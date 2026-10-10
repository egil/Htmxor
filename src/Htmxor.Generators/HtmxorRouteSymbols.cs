using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

internal sealed class HtmxorRouteSymbols
{
	private HtmxorRouteSymbols(
		INamedTypeSymbol htmxRoute,
		INamedTypeSymbol? component,
		INamedTypeSymbol? route,
		INamedTypeSymbol? disableHtmxDirectRouting)
	{
		HtmxRoute = htmxRoute;
		Component = component;
		Route = route;
		DisableHtmxDirectRouting = disableHtmxDirectRouting;
	}

	public INamedTypeSymbol HtmxRoute { get; }

	public INamedTypeSymbol? Component { get; }

	public INamedTypeSymbol? Route { get; }

	public INamedTypeSymbol? DisableHtmxDirectRouting { get; }

	public static HtmxorRouteSymbols? Resolve(Compilation compilation)
	{
		var htmxRoute = compilation.GetTypeByMetadataName("Htmxor.HtmxRouteAttribute");
		if (htmxRoute is null)
		{
			return null;
		}

		return new HtmxorRouteSymbols(
			htmxRoute,
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent"),
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.RouteAttribute"),
			compilation.GetTypeByMetadataName("Htmxor.DisableHtmxDirectRoutingAttribute"));
	}
}
