using System.Net;
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
/// Htmx does not process response headers on HTTP 3xx responses.
/// </remarks>
public sealed class HtmxResponse(HttpContext context)
{
	private const string ItemsKeyPrefix = "02E0A668-6E6B-4C53-83A6-17E576073E96";
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
	/// Sets <c>HX-Location</c> to the exact relative or same-origin HTTP(S) URI reference
	/// in <paramref name="path"/> and suppresses component output.
	/// </summary>
	/// <param name="path">
	/// The destination. It must be a well-formed relative reference or resolve to the active
	/// request origin over HTTP(S). The value is not trimmed or normalized.
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
	/// request origin over HTTP(S).
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
	/// literal, and it is not trimmed or normalized.
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
	/// <param name="url">The destination, which must not represent <c>true</c> or <c>false</c>.</param>
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
	/// <param name="url">The destination. The value is not trimmed or normalized.</param>
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
	/// <param name="url">The destination.</param>
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
	/// literal, and it is not trimmed or normalized.
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
	/// <param name="url">The destination, which must not represent <c>true</c> or <c>false</c>.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse ReplaceUrl(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);
		return ReplaceUrl(url.OriginalString);
	}

	/// <summary>
	/// Allows you to specify the complete hx-swap value for the response.
	/// </summary>
	/// <param name="modifier">The swap style and any modifiers, including extension-defined values.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Reswap(string modifier)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(modifier);
		AssertIsHtmxRequest();
		headers[HtmxResponseHeaderNames.Reswap] = modifier;
		return this;
	}

	/// <summary>
	/// Allows you to specify how the response will be swapped.
	/// </summary>
	/// <param name="swapStyle"></param>
	/// <param name="modifier">The hx-swap attributes supports modifiers for changing the behavior of the swap.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Reswap(SwapStyle swapStyle, string? modifier = null)
	{
		AssertIsHtmxRequest();

		if (swapStyle is SwapStyle.Default)
		{
			Reswap(modifier!);
			return this;
		}

		var style = swapStyle.ToHtmxString();
		var value = !string.IsNullOrWhiteSpace(modifier)
			? $"{style} {modifier}"
			: style;

		headers[HtmxResponseHeaderNames.Reswap] = value;

		return this;
	}

	/// <summary>
	/// A CSS selector that updates the target of the content update to a different element on the page.
	/// </summary>
	/// <param name="selector"></param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Retarget(string selector)
	{
		AssertIsHtmxRequest();

		headers[HtmxResponseHeaderNames.Retarget] = selector;

		return this;
	}

	/// <summary>
	/// A CSS selector that allows you to choose which part of the response is used to be swapped in.
	/// </summary>
	/// <param name="selector"></param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Reselect(string selector)
	{
		AssertIsHtmxRequest();

		headers[HtmxResponseHeaderNames.Reselect] = selector;

		return this;
	}

	/// <summary>
	/// Sets response code to stop polling
	/// </summary>
	/// <returns></returns>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse StopPolling()
	{
		context.Response.StatusCode = HtmxStatusCodes.StopPolling;

		return this;
	}

	/// <summary>
	/// Allows you to trigger client-side events.
	/// </summary>
	/// <param name="eventName">The name of client side event to trigger.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Trigger(string eventName)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(eventName);
		AssertIsHtmxRequest();

		MergeTrigger(eventName, default(object), null);

		return this;
	}

	/// <summary>
	/// Allows you to trigger client-side events.
	/// </summary>
	/// <param name="eventName">The name of client side event to trigger.</param>
	/// <param name="detail">The details to pass the client side event.</param>
	/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use to convert the <paramref name="detail"/> into JSON. 
	/// If not specified, the application's configured <see cref="JsonOptions.SerializerOptions"/> are used if available.</param>
	/// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
	public HtmxResponse Trigger<TEventDetail>(string eventName, TEventDetail detail, JsonSerializerOptions? jsonSerializerOptions = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(eventName);
		AssertIsHtmxRequest();

		MergeTrigger(eventName, detail, jsonSerializerOptions);

		return this;
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

	private void MergeTrigger<TEventDetail>(string eventName, TEventDetail? detail, JsonSerializerOptions? jsonSerializerOptions)
	{
		jsonSerializerOptions ??= context.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions;
		var itemsKey = ItemsKeyPrefix + HtmxResponseHeaderNames.Trigger;
		if (!context.Items.TryGetValue(itemsKey, out var current) || current is not List<TriggerHeaderEventSet> headerValueSet)
		{
			headerValueSet = [];
		}

		if (headerValueSet.Count == 0 || !headerValueSet.Exists(other => other.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase)))
		{
			headerValueSet.Add(new(eventName, detail is not null ? JsonSerializer.Serialize(detail, jsonSerializerOptions) : null));
		}

		context.Items[itemsKey] = headerValueSet;

		if (headerValueSet.TrueForAll(x => x.Detail is null))
		{
			headers[HtmxResponseHeaderNames.Trigger] = string.Join(',', headerValueSet.Select(x => x.EventName));
		}
		else
		{
			headers[HtmxResponseHeaderNames.Trigger] = $"{{{string.Join(',', headerValueSet)}}}";
		}
	}

	private readonly record struct TriggerHeaderEventSet(string EventName, string? Detail)
	{
		public override string ToString()
			=> Detail is null
			? $"\"{EventName}\":null"
			: $"\"{EventName}\":{Detail}";
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
