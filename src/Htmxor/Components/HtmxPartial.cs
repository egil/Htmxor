using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

public sealed class HtmxPartial : FragmentBase
{
    [Parameter, EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    [Parameter] public bool Condition { get; set; } = true;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out var childContent))
        {
            throw new ArgumentException($"{nameof(HtmxPartial)} requires a value for the parameter {nameof(ChildContent)}.", nameof(parameters));
        }

        ChildContent = childContent;
        Condition = parameters.GetValueOrDefault(nameof(Condition), true);
        return base.SetParametersAsync(parameters);
    }

    protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
        => builder.AddContent(0, ChildContent);

    protected override bool ShouldRender() => Condition;
}

