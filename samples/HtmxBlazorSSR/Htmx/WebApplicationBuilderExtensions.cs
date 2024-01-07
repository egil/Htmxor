using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HtmxBlazorSSR.Htmx;

public static class WebApplicationBuilderExtensions
{
    public static void AddHtmx(this WebApplicationBuilder builder, Action<HtmxConfig>? configBuilder = null)
    {
        builder.Services.AddSingleton<HtmxConfig>(x =>
        {
            var config = new HtmxConfig();
            configBuilder?.Invoke(config);
            config.Antiforgery = new HtmxAntiforgeryOptions(x.GetRequiredService<IOptions<AntiforgeryOptions>>());
            return config;
        });

        builder.Services.AddScoped<HtmxContext>(srv => srv.GetRequiredService<IHttpContextAccessor>().HttpContext!.GetHtmxContext());
    }
}
