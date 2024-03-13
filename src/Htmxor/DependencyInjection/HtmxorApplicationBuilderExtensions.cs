using Htmxor.Antiforgery;
using Htmxor.Builder;
using Htmxor.Configuration;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// This class has extension methods for <see cref="IHostApplicationBuilder"/> and <see cref="IApplicationBuilder"/> 
/// that enable configuration of Htmx in the application.
/// </summary>
public static class HtmxorApplicationBuilderExtensions
{
    /// <summary>
    /// Add and configure Htmx.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configurHtmx"></param>
    public static IServiceCollection AddHtmx(this IServiceCollection services, Action<HtmxConfig>? configurHtmx = null)
    {
        services.AddSingleton<HtmxConfig>(x =>
        {
            var config = new HtmxConfig
            {
                Antiforgery = new HtmxAntiforgeryOptions(x.GetRequiredService<IOptions<AntiforgeryOptions>>()),
            };
            configurHtmx?.Invoke(config);
            return config;
        });

        services.AddScoped(srv => srv.GetRequiredService<IHttpContextAccessor>().HttpContext!.GetHtmxContext());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, HtmxorComponentEndpointMatcherPolicy>());

        // services.TryAddScoped<IHtmxorComponentEndpointInvoker, HtmxorComponentEndpointInvoker>();

        // Common services required for components server side rendering
        //services.TryAddSingleton<ServerComponentSerializer>(services => new ServerComponentSerializer(services.GetRequiredService<IDataProtectionProvider>()));
        //services.TryAddScoped<EndpointHtmxorRenderer>();
        //services.TryAddScoped<IComponentPrerenderer>(services => services.GetRequiredService<EndpointHtmlRenderer>());
        //services.TryAddScoped<NavigationManager, HttpNavigationManager>();
        //services.TryAddScoped<IJSRuntime, UnsupportedJavaScriptRuntime>();
        //services.TryAddScoped<INavigationInterception, UnsupportedNavigationInterception>();
        //services.TryAddScoped<IScrollToLocationHash, UnsupportedScrollToLocationHash>();
        //services.TryAddScoped<ComponentStatePersistenceManager>();
        //services.TryAddScoped<PersistentComponentState>(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        //services.TryAddScoped<IErrorBoundaryLogger, PrerenderingErrorBoundaryLogger>();
        //services.TryAddEnumerable(
        //ServiceDescriptor.Singleton<IPostConfigureOptions<RazorComponentsServiceOptions>, DefaultRazorComponentsServiceOptionsConfiguration>());
        //services.TryAddScoped<EndpointRoutingStateProvider>();
        //services.TryAddScoped<IRoutingStateProvider>(sp => sp.GetRequiredService<EndpointRoutingStateProvider>());
        //services.AddSupplyValueFromQueryProvider();
        //services.TryAddCascadingValue(sp => sp.GetRequiredService<EndpointHtmlRenderer>().HttpContext);

        return services;
    }
}