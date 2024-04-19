using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Htmxor.Configuration;
using Htmxor.Http.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Http;

public class HtmxResponse
{
    private readonly IHeaderDictionary _headers = context.Response.Headers;

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, false)
        }
    };

    /// <summary>
    ///     Allows you to do a client-side redirect that does not do a full page reload.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public HtmxResponse Location(string path, AjaxContext? context = null)
    {
        if (context == null)
	        _headers[HtmxResponseHeaderNames.Location] = path;
        else
        {
	        JsonObject json = new();
            json.Add("path", JsonValue.Create(path));

            var ctxNode = JsonSerializer.SerializeToNode(context)!.AsObject();

            foreach (var prop in ctxNode.AsEnumerable())
            {
                if (prop.Value != null)
	                json.Add(prop.Key, prop.Value.DeepClone());
            }

            _headers[HtmxResponseHeaderNames.Location] = JsonSerializer.Serialize(json);
        }

        return this;
    }

    /// <summary>
    ///     Pushes a new url onto the history stack.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HtmxResponse PushUrl(string url)
    {
        _headers[HtmxResponseHeaderNames.PushUrl] = url;

        return this;
    }

    /// <summary>
    ///     Prevents the browser’s history from being updated.
    ///     Overwrites PushUrl response if already present.
    /// </summary>
    /// <returns></returns>
    public HtmxResponse PreventBrowserHistoryUpdate()
    {
	    _headers[HtmxResponseHeaderNames.PushUrl] = "false";

	    return this;
    }

    /// <summary>
    ///     Prevents the browser’s current url from being updated
    ///     Overwrites ReplaceUrl response if already present.
    /// </summary>
    /// <returns></returns>
    public HtmxResponse PreventBrowserCurrentUrlUpdate()
    {
	    _headers[HtmxResponseHeaderNames.ReplaceUrl] = "false";

	    return this;
    }

    /// <summary>
    ///     Can be used to do a client-side redirect to a new location.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HtmxResponse Redirect(string url)
    {
        _headers[HtmxResponseHeaderNames.Redirect] = url;

        return this;
    }

    /// <summary>
    ///     Enables a client-side full refresh of the page
    /// </summary>
    /// <returns></returns>
    public HtmxResponse Refresh()
    {
        _headers[HtmxResponseHeaderNames.Refresh] = "true";

        return this;
    }

    /// <summary>
    ///     Replaces the current URL in the location bar
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HtmxResponse ReplaceUrl(string url)
    {
        _headers[HtmxResponseHeaderNames.ReplaceUrl] = url;

        return this;
    }

    /// <summary>
    ///     Allows you to specify how the response will be swapped.
    /// </summary>
    /// <param name="swapStyle"></param>
    /// <returns></returns>
    public HtmxResponse Reswap(SwapStyle swapStyle)
    {
        var style = swapStyle switch
        {
            SwapStyle.InnerHTML => "innerHTML",
            SwapStyle.OuterHTML => "outerHTML",
            _ => swapStyle.ToString().ToLowerInvariant()
        };

        _headers[HtmxResponseHeaderNames.Reswap] = style;

        return this;
    }

    /// <summary>
    ///     A CSS selector that updates the target of the content update to a different element on the page.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    public HtmxResponse Retarget(string selector)
    {
        _headers[HtmxResponseHeaderNames.Retarget] = selector;

        return this;
    }

    /// <summary>
    ///     A CSS selector that allows you to choose which part of the response is used to be swapped in.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    public HtmxResponse Reselect(string selector)
    {
        _headers[HtmxResponseHeaderNames.Reselect] = selector;

        return this;
    }

    /// <summary>
    ///     Allows you to trigger client-side events.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="detail"></param>
    /// <param name="timing"></param>
    /// <returns></returns>
    public HtmxResponse Trigger(string eventName, object? detail = null, TriggerTiming timing = TriggerTiming.Default)
    {
        var headerKey = timing switch
        {
            TriggerTiming.AfterSwap => HtmxResponseHeaderNames.TriggerAfterSwap,
            TriggerTiming.AfterSettle => HtmxResponseHeaderNames.TriggerAfterSettle,
            _ => HtmxResponseHeaderNames.Trigger
        };

        MergeTrigger(headerKey, eventName, detail);

        return this;
    }

    /// <summary>
    ///     Aggregate existing headers and merge event with detail into the result
    /// </summary>
    /// <param name="headerKey"></param>
    /// <param name="eventName"></param>
    /// <param name="detail"></param>
    private void MergeTrigger(string headerKey, string eventName, object? detail = null)
    {
        var (sb, isComplex) = BuildExistingTriggerJson(headerKey);

        // If this event doesn't have a detail and any existing events also
        // don't have details we can simplify the output to comma-delimited event names
        if (detail == null && !isComplex)
        {
            var header = _headers[headerKey];
            _headers[headerKey] = StringValues.Concat(header, eventName).ToString();
        }
        else
        {
            var detailJson = JsonSerializer.Serialize(detail, _serializerOptions);

            if (sb.Length > 0) sb.Append(',');

            // Append the key/value to the output json
            sb.Append($"\"{eventName}\":{detailJson}");

            // Wrap the entire sb contents to turn it into valid json
            sb.Insert(0, '{');
            sb.Append('}');

            _headers[headerKey] = sb.ToString();
        }
    }

    /// <summary>
    ///     Create a stringBuilder containing the serialized json for the aggregated properties across
    ///     all header values that exist for this header key - duplicate keys are not removed for performance
    ///     reasons because the json produced is still valid
    ///     This approach does not validate any syntax of existing headers as a performance consideration.
    /// </summary>
    /// <param name="headerKey"></param>
    /// <returns></returns>
    private (StringBuilder, bool) BuildExistingTriggerJson(string headerKey)
    {
        var isComplex = false;
        StringBuilder sb = new();

        var header = _headers[headerKey];

        // header as StringValues can have no values, one value, or many values
        // so foreach is safest way to iterate through multiple possible headers
        foreach (var headerValue in header)
        {
            if (headerValue is null) continue;

            // Is this headerValue possibly a Json object?
            if (headerValue.StartsWith("{"))
            {
                isComplex = true;

                if (sb.Length > 0) sb.Append(',');
                sb.Append(headerValue.Substring(1, headerValue.Length - 2));
            }
            else
            {
                var eventNames = headerValue.Split(',');

                foreach (var name in eventNames)
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append("\"" + name + "\":\"\"");
                }
            }
        }

        return (sb, isComplex);
    }
}