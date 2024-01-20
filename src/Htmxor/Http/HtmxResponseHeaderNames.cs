namespace Htmxor.Http;

/// <summary>
/// The HTMX response header names.
/// Based on this version: <seealso href="https://github.com/bigskysoftware/htmx/blob/5aa0ec7e27c0dc282dd728886a77c0e321d3ca67/www/content/reference.md#response-headers-reference-response_headers"/>
/// </summary>
public static class HtmxResponseHeaderNames
{
    /// <summary>
    /// Allows you to do a client-side redirect that does not do a full page reload.
    /// </summary>
    public const string Location = "HX-Location";

    /// <summary>
    /// Pushes a new url into the history stack.
    /// </summary>
    public const string PushUrl = "HX-Push-Url";

    /// <summary>
    /// Can be used to do a client-side redirect to a new location.
    /// </summary>
    public const string Redirect = "HX-Redirect";

    /// <summary>
    /// If set to <see langword="true" /> the client-side will do a full refresh of the page.
    /// </summary>
    public const string Refresh = "HX-Refresh";

    /// <summary>
    /// Replaces the current URL in the location bar.
    /// </summary>
    public const string ReplaceUrl = "HX-Replace-Url";

    /// <summary>
    /// Allows you to specify how the response will be swapped.
    /// </summary>
    public const string Reswap = "HX-Reswap";

    /// <summary>
    /// A CSS selector that updates the target of the content update to a different element on the page.
    /// </summary>
    public const string Retarget = "HX-Retarget";

    /// <summary>
    /// A CSS selector that allows you to choose which part of the response is used to be swapped in.
    /// </summary>
    public const string Reselect = "HX-Reselect";

    /// <summary>
    /// Allows you to trigger client-side events.
    /// </summary>
    public const string Trigger = "HX-Trigger";

    /// <summary>
    /// Allows you to trigger client-side events after the settle step.
    /// </summary>
    public const string TriggerAfterSettle = "HX-Trigger-After-Settle";

    /// <summary>
    /// Allows you to trigger client-side events after the swap step.
    /// </summary>
    public const string TriggerAfterSwap = "HX-Trigger-After-Swap";
}
