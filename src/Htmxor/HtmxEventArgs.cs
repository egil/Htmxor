using Htmxor.Http;

namespace Htmxor;

public class HtmxEventArgs : EventArgs
{
    private readonly HtmxContext context;

    public HtmxRequest Request { get; }

    public HtmxResponse Response { get; }

    public HtmxEventArgs(HtmxContext context)
    {
        Request = context.Request;
        Response = context.Response;
        this.context = context;
    }
}
