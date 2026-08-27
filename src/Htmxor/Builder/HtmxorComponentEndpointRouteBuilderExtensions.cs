using System.Reflection;
using Htmxor.Builder;
using Htmxor.Endpoints;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IDE0130
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130

public static class HtmxorComponentEndpointRouteBuilderExtensions
{
	private static readonly RootComponentMetadata DirectRootComponent = new(typeof(HtmxorDirectRenderHost));

	public static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints)
		=> AddHtmxorComponentEndpoints(builder, endpoints, []);

	internal static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints,
		IReadOnlyList<HtmxorComponentActionDescriptor> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(endpoints);
		ArgumentNullException.ThrowIfNull(generatedActions);
		builder.Finally(endpointBuilder => ConfigureEndpoint(endpointBuilder, generatedActions));

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

	private static void ConfigureEndpoint(
		EndpointBuilder endpointBuilder,
		IReadOnlyList<HtmxorComponentActionDescriptor> generatedActions)
	{
		if (endpointBuilder is not RouteEndpointBuilder routeEndpointBuilder ||
			endpointBuilder.RequestDelegate is not { } stockRequestDelegate ||
			!endpointBuilder.Metadata.OfType<ComponentTypeMetadata>().Any() ||
			!endpointBuilder.Metadata.OfType<RootComponentMetadata>().Any())
		{
			return;
		}

		var endpointActions = GetEndpointActions(routeEndpointBuilder, generatedActions);
		AddActionMetadata(endpointBuilder, endpointActions);
		endpointBuilder.RequestDelegate = context => InvokeEndpoint(context, stockRequestDelegate, endpointActions);
	}

	private static HtmxorComponentActionDescriptor[] GetEndpointActions(
		RouteEndpointBuilder endpointBuilder,
		IReadOnlyList<HtmxorComponentActionDescriptor> generatedActions)
	{
		var componentType = endpointBuilder.Metadata.OfType<ComponentTypeMetadata>().Last().Type;
		var route = endpointBuilder.RoutePattern.RawText;
		var endpointActions = generatedActions
			.Where(action =>
				action.ComponentType == componentType &&
				string.Equals(action.NormalizedRoute, route, StringComparison.Ordinal))
			.ToArray();
		var duplicateMethod = endpointActions
			.GroupBy(action => action.HttpMethod, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault(group => group.Count() > 1);
		if (duplicateMethod is not null)
		{
			throw new InvalidOperationException(
				$"Component route '{route}' declares more than one '{duplicateMethod.Key}' action.");
		}

		return endpointActions;
	}

	private static void AddActionMetadata(
		EndpointBuilder endpointBuilder,
		HtmxorComponentActionDescriptor[] endpointActions)
	{
		if (endpointActions.Length == 0)
		{
			return;
		}

		var currentMethods = endpointBuilder.Metadata.OfType<HttpMethodMetadata>().LastOrDefault();
		var methods = (currentMethods?.HttpMethods ?? [])
			.Concat(endpointActions.Select(action => action.HttpMethod))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		endpointBuilder.Metadata.Add(new HttpMethodMetadata(
			methods,
			currentMethods?.AcceptCorsPreflight ?? false));
		foreach (var action in endpointActions)
		{
			endpointBuilder.Metadata.Add(action);
		}

		endpointBuilder.Metadata.Add(new RequireAntiforgeryTokenAttribute());
	}

	private static async Task InvokeEndpoint(
		HttpContext context,
		RequestDelegate stockRequestDelegate,
		IReadOnlyList<HtmxorComponentActionDescriptor> endpointActions)
	{
		var action = endpointActions.SingleOrDefault(action =>
			string.Equals(action.HttpMethod, context.Request.Method, StringComparison.OrdinalIgnoreCase));
		if (action is not null)
		{
			await InvokeActionEndpoint(context, stockRequestDelegate, action);
			return;
		}

		if ((!HttpMethods.IsGet(context.Request.Method) &&
			!HttpMethods.IsPost(context.Request.Method)) ||
			context.GetHtmxContext().Request.RoutingMode is not RoutingMode.Direct)
		{
			await stockRequestDelegate(context);
			return;
		}

		await InvokeDirectEndpoint(context, stockRequestDelegate);
	}

	private static async Task InvokeActionEndpoint(
		HttpContext context,
		RequestDelegate stockRequestDelegate,
		HtmxorComponentActionDescriptor action)
	{
		var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
		try
		{
			// Use one fail-closed path because ASP.NET Core antiforgery middleware skips DELETE.
			await antiforgery.ValidateRequestAsync(context);
		}
		catch (AntiforgeryValidationException)
		{
			context.Response.StatusCode = StatusCodes.Status400BadRequest;
			return;
		}

		context.RequestServices.GetRequiredService<HtmxorComponentActionRequest>().Activate(action);
		await InvokeDirectEndpoint(context, stockRequestDelegate);
	}

	private static async Task InvokeDirectEndpoint(HttpContext context, RequestDelegate stockRequestDelegate)
	{
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
