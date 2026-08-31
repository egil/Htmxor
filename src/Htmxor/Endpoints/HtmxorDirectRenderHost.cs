using Htmxor.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Endpoints;

internal sealed class HtmxorDirectRenderHost : ComponentBase
{
	[Inject]
	private IRoutingStateProvider RoutingStateProvider { get; set; } = default!;

	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var routeData = RoutingStateProvider.RouteData
			?? throw new InvalidOperationException("The stock Razor component invoker did not initialize route data.");
		// Router consumes the endpoint-selected RouteData and processes its values without matching the route again.
		builder.OpenComponent<Router>(0);
		builder.AddComponentParameter(1, nameof(Router.AppAssembly), routeData.PageType.Assembly);
		// The processor carries Router's compiled template while this metadata retains the request-owned component.
		var componentType = HttpContextAccessor.HttpContext?
			.GetEndpoint()?
			.Metadata
			.GetMetadata<HtmxorRouteProcessorMetadata>()?
			.ComponentType;
		builder.AddComponentParameter(
			2,
			nameof(Router.Found),
			(RenderFragment<RouteData>)(route => RenderRoute(route, componentType)));
		builder.CloseComponent();
	}

	internal static RenderFragment RenderRoute(RouteData routeData) => builder =>
		RenderRoute(routeData, componentType: null)(builder);

	private static RenderFragment RenderRoute(RouteData routeData, Type? componentType) => builder =>
	{
		builder.OpenComponent<DynamicComponent>(0);
		builder.AddComponentParameter(
			1,
			nameof(DynamicComponent.Type),
			componentType ?? routeData.PageType);
		builder.AddComponentParameter(
			2,
			nameof(DynamicComponent.Parameters),
			routeData.RouteValues.ToDictionary(pair => pair.Key, pair => pair.Value));
		builder.CloseComponent();
	};
}
