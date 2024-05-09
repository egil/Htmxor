using Htmxor.Http;

namespace Htmxor;

public sealed class TriggerBuilder : ITriggerBuilder
{
	private readonly List<HtmxTriggerSpecification> triggers = new();

	public TriggerModifierBuilder OnEvent(string eventName)
	{
		var spec = new HtmxTriggerSpecification { Trigger = eventName };
		return new TriggerModifierBuilder(spec, this);
	}

	public TriggerModifierBuilder Sse(string sseEventName)
	{
		var spec = new HtmxTriggerSpecification { Trigger = "sse", SseEvent = sseEventName};
		return new TriggerModifierBuilder(spec, this);
	}

	public TriggerModifierBuilder Load()
	{
		var spec = new HtmxTriggerSpecification { Trigger = "load" };
		return new TriggerModifierBuilder(spec, this);
	}

	public TriggerModifierBuilder Revealed()
	{
		var spec = new HtmxTriggerSpecification { Trigger = "revealed" };
		return new TriggerModifierBuilder(spec, this);
	}

	public TriggerModifierBuilder Intersect(string? root = null, float? threshold = null)
	{
		var thresholdValue = threshold != null
			? Convert.ToString(threshold, System.Globalization.CultureInfo.InvariantCulture)
			: null;

		var spec = new HtmxTriggerSpecification { Trigger = "intersect", Root = root, Threshold = thresholdValue };

		return new TriggerModifierBuilder(spec, this);
	}

	public TriggerModifierBuilder Every(TimeSpan interval)
	{
		var spec = new HtmxTriggerSpecification { Trigger = $"every", PollInterval = (int)interval.TotalMilliseconds };
		return new TriggerModifierBuilder(spec, this);
	}

	internal void AddTrigger(HtmxTriggerSpecification trigger)
	{
		if (!triggers.Contains(trigger))
			triggers.Add(trigger);
	}

	public override string ToString()
	{
		return string.Join(", ", triggers.Select(FormatTrigger));
	}

	public KeyValuePair<string, List<HtmxTriggerSpecification>> Build()
	{
		return new KeyValuePair<string, List<HtmxTriggerSpecification>>(this.ToString(), triggers);
	}

	private string FormatTrigger(HtmxTriggerSpecification spec)
	{
		var parts = new List<string> { spec.Trigger };

		// every 2s
		if (spec.Trigger == "every")
			parts[0] += " " + FormatTimeSpan(TimeSpan.FromMilliseconds(spec.PollInterval ?? 0));

		if (!string.IsNullOrEmpty(spec.EventFilter))
			parts[0] += $"[{spec.EventFilter}]";

		if (!string.IsNullOrEmpty(spec.SseEvent))
			parts.Add($"sseEvent:{spec.SseEvent}");

		if (spec.Once == true)
			parts.Add("once");

		if (spec.Changed == true)
			parts.Add("changed");

		if (spec.Delay.HasValue)
			parts.Add($"delay:{FormatTimeSpan(TimeSpan.FromMilliseconds(spec.Delay.Value))}");

		if (spec.Throttle.HasValue)
			parts.Add($"throttle:{FormatTimeSpan(TimeSpan.FromMilliseconds(spec.Throttle.Value))}");

		if (!string.IsNullOrEmpty(spec.From))
			parts.Add($"from:{spec.From}");

		if (!string.IsNullOrEmpty(spec.Target))
			parts.Add($"target:{spec.Target}");

		if (spec.Consume == true)
			parts.Add("consume");

		if (!string.IsNullOrEmpty(spec.Queue))
			parts.Add($"queue:{spec.Queue}");

		if (!string.IsNullOrEmpty(spec.Root))
			parts.Add($"root:{spec.Root}");

		if (!string.IsNullOrEmpty(spec.Threshold))
			parts.Add($"threshold:{spec.Threshold}");

		return string.Join(" ", parts);
	}

	private static string FormatTimeSpan(TimeSpan timing)
	{
		if (timing.TotalSeconds < 1)
		{
			return $"{timing.TotalMilliseconds}ms";
		}
		return $"{timing.TotalSeconds}s";
	}
}
