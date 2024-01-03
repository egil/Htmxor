namespace HtmxBlazorSSR.Htmx;

public class HtmxContext
{
    public HtmxRequest Request { get; }

    public HtmxResponse Response { get; }

    public HtmxContext(HttpContext context)
    {
        Request = new HtmxRequest(context);
        Response = new HtmxResponse(context);
    }
}
