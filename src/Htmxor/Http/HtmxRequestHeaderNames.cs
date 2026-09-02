namespace Htmxor.Http;

/// <summary>
/// The HTMX request header names.
/// Based on the htmx 4 request headers: <seealso href="https://four.htmx.org/docs/whats-new-in-htmx-4#request-headers"/>
/// </summary>
public static class HtmxRequestHeaderNames
{
	/// <summary>
	/// Indicates that the request is via an element using <c>hx-boost</c>.
	/// Htmxor recognizes the value only when exactly one normalized lowercase
	/// <c>true</c> is supplied.
	/// </summary>
	public const string Boosted = "HX-Boosted";

	/// <summary>
	/// The current URL of the browser. Htmxor exposes one normalized absolute HTTP(S) value
	/// as an untrusted <see cref="Uri"/>; invalid or repeated values are ignored.
	/// </summary>
	public const string CurrentUrl = "HX-Current-URL";

	/// <summary>
	/// <see langword="true" /> if the request is for history restoration after a miss in the local history cache.
	/// Htmxor recognizes the value only when exactly one normalized lowercase <c>true</c> is supplied.
	/// </summary>
	public const string HistoryRestoreRequest = "HX-History-Restore-Request";

	/// <summary>
	/// Indicates whether the request targets a specific element or the whole page.
	/// Only one normalized lowercase <c>full</c> or <c>partial</c> value is recognized.
	/// </summary>
	public const string RequestType = "HX-Request-Type";

	/// <summary>
	/// An untrusted request marker. Htmxor recognizes exactly one lowercase
	/// <see langword="true" /> value after trimming surrounding HTTP optional whitespace;
	/// other shapes do not select htmx behavior.
	/// </summary>
	public const string HtmxRequest = "HX-Request";

	/// <summary>
	/// The exact untrusted target element hint, normally in <c>tag#id</c> or tag-only form.
	/// Htmxor does not use it as route, method, action, authorization, or antiforgery authority.
	/// </summary>
	public const string Target = "HX-Target";

	/// <summary>
	/// The exact untrusted source element hint, normally in <c>tag#id</c> or tag-only form.
	/// Htmxor does not use it as route, method, action, authorization, or antiforgery authority.
	/// </summary>
	public const string Source = "HX-Source";

	/// <summary>
	/// The `id` of the event handler to trigger on request.
	/// </summary>
	internal const string EventHandlerId = "HXOR-Event-Handler-Id";
}
