using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Htmxor.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Http;

public sealed class HtmxResponse(HttpContext context)
{
    private const string ItemsKeyPrefix = "02E0A668-6E6B-4C53-83A6-17E576073E96";
    private readonly IHeaderDictionary headers = context.Response.Headers;
    private readonly bool isHtmxRequest = context.Request.Headers.ContainsKey(HtmxRequestHeaderNames.HtmxRequest);

    internal bool EmptyResponseBodyRequested { get; private set; }

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
        EmptyResponseBodyRequested = true;
        return this;
    }

    /// <summary>
    /// Allows you to do a client-side redirect that does not do a full page reload.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Location(string path)
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.Location] = path;
        return this;
    }

    /// <summary>
    /// Allows you to do a client-side redirect that does not do a full page reload.
    /// </summary>
    /// <param name="locationTarget"></param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Location(LocationTarget locationTarget)
    {
        AssertIsHtmxRequest();
        var json = JsonSerializer.Serialize(locationTarget, HtmxJsonSerializerContext.Default.LocationTarget);
        headers[HtmxResponseHeaderNames.Location] = json;
        return this;
    }

    /// <summary>
    /// Pushes a new url onto the history stack.
    /// </summary>
    /// <param name="url"></param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse PushUrl(string url)
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.PushUrl] = url;
        return this;
    }

    /// <summary>
    /// Prevents the browser’s history from being updated.
    /// Overwrites PushUrl response if already present.
    /// </summary>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse PreventBrowserHistoryUpdate()
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.PushUrl] = "false";
        return this;
    }

    /// <summary>
    /// Prevents the browser’s current url from being updated
    /// Overwrites ReplaceUrl response if already present.
    /// </summary>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse PreventBrowserCurrentUrlUpdate()
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.ReplaceUrl] = "false";
        return this;
    }

    /// <summary>
    /// Can be used to do a client-side redirect to a new location.
    /// </summary>
    /// <param name="url"></param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Redirect(string url)
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.Redirect] = url;
        EmptyResponseBodyRequested = true;
        return this;
    }

    /// <summary>
    /// Enables a client-side full refresh of the page.
    /// </summary>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Refresh()
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.Refresh] = "true";
        EmptyResponseBodyRequested = true;
        return this;
    }

    /// <summary>
    /// Replaces the current URL in the location bar.
    /// </summary>
    /// <param name="url"></param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse ReplaceUrl(string url)
    {
        AssertIsHtmxRequest();
        headers[HtmxResponseHeaderNames.ReplaceUrl] = url;
        return this;
    }

    /// <summary>
    /// Allows you to specify how the response will be swapped.
    /// </summary>
    /// <param name="swapStyle"></param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Reswap(SwapStyle swapStyle)
    {
        AssertIsHtmxRequest();

        var style = swapStyle switch
        {
            SwapStyle.InnerHTML => "innerHTML",
            SwapStyle.OuterHTML => "outerHTML",
            SwapStyle.BeforeBegin => "beforebegin",
            SwapStyle.AfterBegin => "afterbegin",
            SwapStyle.BeforeEnd => "beforeend",
            SwapStyle.AfterEnd => "afterend",
            SwapStyle.Delete => "delete",
            SwapStyle.None => "none",
            _ => throw new SwitchExpressionException(swapStyle),
        };

        headers[HtmxResponseHeaderNames.Reswap] = style;

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
    /// Allows you to trigger client-side events.
    /// </summary>
    /// <param name="eventName">The name of client side event to trigger.</param>
    /// <param name="timing">When the event should be triggered.</param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Trigger(string eventName, TriggerTiming timing = TriggerTiming.Default)
    {
        AssertIsHtmxRequest();

        var headerKey = timing switch
        {
            TriggerTiming.AfterSwap => HtmxResponseHeaderNames.TriggerAfterSwap,
            TriggerTiming.AfterSettle => HtmxResponseHeaderNames.TriggerAfterSettle,
            _ => HtmxResponseHeaderNames.Trigger
        };

        MergeTrigger(headerKey, eventName, default(object), null);

        return this;
    }

    /// <summary>
    /// Allows you to trigger client-side events.
    /// </summary>
    /// <param name="eventName">The name of client side event to trigger.</param>
    /// <param name="detail">The details to pass the client side event.</param>
    /// <param name="timing">When the event should be triggered.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use to convert the <paramref name="detail"/> into JSON. 
    /// If not specified, a <see cref="JsonOptions.SerializerOptions"/> is retrieved <see cref="HttpContext.RequestServices"/> and used if available.</param>
    /// <returns>This <see cref="HtmxResponse"/> object instance.</returns>
    public HtmxResponse Trigger<TEventDetail>(string eventName, TEventDetail detail, TriggerTiming timing = TriggerTiming.Default, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        AssertIsHtmxRequest();

        var headerKey = timing switch
        {
            TriggerTiming.AfterSwap => HtmxResponseHeaderNames.TriggerAfterSwap,
            TriggerTiming.AfterSettle => HtmxResponseHeaderNames.TriggerAfterSettle,
            _ => HtmxResponseHeaderNames.Trigger
        };

        MergeTrigger(headerKey, eventName, detail, jsonSerializerOptions);

        return this;
    }

    private void MergeTrigger<TEventDetail>(string headerKey, string eventName, TEventDetail? detail, JsonSerializerOptions? jsonSerializerOptions)
    {
        jsonSerializerOptions ??= context.RequestServices.GetService<JsonOptions>()?.SerializerOptions;
        var itemsKey = ItemsKeyPrefix + headerKey;
        if (!context.Items.TryGetValue(itemsKey, out var current) || current is not List<TriggerHeaderEventSet> headerValueSet)
        {
            headerValueSet = [];
        }

        if (headerValueSet.Count == 0 || !headerValueSet.Exists(other => other.EventName.Equals(eventName)))
        {
            headerValueSet.Add(new(eventName, detail is not null ? JsonSerializer.Serialize(detail, jsonSerializerOptions) : null));
        }

        context.Items[itemsKey] = headerValueSet;

        if (headerValueSet.TrueForAll(x => x.Detail is null))
        {
            headers[headerKey] = string.Join(',', headerValueSet.Select(x => x.EventName));
        }
        else
        {
            headers[headerKey] = $"{{{string.Join(',', headerValueSet)}}}";
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
        if(!isHtmxRequest)
        {
            throw new InvalidOperationException(
                "The active request is not an htmx request. " +
                "Setting response headers during request has no effect.");
        }
    }
}