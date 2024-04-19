using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public class HtmxContext
{
    internal bool NoContentResponseRequested { get; private set; }

    public HtmxRequest Request { get; }

    public HtmxResponse Response { get; }

    public HtmxContext(HttpContext context)
    {
        Request = new HtmxRequest(context);
        Response = new HtmxResponse();
    }

    public void NoContentResponse()
        => NoContentResponseRequested = true;
}
