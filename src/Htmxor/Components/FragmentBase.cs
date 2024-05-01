using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public abstract class FragmentBase : ComponentBase
{
    internal bool WillRender() => ShouldRender();
}

