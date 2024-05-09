using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Htmxor;

/// <summary>
/// htmx configuration allows for the creation of a trigger specification cache in order to improve
/// trigger-handling performance.  The cache is a key/value store mapping well-formed hx-trigger parameters
/// to their parsed specifications.
/// </summary>
public class TriggerSpecificationCache : Dictionary<string, List<HtmxTriggerSpecification>>
{
	public TriggerSpecificationCache() : base()
	{
	}

	public TriggerSpecificationCache(IEnumerable<KeyValuePair<string, List<HtmxTriggerSpecification>>> triggers)
		: base(triggers)
	{
	}

	public TriggerSpecificationCache(params KeyValuePair<string, List<HtmxTriggerSpecification>>[] triggers) : base (triggers)
	{
	}

	public TriggerSpecificationCache(params ITriggerBuilder[] builders)
	{
		builders ??= Array.Empty<ITriggerBuilder>();

		foreach (var triggerBuilder in builders)
		{
			var trigger = triggerBuilder.Build();
			Add(trigger.Key, trigger.Value);
		}
	}

}

/// <summary>
/// Represents an htmx trigger definition
/// </summary>
public sealed record HtmxTriggerSpecification
{
	[JsonPropertyName("trigger")]
	public string Trigger { get; set; } = string.Empty;
	[JsonPropertyName("sseEvent")]
	public string? SseEvent { get; set; }
	[JsonPropertyName("eventFilter")]
	public string? EventFilter { get; set; }
	[JsonPropertyName("changed")]
	public bool? Changed { get; set; }
	[JsonPropertyName("once")]
	public bool? Once { get; set; }
	[JsonPropertyName("consume")]
	public bool? Consume { get; set; }
	[JsonPropertyName("from")]
	public string? From { get; set; }
	[JsonPropertyName("target")]
	public string? Target { get; set; }
	[JsonPropertyName("throttle")]
	public int? Throttle { get; set; }
	[JsonPropertyName("queue")]
	public string? Queue { get; set; }
	[JsonPropertyName("root")]
	public string? Root { get; set; }
	[JsonPropertyName("threshold")]
	public string? Threshold { get; set; }
	[JsonPropertyName("delay")]
	public int? Delay { get; set; }
	[JsonPropertyName("pollInterval")]
	public int? PollInterval { get; set; }
}
