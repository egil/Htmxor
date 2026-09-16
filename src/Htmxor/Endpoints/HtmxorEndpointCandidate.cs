// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs | reimplements
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted for the endpoint candidate introduced in #188 from ASP.NET Core v10.0.11 at
// commit a5383385245bdacc20ec19f30e46090a8154d8da, synchronized 2026-09-05:
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs
// Form coordination added for #189; exact dependency inventory: docs/engineering/candidate-form-adapter.md.
// .NET 11 protection, persisted state, and browser configuration follow v11.0.0-rc.1.26425.128 at
// c3325eeb6b47bc6383c127d4f4827dc9642a2b6e, synchronized 2026-09-13; see that inventory.
// CacheView write-path coordination follows the same commit, synchronized 2026-09-16: the capture and
// descendant-guard branches reimplement EndpointHtmlRenderer.WriteComponentHtml; approved #219 dependencies
// and their exact sources are in docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | reimplements
// Htmxor upstream dependency: src/Shared/MiddlewareInvokedKeys.cs | mirrors
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs | reimplements
// Issue #184 relationships: reimplements RazorComponentEndpointInvoker, subclasses StaticHtmlRenderer,
// implements IRazorComponentEndpointInvoker, consumes ComponentState through supported seams, and reimplements
// the RazorComponentsServiceCollectionExtensions cascading HttpContext registration selection.

using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Htmxor.Components;
using Htmxor.DependencyInjection;
using Htmxor.Http;
using Htmxor.Rendering;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using RouteData = Microsoft.AspNetCore.Components.RouteData;

namespace Htmxor.Endpoints;

internal sealed record HtmxorEndpointCandidateWebAssemblySettings(
	string EnvironmentName,
	Dictionary<string, string> EnvironmentVariables);

internal static class HtmxorEndpointCandidateServices
{
	public static void Add(IServiceCollection services)
	{
		var formServices = HtmxorEndpointCandidateFormServices.Create();
#if NET11_0_OR_GREATER
		var sessionServices = HtmxorEndpointCandidateSessionServices.Create();
		var tempDataServices = HtmxorEndpointCandidateTempDataServices.Create();
		var cacheViewServices = HtmxorEndpointCandidateCacheViewServices.Create();
#endif
		// AddRazorComponents does not expose a supported replacement hook for its HttpContext cascade.
		// Issue #184 watches this registration shape so upstream drift is reviewed before adopting framework changes.
		var stockHttpContextSuppliers = services
			.Where(IsScopedFactory)
			.Where(IsCascadingHttpContextSupplier)
			.ToArray();
		if (stockHttpContextSuppliers.Length is not 1)
		{
			throw new InvalidOperationException(
				$"Expected exactly one scoped cascading HttpContext supplier registered by AddRazorComponents, but found {stockHttpContextSuppliers.Length}. ASP.NET Core's upstream registration shape may have changed.");
		}

#if NET11_0_OR_GREATER
		// AddTempData cascades ITempData through the stock EndpointHtmlRenderer's request, which the candidate
		// replaces. Issue #184 watches this registration shape so upstream drift is reviewed before adopting it.
		var stockTempDataSuppliers = services
			.Where(IsScopedFactory)
			.Where(IsCascadingTempDataSupplier)
			.ToArray();
		if (stockTempDataSuppliers.Length is not 1)
		{
			throw new InvalidOperationException(
				$"Expected exactly one scoped cascading {nameof(ITempData)} supplier registered by AddRazorComponents, but found {stockTempDataSuppliers.Length}. ASP.NET Core's upstream registration shape may have changed.");
		}
#endif

		services.AddSingleton(formServices);
#if NET11_0_OR_GREATER
		services.AddSingleton(sessionServices);
		services.AddSingleton(tempDataServices);
		services.AddSingleton(cacheViewServices);
#endif
		services.AddScoped<HtmxorEndpointCandidateRenderer>();
		services.AddScoped<HtmxorEndpointCandidateInvoker>();
		services.RemoveAll<IRazorComponentEndpointInvoker>();
		services.AddScoped<IRazorComponentEndpointInvoker>(serviceProvider =>
			serviceProvider.GetRequiredService<HtmxorEndpointCandidateInvoker>());

		services.Remove(stockHttpContextSuppliers[0]);
		services.AddCascadingValue(serviceProvider =>
			serviceProvider.GetRequiredService<HtmxorEndpointCandidateRenderer>().HttpContext);
#if NET11_0_OR_GREATER
		services.Remove(stockTempDataSuppliers[0]);
		services.AddCascadingValue(serviceProvider =>
		{
			var httpContext = serviceProvider.GetRequiredService<HtmxorEndpointCandidateRenderer>().HttpContext;
			return httpContext is null ? null : tempDataServices.GetOrCreate(httpContext);
		});
#endif
	}

