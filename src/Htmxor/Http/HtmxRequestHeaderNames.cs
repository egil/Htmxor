namespace Htmxor.Http;

/// <summary>
/// The HTMX request header names.
/// Based on the htmx 4 request headers: <seealso href="https://four.htmx.org/docs/whats-new-in-htmx-4#request-headers"/>
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
	/// Indicates whether the request targets a specific element or the whole page.
	/// </summary>
	public const string RequestType = "HX-Request-Type";

	/// <summary>
	/// An untrusted request marker. Htmxor recognizes exactly one lowercase
	/// <see langword="true" /> value after trimming surrounding HTTP optional whitespace;
	/// other shapes do not select htmx behavior.
	/// </summary>
	public const string HtmxRequest = "HX-Request";

	/// <summary>
	/// The target element identity in `tag#id` or `tag` form.
	/// </summary>
	public const string Target = "HX-Target";

	/// <summary>
	/// The source element identity in `tag#id` or `tag` form.
	/// </summary>
	public const string Source = "HX-Source";

	/// <summary>
	/// The `id` of the event handler to trigger on request.
	/// </summary>
	internal const string EventHandlerId = "HXOR-Event-Handler-Id";
}
