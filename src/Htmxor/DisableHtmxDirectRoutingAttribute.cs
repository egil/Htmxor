namespace Htmxor;

/// <summary>
/// Keeps the component's stock Blazor route but removes its direct htmx partial representation.
/// </summary>
/// <remarks>
/// Normal, boosted, and <c>HX-Request-Type: full</c> requests still receive the stock page.
/// This is not an authorization boundary and does not make htmx request headers trusted.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableHtmxDirectRoutingAttribute : Attribute;
