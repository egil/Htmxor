using Htmxor.Antiforgery;
using Htmxor.Configuration;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Htmxor;

/// <summary>
/// This class has extension methods for <see cref="IHostApplicationBuilder"/> and <see cref="IApplicationBuilder"/> 
/// that enable configuration of Htmx in the application.
/// </summary>
public static class HtmxorApplicationBuilderExtensions
{
    /// <summary>
    /// Add and configure Htmx.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configBuilder"></param>
    public static IHostApplicationBuilder AddHtmx(this IHostApplicationBuilder builder, Action<HtmxConfig>? configBuilder = null)
    {
        builder.Services.AddSingleton<HtmxConfig>(x =>
        {
            var config = new HtmxConfig
            {
                Antiforgery = new HtmxAntiforgeryOptions(x.GetRequiredService<IOptions<AntiforgeryOptions>>()),
            };
            configBuilder?.Invoke(config);
            return config;
        });

        builder.Services.AddScoped(srv => srv.GetRequiredService<IHttpContextAccessor>().HttpContext!.GetHtmxContext());

        return builder;
    }
    
    /// <summary>
    /// Enable Htmx to use antiforgery tokens to secure requests.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseHtmxorAntiforgery(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<HtmxAntiforgeryMiddleware>();
        return builder;
    }
}
