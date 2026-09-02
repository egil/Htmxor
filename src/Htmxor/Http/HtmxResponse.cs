using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Htmxor.Http;

/// <summary>
/// Provides htmx response operations. Core response-header operations require a request
/// with exactly one normalized <c>HX-Request: true</c> marker.
/// </summary>
/// <remarks>
/// Navigation operations validate their arguments before checking the htmx request marker,
/// replace any earlier core htmx navigation operation, and do not change the HTTP status code.
/// Swap and selection operations also validate before the marker guard, preserve the exact
/// ASCII HTTP-header-safe application-authored value, and replace only an earlier value for
/// the same response header.
/// Trigger operations merge exact, case-sensitive event names into one compact JSON object;
/// a later duplicate replaces its detail without moving the event from its first position.
/// Htmx does not process response headers on HTTP 3xx responses.
/// </remarks>
public sealed class HtmxResponse(HttpContext context)
{
	private static readonly object TriggerEventsItemsKey = new();
	private static readonly string[] NavigationHeaderNames =
	[
		HtmxResponseHeaderNames.Location,
		HtmxResponseHeaderNames.PushUrl,
		HtmxResponseHeaderNames.Redirect,
		HtmxResponseHeaderNames.Refresh,
		HtmxResponseHeaderNames.ReplaceUrl,
	];
	private readonly IHeaderDictionary headers = context.Response.Headers;
	private readonly bool isHtmxRequest = HtmxRequestMarkerClassifier.IsHtmxRequest(context.Request.Headers);
	private bool explicitEmptyResponseBodyRequested;
	private bool navigationSuppressesResponseBody;

	internal bool EmptyResponseBodyRequested
		=> explicitEmptyResponseBodyRequested || navigationSuppressesResponseBody;

	internal bool SuppressResponseBodyWrite()
	{
		if (!EmptyResponseBodyRequested)
		{
			return false;
		}

		if (!context.Response.HasStarted && context.Response.ContentLength is > 0)
		{
			context.Response.ContentLength = 0;
		}

		return true;
	}

	internal void BeginRenderExecution()
	{
		explicitEmptyResponseBodyRequested = false;
		navigationSuppressesResponseBody = false;
	}

	internal void CompleteRenderExecution()
	{
		try
		{
			_ = SuppressResponseBodyWrite();
		}
		finally
		{
			explicitEmptyResponseBodyRequested = false;
			navigationSuppressesResponseBody = false;
		}
	}

	/// <summary>
	/// Sets the response status code to <paramref name="statusCode"/>.
	/// </summary>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse StatusCode(HttpStatusCode statusCode)
	{
		AssertIsHtmxRequest();
		context.Response.StatusCode = (int)statusCode;
		return this;
	}

	/// <summary>
	/// Do not render any component markup to the client, even if the component would have
	/// produced markup normally. Headers and cookies are still returned as normal.
	/// </summary>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse EmptyBody()
	{
		AssertIsHtmxRequest();
		explicitEmptyResponseBodyRequested = true;
		return this;
	}

	/// <summary>
	/// Sets one application-owned extension response header to its exact value. The field name
	/// must be an unprotected <c>HX-*</c> HTTP field name, and its ASCII header-safe value must
	/// be well-formed UTF-16 and no longer than 4096 UTF-8 bytes. The operation validates both
	/// arguments before checking the htmx marker, replaces only the same header, and does not
	/// change status or component-body behavior.
	/// </summary>
	/// <param name="name">The application-owned extension field name.</param>
	/// <param name="value">The exact extension value; empty values are allowed.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse SetExtensionHeader(string name, string value)
	{
		HtmxExtensionHeaderPolicy.ValidateResponseInput(name, value);
		AssertIsHtmxRequest();
		headers[name] = value;
		return this;
	}