	private static bool IsScopedFactory(ServiceDescriptor service)
		=> service.Lifetime is ServiceLifetime.Scoped &&
			service.ImplementationType is null &&
			service.ImplementationFactory is not null;

	private static bool IsCascadingHttpContextSupplier(ServiceDescriptor service)
		=> service.ImplementationFactory?.Target?.ToString()?.Contains(
			typeof(HttpContext).FullName!,
			StringComparison.Ordinal) == true;

#if NET11_0_OR_GREATER
	private static bool IsCascadingTempDataSupplier(ServiceDescriptor service)
		=> service.ImplementationFactory?.Target?.ToString()?.Contains(
			typeof(ITempData).FullName!,
			StringComparison.Ordinal) == true;
#endif
}

internal sealed class HtmxorEndpointCandidateInvoker(HtmxorEndpointCandidateRenderer renderer)
	: IRazorComponentEndpointInvoker
{
	private const string DefaultContentType = "text/html; charset=utf-8";
	private const string EnhancedNavigationHeader = "blazor-enhanced-nav";

	public Task Render(HttpContext context)
		=> renderer.Dispatcher.InvokeAsync(() => RenderComponentCore(context));

	private async Task RenderComponentCore(HttpContext context)
	{
		context.Response.ContentType = DefaultContentType;
		var isErrorHandler = context.Features.Get<IExceptionHandlerFeature>() is not null;
		var isReexecuted = context.Features.Get<IStatusCodeReExecuteFeature>() is not null;
		renderer.InitializeStreamingRenderingFraming(context,
			isErrorHandler || isReexecuted || context.GetHtmxContext().Request.RoutingMode is RoutingMode.Direct);
		if (!isReexecuted)
		{
			context.Response.Headers[EnhancedNavigationHeader] = "allow";
		}

		var endpoint = context.GetEndpoint()
			?? throw new InvalidOperationException($"An endpoint must be set on the '{nameof(HttpContext)}'.");
		var rootComponent = endpoint.Metadata.GetRequiredMetadata<RootComponentMetadata>().Type;
		var pageComponent = endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type;

		var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
		var antiforgeryMetadata = endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>();
		var request = await HtmxorEndpointCandidateFormRequest.ValidateAsync(
			context, antiforgeryMetadata?.RequiresValidation == true ? antiforgery : null);
		if (!request.IsValid)
		{
			return;
		}

		await renderer.InitializeStandardComponentServicesAsync(context, pageComponent, request.HandlerName, request.Form);
		HtmlRootComponent htmlContent;
		try
		{
			htmlContent = renderer.BeginRenderEndpointComponent(rootComponent, ParameterView.Empty);
		}
		catch (NavigationException navigationException)
		{
			// Deliberately still the bare stock redirect, and deliberately still before write-back: unifying this
			// with the submit path also has to stop discarding Session and TempData values, which needs its own
			// protected behavior and evidence. Tracked by #230.
			context.Response.Redirect(navigationException.Location);
			return;
		}
		Task quiesceTask;
		bool? hasPendingInitialRenderWork = null;
		if (!request.IsPost || isReexecuted)
		{
			quiesceTask = htmlContent.QuiescenceTask;
			await renderer.WaitForNonStreamingPendingTasks();
			hasPendingInitialRenderWork = !quiesceTask.IsCompleted && renderer.HasStreamingComponent;
		}
		else
		{
			await htmlContent.QuiescenceTask;
			try
			{
				quiesceTask = renderer.DispatchSubmitEventAsync(request.HandlerName, out var isBadRequest);
				if (isBadRequest)
				{
					return;
				}

				await renderer.WaitForNonStreamingPendingTasks();
			}
			catch (NavigationException navigationException)
			{
				// Stock redirects a completed submit and still runs TempData and Session write-back, so a
				// post-redirect-get message survives.
				HandleNavigationBeforeResponseStarted(context, navigationException);
				quiesceTask = Task.CompletedTask;
			}
		}
		if (!renderer.HasStreamingComponent)
		{
			await quiesceTask;
		}

		if (renderer.NotFoundEventArgs is not null)
		{
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.Response.ContentType = null;
#if NET11_0_OR_GREATER
			// Completed root components retain Session and TempData updates even when the 404 suppresses output.
			if (quiesceTask.IsCompletedSuccessfully)
			{
				await context.RequestServices.GetRequiredService<HtmxorEndpointCandidateSessionServices>().PersistAsync(context);
				context.RequestServices.GetRequiredService<HtmxorEndpointCandidateTempDataServices>().Persist(context);
			}
#endif
			return;
		}
		if (hasPendingInitialRenderWork ?? !quiesceTask.IsCompleted)
		{
			PrepareStreamingResponse(context, antiforgery);
		}
		else
		{
			context.RequestServices.GetRequiredService<HtmxorEndpointCandidateFormServices>()
				.DisableTokenGenerationForCompletedResponse(context, endpoint);
		}

		const int defaultBufferSize = 16 * 1024;
		await using var writer = new HttpResponseStreamWriter(
			context.Response.Body,
			Encoding.UTF8,
			defaultBufferSize,
			ArrayPool<byte>.Shared,
			ArrayPool<char>.Shared);
		renderer.WriteResponseHtml(htmlContent, context.GetHtmxContext(), writer);
		if (hasPendingInitialRenderWork ?? !quiesceTask.IsCompletedSuccessfully)
		{
			await renderer.SendStreamingUpdatesAsync(context, quiesceTask, writer);
			if (renderer.NotFoundEventArgs is not null)
			{
				renderer.WriteNotFoundAfterResponseStarted(context, writer);
			}
		}
		else
		{
			renderer.EmitInitializersIfNecessary(context, writer);
		}
#if NET11_0_OR_GREATER
		// Selection must not suppress write-back from completed component work outside the selected HTML.
		await context.RequestServices.GetRequiredService<HtmxorEndpointCandidateSessionServices>().PersistAsync(context);
		context.RequestServices.GetRequiredService<HtmxorEndpointCandidateTempDataServices>().Persist(context);
#endif
		if (!isErrorHandler
#if !NET11_0_OR_GREATER
			&& !isReexecuted
#endif
			)
		{
			var modes = context.RequestServices.GetRequiredService<HtmxorEndpointCandidateFormServices>()
				.GetConfiguredRenderModes(endpoint);
			if (modes.Length > 0)
			{
				await renderer.WritePersistedStateAsync(writer, modes);
			}
		}
		await writer.FlushAsync();
	}

	// Mirrors stock EndpointHtmlRenderer.HandleNavigationBeforeResponseStarted, adding only the htmx branch:
	// an htmx request cannot follow a 302 for a partial response, so Htmxor asks the client to navigate instead.
	private static void HandleNavigationBeforeResponseStarted(HttpContext context, NavigationException navigationException)
	{
		var destination = navigationException.Location;
		var htmxContext = context.GetHtmxContext();
		if (htmxContext.Request.IsHtmxRequest && IsHttpDestination(destination))
		{
			htmxContext.Response.Redirect(new Uri(ToClientDestination(context.Request, destination), UriKind.RelativeOrAbsolute));
			return;
		}

		if (IsPossibleExternalDestination(context.Request, destination) &&
			HtmxorEndpointCandidateFormServices.IsProgressivelyEnhancedNavigation(context.Request))
		{
			// Enhanced navigation prefers an opaque redirection for an external URL, so post-redirect-get keeps
			// working without forcing the request to be retried.
			context.Response.Headers.Append(
				"blazor-enhanced-nav-redirect-location",
				OpaqueRedirection.CreateProtectedRedirectionUrl(context, destination));
			return;
		}

		context.Response.Redirect(destination);
	}

	// Prefer the request-relative form, which the absolute location's path and query already carry complete
	// with the path base. A relative reference carrying a fragment is not a well-formed URI, so a destination
	// with one keeps its absolute form rather than losing the anchor.
	private static string ToClientDestination(HttpRequest request, string destination)
		=> IsPossibleExternalDestination(request, destination) ||
			!Uri.TryCreate(destination, UriKind.Absolute, out var absolute) ||
			absolute.Fragment.Length > 0
				? destination
				: absolute.PathAndQuery;

	// HX-Redirect only accepts http(s); any other scheme keeps the stock representation instead of failing.
	private static bool IsHttpDestination(string destination)
		=> !Uri.TryCreate(destination, UriKind.Absolute, out var absolute) ||
			absolute.Scheme is "http" or "https";

	private static bool IsPossibleExternalDestination(HttpRequest request, string destination)
		=> Uri.TryCreate(destination, UriKind.Absolute, out var absolute) &&
			(!string.Equals(absolute.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(absolute.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase));

	private static void PrepareStreamingResponse(HttpContext context, IAntiforgery antiforgery)
	{
		context.Features.GetRequiredFeature<IHttpResponseBodyFeature>().DisableBuffering();
#if NET11_0_OR_GREATER
		if (context.Items.ContainsKey("__AntiforgeryMiddlewareWithEndpointInvoked"))
#endif
		{
			antiforgery.GetAndStoreTokens(context);
		}
		context.Response.Headers.ContentEncoding = "identity";
	}
}

internal partial class HtmxorEndpointCandidateRenderer : StaticHtmlRenderer
{
	private readonly IServiceProvider services;
	private readonly EndpointRoutingStateProvider routingState;
	private HttpContext httpContext = default!;
	private NotFoundEventArgs? notFoundEventArgs;
	private int invocationSequence;
	private Guid invocationId;
	private bool browserSettingsEmitted;
	private ResourceAssetCollection? resourceCollection;
	private readonly Dictionary<IComponent, IComponentRenderMode> componentRenderModes = new(ReferenceEqualityComparer.Instance);

	public HtmxorEndpointCandidateRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
		: this(services, loggerFactory, new EndpointRoutingStateProvider())
	{
	}

	private HtmxorEndpointCandidateRenderer(
		IServiceProvider services,
		ILoggerFactory loggerFactory,
		EndpointRoutingStateProvider routingState)
		: base(new CandidateComponentServiceProvider(services, routingState), loggerFactory)
	{
		this.services = services;
		this.routingState = routingState;
	}

	internal HttpContext? HttpContext => httpContext;

	internal NotFoundEventArgs? NotFoundEventArgs => notFoundEventArgs;

	internal async Task InitializeStandardComponentServicesAsync(
		HttpContext context, Type pageComponent, string? handler = null, IFormCollection? form = null)
	{
		httpContext = context;
		context.GetHtmxContext().UsesCompletedFragmentSelection = true;
		renderedFragments.Clear();
		notFoundEventArgs = null;
		invocationSequence = -1;
		invocationId = Guid.NewGuid();
		browserSettingsEmitted = false;
		componentRenderModes.Clear();
		services.GetRequiredService<HtmxorEndpointCandidateFormServices>().InitializeResourceCollection(context);
		var navigationManager = services.GetRequiredService<NavigationManager>();
		if (navigationManager is IHostEnvironmentNavigationManager hostNavigationManager)
		{
			hostNavigationManager.Initialize(GetContextBaseUri(context.Request), GetFullUri(context.Request));
		}
		navigationManager.OnNotFound += (_, args) => notFoundEventArgs = args;

		var authenticationStateProvider = services.GetService<AuthenticationStateProvider>();
		if (authenticationStateProvider is IHostEnvironmentAuthenticationStateProvider hostAuthenticationStateProvider)
		{
			hostAuthenticationStateProvider.SetAuthenticationState(
				Task.FromResult(new AuthenticationState(context.User)));
		}

		if (authenticationStateProvider is not null)
		{
			var authenticationState = authenticationStateProvider.GetAuthenticationStateAsync();
			foreach (var listener in services.GetServices<IHostEnvironmentAuthenticationStateProvider>())
			{
				listener.SetAuthenticationState(authenticationState);
			}
		}

		services.GetRequiredService<HtmxorEndpointCandidateFormServices>().Initialize(context, handler, form);
#if NET11_0_OR_GREATER
		services.GetRequiredService<HtmxorEndpointCandidateSessionServices>().Initialize(context);
		services.GetRequiredService<HtmxorEndpointCandidateTempDataServices>().Initialize(context);
#endif
		var stateManager = services.GetRequiredService<ComponentStatePersistenceManager>();
		stateManager.SetPlatformRenderMode(RenderMode.InteractiveAuto);
		await stateManager.RestoreStateAsync(new HtmxorEndpointCandidateStateStore());
		SetRouteData(context, pageComponent);
	}

	internal HtmlRootComponent BeginRenderEndpointComponent(
		Type rootComponent,
		ParameterView parameters)
		=> BeginRenderingComponent(rootComponent, parameters);

	internal async Task<HtmlRootComponent> RenderEndpointComponentAsync(
		Type rootComponent,
		ParameterView parameters)
	{
		var result = BeginRenderEndpointComponent(rootComponent, parameters);
		await result.QuiescenceTask;
		return result;
	}

	protected override IComponent ResolveComponentForRenderMode(
		Type componentType,
		int? parentComponentId,
		IComponentActivator componentActivator,
		IComponentRenderMode renderMode)
	{
#if NET11_0_OR_GREATER
		if (parentComponentId.HasValue &&
			FindRenderModeBoundary(GetComponentState(parentComponentId.Value)) is not null)
		{
			return componentActivator.CreateInstance(componentType);
		}
#else
		if (parentComponentId.HasValue &&
			GetComponentState(parentComponentId.Value).Component is HtmxorEndpointCandidateRenderModeBoundary boundary)
		{
			var component = componentActivator.CreateInstance(componentType);
			componentRenderModes.Add(component, boundary.RenderMode);
			return component;
		}
#endif

		return new HtmxorEndpointCandidateRenderModeBoundary(
			httpContext,
			services.GetRequiredService<HtmxorEndpointCandidateFormServices>(),
			componentType,
			renderMode);
	}

	protected override void WriteComponentHtml(int componentId, TextWriter output)
		=> WriteComponentHtml(componentId, output, 0, null, allowStreamingMarkers: true);

	protected override void RenderChildComponent(TextWriter output, ref RenderTreeFrame componentFrame)
		=> WriteComponentHtml(componentFrame.ComponentId, output, componentFrame.Sequence, componentFrame.ComponentKey, allowStreamingMarkers: true);

	protected override IComponentRenderMode? GetComponentRenderMode(IComponent component)
#if NET11_0_OR_GREATER
		=> FindRenderModeBoundary(GetComponentState(component))?.RenderMode;
#else
		=> component is HtmxorEndpointCandidateRenderModeBoundary boundary
			? boundary.RenderMode
			: componentRenderModes.GetValueOrDefault(component);
#endif

#if NET11_0_OR_GREATER
	private static HtmxorEndpointCandidateRenderModeBoundary? FindRenderModeBoundary(ComponentState state)
	{
		for (ComponentState? current = state; current is not null; current = current.ParentComponentState)
		{
			if (current.Component is HtmxorEndpointCandidateRenderModeBoundary boundary)
			{
				return boundary;
			}
		}

		return null;
	}
#endif

	protected override ResourceAssetCollection Assets
		=> resourceCollection ??= httpContext.GetEndpoint()?.Metadata.GetMetadata<ResourceAssetCollection>() ?? base.Assets;

	internal void EmitInitializersIfNecessary(HttpContext context, TextWriter writer)
	{
		var initializers = services.GetRequiredService<HtmxorEndpointCandidateFormServices>().GetJavaScriptInitializers(
			services.GetRequiredService<IOptions<RazorComponentsServiceOptions>>().Value);
		if (initializers is not null && !HtmxorEndpointCandidateFormServices.IsProgressivelyEnhancedNavigation(context.Request))
		{
			writer.Write("<!--Blazor-Web-Initializers:");
			writer.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(initializers)));
			writer.Write("-->");
		}
	}

	internal async Task WritePersistedStateAsync(TextWriter writer, IComponentRenderMode[] configuredModes)
	{
		var manager = services.GetRequiredService<ComponentStatePersistenceManager>();
		if (configuredModes.Length == 1)
		{
			switch (configuredModes[0])
			{
				case Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode:
					var server = new HtmxorEndpointCandidateProtectedStateStore(services.GetRequiredService<IDataProtectionProvider>());
					await manager.PersistStateAsync(server, this);
					await WritePersistedStateMarkersAsync(writer, server, null);
					return;
				case Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode:
					var webAssembly = new HtmxorEndpointCandidateStateStore();
					await manager.PersistStateAsync(webAssembly, this);
					await WritePersistedStateMarkersAsync(writer, null, webAssembly);
					return;
				default:
					throw new InvalidOperationException("Invalid configured render mode.");
			}
		}

		var composite = new HtmxorEndpointCandidateCompositeStateStore();
		await manager.PersistStateAsync(composite, this);
		foreach (var entry in composite.Auto.Saved)
		{
			composite.Server.Saved.Add(entry.Key, entry.Value);
			composite.WebAssembly.Saved.Add(entry.Key, entry.Value);
		}

		var protectedStore = new HtmxorEndpointCandidateProtectedStateStore(services.GetRequiredService<IDataProtectionProvider>());
		var plainStore = new HtmxorEndpointCandidateStateStore();
		await Task.WhenAll(
			composite.Server.Saved.Count == 0 ? Task.CompletedTask : protectedStore.PersistStateAsync(composite.Server.Saved),
			composite.WebAssembly.Saved.Count == 0 ? Task.CompletedTask : plainStore.PersistStateAsync(composite.WebAssembly.Saved));
		await WritePersistedStateMarkersAsync(
			writer,
			composite.Server.Saved.Count == 0 ? null : protectedStore,
			composite.WebAssembly.Saved.Count == 0 ? null : plainStore);
	}

	private static async Task WritePersistedStateMarkersAsync(
		TextWriter writer,
		HtmxorEndpointCandidateProtectedStateStore? server,
		HtmxorEndpointCandidateStateStore? webAssembly)
	{
		if (server?.PersistedState is not null)
		{
			await writer.WriteAsync("<!--Blazor-Server-Component-State:");
			await writer.WriteAsync(server.PersistedState);
			await writer.WriteAsync("-->");
		}
		if (webAssembly?.PersistedState is not null)
		{
			await writer.WriteAsync("<!--Blazor-WebAssembly-Component-State:");
			await writer.WriteAsync(webAssembly.PersistedState);
			await writer.WriteAsync("-->");
		}
	}

	private void WriteComponentHtml(int componentId, TextWriter output, int sequence, object? key, bool allowStreamingMarkers = true)
	{
		visitedComponentIdsInCurrentStreamingBatch.Add(componentId);
#if NET11_0_OR_GREATER
		if (GetComponentState(componentId).Component is CacheView cacheView && TryWriteCacheView(cacheView, componentId, output))
		{
			return;
		}

		var pausedCapture = TryPauseCaptureForUncacheableComponent(componentId, output);
		try
		{
			WriteComponentHtmlCore(componentId, output, sequence, key, allowStreamingMarkers);
		}
		finally
		{
			if (pausedCapture is { } paused)
			{
				CacheViewServices.ResumeCapture(paused);
			}
		}
#else
		WriteComponentHtmlCore(componentId, output, sequence, key, allowStreamingMarkers);
#endif
	}

	private void WriteComponentHtmlCore(int componentId, TextWriter output, int sequence, object? key, bool allowStreamingMarkers)
	{
		if (GetComponentState(componentId).Component is not HtmxorEndpointCandidateRenderModeBoundary boundary)
		{
			WriteStaticComponentHtml(componentId, output, allowStreamingMarkers);
			return;
		}
		if (httpContext.Features.Get<IExceptionHandlerFeature>() is not null)
		{
			WriteStaticComponentHtml(componentId, output, allowStreamingMarkers);
			return;
		}

		var marker = boundary.CreateMarker(httpContext, sequence, key, ++invocationSequence, invocationId);
		if (marker.Type is "server" or "auto")
		{
			httpContext.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
		}
#if NET11_0_OR_GREATER
		EmitBrowserConfigurationOnce(output);
#else
		if (marker.Type is "webassembly" or "auto" && !browserSettingsEmitted)
		{
			browserSettingsEmitted = true;
			var environment = services.GetRequiredService<IWebHostEnvironment>();
			var settings = new HtmxorEndpointCandidateWebAssemblySettings(
				environment.EnvironmentName,
				GetWebAssemblyEnvironmentVariables());
			output.Write("<!--Blazor-WebAssembly:");
			output.Write(JsonSerializer.Serialize(settings, HtmxorEndpointCandidateJson.Options));
			output.Write("-->");
		}
#endif
		output.Write("<!--Blazor:");
		output.Write(JsonSerializer.Serialize(marker, HtmxorEndpointCandidateJson.Options));
		output.Write("-->");
		WriteStaticComponentHtml(componentId, output, allowStreamingMarkers);
		if (marker.PrerenderId is not null)
		{
			output.Write("<!--Blazor:{\"prerenderId\":\"");
			output.Write(marker.PrerenderId);
			output.Write("\"}-->");
		}
	}

