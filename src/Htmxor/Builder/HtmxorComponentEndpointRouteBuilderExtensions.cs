using System.Reflection;
using Htmxor.Builder;
using Htmxor.Endpoints;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#pragma warning disable IDE0130
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130

public static class HtmxorComponentEndpointRouteBuilderExtensions
{
	private static readonly RootComponentMetadata DirectRootComponent = new(typeof(HtmxorDirectRenderHost));

	public static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(endpoints);
		builder.Finally(ConfigureEndpoint);

		return builder;
	}

	// The legacy test application retains duplicate prototype endpoints until their deferred behavior is replaced.
	internal static RazorComponentsEndpointConventionBuilder AddLegacyHtmxorComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(endpoints);
		var componentTypes = builder.GetDiscoveredComponents();
		endpoints.DataSources.Add(new ComponentEndpointDataSource(componentTypes));

		return builder;
	}

	private static void ConfigureEndpoint(EndpointBuilder endpointBuilder)
	{
		if (endpointBuilder is not RouteEndpointBuilder ||
			endpointBuilder.RequestDelegate is not { } stockRequestDelegate ||
			!endpointBuilder.Metadata.OfType<ComponentTypeMetadata>().Any() ||
			!endpointBuilder.Metadata.OfType<RootComponentMetadata>().Any())
		{
			return;
		}

		endpointBuilder.RequestDelegate = context => InvokeEndpoint(context, stockRequestDelegate);
	}

	private static async Task InvokeEndpoint(HttpContext context, RequestDelegate stockRequestDelegate)
	{
		if (!HttpMethods.IsGet(context.Request.Method) ||
			context.GetHtmxContext().Request.RoutingMode is not RoutingMode.Direct)
		{
			await stockRequestDelegate(context);
			return;
		}

		var selectedEndpoint = context.GetEndpoint() as RouteEndpoint
			?? throw new InvalidOperationException("A routed Razor component endpoint must be selected before invocation.");
		// The stock invoker reads its root component from the selected endpoint.
		// Change only this request's view of that endpoint.
		context.SetEndpoint(CreateDirectEndpoint(selectedEndpoint));
		try
		{
			await stockRequestDelegate(context);
		}
		finally
		{
			context.SetEndpoint(selectedEndpoint);
		}
	}

	private static RouteEndpoint CreateDirectEndpoint(RouteEndpoint selectedEndpoint)
	{
		var requestDelegate = selectedEndpoint.RequestDelegate
			?? throw new InvalidOperationException("A routed Razor component endpoint must have a request delegate.");
		var metadata = selectedEndpoint.Metadata
			.Select(item => item is RootComponentMetadata ? DirectRootComponent : item)
			.ToArray();
		return new RouteEndpoint(
			requestDelegate,
			selectedEndpoint.RoutePattern,
			selectedEndpoint.Order,
			new EndpointMetadataCollection(metadata),
			selectedEndpoint.DisplayName);
	}

	// Instead of reimplementing the discovery logic from Blazor with all the configuration options it provides,
	// lets just steal the gather components. Can perhaps be refactored to use UnsafeAccessor for better perf.
	private static List<ComponentInfo> GetDiscoveredComponents(this RazorComponentsEndpointConventionBuilder builder)
	{
		var builderType = builder.GetType();
		var appBuilder = builderType.GetProperty("ApplicationBuilder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder);
		var appBuilderType = appBuilder!.GetType();
		var componentCollectionBuilder = appBuilderType.GetProperty("Components", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(appBuilder);
		var componentCollectionBuilderType = componentCollectionBuilder!.GetType();
		var componentInfos = componentCollectionBuilderType.GetMethod("ToComponentCollection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(componentCollectionBuilder, null);
		var componentInfoType = componentInfos!.GetType().GetElementType();
		var getComponentTypeProperty = componentInfoType!.GetProperty("ComponentType", BindingFlags.Instance | BindingFlags.Public);
		var getRenderModeProperty = componentInfoType!.GetProperty("RenderMode", BindingFlags.Instance | BindingFlags.Public);

		var componentTypes = new List<ComponentInfo>();
		foreach (var componentInfo in (Array)componentInfos)
		{
			var type = (Type)getComponentTypeProperty!.GetValue(componentInfo)!;
			var renderMode = (IComponentRenderMode?)getRenderModeProperty!.GetValue(componentInfo);
			componentTypes.Add(new ComponentInfo(type, renderMode));
		}

		return componentTypes;
	}
}
