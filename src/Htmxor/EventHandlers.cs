using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Htmxor;

[EventHandler("onget", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onget", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onpost", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onput", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onpatch", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("ondelete", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
public static class EventHandlers
{
}

public class HtmxContextEventArgs : EventArgs
{
    public HtmxRequest Request { get; }

    public HtmxResponse Response { get; }

    public HtmxContextEventArgs(HtmxContext context)
    {
        Request = context.Request;
        Response = context.Response;
    }

    public void NoContentResponse()
    {
    }
}