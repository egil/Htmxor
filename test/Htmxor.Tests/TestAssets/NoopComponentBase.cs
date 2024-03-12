using Microsoft.AspNetCore.Components;

namespace Htmxor.TestAssets;

public abstract class NoopComponentBase : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
    }

    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}