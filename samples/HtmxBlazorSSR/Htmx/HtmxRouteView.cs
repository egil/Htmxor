using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HtmxBlazorSSR.Htmx;

/// <summary>
/// Displays the specified page component, rendering it inside its layout
/// and any further nested layouts.
/// </summary>
public class HtmxRouteView : RouteView
{
    [Inject]
    private HtmxContext Context { get; set; } = default!;

    protected override void Render(RenderTreeBuilder builder)
    {
        if (Context.Request.IsHtmxRequest && Context.Request.Trigger is not null)
        {
            builder.OpenComponent(0, RouteData.PageType);
            foreach (var kvp in RouteData.RouteValues)
            {
                builder.AddComponentParameter(1, kvp.Key, kvp.Value);
            }
            builder.CloseComponent();
        }
        else
        {
            base.Render(builder);
        }
    }
}