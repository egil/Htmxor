using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor;

/// <summary>
/// Indicates that the associated component should match the specified route template pattern and one or more of the optional properties.
/// </summary>
/// <remarks>
/// If one or more additional properties is specified on the attribute, all specified properties much match for the route to be used.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HxRouteAttribute : Attribute, IEquatable<HxRouteAttribute>
{
    public static readonly IReadOnlyList<string> DefaultHttpMethods = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    /// <summary>
    /// Gets the route template.
    /// </summary>
    [StringSyntax("Route")]
    public string Template { get; }

    /// <summary>
    /// Gets the HTTP methods supported by the route.
    /// If null or empty, this route allow all HTTP methods Htmx supports (<see cref="DefaultHttpMethods"/>).
    /// </summary>
    public IReadOnlyList<string> Methods { get; init; } = DefaultHttpMethods;

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
    public IReadOnlyList<string> Targets { get; init; } = [];

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

    public override bool Equals(object? obj)
    {
        return Equals(obj as HxRouteAttribute);
    }

    public bool Equals(HxRouteAttribute? other)
    {
        return other is not null
            && Template.Equals(other.Template, StringComparison.OrdinalIgnoreCase)
            && Methods.SequenceEqual(other.Methods)
            && (CurrentURL?.Equals(other.CurrentURL, StringComparison.OrdinalIgnoreCase) ?? true)
            && (Target?.Equals(other.Target, StringComparison.OrdinalIgnoreCase) ?? true)
            && Targets.SequenceEqual(other.Targets, StringComparer.OrdinalIgnoreCase)
            && (Trigger?.Equals(other.Trigger, StringComparison.OrdinalIgnoreCase) ?? true)
            && (TriggerName?.Equals(other.TriggerName, StringComparison.OrdinalIgnoreCase) ?? true);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Template);
        for (int i = 0; i < Methods.Count; i++)
        {
            hash.Add(Methods[i]);
        }

        if (CurrentURL is not null)
        {
            hash.Add(CurrentURL);
        }

        if (Target is not null)
        {
            hash.Add(Target);
        }

        for (int i = 0; i < Targets.Count; i++)
        {
            hash.Add(Targets[i]);
        }

        if (Trigger is not null)
        {
            hash.Add(Trigger);
        }

        if (TriggerName is not null)
        {
            hash.Add(TriggerName);
        }

        return hash.ToHashCode();
    }
}
