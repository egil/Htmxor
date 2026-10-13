// Public DynamicComponent.Instance capture reviewed 2026-10-13 at ASP.NET Core v10.0.11,
// a5383385245bdacc20ec19f30e46090a8154d8da, and v11.0.0-rc.1.26425.128,
// c3325eeb6b47bc6383c127d4f4827dc9642a2b6e. Exact sources: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Components/src/DynamicComponent.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Rendering/RenderTreeBuilder.cs | depends-on-behaviour

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
