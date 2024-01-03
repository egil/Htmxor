
namespace HtmxBlazorSSR.Htmx;

public static class HttpContextExtensions
{
    public static HtmxContext GetHtmxContext(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return new HtmxContext(httpContext);
    }
}