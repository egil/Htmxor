using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Explicit.Anywhere;

public sealed class Issue180ActivationProbe
{
    public List<IComponent> Created { get; } = [];
    public void Reset() => Created.Clear();
}

internal sealed class Issue180RecordingActivator(
    IServiceProvider services, Issue180ActivationProbe probe) : IComponentActivator
{
    public IComponent CreateInstance(Type componentType)
    {
        var component = (IComponent)ActivatorUtilities.CreateInstance(services, componentType);
        probe.Created.Add(component);
        return component;
    }
}
