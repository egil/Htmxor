using Htmxor.Http;
using static Htmxor.Constants;
using System.Runtime.CompilerServices;

namespace Htmxor;

/// <summary>
/// Provides methods to add modifiers to htmx trigger definitions within a <see cref="TriggerBuilder"/> context.
/// </summary>
/// <remarks>
/// The <see cref="TriggerModifierBuilder"/> class extends the functionality of the <see cref="TriggerBuilder"/> 
/// by allowing the addition of conditions, delays, throttles, source elements, target filters, consumption flags, 
/// and queuing options to the htmx trigger definitions.
/// </remarks>
public sealed class TriggerModifierBuilder : ITriggerBuilder
{
	private readonly HtmxTriggerSpecification specification;
	private readonly TriggerBuilder parentBuilder;

	internal TriggerModifierBuilder(HtmxTriggerSpecification specification, TriggerBuilder parentBuilder)
	{
		this.specification = specification;
		this.parentBuilder = parentBuilder;

		parentBuilder.AddTrigger(specification);
	}

	/// <summary>
	/// Adds a condition to the trigger by setting an event filter <c>[<paramref name="condition"/>]</c>.
	/// </summary>
	/// <remarks>
	/// This method adds a JavaScript expression as a condition for the event to be triggered.
	/// </remarks>
	/// <param name="condition">The JavaScript expression to evaluate.</param>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").WithCondition("ctrlKey")
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/clicked" hx-trigger="click[ctrlKey]">Control Click Me]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder WithCondition(string condition)
	{
		specification.EventFilter = condition;
		return this;
	}

	/// <summary>
	/// Specifies that the event should trigger only once by adding the modifier <c>once</c>.
	/// </summary>
	/// <remarks>
	/// This method adds the "once" modifier to the trigger, making it fire only the first time the event occurs.
	/// </remarks>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").Once()
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/clicked" hx-trigger="click once">Click Me Once]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Once()
	{
		specification.Once = true;
		return this;
	}

	/// <summary>
	/// Specifies that the event should trigger only when the value changes by adding the modifier <c>changed</c>.
	/// </summary>
	/// <remarks>
	/// This method adds the "changed" modifier to the trigger, making it fire only when the value of the element has changed.
	/// </remarks>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("keyup").Changed().Delay(TimeSpan.FromSeconds(1))
	/// // Resulting hx-trigger: <![CDATA[<input hx-get="/search" hx-trigger="keyup changed delay:1s"/>]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Changed()
	{
		specification.Changed = true;
		return this;
	}

	/// <summary>
	/// Adds a delay before the event triggers the request by adding the modifier <c>delay:<paramref name="timing"/></c>.
	/// </summary>
	/// <remarks>
	/// This method adds a delay to the trigger, resetting the delay if the event occurs again before the delay completes.
	/// </remarks>
	/// <param name="timing">The delay time as a <see cref="TimeSpan"/>.</param>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("keyup").Delay(TimeSpan.FromSeconds(1))
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/search" hx-trigger="keyup delay:1s">Search Me]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Delay(TimeSpan timing)
	{
		specification.Delay = (int)timing.TotalMilliseconds;
		return this;
	}

	/// <summary>
	/// Adds a throttle after the event triggers the request by adding the modifier <c>throttle:<paramref name="timing"/></c>.
	/// </summary>
	/// <remarks>
	/// This method adds a throttle to the trigger, ignoring subsequent events until the throttle period completes.
	/// </remarks>
	/// <param name="timing">The throttle time as a <see cref="TimeSpan"/>.</param>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").Throttle(TimeSpan.FromSeconds(2))
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/updates" hx-trigger="click throttle:2s">Click Me]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Throttle(TimeSpan timing)
	{
		specification.Throttle = (int)timing.TotalMilliseconds;
		return this;
	}

	/// <summary>
	/// Specifies that the event should trigger from another element by adding the modifier <c>from:<paramref name="extendedCssSelector"/></c>.
	/// </summary>
	/// <remarks>
	/// This method allows listening to events on a different element specified by the extended CSS selector.
	/// </remarks>
	/// <param name="extendedCssSelector">The extended CSS selector of the element to listen for events from.</param>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("keydown").From("document")
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/hotkeys" hx-trigger="keydown from:document">Listen on Document]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder From(string extendedCssSelector)
	{
		specification.From = extendedCssSelector;
		return this;
	}

	/// <summary>
	/// Filters the event trigger to a specific target element by adding the modifier <c>target:<paramref name="cssSelector"/></c>.
	/// </summary>
	/// <remarks>
	/// This method allows the event trigger to be filtered to a target element specified by the CSS selector.
	/// </remarks>
	/// <param name="cssSelector">The CSS selector of the target element.</param>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").Target(".child")
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/dynamic" hx-trigger="click target:.child">Click Child]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Target(string cssSelector)
	{
		specification.Target = cssSelector;
		return this;
	}

	/// <summary>
	/// Specifies that the event should not trigger any other htmx requests on parent elements by adding the modifier <c>consume</c>.
	/// </summary>
	/// <remarks>
	/// This method prevents the event from triggering other htmx requests on parent elements.
	/// </remarks>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").Consume()
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/click" hx-trigger="click consume">Click Me]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Consume()
	{
		specification.Consume = true;
		return this;
	}

	/// <summary>
	/// Specifies how events are queued when an event occurs while a request is in flight by adding the modifier <c>queue:<paramref name="option"/></c>.
	/// </summary>
	/// <remarks>
	/// This method sets the event queuing option, such as "first", "last", "all", or "none".
	/// </remarks>
	/// <param name="option">(optional) The queue option as a <see cref="TriggerQueueOption"/>.</param>
	/// <returns>This <see cref="TriggerModifierBuilder"/> instance.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").Queue(TriggerQueueOption.All)
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/process" hx-trigger="click queue:all">Queue All]]>
	/// </code>
	/// </example>
	public TriggerModifierBuilder Queue(TriggerQueueOption option = TriggerQueueOption.Last)
	{
		var value = option switch
		{
			TriggerQueueOption.First => "first",
			TriggerQueueOption.Last => "last",
			TriggerQueueOption.None => "none",
			TriggerQueueOption.All => "all",
			_ => throw new SwitchExpressionException(option),
		};

		specification.Queue = value;

		return this;
	}

	/// <summary>
	/// Combines multiple triggers for a single element, allowing continuation of trigger configuration.
	/// </summary>
	/// <remarks>
	/// This method is used to combine multiple triggers for a single element, returning the parent builder for further configuration.
	/// </remarks>
	/// <returns>The parent <see cref="TriggerBuilder"/> instance for additional configuration.</returns>
	/// <example>
	/// <code>
	/// Trigger.OnEvent("click").Or().Load()
	/// // Resulting hx-trigger: <![CDATA[<div hx-get="/news" hx-trigger="click, load">Click or Load]]>
	/// </code>
	/// </example>
	public TriggerBuilder Or()
	{
		return parentBuilder;
	}

	public KeyValuePair<string, List<HtmxTriggerSpecification>> Build() => parentBuilder.Build();

	public override string ToString() => parentBuilder.ToString();
}

