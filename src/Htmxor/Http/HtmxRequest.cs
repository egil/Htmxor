using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public sealed class HtmxRequest
{
    private readonly HttpContext context;

    /// <summary>
    /// Gets whether or not the current request should be treated as a full page request.
    /// </summary>
    internal bool IsFullPageRequest => !IsHtmxRequest || IsBoosted;

    /// <summary>
    /// Gets the HTTP method of the current request.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets whether or not the current request is an Htmx triggered request.
    /// </summary>
    public bool IsHtmxRequest { get; }

    /// <summary>
    /// Gets whether or not the current request is an request initiated via an element using hx-boost.
    /// </summary>
    public bool IsBoosted { get; }

    /// <summary>
    /// Gets whether or not the current request is an Htmx history restore request.
    /// </summary>
    public bool IsHistoryRestoreRequest { get; }

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

    /// <summary>
    /// The `id` of the event handler to trigger on request.
    /// </summary>
    internal string? EventHandlerId => IsHtmxRequest
        && context.Request.Headers.TryGetValue(HtmxRequestHeaderNames.EventHandlerId, out var values)
        && values.Count > 0
        && !string.IsNullOrWhiteSpace(values[0])
        ? values[0]
        : null;

    /// <summary>
    /// Creates a new instance of <see cref="HtmxRequest"/>.
    /// </summary>
    public HtmxRequest(HttpContext context)
    {
        this.context = context;
        var ishtmx = IsHtmxRequest = context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.HtmxRequest);
        IsBoosted = ishtmx && context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.Boosted);
        IsHistoryRestoreRequest = ishtmx && context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.HistoryRestoreRequest);
        Method = context.Request.Method;
    }
}