#if NET11_0_OR_GREATER
	// True once the capture in progress has met content it must not store, so that boundary discards its entry
	// instead of caching something that would be wrong to replay.
	private bool captureAbandoned;

	private HtmxorEndpointCandidateCacheViewServices? cacheViewServices;

	private HtmxorEndpointCandidateCacheViewServices CacheViewServices
		=> cacheViewServices ??= services.GetRequiredService<HtmxorEndpointCandidateCacheViewServices>();

	// Saved and restored rather than simply cleared. The framework refuses a CacheView nested inside a
	// *capturing* writer, but not one inside a paused region, so an inner boundary must not hand its own clean
	// state back to the outer one that already met content it refused to store.
	private bool TryWriteCacheView(CacheView cacheView, int componentId, TextWriter output)
	{
		var enclosing = captureAbandoned;

		// A boundary beneath an interactive one would capture prerendered interactive content and key it more
		// weakly than stock, so it stores nothing. It still begins and discards a capture rather than skipping
		// one, because that is what makes stock's refusal guard run over what it holds.
		captureAbandoned = HasRenderModeBoundaryAncestor(GetComponentState(componentId));
		try
		{
			return CacheViewServices.TryWrite(
				services,
				cacheView,
				output,
				target => base.WriteComponentHtml(componentId, target),
				() => !captureAbandoned);
		}
		finally
		{
			captureAbandoned = enclosing;
		}
	}

	private static bool HasRenderModeBoundaryAncestor(ComponentState componentState)
	{
		for (var ancestor = componentState.ParentComponentState; ancestor is not null; ancestor = ancestor.ParentComponentState)
		{
			if (ancestor.Component is HtmxorEndpointCandidateRenderModeBoundary)
			{
				return true;
			}
		}

		return false;
	}

	// Mirrors stock's second CacheView block: during an active capture, a component whose output depends on
	// per-request state is excluded from the entry and recorded so a later hit renders it live instead.
	private object? TryPauseCaptureForUncacheableComponent(int componentId, TextWriter output)
	{
		if (!CacheViewServices.IsActiveCapture(output, out var writer) || writer is null)
		{
			return null;
		}

		var componentState = GetComponentState(componentId);
		var component = componentState.Component;

		// Three reasons a subtree must not be stored. A *named* fragment is registered for selection when its
		// component is constructed, which serving stored output never does; an unnamed one cannot be selected
		// and is ordinary content. An IConditionalRender decides whether to produce markup from the request
		// itself — HtmxAsyncLoad varies on the trigger and target elements, which no cache key carries — so
		// replaying it would hand one request another's markup. An interactive boundary is outside this slice.
		// Each renders normally and is never stored.
		if (component is HtmxFragment { Name: not null } or IConditionalRender or HtmxorEndpointCandidateRenderModeBoundary)
		{
			captureAbandoned = true;
			return null;
		}

		// Upstream also conjoins allowBoundaryMarkers here. The only caller passing it false writes to the
		// streaming-update writer, never a capture writer, so the term cannot change this decision today; it is
		// left out rather than carried as a condition that is always true at this point.
		// Upstream pauses on the inherited value, not this component's own attribute. Using the own-type answer
		// descended into a subtree stock had already paused, and validated content stock never validates, so a
		// streaming page carrying an AuthorizeView under an ordinary wrapper failed where stock renders it.
		if (CacheViewServices.IsCacheable(writer, component.GetType()) && !IsInStreamingContext(componentId))
		{
			return null;
		}

		CacheViewServices.PauseCapture(writer);
		if (!CacheViewServices.IsValidationOnlyCapture(writer))
		{
			CacheViewServices.CreateLiveCachedComponent(
				services, writer, component.GetType(), null, CaptureParameterFrames(componentState));
		}

		return writer;
	}

	private RenderTreeFrame[] CaptureParameterFrames(ComponentState componentState)
	{
		if (componentState.ParentComponentState is { } parent)
		{
			var frames = GetCurrentRenderTreeFrames(parent.ComponentId);
			for (var index = 0; index < frames.Count; index++)
			{
				ref readonly var frame = ref frames.Array[index];
				if (frame.FrameType is RenderTreeFrameType.Component && ReferenceEquals(frame.Component, componentState.Component))
				{
					var slice = new RenderTreeFrame[frame.ComponentSubtreeLength];
					Array.Copy(frames.Array, index, slice, 0, slice.Length);
					return slice;
				}
			}
		}

		throw new InvalidOperationException(
			$"CacheView could not locate the live cached component '{componentState.Component.GetType().FullName}' in its parent's render tree.");
	}
