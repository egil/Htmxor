using Htmxor.Http;

namespace Htmxor;

public class HtmxContextEventArgs : EventArgs
{
    private readonly HtmxContext context;

    public HtmxRequest Request { get; }

    public HtmxResponse Response { get; }

    public HtmxContextEventArgs(HtmxContext context)
    {
        Request = context.Request;
        Response = context.Response;
        this.context = context;
    }
}