using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Htmxor.DependencyInjection;

internal sealed class HtmxorEndpointRoutingStateProvider : IRoutingStateProvider
{
    [DynamicallyAccessedMembers(LinkerFlags.Component)]
    public Type? LayoutType { get; internal set; }

    public RouteData? RouteData { get; internal set; }

    public RoutePattern? RoutePattern { get; internal set; }
}