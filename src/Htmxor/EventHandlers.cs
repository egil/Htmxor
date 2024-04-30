using Microsoft.AspNetCore.Components;

namespace Htmxor;

/// <summary>
/// Custom Htmxor Blazor event handlers.
/// </summary>
[EventHandler("onget", typeof(HtmxEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onpost", typeof(HtmxEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onput", typeof(HtmxEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("onpatch", typeof(HtmxEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
[EventHandler("ondelete", typeof(HtmxEventArgs), enableStopPropagation: false, enablePreventDefault: false)]
public static class EventHandlers
{
}
