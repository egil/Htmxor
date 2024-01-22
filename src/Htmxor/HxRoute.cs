using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;

namespace Htmxor;

/// <summary>
/// Indicates that the associated component should match the specified route template pattern and one or more of the optional properties.
/// </summary>
/// <remarks>
/// If one or more additional properties is specified on the attribute, all specified properties much match for the route to be used.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HxRouteAttribute : Attribute
{
    /// <summary>
    /// Gets the route template.
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Uri)]
    public string Template { get; }

    /// <summary>
    /// Gets the HTTP methods supported by the route.
    /// </summary>
    public HttpMethod Methods { get; init; } = HttpMethod.All;

    /// <summary>
    /// Specify to only use this route if the <see cref="HtmxRequestHeaderNames.CurrentURL"/> header matches the specified value. 
    /// If null or whitespace, this route is not limited to a specific URL.
    /// </summary>
    public string? CurrentURL { get; init; }

    /// <summary>
    /// Specify to only use this route if the <see cref="HtmxRequestHeaderNames.Target"/> header matches the specified value. 
    /// If null or whitespace, this route is not limited to a specific target.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Specify to only use this route if the <see cref="HtmxRequestHeaderNames.Target"/> header matches one of the specified values.
    /// If null or empty, this route is not limited to a specific set of targets.
    /// </summary>
    public string[]? Targets { get; init; }

    /// <summary>
    /// Specify to only use this route if the <see cref="HtmxRequestHeaderNames.Trigger"/> header matches the specified value. 
    /// If null or whitespace, this route is not limited to a specific trigger.
    /// </summary>
    public string? Trigger { get; init; }

    /// <summary>
    /// Specify to only use this route if the <see cref="HtmxRequestHeaderNames.TriggerName"/> header matches the specified value. 
    /// If null or whitespace, this route is not limited to a specific trigger name.
    /// </summary>
    public string? TriggerName { get; init; }

    /// <summary>
    /// Constructs an instance of <see cref="HxRouteAttribute"/>.
    /// </summary>
    /// <param name="template">The route template.</param>
    public HxRouteAttribute([StringSyntax(StringSyntaxAttribute.Uri)] string template)
    {
        Template = template;
    }
}
