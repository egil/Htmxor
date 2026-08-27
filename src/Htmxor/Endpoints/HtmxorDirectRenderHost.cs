using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Htmxor.Endpoints;

internal sealed class HtmxorDirectRenderHost : ComponentBase
{
	[Inject]
	private IRoutingStateProvider RoutingStateProvider { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var routeData = RoutingStateProvider.RouteData
			?? throw new InvalidOperationException("The stock Razor component invoker did not initialize route data.");
		// Router consumes the endpoint-selected RouteData and processes its values without matching the route again.
		builder.OpenComponent<Router>(0);
		builder.AddComponentParameter(1, nameof(Router.AppAssembly), routeData.PageType.Assembly);
		builder.AddComponentParameter(2, nameof(Router.Found), (RenderFragment<RouteData>)RenderRoute);
		builder.CloseComponent();
	}

	internal static RenderFragment RenderRoute(RouteData routeData) => builder =>
	{
		builder.OpenComponent<DynamicComponent>(0);
		builder.AddComponentParameter(1, nameof(DynamicComponent.Type), routeData.PageType);
		builder.AddComponentParameter(
			2,
			nameof(DynamicComponent.Parameters),
			routeData.RouteValues.ToDictionary(pair => pair.Key, pair => pair.Value));
		builder.CloseComponent();
	};
}
