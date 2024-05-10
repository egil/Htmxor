using Htmxor.Http;

namespace Htmxor;


/// <summary>
/// Provides methods to construct and manage htmx trigger definitions for htmx requests.
/// </summary>
/// <remarks>
/// The <see cref="TriggerBuilder"/> class offers a fluent API to specify various triggers for htmx requests. 
/// It allows the combination of standard events, server-sent events, and custom triggers, as well as modifiers 
/// to control the behavior of these triggers.
/// </remarks>
/// <example>
/// <code>
/// var trigger = Trigger
///     .OnEvent("click")
///     .WithCondition("ctrlKey")
///     .Delay(TimeSpan.FromSeconds(1))
///     .Throttle(TimeSpan.FromSeconds(2))
///     .From("document")
///     .Target(".child")
///     .Consume()
///     .Queue(TriggerQueueOption.All);
///     
/// string triggerDefinition = trigger.ToString();
/// // Resulting hx-trigger: "click[ctrlKey] delay:1s throttle:2s from:document target:.child consume queue:all"
/// </code>
/// </example>
public sealed class TriggerBuilder : ITriggerBuilder
{
	private readonly List<HtmxTriggerSpecification> triggers = new();

	/// <summary>
	/// Specifies a standard event as the trigger by setting the event name <c>hx-trigger="<paramref name="eventName"/>"</c>.
	/// </summary>
	/// <remarks>
	/// This method builds a javascript event trigger. For example, specifying "click" will trigger the request on a click event.
	/// </remarks>
	/// <param name="eventName">The name of the standard event (e.g., "click").</param>
	/// <returns>A <see cref="TriggerModifierBuilder"/> instance to allow further configuration of the trigger.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click")
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/clicked" hx-trigger="click">Click Me</div>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder OnEvent(string eventName)
	{
		var spec = new HtmxTriggerSpecification { Trigger = eventName };
		return new TriggerModifierBuilder(spec, this);
	}

	/// <summary>
	/// Specifies a Server-Sent Event (SSE) as the trigger by setting the event name and SSE event <c>hx-trigger="sse: <paramref name="sseEventName"/>"</c>.
	/// </summary>
	/// <remarks>
	/// This method sets the SSE trigger for an AJAX request. For example, specifying "message" will trigger the request on the message event.
	/// </remarks>
	/// <param name="sseEventName">The name of the SSE event.</param>
	/// <returns>A <see cref="TriggerModifierBuilder"/> instance to allow further configuration of the trigger.</returns>
	/// <example>
	/// <code>
	/// Trigger.Sse("message")
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/updates" hx-trigger="sse:message">Update Me</div>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Sse(string sseEventName)
	{
		var spec = new HtmxTriggerSpecification { Trigger = "sse", SseEvent = sseEventName};
		return new TriggerModifierBuilder(spec, this);
	}

	/// <summary>
	/// Specifies that the trigger occurs on page load by setting the event name <c>hx-trigger="load"</c>.
	/// </summary>
	/// <remarks>
	/// This method sets the load event trigger, useful for lazy-loading content.
	/// </remarks>
	/// <returns>A <see cref="TriggerModifierBuilder"/> instance to allow further configuration of the trigger.</returns>
	/// <example>
	/// <code>
	/// Trigger.Load()
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/load" hx-trigger="load">Load Me</div>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Load()
	{
		var spec = new HtmxTriggerSpecification { Trigger = "load" };
		return new TriggerModifierBuilder(spec, this);
	}

	/// <summary>
	/// Specifies that the trigger occurs when an element is scrolled into the viewport by setting the event name <c>hx-trigger="revealed"</c>.
	/// </summary>
	/// <remarks>
	/// This method sets the revealed event trigger for an AJAX request, useful for lazy-loading content as it enters the viewport.
	/// </remarks>
	/// <returns>A <see cref="TriggerModifierBuilder"/> instance to allow further configuration of the trigger.</returns>
	/// <example>
	/// <code>
	/// Trigger.Revealed()
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/load" hx-trigger="revealed">Reveal Me</div>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Revealed()
	{
		var spec = new HtmxTriggerSpecification { Trigger = "revealed" };
		return new TriggerModifierBuilder(spec, this);
	}

