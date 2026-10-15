// Public DynamicComponent.Instance capture reviewed 2026-10-13 at ASP.NET Core v10.0.13,
// 4099f83977f4d29604a9aeb971fb0a7bc1d5f699, and v11.0.0-rc.2.26504.105,
// 820230c386db5b0106c5430b3e5aee92a7826bf5. Exact sources: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Components/src/DynamicComponent.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Rendering/RenderTreeBuilder.cs | depends-on-behaviour

// Shipped VMR baselines re-reviewed 2026-10-15 for #329; source deltas: docs/engineering/patched-framework-baselines.md.

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

	[Inject]
	private HtmxorComponentActionRequest ActionRequest { get; set; } = default!;

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
			(RenderFragment<RouteData>)(route => RenderRoute(route, componentType,
				ActionRequest.HasExplicitAction ? ActionRequest.CaptureExplicitRouteCarrier : null)));
		builder.CloseComponent();
	}

	internal static RenderFragment RenderRoute(RouteData routeData, Action<DynamicComponent>? capture = null) => builder =>
		RenderRoute(routeData, componentType: null, capture)(builder);

	private static RenderFragment RenderRoute(RouteData routeData, Type? componentType, Action<DynamicComponent>? capture) => builder =>
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
		if (capture is not null)
		{
			builder.AddComponentReferenceCapture(3, instance => capture((DynamicComponent)instance));
		}
		builder.CloseComponent();
	};
}
