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
// Form coordination added for #189; exact dependency inventory: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs | reimplements
// Issue #184 relationships: reimplements RazorComponentEndpointInvoker, subclasses StaticHtmlRenderer,
// implements IRazorComponentEndpointInvoker, consumes ComponentState through supported seams, and reimplements
// the RazorComponentsServiceCollectionExtensions cascading HttpContext registration selection.

using System.Buffers;
using System.Text;
using Htmxor.DependencyInjection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RouteData = Microsoft.AspNetCore.Components.RouteData;

namespace Htmxor.Endpoints;

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
		context.Response.Headers[EnhancedNavigationHeader] = "allow";

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

		renderer.InitializeStandardComponentServices(context, pageComponent, request.HandlerName, request.Form);
		HtmlRootComponent htmlContent;
		try
		{
			htmlContent = await renderer.RenderEndpointComponentAsync(rootComponent, ParameterView.Empty);
		}
		catch (NavigationException navigationException)
		{
			context.Response.Redirect(navigationException.Location);
			return;
		}
		if (request.IsPost)
		{
			await renderer.DispatchSubmitEventAsync(request.HandlerName, out var isBadRequest);
			if (isBadRequest)
			{
				return;
			}
		}

		context.RequestServices.GetRequiredService<HtmxorEndpointCandidateFormServices>()
			.DisableTokenGenerationForCompletedResponse(context, endpoint);

		const int defaultBufferSize = 16 * 1024;
		await using var writer = new HttpResponseStreamWriter(
			context.Response.Body,
			Encoding.UTF8,
			defaultBufferSize,
			ArrayPool<byte>.Shared,
			ArrayPool<char>.Shared);
		htmlContent.WriteHtmlTo(writer);
		await writer.FlushAsync();
	}
}

internal partial class HtmxorEndpointCandidateRenderer : StaticHtmlRenderer
{
	private readonly IServiceProvider services;
	private readonly EndpointRoutingStateProvider routingState;
	private HttpContext httpContext = default!;

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

	internal void InitializeStandardComponentServices(
		HttpContext context, Type pageComponent, string? handler = null, IFormCollection? form = null)
	{
		httpContext = context;
		var navigationManager = services.GetRequiredService<NavigationManager>();
		if (navigationManager is IHostEnvironmentNavigationManager hostNavigationManager)
		{
			hostNavigationManager.Initialize(GetContextBaseUri(context.Request), GetFullUri(context.Request));
		}

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
		SetRouteData(context, pageComponent);
	}

	internal async Task<HtmlRootComponent> RenderEndpointComponentAsync(
		Type rootComponent,
		ParameterView parameters)
	{
		var result = BeginRenderingComponent(rootComponent, parameters);
		await result.QuiescenceTask;
		return result;
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
			=> serviceType == typeof(IRoutingStateProvider)
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
