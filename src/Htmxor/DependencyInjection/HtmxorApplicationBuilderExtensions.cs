using Htmxor.Antiforgery;
using Htmxor.Configuration;
using Htmxor.Endpoints;
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

        return services;
    }
}