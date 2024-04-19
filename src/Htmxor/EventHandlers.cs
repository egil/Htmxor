using Microsoft.AspNetCore.Components;

namespace Htmxor;

[EventHandler("onget", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onpost", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onput", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onpatch", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("ondelete", typeof(HtmxContextEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
public static class EventHandlers
{
}
