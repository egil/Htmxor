using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Htmxor.Rendering;

internal class HtmxorComponentState : ComponentState
{
    public HtmxorComponentState(Renderer renderer, int componentId, IComponent component, HtmxorComponentState? parentComponentState)
        : base(renderer, componentId, component, parentComponentState)
    {
    }

    public override ValueTask DisposeAsync()
    {
        return base.DisposeAsync();
    }

    internal bool HasPartialFragments()
    {
        throw new NotImplementedException();
    }
}
