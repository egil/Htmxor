namespace Htmxor;

/// <summary>
/// Creates builder objects to begin the fluent chain for constructing triggers 
/// </summary>
public static class Trigger
{
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
    public static TriggerModifierBuilder OnEvent(string eventName) => new TriggerBuilder().OnEvent(eventName);

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
    public static TriggerModifierBuilder Sse(string sseEventName) => new TriggerBuilder().Sse(sseEventName);

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
    public static TriggerModifierBuilder Load() => new TriggerBuilder().Load();

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
    public static TriggerModifierBuilder Revealed() => new TriggerBuilder().Revealed();

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
    public static TriggerModifierBuilder Intersect(string? root = null, float? threshold = null) => new TriggerBuilder().Intersect(root, threshold);

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
    public static TriggerModifierBuilder Every(TimeSpan interval) => new TriggerBuilder().Every(interval);

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
    public static TriggerBuilder Custom(string triggerDefinition) => new TriggerBuilder().Custom(triggerDefinition);
}
