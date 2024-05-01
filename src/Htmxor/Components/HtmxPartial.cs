using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public sealed class HtmxPartial : PartialBase
{
    [Parameter] public bool Condition { get; set; } = true;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Condition = parameters.GetValueOrDefault(nameof(Condition), true);
        return base.SetParametersAsync(parameters);
    }

    protected internal override bool ShouldRender() => Condition;
}

