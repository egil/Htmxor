using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Htmxor.Configuration;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public class HtmxResponse(HttpContext context)
{
	private readonly IHeaderDictionary _headers = context.Response.Headers;

	private readonly JsonSerializerOptions _serializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
		},
	};

    /// <summary>
    /// Allows you to do a client-side redirect that does not do a full page reload.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public HtmxResponse Location(string url)
	{
        _headers[HtmxResponseHeaderNames.Location] = url;

		return this;
    }

    /// <summary>
    /// Pushes a new url onto the history stack.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HtmxResponse PushUrl(string url)
	{
		_headers[HtmxResponseHeaderNames.PushUrl] = url;

        return this;
	}

    /// <summary>
    /// Can be used to do a client-side redirect to a new location.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
	public HtmxResponse Redirect(string url)
	{
		_headers[HtmxResponseHeaderNames.Redirect] = url;

        return this;
	}

    /// <summary>
    /// Enables a client-side full refresh of the page
    /// </summary>
    /// <returns></returns>
    public HtmxResponse Refresh ()
    {
	    _headers[HtmxResponseHeaderNames.Refresh] = "true";

	    return this;
    }

    /// <summary>
    /// Replaces the current URL in the location bar
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HtmxResponse ReplaceUrl(string url)
    {
	    _headers[HtmxResponseHeaderNames.ReplaceUrl] = url;

		return this;
    }

    /// <summary>
    /// Allows you to specify how the response will be swapped.
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
    /// A CSS selector that updates the target of the content update to a different element on the page.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    public HtmxResponse Retarget(string selector)
    {
	    _headers[HtmxResponseHeaderNames.Retarget] = selector;

        return this;
    }

    /// <summary>
    /// A CSS selector that allows you to choose which part of the response is used to be swapped in.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
	public HtmxResponse Reselect(string selector)
	{
		_headers[HtmxResponseHeaderNames.Reselect] = selector;

        return this;
	}

    /// <summary>
    /// Allows you to trigger client-side events.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="detail"></param>
    /// <param name="timing"></param>
    /// <returns></returns>
    public HtmxResponse Trigger(string eventName, object? detail = null, TriggerStyle timing = TriggerStyle.Default)
    {
	    var headerKey = timing switch
	    {
		    TriggerStyle.AfterSwap => HtmxResponseHeaderNames.TriggerAfterSwap,
		    TriggerStyle.AfterSettle => HtmxResponseHeaderNames.TriggerAfterSettle,
		    _ => HtmxResponseHeaderNames.Trigger
	    };

	    MergeTrigger(headerKey, eventName, detail);

		return this;
	}

    /// <summary>
    /// Clean up any duplicated headers and merge event with detail into the result
    /// </summary>
    /// <param name="headerKey"></param>
    /// <param name="eventName"></param>
    /// <param name="detail"></param>
    private void MergeTrigger(string headerKey, string eventName, object? detail = null)
    {
	    var (json, isComplex) = BuildExistingTriggerJson(headerKey);

        // If this event doesn't have a detail and any existing events also
        // don't have details we can simplify the output to comma-delimited event names
        if (detail == null && !isComplex)
        {
	        bool exists = false;
	        List<string> events = new();

            foreach (var property in json.AsEnumerable())
	        {
                events.Add(property.Key);

                if (property.Key == eventName)
	                exists = true;
	        }

            // Add additional event
            if (!exists)
		        events.Add(eventName);

            _headers[headerKey] = String.Join(',', events);
        }
        else
        {
	        var detailNode = JsonSerializer.SerializeToNode(detail, _serializerOptions);

	        if (!json.TryAdd(eventName, detailNode ?? JsonValue.Create(string.Empty)))
	        {
		        json[eventName] = detailNode ?? JsonValue.Create(string.Empty);
	        }

	        _headers[headerKey] = json.ToJsonString(_serializerOptions);
        }
    }

    /// <summary>
    /// Create a JsonObject representing the aggregated properties across
    /// all header values that exist for this header key
    /// </summary>
    /// <param name="headerKey"></param>
    /// <returns></returns>
    private (JsonObject, bool) BuildExistingTriggerJson(string headerKey)
    {
	    bool isComplex = false;
	    var json = new JsonObject();
	    var header = _headers[headerKey];

	    foreach (var headerValue in header)
	    {
		    if (headerValue is null) continue;

		    if (headerValue.StartsWith("{"))
		    {
			    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(headerValue));
			    var detail = JsonNode.Parse(ref reader)?.AsObject();

			    if (detail is null) continue;

                // Once we see a single instance of a json header we assume
                // the header is complex
			    isComplex = true;

			    foreach (var property in detail.AsEnumerable())
			    {
				    var clone = property.Value?.DeepClone();

                    if (!json.TryAdd(property.Key, clone))
				    {
					    json[property.Key] = clone;
 				    }
			    }
            }
		    else
		    {
			    var eventNames = headerValue.Split(",");

			    foreach (var eventName in eventNames)
			    {
				    if (!json.TryAdd(eventName, JsonValue.Create(string.Empty)))
				    {
					    json[eventName] = JsonValue.Create(string.Empty);
				    }
                }
		    }
	    }

	    return (json, isComplex);
    }
}