using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.TestAssets;

internal static class AlbaScenarioExtensions
{
	public static Scenario WithHxHeaders(
		this Scenario scenario,
		bool isHtmxRequest = true,
		bool? isBoosted = null,
		bool? isHistoryRestoreRequest = null,
		string? currentURL = null,
		string? target = null,
		string? triggerName = null,
		string? trigger = null,
		string? prompt = null,
		string? eventHandlerId = null)
	{
		if (isHtmxRequest)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.HtmxRequest, "true");
		}

		if (isBoosted is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.Boosted, "true");
		}

		if (isHistoryRestoreRequest is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.HistoryRestoreRequest, "true");
		}

		if (currentURL is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.CurrentURL, currentURL);
		}

		if (target is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.Target, target);
		}

		if (triggerName is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.TriggerName, triggerName);
		}

		if (trigger is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.Trigger, trigger);
		}

		if (prompt is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.Prompt, prompt);
		}

		if (eventHandlerId is not null)
		{
			scenario.WithRequestHeader(HtmxRequestHeaderNames.EventHandlerId, eventHandlerId);
		}

		return scenario;
	}

	public static Scenario WithAntiforgeryTokensFrom(this Scenario scenario, IAlbaHost host)
	{
		var tempContext = new DefaultHttpContext();

		var defaultTokenSet = host.Services.GetRequiredService<IAntiforgery>().GetAndStoreTokens(tempContext);

		var cookieTokenSet = tempContext
			.Response
			.Headers["Set-Cookie"]
			.Single(x => x is not null && x.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))!
			.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		scenario.WithRequestHeader(defaultTokenSet.HeaderName!, defaultTokenSet.RequestToken!);
		scenario.WithRequestHeader("Cookie", $"{cookieTokenSet[0]}={defaultTokenSet.CookieToken}");

		return scenario;
	}

	/// <summary>
	/// Write the dictionary values to the HttpContext.Request.Body.
	/// Also sets content-length and content-type header to
	/// application/x-www-form-urlencoded
	/// </summary>
	/// <param name="context"></param>
	/// <param name="values"></param>
	public static void WithFormData(this Scenario scenario, params (string Key, string Value)[] values)
	{
		scenario.ConfigureHttpContext(context =>
		{
			using var form = new FormUrlEncodedContent(values.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));

			form.CopyTo(context.Request.Body, null, CancellationToken.None);

			context.Request.Headers.ContentType = form.Headers.ContentType!.ToString();
			context.Request.Headers.ContentLength = form.Headers.ContentLength;
		});
	}
}