#endif

	private void WriteStaticComponentHtml(int componentId, TextWriter output, bool allowStreamingMarkers)
	{
		var streaming = allowStreamingMarkers && IsStreamingComponent(componentId);
		if (streaming)
		{
			output.Write("<!--bl:");
			output.Write(componentId);
			output.Write("-->");
		}
		base.WriteComponentHtml(componentId, output);
		if (streaming)
		{
			output.Write("<!--/bl:");
			output.Write(componentId);
			output.Write("-->");
		}
	}

	private static Dictionary<string, string> GetWebAssemblyEnvironmentVariables()
	{
		var result = new Dictionary<string, string>();
		AddEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES", result);
		AddEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS", result);
		return result;
	}

	private static void AddEnvironmentVariable(string name, Dictionary<string, string> target)
	{
		if (Environment.GetEnvironmentVariable(name) is { Length: > 0 } value)
		{
			target.Add(name, value);
		}
	}

	private static class HtmxorEndpointCandidateJson
	{
		internal static JsonSerializerOptions Options { get; } = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};
	}

	internal void WriteCompletedComponentHtml(int componentId, TextWriter output)
		=> WriteComponentHtml(componentId, output);

	private void SetRouteData(HttpContext context, Type pageComponent)
	{
		routingState.RouteData = new RouteData(pageComponent, context.GetRouteData().Values);
		if (context.GetEndpoint() is RouteEndpoint routeEndpoint)
		{
			routingState.RoutePattern = routeEndpoint.RoutePattern;
			routingState.RouteData.Template = routeEndpoint.RoutePattern.RawText;
		}
	}

	private static string GetFullUri(HttpRequest request)
		=> UriHelper.BuildAbsolute(
			request.Scheme,
			request.Host,
			request.PathBase,
			request.Path,
			request.QueryString);

	private static string GetContextBaseUri(HttpRequest request)
	{
		var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);
