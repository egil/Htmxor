using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace HtmxBlazorSSR.Htmx;

internal sealed class HtmxAntiforgeryMiddleware(IAntiforgery antiforgery, IOptions<AntiforgeryOptions> options, RequestDelegate next)
{
    private const string AntiforgeryMiddlewareSetKey = "__AntiforgeryMiddlewareSet";
    private const string AntiforgeryMiddlewareWithEndpointInvokedKey = "__AntiforgeryMiddlewareWithEndpointInvoked";

    public async Task Invoke(HttpContext context)
    {
        var opt = options.Value;
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("HX-XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false });
        await next.Invoke(context);
    }
}