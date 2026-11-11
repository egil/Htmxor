// Public DynamicComponent.Instance capture reviewed 2026-10-13 at ASP.NET Core v10.0.15,
// 8e0f8ed2312954727f8919d7d1c2f78b483b4ee7, and v11.0.0,
// 297acf930fb0f5c07fd4e51d78e521b4ad2e5d1f. Exact sources: docs/engineering/candidate-form-adapter.md.
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
		var pageType = componentType ?? routeData.PageType;
		builder.OpenComponent<LayoutView>(0);
		builder.AddComponentParameter(1, nameof(LayoutView.Layout), ComponentInfo.ResolveLayoutType(pageType));
		builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), RenderComponent(routeData, pageType, capture));
		builder.CloseComponent();
	};

	private static RenderFragment RenderComponent(RouteData routeData, Type componentType, Action<DynamicComponent>? capture) => builder =>
	{
		builder.OpenComponent<DynamicComponent>(0);
		builder.AddComponentParameter(
			1,
			nameof(DynamicComponent.Type),
			componentType);
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
