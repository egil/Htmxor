using Htmxor.Http;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public sealed class HtmxPartial : IComponent
{
    private RenderHandle renderHandle;

    [Parameter, EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
        this.renderHandle = renderHandle;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (!parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out var childContent))
        {
            throw new ArgumentException($"{nameof(HtmxPartial)} requires a value for the parameter {nameof(ChildContent)}.");
        }

        ChildContent = childContent;
        renderHandle.Render(childContent);
        return Task.CompletedTask;
    }

    internal bool IsMatchingRequest(HtmxContext htmxContext) => true;
}

