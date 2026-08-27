using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Htmxor.Endpoints;

internal sealed class HtmxorDirectComponentHost : ComponentBase
{
	[Inject]
	private IRoutingStateProvider RoutingStateProvider { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var routeData = RoutingStateProvider.RouteData
			?? throw new InvalidOperationException("The stock Razor component invoker did not initialize route data.");
		builder.AddContent(0, HtmxorDirectRenderHost.RenderRoute(routeData));
	}
}
