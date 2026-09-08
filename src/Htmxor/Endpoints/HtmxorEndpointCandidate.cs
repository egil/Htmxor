// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs | reimplements
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted for the inactive issue #188 candidate from ASP.NET Core v10.0.11 at
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
// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs | reimplements
// Issue #184 relationships: reimplements RazorComponentEndpointInvoker, subclasses StaticHtmlRenderer,
// implements IRazorComponentEndpointInvoker, consumes ComponentState through supported seams, and reimplements
// the RazorComponentsServiceCollectionExtensions cascading HttpContext registration selection.

using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Htmxor.DependencyInjection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Components.RenderTree;
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
		// AddRazorComponents does not expose a supported replacement hook for its HttpContext cascade.
		// Issue #184 watches this registration shape so upstream drift is reviewed before candidate adoption.
		var stockHttpContextSuppliers = services
			.Where(IsScopedFactory)
			.Where(IsCascadingHttpContextSupplier)
			.ToArray();
		if (stockHttpContextSuppliers.Length is not 1)
		{
			throw new InvalidOperationException(
				$"Expected exactly one scoped cascading HttpContext supplier registered by AddRazorComponents, but found {stockHttpContextSuppliers.Length}. ASP.NET Core's upstream registration shape may have changed.");
		}

		services.AddSingleton(formServices);
		services.AddScoped<HtmxorEndpointCandidateRenderer>();
		services.AddScoped<HtmxorEndpointCandidateInvoker>();
		services.RemoveAll<IRazorComponentEndpointInvoker>();
		services.AddScoped<IRazorComponentEndpointInvoker>(serviceProvider =>
			serviceProvider.GetRequiredService<HtmxorEndpointCandidateInvoker>());

		services.Remove(stockHttpContextSuppliers[0]);
		services.AddCascadingValue(serviceProvider =>
			serviceProvider.GetRequiredService<HtmxorEndpointCandidateRenderer>().HttpContext);
	}

	private static bool IsScopedFactory(ServiceDescriptor service)
		=> service.Lifetime is ServiceLifetime.Scoped &&
			service.ImplementationType is null &&
			service.ImplementationFactory is not null;

	private static bool IsCascadingHttpContextSupplier(ServiceDescriptor service)
		=> service.ImplementationFactory?.Target?.ToString()?.Contains(
			typeof(HttpContext).FullName!,
			StringComparison.Ordinal) == true;
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
		renderer.InitializeStreamingRenderingFraming(context, isErrorHandler || isReexecuted);
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
			quiesceTask = renderer.DispatchSubmitEventAsync(request.HandlerName, out var isBadRequest);
			if (isBadRequest)
			{
				return;
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
			return;
		}
		if (hasPendingInitialRenderWork ?? !quiesceTask.IsCompleted)
		{
			context.Features.GetRequiredFeature<IHttpResponseBodyFeature>().DisableBuffering();
			antiforgery.GetAndStoreTokens(context);
			context.Response.Headers.ContentEncoding = "identity";
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
		htmlContent.WriteHtmlTo(writer);
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
		if (!isErrorHandler && !isReexecuted)
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
}

internal partial class HtmxorEndpointCandidateRenderer : StaticHtmlRenderer
{
	private readonly IServiceProvider services;
	private readonly EndpointRoutingStateProvider routingState;
	private HttpContext httpContext = default!;
	private NotFoundEventArgs? notFoundEventArgs;
	private int invocationSequence;
	private Guid invocationId;
	private bool webAssemblySettingsEmitted;
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
		notFoundEventArgs = null;
		invocationSequence = -1;
		invocationId = Guid.NewGuid();
		webAssemblySettingsEmitted = false;
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
		if (parentComponentId.HasValue &&
			GetComponentState(parentComponentId.Value).Component is HtmxorEndpointCandidateRenderModeBoundary boundary)
		{
			var component = componentActivator.CreateInstance(componentType);
			componentRenderModes.Add(component, boundary.RenderMode);
			return component;
		}

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
		=> component is HtmxorEndpointCandidateRenderModeBoundary boundary
			? boundary.RenderMode
			: componentRenderModes.GetValueOrDefault(component);

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
		if (marker.Type is "webassembly" or "auto" && !webAssemblySettingsEmitted)
		{
			webAssemblySettingsEmitted = true;
			var environment = services.GetRequiredService<IWebHostEnvironment>();
			var settings = new HtmxorEndpointCandidateWebAssemblySettings(
				environment.EnvironmentName,
				GetWebAssemblyEnvironmentVariables());
			output.Write("<!--Blazor-WebAssembly:");
			output.Write(JsonSerializer.Serialize(settings, HtmxorEndpointCandidateJson.Options));
			output.Write("-->");
		}
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
		return result.EndsWith('/') ? result : result += "/";
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