#if NET11_0_OR_GREATER
		return result.EndsWith('/', StringComparison.Ordinal) ? result : result += "/";
#else
		return result.EndsWith('/') ? result : result += "/";
#endif
	}

	private sealed class CandidateComponentServiceProvider(
		IServiceProvider services,
		EndpointRoutingStateProvider routingState)
		: IServiceProvider, IServiceProviderIsService, IKeyedServiceProvider, IServiceProviderIsKeyedService
	{
		public object? GetService(Type serviceType)
			=> serviceType == typeof(IServiceProvider)
				? this
				: serviceType == typeof(IRoutingStateProvider)
					? routingState
					: services.GetService(serviceType);

		public bool IsService(Type serviceType)
			=> serviceType == typeof(IRoutingStateProvider) ||
				((IServiceProviderIsService)services).IsService(serviceType);

		public object? GetKeyedService(Type serviceType, object? serviceKey)
			=> ((IKeyedServiceProvider)services).GetKeyedService(serviceType, serviceKey);

		public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
			=> ((IKeyedServiceProvider)services).GetRequiredKeyedService(serviceType, serviceKey);

		public bool IsKeyedService(Type serviceType, object? serviceKey)
			=> ((IServiceProviderIsKeyedService)services).IsKeyedService(serviceType, serviceKey);
	}
}
