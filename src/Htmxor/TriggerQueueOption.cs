namespace Htmxor;

/// <summary>
/// Determines how events are queued if an event occurs while a request for another event is in flight.
/// </summary>
public enum TriggerQueueOption
{
    /// <summary>
    /// queue the first event
    /// </summary>
    First,

    /// <summary>
    /// queue the last event (default)
    /// </summary>
    Last,

    /// <summary>
    /// queue all events (issue a request for each event)
    /// </summary>
    All,

    /// <summary>
    /// do not queue new events
    /// </summary>
    None
}
