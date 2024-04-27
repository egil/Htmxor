using Htmxor;
using Htmxor.Antiforgery;
using Htmxor.Builder;
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
        services.AddScoped<HtmxorEndpointRoutingStateProvider>();

        services.Remove(services.Single(x => x.ServiceType == typeof(IRoutingStateProvider)));
        services.AddScoped<IRoutingStateProvider>(sp => sp.GetRequiredService<HtmxorEndpointRoutingStateProvider>());

        // Override rendering
        services.AddScoped<IHtmxorComponentEndpointInvoker, HtmxorComponentEndpointInvoker>();

        services.Remove(services.Single(x => x.ServiceType == typeof(IRazorComponentEndpointInvoker)));
        services.AddScoped<IRazorComponentEndpointInvoker>(x => x.GetRequiredService<IHtmxorComponentEndpointInvoker>());

        services.Remove(services.Single(x => x.Lifetime is ServiceLifetime.Scoped && x.ServiceType.FullName?.Equals("Microsoft.AspNetCore.Components.Endpoints.EndpointHtmlRenderer") == true));
        services.AddScoped<EndpointHtmxorRenderer>();

        // Adding the same cascading value does not seem to override existing values added previously.
        // Instead, the previous value is still used. Is this expected behavior or a bug?
        // TODO: create issue in aspnetcore repo.
        // This removes `services.TryAddCascadingValue(sp => sp.GetRequiredService<EndpointHtmlRenderer>().HttpContext)`.
        var existingCascadingHttpContextProvider = services
            .Where(x => x.Lifetime is ServiceLifetime.Scoped && x.ImplementationType is null && x.ImplementationFactory is not null)
            .Single(x => x.ImplementationFactory?.Target?.ToString()?.Contains(typeof(HttpContext).FullName!) == true);
        services.Remove(existingCascadingHttpContextProvider);

        services.AddCascadingValue(sp => sp.GetRequiredService<EndpointHtmxorRenderer>().HttpContext);
        services.AddScoped(sp => sp.GetRequiredService<EndpointHtmxorRenderer>().HttpContext!);

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