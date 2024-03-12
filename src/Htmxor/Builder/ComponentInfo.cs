using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;

internal sealed record class ComponentInfo
{
    public Type ComponentType { get; }

    public IComponentRenderMode? RenderMode { get; }

    public IReadOnlyList<HxRouteAttribute> HxRoutes { get; }

    public bool IsHtmxorCompatible
        => HxRoutes.Count > 0; // TODO: figure out if certain render modes are incompatible with htmxor

    public ComponentInfo(Type componentType, IComponentRenderMode? renderMode)
    {
        ComponentType = componentType;
        RenderMode = renderMode;
        HxRoutes = componentType.GetCustomAttributes<HxRouteAttribute>(true)?.ToArray() ?? [];
    }
}