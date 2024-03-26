using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Htmxor.DependencyInjection;

internal sealed class EndpointRoutingStateProvider : IRoutingStateProvider
{
    public RouteData? RouteData { get; internal set; }

    public RoutePattern? RoutePattern { get; internal set; }
}