using System.ComponentModel;
using System.Reflection;
using Htmxor.Builder;
using Htmxor.Endpoints;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IDE0130
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130

public static class HtmxorComponentEndpointRouteBuilderExtensions
{
	private static readonly RootComponentMetadata PageRouteDirectRoot = new(typeof(HtmxorDirectRenderHost));
	private static readonly RootComponentMetadata HtmxOnlyDirectRoot = new(typeof(HtmxorDirectComponentHost));

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints,
		Assembly applicationAssembly,
		IReadOnlyList<string> projectRootComponentTypeNames)
		=> AddHtmxorAttributedComponentEndpoints(
			builder,
			endpoints,
			applicationAssembly,
			projectRootComponentTypeNames,
			[]);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static RazorComponentsEndpointConventionBuilder AddHtmxorAttributedComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints,
		Assembly applicationAssembly,
		IReadOnlyList<string> projectRootComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(endpoints);
		ArgumentNullException.ThrowIfNull(applicationAssembly);
		ArgumentNullException.ThrowIfNull(projectRootComponentTypeNames);
		ArgumentNullException.ThrowIfNull(generatedActions);
		HtmxorGeneratedComponentActionCatalog.Validate(
			applicationAssembly,
			projectRootComponentTypeNames,
			generatedActions);
		var descriptors = HtmxorAttributedRouteCatalog.Build(
			applicationAssembly,
			projectRootComponentTypeNames,
			generatedActions);

		AddHtmxorComponentEndpoints(builder, endpoints, [], generatedActions);
		foreach (var descriptor in descriptors)
		{
			endpoints.MapHtmxorComponentEndpoint(descriptor, generatedActions);
		}

		return builder;
	}

	internal static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
		this RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints,
		IReadOnlyList<HtmxorComponentActionDescriptor> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(endpoints);
		ArgumentNullException.ThrowIfNull(generatedActions);
		AddHtmxorComponentEndpoints(builder, endpoints, generatedActions, []);

		return builder;
	}

	private static void AddHtmxorComponentEndpoints(
		RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints,
		IReadOnlyList<HtmxorComponentActionDescriptor> actionDescriptors,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(endpoints);
		ArgumentNullException.ThrowIfNull(actionDescriptors);
		ArgumentNullException.ThrowIfNull(generatedActions);
		builder.Finally(endpointBuilder => ConfigureEndpoint(
			endpointBuilder,
			actionDescriptors,
			generatedActions));
	}

	internal static IEndpointConventionBuilder MapHtmxorComponentEndpoint(
		this IEndpointRouteBuilder endpoints,
		HtmxorComponentRouteDescriptor generatedRoute,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		ArgumentNullException.ThrowIfNull(endpoints);
		ArgumentNullException.ThrowIfNull(generatedRoute);
		ArgumentException.ThrowIfNullOrWhiteSpace(generatedRoute.NormalizedRoute);
		ArgumentNullException.ThrowIfNull(generatedRoute.Metadata);
		ArgumentNullException.ThrowIfNull(generatedActions);
		RequestDelegate requestDelegate = static context => context.RequestServices
			.GetRequiredService<IRazorComponentEndpointInvoker>()
			.Render(context);
		var builder = endpoints.MapMethods(
			generatedRoute.NormalizedRoute,
			generatedRoute.HttpMethods,
			requestDelegate);
		builder.Add(endpointBuilder => ConfigureGeneratedEndpoint(
			endpointBuilder,
			generatedRoute,
			generatedActions));

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
		IReadOnlyList<HtmxorComponentActionDescriptor> actionDescriptors,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		if (endpointBuilder is not RouteEndpointBuilder routeEndpointBuilder ||
			endpointBuilder.RequestDelegate is not { } stockRequestDelegate ||
			!endpointBuilder.Metadata.OfType<ComponentTypeMetadata>().Any() ||
			!endpointBuilder.Metadata.OfType<RootComponentMetadata>().Any())
		{
			return;
		}

		var endpointActions = GetEndpointActions(
			routeEndpointBuilder,
			actionDescriptors,
			generatedActions);
		endpointBuilder.Metadata.Add(new HtmxorComponentRoutePatternMetadata(routeEndpointBuilder.RoutePattern));
		AddActionMetadata(endpointBuilder, endpointActions);
		endpointBuilder.RequestDelegate = context => InvokeEndpoint(context, stockRequestDelegate, endpointActions);
	}

	private static void ConfigureGeneratedEndpoint(
		EndpointBuilder endpointBuilder,
		HtmxorComponentRouteDescriptor generatedRoute,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		foreach (var metadata in generatedRoute.Metadata)
		{
			endpointBuilder.Metadata.Add(metadata);
		}

		var routeEndpointBuilder = endpointBuilder as RouteEndpointBuilder
			?? throw new InvalidOperationException("An HTMX-only component endpoint must have a route pattern.");
		endpointBuilder.Metadata.Add(new HtmxorComponentRoutePatternMetadata(routeEndpointBuilder.RoutePattern));
		endpointBuilder.Metadata.Add(new SuppressLinkGenerationMetadata());
		endpointBuilder.Metadata.Add(new ComponentTypeMetadata(generatedRoute.ComponentType));
		endpointBuilder.Metadata.Add(HtmxOnlyDirectRoot);
		endpointBuilder.Metadata.Add(HtmxorDirectEndpointMetadata.Instance);
		if (generatedRoute.HttpMethods.Any(IsUnsafeMethod))
		{
			RequireAntiforgery(endpointBuilder);
		}
		var endpointActions = HtmxorGeneratedComponentActionCatalog.Bind(
			generatedRoute.ComponentType,
			generatedRoute.NormalizedRoute,
			generatedActions);
		AddRouteProcessorMetadata(endpointBuilder, generatedRoute, endpointActions);
		AddActionMetadata(endpointBuilder, endpointActions.ToArray());
		var renderDelegate = endpointBuilder.RequestDelegate
			?? throw new InvalidOperationException("An HTMX-only component endpoint must have a request delegate.");
		endpointBuilder.RequestDelegate = context => InvokeGeneratedEndpoint(
			context,
			renderDelegate,
			endpointActions);

		endpointBuilder.DisplayName =
			$"{generatedRoute.NormalizedRoute} ({generatedRoute.ComponentType.Name}) (HTMX-only component)";
	}

	private static void AddRouteProcessorMetadata(
		EndpointBuilder endpointBuilder,
		HtmxorComponentRouteDescriptor generatedRoute,
		IReadOnlyList<HtmxorComponentActionDescriptor> endpointActions)
	{
		var routeProcessors = endpointActions
			.Select(static action => action.GeneratedAction?.RouteProcessorType)
			.Where(static type => type is not null)
			.Distinct()
			.ToArray();
		if (routeProcessors.Length > 1)
		{
			throw new InvalidOperationException(
				$"Component route '{generatedRoute.NormalizedRoute}' has conflicting route processors.");
		}
		if (routeProcessors.Length == 1)
		{
			endpointBuilder.Metadata.Add(new HtmxorRouteProcessorMetadata(
				generatedRoute.ComponentType,
				routeProcessors[0]!));
		}
	}

	private static HtmxorComponentActionDescriptor[] GetEndpointActions(
		RouteEndpointBuilder endpointBuilder,
		IReadOnlyList<HtmxorComponentActionDescriptor> actionDescriptors,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		var componentType = endpointBuilder.Metadata.OfType<ComponentTypeMetadata>().Last().Type;
		var route = endpointBuilder.RoutePattern.RawText
			?? throw new InvalidOperationException("A routed Razor component endpoint must have a route pattern.");
		var endpointActions = actionDescriptors
			.Where(action =>
				action.ComponentType == componentType &&
				string.Equals(action.NormalizedRoute, route, StringComparison.Ordinal))
			.Concat(HtmxorGeneratedComponentActionCatalog.Bind(
				componentType,
				route,
				generatedActions))
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

		if (endpointActions.Any(action => IsUnsafeMethod(action.HttpMethod)))
		{
			RequireAntiforgery(endpointBuilder);
		}
	}

	private static void RequireAntiforgery(EndpointBuilder endpointBuilder)
	{
		if (endpointBuilder.Metadata.OfType<IAntiforgeryMetadata>().LastOrDefault()?.RequiresValidation != true)
		{
			endpointBuilder.Metadata.Add(new RequireAntiforgeryTokenAttribute());
		}
	}

	private static bool IsUnsafeMethod(string method)
		=> HttpMethods.IsPost(method) ||
			HttpMethods.IsPut(method) ||
			HttpMethods.IsPatch(method) ||
			HttpMethods.IsDelete(method);

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
			await InvokeStockEndpoint(context, stockRequestDelegate);
			return;
		}

		await InvokeDirectEndpoint(context, stockRequestDelegate);
		if (HttpMethods.IsGet(context.Request.Method))
		{
			AdaptLocalNavigationRedirect(context);
		}
	}

	private static void AdaptLocalNavigationRedirect(HttpContext context)
	{
		var location = context.Response.Headers.Location.ToString();
		if (context.Response.HasStarted ||
			context.Response.StatusCode != StatusCodes.Status302Found ||
			context.Response.Headers.Location.Count != 1 ||
			!Uri.TryCreate(location, UriKind.Absolute, out var redirectUri) ||
			!HasSameOrigin(context.Request, redirectUri))
		{
			return;
		}

		try
		{
			context.GetHtmxContext().Response.Redirect(redirectUri);
		}
		catch (ArgumentException)
		{
			return;
		}

		context.Response.StatusCode = StatusCodes.Status200OK;
		context.Response.Headers.Remove("Location");
	}

	private static bool HasSameOrigin(HttpRequest request, Uri redirectUri)
	{
		var redirectHost = HostString.FromUriComponent(redirectUri);
		return string.Equals(redirectUri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(redirectHost.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
			(request.Host.Port is { } requestPort
				? redirectHost.Port == requestPort
				: redirectUri.IsDefaultPort);
	}

	private static async Task InvokeActionEndpoint(
		HttpContext context,
		RequestDelegate stockRequestDelegate,
		HtmxorComponentActionDescriptor action)
	{
		if (!await TryActivateAction(context, action))
		{
			return;
		}

		if (context.GetHtmxContext().Request.RoutingMode is RoutingMode.Direct)
		{
			await InvokeDirectEndpoint(context, stockRequestDelegate);
			return;
		}

		await InvokeStockEndpoint(context, stockRequestDelegate);
	}

	private static async Task InvokeGeneratedEndpoint(
		HttpContext context,
		RequestDelegate renderDelegate,
		IReadOnlyList<HtmxorComponentActionDescriptor> endpointActions)
	{
		var action = endpointActions.SingleOrDefault(action =>
			string.Equals(action.HttpMethod, context.Request.Method, StringComparison.OrdinalIgnoreCase));
		if (action is null)
		{
			if (!IsUnsafeMethod(context.Request.Method) || await ValidateAntiforgery(context))
			{
				await InvokeGeneratedRenderEndpoint(context, renderDelegate);
			}

			return;
		}

		if (await TryActivateAction(context, action))
		{
			await InvokeGeneratedRenderEndpoint(context, renderDelegate);
		}
	}

	private static async Task InvokeGeneratedRenderEndpoint(
		HttpContext context,
		RequestDelegate renderDelegate)
	{
		var selectedEndpoint = context.GetEndpoint() as RouteEndpoint
			?? throw new InvalidOperationException("A generated component endpoint must be selected before invocation.");
		var routeProcessor = selectedEndpoint.Metadata.GetMetadata<HtmxorRouteProcessorMetadata>();
		if (routeProcessor is null)
		{
			await InvokeRenderEndpoint(context, renderDelegate);
			return;
		}

		// Present the generated processor only to the stock invoker and Router; the selected endpoint keeps owning reachability.
		context.SetEndpoint(CreateDirectEndpoint(
			selectedEndpoint,
			PageRouteDirectRoot,
			routeProcessor.ProcessorType));
		try
		{
			await InvokeRenderEndpoint(context, renderDelegate);
		}
		finally
		{
			context.SetEndpoint(selectedEndpoint);
		}
	}

	private static async Task<bool> TryActivateAction(
		HttpContext context,
		HtmxorComponentActionDescriptor action)
	{
		if (IsUnsafeMethod(action.HttpMethod) && !await ValidateAntiforgery(context))
		{
			return false;
		}

		context.RequestServices.GetRequiredService<HtmxorComponentActionRequest>().Activate(action);
		return true;
	}

	private static async Task<bool> ValidateAntiforgery(HttpContext context)
	{
		if (context.Features.Get<IAntiforgeryValidationFeature>() is { } validationFeature)
		{
			if (!validationFeature.IsValid)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return false;
			}
		}
		else
		{
			try
			{
				// Use one fail-closed path because ASP.NET Core antiforgery middleware skips DELETE.
				await context.RequestServices
					.GetRequiredService<IAntiforgery>()
					.ValidateRequestAsync(context);
			}
			catch (AntiforgeryValidationException)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return false;
			}
		}

		return true;
	}

	private static async Task InvokeDirectEndpoint(HttpContext context, RequestDelegate stockRequestDelegate)
	{
		var selectedEndpoint = context.GetEndpoint() as RouteEndpoint
			?? throw new InvalidOperationException("A routed Razor component endpoint must be selected before invocation.");
		// The stock invoker reads its root component from the selected endpoint.
		// Change only this request's view of that endpoint.
		context.SetEndpoint(CreateDirectEndpoint(
			selectedEndpoint,
			PageRouteDirectRoot,
			componentType: null));
		try
		{
			await InvokeRenderEndpoint(context, stockRequestDelegate);
		}
		finally
		{
			context.SetEndpoint(selectedEndpoint);
		}
	}

	private static async Task InvokeStockEndpoint(HttpContext context, RequestDelegate stockRequestDelegate)
	{
		var selectedEndpoint = context.GetEndpoint() as RouteEndpoint
			?? throw new InvalidOperationException("A routed Razor component endpoint must be selected before invocation.");
		var authoredRoutePattern = selectedEndpoint.Metadata
			.GetRequiredMetadata<HtmxorComponentRoutePatternMetadata>()
			.RoutePattern;
		if (ReferenceEquals(authoredRoutePattern, selectedEndpoint.RoutePattern))
		{
			await InvokeRenderEndpoint(context, stockRequestDelegate);
			return;
		}
		var stockEndpoint = CreateEndpoint(
			selectedEndpoint,
			rootComponent: null,
			componentType: null);
		context.SetEndpoint(stockEndpoint);
		try
		{
			await InvokeRenderEndpoint(context, stockRequestDelegate);
		}
		finally
		{
			context.SetEndpoint(selectedEndpoint);
		}
	}

	private static async Task InvokeRenderEndpoint(HttpContext context, RequestDelegate renderDelegate)
	{
		var htmxContext = context.GetHtmxContext();
		if (!htmxContext.Request.IsHtmxRequest)
		{
			await renderDelegate(context);
			return;
		}

		var response = context.Response;
		var originalBody = response.Body;
		htmxContext.Response.BeginRenderExecution();
		response.Body = new ConditionalResponseBodyStream(originalBody, htmxContext.Response);
		try
		{
			await renderDelegate(context);
		}
		finally
		{
			try
			{
				htmxContext.Response.CompleteRenderExecution();
			}
			finally
			{
				response.Body = originalBody;
			}
		}
	}

	private static RouteEndpoint CreateDirectEndpoint(
		RouteEndpoint selectedEndpoint,
		RootComponentMetadata rootComponent,
		Type? componentType)
		=> CreateEndpoint(selectedEndpoint, rootComponent, componentType);

	private static RouteEndpoint CreateEndpoint(
		RouteEndpoint selectedEndpoint,
		RootComponentMetadata? rootComponent,
		Type? componentType)
	{
		var requestDelegate = selectedEndpoint.RequestDelegate
			?? throw new InvalidOperationException("A routed Razor component endpoint must have a request delegate.");
		var metadata = selectedEndpoint.Metadata
			.Select(item => item switch
			{
				RootComponentMetadata when rootComponent is not null => rootComponent,
				ComponentTypeMetadata when componentType is not null => new ComponentTypeMetadata(componentType),
				_ => item,
			})
			.ToArray();
		var routePattern = selectedEndpoint.Metadata
			.GetRequiredMetadata<HtmxorComponentRoutePatternMetadata>()
			.RoutePattern;
		return new RouteEndpoint(
			requestDelegate,
			routePattern,
			selectedEndpoint.Order,
			new EndpointMetadataCollection(metadata),
			selectedEndpoint.DisplayName);
	}

	private static List<ComponentInfo> GetDiscoveredComponents(this RazorComponentsEndpointConventionBuilder builder)
		=> ComponentEndpointConventionBuilderHelper
			.GetEndpointRouteBuilder(builder)
			.DataSources
			.SelectMany(static dataSource => dataSource.Endpoints)
			.Select(static endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>())
			.Where(static metadata => metadata is not null)
			.Select(static metadata => metadata!.Type)
			.Distinct()
			.Select(static componentType => new ComponentInfo(componentType, renderMode: null))
			.ToList();
}
