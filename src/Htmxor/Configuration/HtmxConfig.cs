using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Htmxor.Antiforgery;
using Htmxor.Configuration.Serialization;

namespace Htmxor.Configuration;

/// <summary>
/// Htmx configuration options.
/// </summary>
public record class HtmxConfig
{
    /// <summary>
    /// Default <see cref="JsonSerializerOptions"/> used with <see cref="HtmxConfig"/>.
    /// </summary>
    public readonly static JsonTypeInfo<HtmxConfig> JsonTypeInfo = HtmxConfigJsonSerializerContext.Default.HtmxConfig;

    /// <summary>
    /// Defaults to <see langword="true" /> if this property is null. really only useful for testing
    /// </summary>
    [JsonPropertyName("historyEnabled")]
    public bool? HistoryEnabled { get; set; }

    /// <summary>
    /// Defaults to <see langword="10"/> if this property is null.
    /// </summary>
    [JsonPropertyName("historyCacheSize")]
    public int? HistoryCacheSize { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// If set to <see langword="true" /> htmx will issue a full page refresh on history misses rather than use an AJAX request.
    /// </summary>
    [JsonPropertyName("refreshOnHistoryMiss")]
    public bool? RefreshOnHistoryMiss { get; set; }

    /// <summary>
    /// Defaults to <see cref="SwapStyle.InnerHTML"/> if this property is null.
    /// </summary>
    [JsonPropertyName("defaultSwapStyle")]
    public SwapStyle? DefaultSwapStyle { get; set; }

    /// <summary>
    /// Defaults to <see langword="0"/> if this property is null.
    /// </summary>
    [JsonPropertyName("defaultSwapDelay")]
    public TimeSpan? DefaultSwapDelay { get; set; }

    /// <summary>
    /// Defaults to <see langword="20"/> if this property is null.
    /// </summary>
    [JsonPropertyName("defaultSettleDelay")]
    public TimeSpan? DefaultSettleDelay { get; set; }

    /// <summary>
    /// Defaults to <see langword="true" /> if this property is null.
    /// Determines if the indicator styles are loaded.
    /// </summary>
    [JsonPropertyName("includeIndicatorStyles")]
    public bool? IncludeIndicatorStyles { get; set; }

    /// <summary>
    /// Defaults to <c>htmx-indicator</c> if this property is null.
    /// </summary>
    [JsonPropertyName("indicatorClass")]
    public string? IndicatorClass { get; set; }

    /// <summary>
    /// Defaults to <c>htmx-request</c> if this property is null.
    /// </summary>
    [JsonPropertyName("requestClass")]
    public string? RequestClass { get; set; }

    /// <summary>
    /// Defaults to <c>htmx-added</c> if this property is null. 
    /// </summary>
    [JsonPropertyName("addedClass")]
    public string? AddedClass { get; set; }

    /// <summary>
    /// Defaults to <c>htmx-settling</c> if this property is null. 
    /// </summary>
    [JsonPropertyName("settlingClass")]
    public string? SettlingClass { get; set; }

    /// <summary>
    /// Defaults to <c>htmx-swapping</c> if this property is null. 
    /// </summary>
    [JsonPropertyName("swappingClass")]
    public string? SwappingClass { get; set; }

    /// <summary>
    /// Defaults to <see langword="true" /> if this property is null. 
    /// Can be used to disable htmx’s use of eval for certain features (e.g. trigger filters).
    /// </summary>
    [JsonPropertyName("allowEval")]
    public bool? AllowEval { get; set; }

    /// <summary>
    /// Defaults to <see langword="true" /> if this property is null.
    /// Determines if htmx will process script tags found in new content
    /// </summary>
    [JsonPropertyName("allowScriptTags")]
    public bool? AllowScriptTags { get; set; }

    /// <summary>
    /// Defaults to <c>''</c> (empty string) if this property is null,
    /// meaning that no nonce will be added to inline scripts
    /// </summary>
    [JsonPropertyName("inlineScriptNonce")]
    public string? InlineScriptNonce { get; set; }

    /// <summary>
    /// Defaults to <c>["class", "style", "width", "height"]</c> if this property is null.
    /// The attributes to settle during the settling phase
    /// </summary>
    [JsonPropertyName("attributesToSettle")]
    public string[]? AttributesToSettle { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// HTML template tags for parsing content from the server (not IE11 compatible!).
    /// </summary>
    [JsonPropertyName("useTemplateFragments")]
    public bool? UseTemplateFragments { get; set; }

    /// <summary>
    /// Defaults to <c>full-jitter</c> if this property is null.
    /// </summary>
    [JsonPropertyName("wsReconnectDelay")]
    public string? WsReconnectDelay { get; set; }

    /// <summary>
    /// Defaults to <c>blob</c> if this property is null.
    /// The type of binary data being received over the WebSocket connection.
    /// </summary>
    [JsonPropertyName("wsBinaryType")]
    public string? WsBinaryType { get; set; }

    /// <summary>
    /// Defaults to <c>[hx-disable], [data-hx-disable]</c> if this property is null.
    /// Htmx will not process elements with this attribute on it or a parent.
    /// </summary>
    [JsonPropertyName("disableSelector")]
    public string? DisableSelector { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// Allow cross-site Access-Control requests using credentials such as cookies, authorization headers or TLS client certificates.
    /// </summary>
    [JsonPropertyName("withCredentials")]
    public bool? WithCredentials { get; set; }

    /// <summary>
    /// Defaults to <see langword="0" /> if this property is null.
    /// The number of milliseconds a request can take before automatically being terminated
    /// </summary>
    [JsonPropertyName("timeout")]
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Defaults to <see cref="ScrollBehavior.Smooth" /> if this property is null.
    /// The behavior for a boosted link on page transitions. 
    /// Smooth will smooth scroll to the top of the page while auto will behave like a vanilla link.
    /// </summary>
    [JsonPropertyName("scrollBehavior")]
    public ScrollBehavior? ScrollBehavior { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null. 
    /// If the focused element should be scrolled into view, and can be overridden using the focus-scroll swap modifier.
    /// </summary>
    [JsonPropertyName("defaultFocusScroll")]
    public bool? DefaultFocusScroll { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// If set to <see langword="true" /> htmx will include a cache-busting parameter in GET requests to avoid caching partial responses by the browser.
    /// </summary>
    [JsonPropertyName("getCacheBusterParam")]
    public bool? GetCacheBusterParam { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// If set to <see langword="true" />, htmx will use the View Transition API when swapping in new content.
    /// </summary>
    [JsonPropertyName("globalViewTransitions")]
    public bool? GlobalViewTransitions { get; set; }

    /// <summary>
    /// Defaults to <c>["get"]</c> if this property is null.
    /// Htmx will format requests with these methods by encoding their parameters in the URL, not the request body.
    /// </summary>
    [JsonPropertyName("methodsThatUseUrlParams")]
    public string[]? MethodsThatUseUrlParams { get; set; }

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// If set to <see langword="true" /> will only allow AJAX requests to the same domain as the current document.
    /// </summary>
    [JsonPropertyName("selfRequestsOnly")]
    public bool SelfRequestsOnly { get; set; } = true;

    /// <summary>
    /// Defaults to <see langword="false" /> if this property is null.
    /// If set to <see langword="true" /> htmx will not update the title of the document when a title tag is found in new content.
    /// </summary>
    [JsonPropertyName("ignoreTitle")]
    public bool? IgnoreTitle { get; set; }

    /// <summary>
    /// Defaults to <see langword="true" /> if this property is null. 
    /// Whether or not the target of a boosted element is scrolled into the viewport. 
    /// If hx-target is omitted on a boosted element, the target defaults to body, causing the page to scroll to the top.
    /// </summary>
    [JsonPropertyName("scrollIntoViewOnBoost")]
    public bool? ScrollIntoViewOnBoost { get; set; }

    [JsonInclude, JsonPropertyName("antiforgery")]
    internal HtmxAntiforgeryOptions? Antiforgery { get; init; }
}
