using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public static class HttpContextExtensions
{
    const string htmxContextKey = "HtmxContext";

    public static HtmxContext GetHtmxContext(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Items.TryGetValue(htmxContextKey, out var value) || value is not HtmxContext result)
        {
            result = new HtmxContext(httpContext);
            httpContext.Items[htmxContextKey] = result;
        }

        return result;
    }
}