using Htmxor.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Antiforgery;

/// <summary>
/// This will add a HX-XSRF-TOKEN to each response, no matter if it was initiated by HTMX or not.
/// </summary>
internal sealed class HtmxAntiforgeryMiddleware(IAntiforgery antiforgery, HtmxConfig htmxConfig, RequestDelegate next)
{
    private static readonly CookieOptions cookieOptions = new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Strict,
        IsEssential = true
    };

    public async Task Invoke(HttpContext context)
    {
        if (htmxConfig.Antiforgery is not null)
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append(htmxConfig.Antiforgery.CookieName, tokens.RequestToken!, cookieOptions);
            await next.Invoke(context);
        }
    }
}