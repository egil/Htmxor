using System.Text.Json.Serialization;

namespace Htmxor;

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

	public override string ToString()
	{
		var parts = new List<string> { Trigger };

		// every 2s
		if (Trigger == Constants.Triggers.Every)
			parts[0] += " " + FormatTimeSpan(TimeSpan.FromMilliseconds(PollInterval ?? 0));

		if (!string.IsNullOrEmpty(EventFilter))
			parts[0] += $"[{EventFilter}]";

		if (!string.IsNullOrEmpty(SseEvent))
			parts.Add($"{Constants.TriggerModifiers.SseEvent}:{SseEvent}");

		if (Once == true)
			parts.Add(Constants.TriggerModifiers.Once);

		if (Changed == true)
			parts.Add(Constants.TriggerModifiers.Changed);

		if (Delay.HasValue)
			parts.Add($"{Constants.TriggerModifiers.Delay}:{FormatTimeSpan(TimeSpan.FromMilliseconds(Delay.Value))}");

		if (Throttle.HasValue)
			parts.Add($"{Constants.TriggerModifiers.Throttle}:{FormatTimeSpan(TimeSpan.FromMilliseconds(Throttle.Value))}");

		if (!string.IsNullOrEmpty(From))
			parts.Add($"{Constants.TriggerModifiers.From}:{FormatExtendedCssSelector(From)}");

		if (!string.IsNullOrEmpty(Target))
			parts.Add($"{Constants.TriggerModifiers.Target}:{FormatCssSelector(Target)}");

		if (Consume == true)
			parts.Add(Constants.TriggerModifiers.Consume);

		if (!string.IsNullOrEmpty(Queue))
			parts.Add($"{Constants.TriggerModifiers.Queue}:{Queue}");

		if (!string.IsNullOrEmpty(Root))
			parts.Add($"{Constants.TriggerModifiers.Root}:{FormatCssSelector(Root)}");

		if (!string.IsNullOrEmpty(Threshold))
			parts.Add($"{Constants.TriggerModifiers.Threshold}:{Threshold}");

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

	private static string FormatExtendedCssSelector(string cssSelector)
	{
		ReadOnlySpan<string> keywords = ["closest", "find", "next", "previous"];
		cssSelector = cssSelector.TrimStart();

		foreach (var keyword in keywords)
		{
			if (cssSelector.StartsWith(keyword + " ", StringComparison.InvariantCulture))
			{
				var selector = cssSelector.Substring(keyword.Length + 1);

				return keyword + " " + FormatCssSelector(selector);
			}
		}

		return FormatCssSelector(cssSelector);
	}

	private static string FormatCssSelector(string cssSelector)
	{
		cssSelector = cssSelector.Trim();

		if ((cssSelector.StartsWith('{') && cssSelector.EndsWith('}')) ||
			(cssSelector.StartsWith('(') && cssSelector.EndsWith(')')))
		{
			return cssSelector;

		}
		else if (cssSelector.Any(char.IsWhiteSpace))
		{
			return "{" + cssSelector + "}";
		}

		return cssSelector;
	}
}
