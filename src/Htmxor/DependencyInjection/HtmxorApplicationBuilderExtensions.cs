using Htmxor;
using Htmxor.Antiforgery;
using Htmxor.Builder;
using Htmxor.Configuration;
using Htmxor.DependencyInjection;
using Htmxor.Http;
using Htmxor.Rendering;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
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
    /// <param name="razorComponentsBuilder"></param>
    /// <param name="configurHtmx"></param>
    public static IRazorComponentsBuilder AddHtmx(this IRazorComponentsBuilder razorComponentsBuilder, Action<HtmxConfig>? configurHtmx = null)
    {
        var services = razorComponentsBuilder.Services;

        // Override routing
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, HtmxorComponentEndpointMatcherPolicy>());
        services.AddScoped<EndpointRoutingStateProvider>();
        services.AddScoped<IRoutingStateProvider>(sp => sp.GetRequiredService<EndpointRoutingStateProvider>());
        
        // Override rendering
        services.AddScoped<IHtmxorComponentEndpointInvoker, HtmxorComponentEndpointInvoker>();
        services.AddScoped<IRazorComponentEndpointInvoker>(x => x.GetRequiredService<IHtmxorComponentEndpointInvoker>());
        services.AddScoped<EndpointHtmxorRenderer>();
        services.AddCascadingValue(sp => sp.GetRequiredService<EndpointHtmxorRenderer>().HttpContext!);

        // Add Htmxor services
        services.AddSingleton(x =>
        {
            var config = new HtmxConfig
            {
                Antiforgery = new HtmxAntiforgeryOptions(x.GetRequiredService<IOptions<AntiforgeryOptions>>()),
            };
            configurHtmx?.Invoke(config);
            return config;
        });
        services.AddScoped(srv => srv.GetRequiredService<EndpointHtmxorRenderer>().HttpContext!.GetHtmxContext());
        services.AddCascadingValue(sp => sp.GetRequiredService<HtmxContext>());

        return razorComponentsBuilder;
    }
}