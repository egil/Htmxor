using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Htmxor.DependencyInjection;

internal sealed class EndpointRoutingStateProvider : IRoutingStateProvider
{
    public RouteData? RouteData { get; internal set; }
}