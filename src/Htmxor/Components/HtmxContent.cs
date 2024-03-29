﻿using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public class HtmxContent : IComponent
{
    private RenderHandle renderHandle;

    [CascadingParameter] private HtmxRequestViewContext ViewContext { get; set; } = default!;

    [Parameter, EditorRequired] public required RenderFragment ChildContent { get; set; }

    public void Attach(RenderHandle renderHandle) => this.renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        ViewContext.SetHtmxContent(ChildContent);
        if (ViewContext.Context.Request.IsHtmxRequest)
        {
            renderHandle.Render(ChildContent);
        }
        return Task.CompletedTask;
    }
}
