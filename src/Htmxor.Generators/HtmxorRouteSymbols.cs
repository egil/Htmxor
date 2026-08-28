using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

internal sealed class HtmxorRouteSymbols
{
	private HtmxorRouteSymbols(
		INamedTypeSymbol htmxRoute,
		INamedTypeSymbol? authorize,
		INamedTypeSymbol? authorizeData,
		INamedTypeSymbol? allowAnonymous,
		INamedTypeSymbol? component,
		INamedTypeSymbol? route)
	{
		HtmxRoute = htmxRoute;
		Authorize = authorize;
		AuthorizeData = authorizeData;
		AllowAnonymous = allowAnonymous;
		Component = component;
		Route = route;
	}

	public INamedTypeSymbol HtmxRoute { get; }

	public INamedTypeSymbol? Authorize { get; }

	public INamedTypeSymbol? AuthorizeData { get; }

	public INamedTypeSymbol? AllowAnonymous { get; }

	public INamedTypeSymbol? Component { get; }

	public INamedTypeSymbol? Route { get; }

	public static HtmxorRouteSymbols? Resolve(Compilation compilation)
	{
		var htmxRoute = compilation.GetTypeByMetadataName("Htmxor.HtmxRouteAttribute");
		if (htmxRoute is null)
		{
			return null;
		}

		return new HtmxorRouteSymbols(
			htmxRoute,
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.AuthorizeAttribute"),
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.IAuthorizeData"),
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.IAllowAnonymous"),
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent"),
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.RouteAttribute"));
	}
}
