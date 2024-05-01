using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public abstract class PartialBase : IComponent
{
    private RenderHandle renderHandle;

    [Parameter, EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
        this.renderHandle = renderHandle;
    }

    public virtual Task SetParametersAsync(ParameterView parameters)
    {
        if (!parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out var childContent))
        {
            throw new ArgumentException($"{nameof(HtmxPartial)} requires a value for the parameter {nameof(ChildContent)}.");
        }

        ChildContent = childContent;
        renderHandle.Render(childContent);
        return Task.CompletedTask;
    }

    protected internal abstract bool ShouldRender();
}

