using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

/// <summary>
/// Provides request data derived from untrusted htmx request headers.
/// </summary>
public sealed class HtmxRequest
{
	private readonly IHeaderDictionary headers;

	/// <summary>
	/// Gets the routing mode for the current request.
	/// </summary>
	public RoutingMode RoutingMode { get; }

	/// <summary>
	/// Gets the HTTP method of the current request.
	/// </summary>
	public string Method { get; }

	/// <summary>
	/// Gets the path of the current request.
	/// </summary>
	public PathString Path { get; }

	/// <summary>
	/// Gets whether the request has exactly one <c>HX-Request</c> value that equals lowercase
	/// <c>true</c> after trimming surrounding HTTP optional whitespace (space or tab). Missing,
	/// blank, false, malformed, comma-joined, and repeated values return <see langword="false"/>
	/// and do not expose dependent htmx context.
	/// </summary>
	public bool IsHtmxRequest { get; }

	/// <summary>
	/// Gets whether htmx requested a full-page or partial representation.
	/// Exactly one lowercase <c>full</c> or <c>partial</c> value is recognized after
	/// trimming HTTP optional whitespace; missing, blank, malformed, comma-joined, repeated,
	/// and contradictory values are exposed as <see langword="null"/>.
	/// </summary>
	public HtmxRequestType? RequestType { get; }

	/// <summary>
	/// Gets whether the current request was initiated via an element using <c>hx-boost</c>.
	/// When the request marker is valid, only one lowercase <c>true</c> value after HTTP optional
	/// whitespace is recognized; otherwise dependent htmx context is suppressed.
	/// </summary>
	public bool IsBoosted { get; }

	/// <summary>
	/// Gets whether the current request is an htmx history restore request.
	/// When the request marker is valid, only one lowercase <c>true</c> value after HTTP optional
	/// whitespace is recognized; otherwise dependent htmx context is suppressed.
	/// </summary>
	public bool IsHistoryRestoreRequest { get; }

	/// <summary>
	/// Gets the browser's current absolute HTTP(S) URL from <c>HX-Current-URL</c>, preserving
	/// its normalized field text through <see cref="Uri.OriginalString"/>.
	/// Missing, repeated, malformed, non-HTTP(S), control-containing, and ill-formed values
	/// are exposed as <see langword="null"/>.
	/// </summary>
	public Uri? CurrentUrl { get; }

	/// <summary>
	/// Gets the exact untrusted target element hint. Htmx normally supplies <c>tag#id</c> or
	/// tag-only text; Htmxor retains one nonblank open value after trimming only HTTP optional
	/// whitespace. Repeated, control-containing, and ill-formed values are exposed as
	/// <see langword="null"/>.
	/// </summary>
	public string? Target { get; }

	/// <summary>
	/// Gets the exact untrusted source element hint. Htmx normally supplies <c>tag#id</c> or
	/// tag-only text; Htmxor retains one nonblank open value after trimming only HTTP optional
	/// whitespace. Repeated, control-containing, and ill-formed values are exposed as
	/// <see langword="null"/>.
	/// </summary>
	public string? Source { get; }

	/// <summary>
	/// The `id` of the event handler to trigger on request.
	/// </summary>
	internal string? EventHandlerId { get; }

	/// <summary>
	/// Creates a new instance of <see cref="HtmxRequest"/>.
	/// </summary>
	public HtmxRequest(HttpContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		headers = context.Request.Headers;
		Method = context.Request.Method;
		Path = context.Request.Path;
		var parsed = HtmxRequestHeaderParser.Parse(context.Request.Headers);
		var isHtmx = IsHtmxRequest = parsed.IsHtmxRequest;

		if (!isHtmx)
		{
			RoutingMode = RoutingMode.Standard;
			return;
		}

		IsBoosted = parsed.IsBoosted;
		IsHistoryRestoreRequest = parsed.IsHistoryRestoreRequest;
		CurrentUrl = parsed.CurrentUrl;
		RequestType = parsed.RequestType;
		Target = parsed.Target;
		Source = parsed.Source;
		EventHandlerId = parsed.EventHandlerId;

		RoutingMode = RequestType is HtmxRequestType.Partial
			? RoutingMode.Direct
			: RoutingMode.Standard;
	}

	/// <summary>
	/// Attempts to get one exact, untrusted value from an application-owned extension request
	/// header. The name must be an unprotected <c>HX-*</c> HTTP field name. Missing, repeated,
	/// unsafe, malformed, or over-4096-byte values return <see langword="false"/> without
	/// trimming, repairing, parsing, or inferring extension semantics.
	/// </summary>
	/// <param name="name">The application-owned extension field name.</param>
	/// <param name="value">The exact single field value when the method returns <see langword="true"/>.</param>
	/// <returns>
	/// <see langword="true"/> when the strict htmx marker and extension field are valid; otherwise,
	/// <see langword="false"/>. The value remains untrusted and cannot grant route, action, method,
	/// authorization, antiforgery, or cache authority.
	/// </returns>
	public bool TryGetExtensionHeader(string name, out string value)
	{
		value = string.Empty;
		return IsHtmxRequest &&
			HtmxExtensionHeaderPolicy.IsAllowedName(name) &&
			headers.TryGetValue(name, out var values) &&
			HtmxExtensionHeaderPolicy.TryGetRequestValue(values, out value);
	}
}