	/// <summary>
	/// Specifies that the trigger occurs when an element intersects the viewport by setting the event name <c>hx-trigger="intersect"</c>.
	/// </summary>
	/// <remarks>
	/// This method sets the intersect event trigger for an AJAX request, which fires when the element first intersects the viewport. Additional options include <c>root:<paramref name="root"/></c> and <c>threshold:<paramref name="threshold"/></c>.
	/// </remarks>
	/// <param name="root">(optional) The CSS selector of the root element for intersection.</param>
	/// <param name="threshold">(optional) A floating point number between 0.0 and 1.0 indicating what amount of intersection to fire the event on.</param>
	/// <returns>A <see cref="TriggerModifierBuilder"/> instance to allow further configuration of the trigger.</returns>
	/// <example>
	/// <code>
	/// Trigger.Intersect(root: ".container", threshold: 0.5f)
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/load" hx-trigger="intersect root:.container threshold:0.5">Intersect Me</div>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Intersect(string? root = null, float? threshold = null)
	{
		var thresholdValue = threshold != null
			? Convert.ToString(threshold, System.Globalization.CultureInfo.InvariantCulture)
			: null;

		var spec = new HtmxTriggerSpecification { Trigger = "intersect", Root = root, Threshold = thresholdValue };

		return new TriggerModifierBuilder(spec, this);
	}

	/// <summary>
	/// Specifies that the trigger occurs at regular intervals by setting the event name <c>hx-trigger="every <paramref name="interval"/> "</c>.
	/// </summary>
	/// <remarks>
	/// This method sets the polling interval for an AJAX request. For example, specifying an interval of 1 second will trigger the request every second.
	/// </remarks>
	/// <param name="interval">The interval at which to poll, as a <see cref="TimeSpan"/>.</param>
	/// <returns>A <see cref="TriggerModifierBuilder"/> instance to allow further configuration of the trigger.</returns>
	/// <example>
	/// <code>
	/// Trigger.Every(TimeSpan.FromSeconds(5))
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/updates" hx-trigger="every 5s">Update Every 5s</div>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Every(TimeSpan interval)
	{
		var spec = new HtmxTriggerSpecification { Trigger = $"every", PollInterval = (int)interval.TotalMilliseconds };
		return new TriggerModifierBuilder(spec, this);
	}

	/// <summary>
	/// Specifies a custom trigger that will be included without changes <c>hx-trigger="<paramref name="triggerDefinition"/>"</c>.
	/// </summary>
	/// <remarks>
	/// This method sets a custom trigger definition.
	/// </remarks>
	/// <param name="triggerDefinition">The custom trigger definition string.</param>
	/// <returns>This <see cref="TriggerBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.Custom("custom-event delay:2s")
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/custom" hx-trigger="custom-event delay:2s">Custom Event</div>]]>
	/// </code>
	/// </example>
	public TriggerBuilder Custom(string triggerDefinition)
	{
		var spec = new HtmxTriggerSpecification { Trigger = triggerDefinition };
		triggers.Add(spec);
		return this;
	}

	internal void AddTrigger(HtmxTriggerSpecification trigger)
	{
		if (!triggers.Contains(trigger))
			triggers.Add(trigger);
	}

	/// <summary>
	/// Returns a properly formatted trigger definition that can be used as an hx-trigger value
	/// </summary>
	/// <returns>trigger definition</returns>
	public override string ToString()
	{
		return string.Join(", ", triggers.Select(FormatTrigger));
	}

	/// <summary>
	/// Builds a key value pair mapping the formatted trigger definition to the <see cref="HtmxTriggerSpecification"/>
	/// </summary>
	/// <returns></returns>
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
			parts.Add($"from:{FormatExtendedCssSelector(spec.From)}");

		if (!string.IsNullOrEmpty(spec.Target))
			parts.Add($"target:{FormatCssSelector(spec.Target)}");

		if (spec.Consume == true)
			parts.Add("consume");

		if (!string.IsNullOrEmpty(spec.Queue))
			parts.Add($"queue:{spec.Queue}");

		if (!string.IsNullOrEmpty(spec.Root))
			parts.Add($"root:{FormatCssSelector(spec.Root)}");

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

	private static string FormatExtendedCssSelector(string cssSelector)
	{
		string[] keywords = ["closest", "find", "next", "previous"];
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
