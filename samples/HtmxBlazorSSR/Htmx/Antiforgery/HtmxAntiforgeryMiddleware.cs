using HtmxBlazorSSR.Htmx.Configuration;
using Microsoft.AspNetCore.Antiforgery;

namespace HtmxBlazorSSR.Htmx;

/// <summary>
/// This will add a HX-XSRF-TOKEN to each response, no matter if it was initiated by HTMX or not.
/// The 
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
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append(htmxConfig.Antiforgery.CookieName, tokens.RequestToken!, cookieOptions);
        await next.Invoke(context);
    }
}