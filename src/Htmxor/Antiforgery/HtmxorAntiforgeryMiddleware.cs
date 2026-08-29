using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Antiforgery;

/// <summary>
/// This will add a HX-XSRF-TOKEN to each response, no matter if it was initiated by HTMX or not.
/// </summary>
internal sealed class HtmxorAntiforgeryMiddleware(IAntiforgery antiforgery, RequestDelegate next)
{
	private const string CookieName = "HX-XSRF-TOKEN";

	private static readonly CookieOptions CookieOptions = new CookieOptions
	{
		HttpOnly = false,
		SameSite = SameSiteMode.Strict,
		IsEssential = true
	};

	public async Task Invoke(HttpContext context)
	{
		var tokens = antiforgery.GetTokens(context);
		context.Response.Cookies.Append(CookieName, tokens.RequestToken!, CookieOptions);
		await next.Invoke(context);
	}
}
