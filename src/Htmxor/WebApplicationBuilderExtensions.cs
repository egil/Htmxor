using Htmxor.Antiforgery;
using Htmxor.Configuration;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Htmxor;

public static class WebApplicationBuilderExtensions
{
    public static void AddHtmx(this IHostApplicationBuilder builder, Action<HtmxConfig>? configBuilder = null)
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
    }
}
