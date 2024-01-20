using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public class HtmxRequest(HttpContext context)
{
    /// <summary>
    /// Gets whether or not the current request is an Htmx triggered request.
    /// </summary>
    public bool IsHtmxRequest => context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.HtmxRequest);

    /// <summary>
    /// Gets whether or not the current request is an request initiated via an element using hx-boost.
    /// </summary>
    public bool IsBoosted => IsHtmxRequest && context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.Boosted);

    /// <summary>
    /// Gets whether or not the current request is an Htmx history restore request.
    /// </summary>
    public bool IsHistoryRestoreRequest => IsHtmxRequest && context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.HistoryRestoreRequest);

    /// <summary>
    /// Gets the current URL of the browser.
    /// </summary>
    public Uri? CurrentURL
        => IsHtmxRequest
        && context.Request.Headers.TryGetValue(HtmxRequestHeaderNames.CurrentURL, out var values)
        && values.Count > 0
        && Uri.TryCreate(values[0], UriKind.RelativeOrAbsolute, out var uri)
        ? uri
        : null;

    /// <summary>
    /// Gets the `id` of the target element if it exists.
    /// </summary>
    public string? Target
        => IsHtmxRequest
        && context.Request.Headers.TryGetValue(HtmxRequestHeaderNames.Target, out var values)
        && values.Count > 0
        ? values[0]
        : null;

    /// <summary>
    /// Gets the `name` of the triggered element if it exists.
    /// </summary>
    public string? TriggerName
        => IsHtmxRequest
        && context.Request.Headers.TryGetValue(HtmxRequestHeaderNames.TriggerName, out var values)
        && values.Count > 0
        ? values[0]
        : null;

    /// <summary>
    /// Gets the `id` of the triggered element if it exists.
    /// </summary>
    public string? Trigger
        => IsHtmxRequest
        && context.Request.Headers.TryGetValue(HtmxRequestHeaderNames.Trigger, out var values)
        && values.Count > 0
        ? values[0]
        : null;

    /// <summary>
    /// Gets the user response to an hx-prompt, if any.
    /// </summary>
    public string? Prompt => IsHtmxRequest
        && context.Request.Headers.TryGetValue(HtmxRequestHeaderNames.Prompt, out var values)
        && values.Count > 0
        ? values[0]
        : null;
}
