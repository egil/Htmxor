using System.Text.Json.Serialization;

namespace HtmxBlazorSSR.Htmx;

/// <summary>
/// Htmx configuration options.
/// </summary>
public class HtmxConfig
{
    /// <summary>
    /// Defaults to true, really only useful for testing
    /// </summary>
    [JsonPropertyName("historyEnabled")]
    public bool HistoryEnabled { get; set; } = true;

    /// <summary>
    /// defaults to 10
    /// </summary>
    [JsonPropertyName("historyCacheSize")]
    public int HistoryCacheSize { get; set; } = 10;

    /// <summary>
    /// defaults to false, if set to true htmx will issue a full page refresh on history misses rather than use an AJAX request
    /// </summary>
    [JsonPropertyName("refreshOnHistoryMiss")]
    public bool RefreshOnHistoryMiss { get; set; } = false;

    /// <summary>
    /// defaults to innerHTML
    /// </summary>
    [JsonPropertyName("defaultSwapStyle")]
    public string DefaultSwapStyle { get; set; } = "innerHTML";

    /// <summary>
    /// defaults to 0
    /// </summary>
    [JsonPropertyName("defaultSwapDelay")]
    public int DefaultSwapDelay { get; set; } = 0;

    /// <summary>
    /// defaults to 20
    /// </summary>
    [JsonPropertyName("defaultSettleDelay")]
    public int DefaultSettleDelay { get; set; } = 20;

    /// <summary>
    /// defaults to true (determines if the indicator styles are loaded)
    /// </summary>
    [JsonPropertyName("includeIndicatorStyles")]
    public bool IncludeIndicatorStyles { get; set; } = true;

    /// <summary>
    /// defaults to htmx-indicator
    /// </summary>
    [JsonPropertyName("indicatorClass")]
    public string IndicatorClass { get; set; } = "htmx-indicator";

    /// <summary>
    /// defaults to htmx-request
    /// </summary>
    [JsonPropertyName("requestClass")]
    public string RequestClass { get; set; } = "htmx-request";

    /// <summary>
    /// defaults to htmx-added
    /// </summary>
    [JsonPropertyName("addedClass")]
    public string AddedClass { get; set; } = "htmx-added";

    /// <summary>
    /// defaults to htmx-settling
    /// </summary>
    [JsonPropertyName("settlingClass")]
    public string SettlingClass { get; set; } = "htmx-settling";

    /// <summary>
    /// defaults to htmx-swapping
    /// </summary>
    [JsonPropertyName("swappingClass")]
    public string SwappingClass { get; set; } = "htmx-swapping";

    /// <summary>
    /// defaults to true, can be used to disable htmx’s use of eval for certain features (e.g. trigger filters)
    /// </summary>
    [JsonPropertyName("allowEval")]
    public bool AllowEval { get; set; } = true;

    /// <summary>
    /// defaults to true, determines if htmx will process script tags found in new content
    /// </summary>
    [JsonPropertyName("allowScriptTags")]
    public bool AllowScriptTags { get; set; } = true;

    /// <summary>
    /// defaults to '', meaning that no nonce will be added to inline scripts
    /// </summary>
    [JsonPropertyName("inlineScriptNonce")]
    public string InlineScriptNonce { get; set; } = "";

    /// <summary>
    /// defaults to ["class", "style", "width", "height"], the attributes to settle during the settling phase
    /// </summary>
    [JsonPropertyName("attributesToSettle")]
    public string[] AttributesToSettle { get; set; } = [ "class", "style", "width", "height" ];

    /// <summary>
    /// defaults to false, HTML template tags for parsing content from the server (not IE11 compatible!)
    /// </summary>
    [JsonPropertyName("useTemplateFragments")]
    public bool UseTemplateFragments { get; set; } = false;

    /// <summary>
    /// defaults to full-jitter
    /// </summary>
    [JsonPropertyName("wsReconnectDelay")]
    public string WsReconnectDelay { get; set; } = "full-jitter";

    /// <summary>
    /// defaults to blob, the type of binary data being received over the WebSocket connection
    /// </summary>
    [JsonPropertyName("wsBinaryType")]
    public string WsBinaryType { get; set; } = "blob";

    /// <summary>
    /// defaults to [hx-disable], [data-hx-disable], htmx will not process elements with this attribute on it or a parent
    /// </summary>
    [JsonPropertyName("disableSelector")]
    public string DisableSelector { get; set; } = "[hx-disable], [data-hx-disable]";

    /// <summary>
    /// defaults to false, allow cross-site Access-Control requests using credentials such as cookies, authorization headers or TLS client certificates
    /// </summary>
    [JsonPropertyName("withCredentials")]
    public bool WithCredentials { get; set; } = false;

    /// <summary>
    /// defaults to 0, the number of milliseconds a request can take before automatically being terminated
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 0;

    /// <summary>
    /// defaults to ‘smooth’, the behavior for a boosted link on page transitions. The allowed values are auto and smooth. Smooth will smoothscroll to the top of the page while auto will behave like a vanilla link.
    /// </summary>
    [JsonPropertyName("scrollBehavior")]
    public string ScrollBehavior { get; set; } = "smooth";

    /// <summary>
    /// if the focused element should be scrolled into view, defaults to false and can be overridden using the focus-scroll swap modifier.
    /// </summary>
    [JsonPropertyName("defaultFocusScroll")]
    public bool DefaultFocusScroll { get; set; } = false;

    /// <summary>
    /// defaults to false, if set to true htmx will include a cache-busting parameter in GET requests to avoid caching partial responses by the browser
    /// </summary>
    [JsonPropertyName("getCacheBusterParam")]
    public bool GetCacheBusterParam { get; set; } = false;

    /// <summary>
    /// if set to true, htmx will use the View Transition API when swapping in new content.
    /// </summary>
    [JsonPropertyName("globalViewTransitions")]
    public bool GlobalViewTransitions { get; set; } = false;

    /// <summary>
    /// defaults to ["get"], htmx will format requests with these methods by encoding their parameters in the URL, not the request body
    /// </summary>
    [JsonPropertyName("methodsThatUseUrlParams")]
    public string[] MethodsThatUseUrlParams { get; set; } = new string[] { "get" };

    /// <summary>
    /// defaults to false, if set to true will only allow AJAX requests to the same domain as the current document
    /// </summary>
    [JsonPropertyName("selfRequestsOnly")]
    public bool SelfRequestsOnly { get; set; } = false;

    /// <summary>
    /// defaults to false, if set to true htmx will not update the title of the document when a title tag is found in new content
    /// </summary>
    [JsonPropertyName("ignoreTitle")]
    public bool IgnoreTitle { get; set; } = false;

    /// <summary>
    /// defaults to true, whether or not the target of a boosted element is scrolled into the viewport. If hx-target is omitted on a boosted element, the target defaults to body, causing the page to scroll to the top.
    /// </summary>
    [JsonPropertyName("scrollIntoViewOnBoost")]
    public bool ScrollIntoViewOnBoost { get; set; } = true;

    /// <summary>
    /// defaults to null, the cache to store evaluated trigger specifications into, improving parsing performance at the cost of more memory usage. You may define a simple object to use a never-clearing cache, or implement your own system using a proxy object
    /// </summary>
    [JsonPropertyName("triggerSpecsCache")]
    public object? TriggerSpecsCache { get; set; } = null;
}
