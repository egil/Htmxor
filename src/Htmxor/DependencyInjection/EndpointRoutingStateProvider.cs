using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Htmxor.DependencyInjection;

internal sealed class EndpointRoutingStateProvider : IRoutingStateProvider
{
    public RouteData? RouteData { get; internal set; }
}