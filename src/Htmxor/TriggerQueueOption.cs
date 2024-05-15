// ReSharper disable InconsistentNaming
namespace Htmxor;

/// <summary>
/// Determines how events are queued if an event occurs while a request for another event is in flight.
/// </summary>
public enum TriggerQueueOption
{
    /// <summary>
    /// queue the first event
    /// </summary>
    first,

    /// <summary>
    /// queue the last event (default)
    /// </summary>
    last,

    /// <summary>
    /// queue all events (issue a request for each event)
    /// </summary>
    all,

    /// <summary>
    /// do not queue new events
    /// </summary>
    none
}
