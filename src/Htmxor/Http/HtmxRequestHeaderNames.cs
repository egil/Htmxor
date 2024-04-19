namespace Htmxor.Http;

/// <summary>
/// The HTMX request header names.
/// Based on this version: <seealso href="https://github.com/bigskysoftware/htmx/blob/5aa0ec7e27c0dc282dd728886a77c0e321d3ca67/www/content/reference.md#request-headers-reference-request_headers"/>
/// </summary>
public static class HtmxRequestHeaderNames
{
    /// <summary>
    /// Indicates that the request is via an element using hx-boost.
    /// </summary>
    public const string Boosted = "HX-Boosted";

    /// <summary>
    /// The current URL of the browser.
    /// </summary>
    public const string CurrentURL = "HX-Current-URL";

    /// <summary>
    /// <see langword="true" /> if the request is for history restoration after a miss in the local history cache.
    /// </summary>
    public const string HistoryRestoreRequest = "HX-History-Restore-Request";

    /// <summary>
    /// The user response to an hx-prompt.
    /// </summary>
    public const string Prompt = "HX-Prompt";

    /// <summary>
    /// Always <see langword="true" />.
    /// </summary>
    public const string HtmxRequest = "HX-Request";

    /// <summary>
    /// The `id` of the target element if it exists.
    /// </summary>
    public const string Target = "HX-Target";

    /// <summary>
    /// The `name` of the triggered element if it exists.
    /// </summary>
    public const string TriggerName = "HX-Trigger-Name";

    /// <summary>
    /// The `id` of the triggered element if it exists.
    /// </summary>
    public const string Trigger = "HX-Trigger";

    /// <summary>
    /// The `id` of the event handler to trigger on request.
    /// </summary>
    internal const string EventHandlerId = "Htmxor-Event-Handler-Id";
}