	/// <summary>
	/// Sets <c>HX-Location</c> to the exact relative or same-origin HTTP(S) URI reference
	/// in <paramref name="path"/> and suppresses component output.
	/// </summary>
	/// <param name="path">
	/// The destination. It must be a well-formed relative reference or resolve to the active
	/// request origin over HTTP(S), and contain only ASCII HTTP-header-safe characters. The value
	/// is not trimmed or normalized.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Location(string path)
	{
		HtmxNavigationDestinationValidator.ValidateLocal(path, context.Request, nameof(path));
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.Location, path, suppressBody: true);
	}

	/// <summary>
	/// Sets <c>HX-Location</c> to <see cref="Uri.OriginalString"/> from the relative or
	/// same-origin HTTP(S) <paramref name="path"/> and suppresses component output.
	/// </summary>
	/// <param name="path">
	/// The destination. It must be a well-formed relative reference or resolve to the active
	/// request origin over HTTP(S), and contain only ASCII HTTP-header-safe characters.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Location(Uri path)
	{
		ArgumentNullException.ThrowIfNull(path);
		return Location(path.OriginalString);
	}

	/// <summary>
	/// Sets <c>HX-Push-Url</c> to the exact relative or same-origin HTTP(S) URI reference
	/// in <paramref name="url"/> and retains component output.
	/// </summary>
	/// <param name="url">
	/// The destination. It must not be the reserved <c>true</c> or <c>false</c> history
	/// literal, contain only ASCII HTTP-header-safe characters, and it is not trimmed or normalized.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse PushUrl(string url)
	{
		HtmxNavigationDestinationValidator.ValidateLocalHistory(url, context.Request, nameof(url));
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.PushUrl, url, suppressBody: false);
	}

	/// <summary>
	/// Sets <c>HX-Push-Url</c> to <see cref="Uri.OriginalString"/> from the relative or
	/// same-origin HTTP(S) <paramref name="url"/> and retains component output.
	/// </summary>
	/// <param name="url">The destination, which must not represent <c>true</c> or <c>false</c> and must contain
	/// only ASCII HTTP-header-safe characters.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse PushUrl(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);
		return PushUrl(url.OriginalString);
	}

	/// <summary>
	/// Sets <c>HX-Push-Url: false</c>, replacing any earlier core htmx navigation operation,
	/// and retains component output.
	/// </summary>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse PreventBrowserHistoryUpdate()
	{
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.PushUrl, "false", suppressBody: false);
	}

	/// <summary>
	/// Sets <c>HX-Replace-Url: false</c>, replacing any earlier core htmx navigation operation,
	/// and retains component output.
	/// </summary>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse PreventBrowserCurrentUrlUpdate()
	{
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.ReplaceUrl, "false", suppressBody: false);
	}

	/// <summary>
	/// Sets <c>HX-Redirect</c> to the exact relative or HTTP(S) URI reference in
	/// <paramref name="url"/> and suppresses component output. Absolute cross-origin
	/// HTTP(S) destinations are allowed for deliberate full-page navigation.
	/// </summary>
	/// <param name="url">The destination. It must contain only ASCII HTTP-header-safe characters; the value is
	/// not trimmed or normalized.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Redirect(string url)
	{
		HtmxNavigationDestinationValidator.ValidateRedirect(url, context.Request, nameof(url));
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.Redirect, url, suppressBody: true);
	}

	/// <summary>
	/// Sets <c>HX-Redirect</c> to <see cref="Uri.OriginalString"/> from the relative or
	/// HTTP(S) <paramref name="url"/> and suppresses component output. Absolute cross-origin
	/// HTTP(S) destinations are allowed for deliberate full-page navigation.
	/// </summary>
	/// <param name="url">The destination, which must contain only ASCII HTTP-header-safe characters.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Redirect(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);
		return Redirect(url.OriginalString);
	}

	/// <summary>
	/// Sets <c>HX-Refresh: true</c>, replacing any earlier core htmx navigation operation,
	/// and suppresses component output.
	/// </summary>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Refresh()
	{
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.Refresh, "true", suppressBody: true);
	}

	/// <summary>
	/// Sets <c>HX-Replace-Url</c> to the exact relative or same-origin HTTP(S) URI reference
	/// in <paramref name="url"/> and retains component output.
	/// </summary>
	/// <param name="url">
	/// The destination. It must not be the reserved <c>true</c> or <c>false</c> history
	/// literal, contain only ASCII HTTP-header-safe characters, and it is not trimmed or normalized.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse ReplaceUrl(string url)
	{
		HtmxNavigationDestinationValidator.ValidateLocalHistory(url, context.Request, nameof(url));
		AssertIsHtmxRequest();
		return SetNavigation(HtmxResponseHeaderNames.ReplaceUrl, url, suppressBody: false);
	}

	/// <summary>
	/// Sets <c>HX-Replace-Url</c> to <see cref="Uri.OriginalString"/> from the relative or
	/// same-origin HTTP(S) <paramref name="url"/> and retains component output.
	/// </summary>
	/// <param name="url">The destination, which must not represent <c>true</c> or <c>false</c> and must contain
	/// only ASCII HTTP-header-safe characters.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse ReplaceUrl(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);
		return ReplaceUrl(url.OriginalString);
	}

	/// <summary>
	/// Sets <c>HX-Reswap</c> to the exact complete htmx or extension-defined value in
	/// <paramref name="modifier"/> without parsing, trimming, or normalizing it.
	/// </summary>
	/// <param name="modifier">
	/// The swap style and any modifiers. It must not be empty or whitespace-only, have
	/// surrounding whitespace, contain control characters, or contain non-ASCII characters.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Reswap(string modifier)
	{
		ValidateOpenResponseValue(modifier, nameof(modifier));
		AssertIsHtmxRequest();
		headers[HtmxResponseHeaderNames.Reswap] = modifier;
		return this;
	}

	/// <summary>
	/// Sets <c>HX-Retarget</c> to the exact complete htmx or extension-defined value in
	/// <paramref name="selector"/> without parsing, trimming, or normalizing it.
	/// </summary>
	/// <param name="selector">
	/// The target selector. It must not be empty or whitespace-only, have surrounding
	/// whitespace, contain control characters, or contain non-ASCII characters.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Retarget(string selector)
	{
		ValidateOpenResponseValue(selector, nameof(selector));
		AssertIsHtmxRequest();
		headers[HtmxResponseHeaderNames.Retarget] = selector;
		return this;
	}

	/// <summary>
	/// Sets <c>HX-Reselect</c> to the exact complete htmx or extension-defined value in
	/// <paramref name="selector"/> without parsing, trimming, or normalizing it.
	/// </summary>
	/// <param name="selector">
	/// The response selector. It must not be empty or whitespace-only, have surrounding
	/// whitespace, contain control characters, or contain non-ASCII characters.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Reselect(string selector)
	{
		ValidateOpenResponseValue(selector, nameof(selector));
		AssertIsHtmxRequest();
		headers[HtmxResponseHeaderNames.Reselect] = selector;
		return this;
	}

	/// <summary>
	/// Adds a client-side event with an empty detail object to the response's compact
	/// <c>HX-Trigger</c> JSON object.
	/// </summary>
	/// <param name="eventName">
	/// The exact, case-sensitive event name. It must not be empty or whitespace-only, have
	/// surrounding whitespace, contain control characters, or contain ill-formed UTF-16.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Trigger(string eventName)
	{
		ValidateTriggerEventName(eventName);
		AssertIsHtmxRequest();
		return MergeTrigger(eventName, detail: null);
	}

	/// <summary>
	/// Adds a client-side event and detail to the response's compact <c>HX-Trigger</c> JSON object.
	/// </summary>
	/// <param name="eventName">
	/// The exact, case-sensitive event name. It must not be empty or whitespace-only, have
	/// surrounding whitespace, contain control characters, or contain ill-formed UTF-16.
	/// </param>
	/// <param name="detail">
	/// The detail to pass to the client-side event. A detail that is or serializes to JSON
	/// <see langword="null"/> is emitted as an empty JSON object.
	/// </param>
	/// <param name="jsonSerializerOptions">
	/// The <see cref="JsonSerializerOptions"/> used to serialize <paramref name="detail"/>.
	/// If omitted, the application's configured <see cref="JsonOptions.SerializerOptions"/>
	/// are used when available. Htmxor owns the final compact, header-safe JSON encoding.
	/// </param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Trigger<TEventDetail>(string eventName, TEventDetail detail, JsonSerializerOptions? jsonSerializerOptions = null)
	{
		ValidateTriggerEventName(eventName);
		jsonSerializerOptions ??= context.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions;
		JsonElement? serializedDetail = detail is null
			? null
			: JsonSerializer.SerializeToElement(detail, jsonSerializerOptions);
		if (serializedDetail?.ValueKind == JsonValueKind.Null)
		{
			serializedDetail = null;
		}

		AssertIsHtmxRequest();
		return MergeTrigger(eventName, serializedDetail);
	}

	private HtmxResponse SetNavigation(string headerName, string value, bool suppressBody)
	{
		foreach (var navigationHeaderName in NavigationHeaderNames)
		{
			headers.Remove(navigationHeaderName);
		}

		headers[headerName] = value;
		navigationSuppressesResponseBody = suppressBody;
		return this;
	}

	private HtmxResponse MergeTrigger(string eventName, JsonElement? detail)
	{
		var events = context.Items.TryGetValue(TriggerEventsItemsKey, out var current) &&
			current is List<TriggerHeaderEvent> ownedEvents
			? new List<TriggerHeaderEvent>(ownedEvents)
			: [];
		var eventIndex = events.FindIndex(other =>
			string.Equals(other.EventName, eventName, StringComparison.Ordinal));
		var triggerEvent = new TriggerHeaderEvent(eventName, detail);
		if (eventIndex >= 0)
		{
			events[eventIndex] = triggerEvent;
		}
		else
		{
			events.Add(triggerEvent);
		}

		var headerValue = SerializeTriggerHeader(events);
		headers[HtmxResponseHeaderNames.Trigger] = headerValue;
		context.Items[TriggerEventsItemsKey] = events;
		return this;
	}

	private static string SerializeTriggerHeader(List<TriggerHeaderEvent> events)
	{
		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(
			buffer,
			new JsonWriterOptions
			{
				// Details already passed the selected serializer depth limit. The outer
				// response object must not impose a second, unrelated lower limit.
				MaxDepth = int.MaxValue,
			}))
		{
			writer.WriteStartObject();
			foreach (var triggerEvent in events)
			{
				writer.WritePropertyName(triggerEvent.EventName);
				if (triggerEvent.Detail is { } detail)
				{
					detail.WriteTo(writer);
				}
				else
				{
					// htmx 4 reads fields from a JSON detail value before dispatch, so null cannot
					// represent the protocol's no-detail form. An empty object preserves that form.
					writer.WriteStartObject();
					writer.WriteEndObject();
				}
			}

			writer.WriteEndObject();
			writer.Flush();
		}

		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	private readonly record struct TriggerHeaderEvent(string EventName, JsonElement? Detail);

	private static void ValidateTriggerEventName(string eventName)
	{
		ValidateOpenResponseValue(eventName, nameof(eventName));
		if (!IsWellFormedUtf16(eventName))
		{
			throw new ArgumentException(
				"The event name must contain well-formed UTF-16.",
				nameof(eventName));
		}
	}

	private static void ValidateOpenResponseValue(string value, string parameterName)
	{
		ArgumentNullException.ThrowIfNull(value, parameterName);
		if (value.Length == 0 ||
			char.IsWhiteSpace(value[0]) ||
			char.IsWhiteSpace(value[^1]) ||
			!HtmxRequestHeaderParser.IsAsciiHeaderSafe(value))
		{
			throw new ArgumentException(
				"The value must not be empty or whitespace-only, have surrounding whitespace, " +
				"or contain control or non-ASCII characters.",
				parameterName);
		}
	}

	private static bool IsWellFormedUtf16(string value)
	{
		var remaining = value.AsSpan();
		while (!remaining.IsEmpty)
		{
			if (Rune.DecodeFromUtf16(remaining, out _, out var charsConsumed) != OperationStatus.Done)
			{
				return false;
			}

			remaining = remaining[charsConsumed..];
		}

		return true;
	}

	private void AssertIsHtmxRequest()
	{
		if (!isHtmxRequest)
		{
			throw new InvalidOperationException(
				"The active request is not an htmx request. " +
				"Setting response headers during request has no effect.");
		}
	}
}
