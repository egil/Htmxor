using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public sealed class HtmxPartial : IComponent, IConditionalOutputComponent
{
    private RenderHandle renderHandle;

    [Parameter, EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    [Parameter] public bool Condition { get; set; } = true;


    public Task SetParametersAsync(ParameterView parameters)
    {
        if (!parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out var childContent))
        {
            throw new ArgumentException($"{nameof(HtmxPartial)} requires a value for the parameter {nameof(ChildContent)}.", nameof(parameters));
        }

        ChildContent = childContent;
        Condition = parameters.GetValueOrDefault(nameof(Condition), true);
        renderHandle.Render(ChildContent);
        return Task.CompletedTask;
    }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        this.renderHandle = renderHandle;
    }

    bool IConditionalOutputComponent.ShouldOutput(int _) => Condition;
}

