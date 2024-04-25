using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;

internal sealed record class ComponentInfo
{
    [DynamicallyAccessedMembers(LinkerFlags.Component)]
    public Type ComponentType { get; }

    [DynamicallyAccessedMembers(LinkerFlags.Component)]
    public Type? ComponentLayoutType { get; }

    public IComponentRenderMode? RenderMode { get; }

    public IReadOnlySet<HxRouteAttribute> HxRoutes { get; }

    // TODO: figure out if certain render modes are incompatible with htmxor
    public bool IsHtmxorCompatible
        => HxRoutes.Count > 0;

    public ComponentInfo(Type componentType, IComponentRenderMode? renderMode)
    {
        ComponentType = componentType;
        RenderMode = renderMode;

        ComponentLayoutType = componentType.GetCustomAttribute<HxLayoutAttribute>(true)?.LayoutType;

        var hxRoutes = componentType
            .GetCustomAttributes<HxRouteAttribute>(true)
            .ToHashSet();

        var routes = componentType
            .GetCustomAttributes<RouteAttribute>(true)
            .Select(x => new HxRouteAttribute(x.Template));

        // Add any normal routes whose template does not overlap with an existing hxRoute.
        // HxRoutes takes precedence.
        foreach (var normalRoutes in routes)
        {
            if (!hxRoutes.Contains(normalRoutes, TemplateOnlyEqualityComparer.Instance))
            {
                hxRoutes.Add(normalRoutes);
            }
        }

        HxRoutes = hxRoutes;
    }

    private sealed class TemplateOnlyEqualityComparer : IEqualityComparer<HxRouteAttribute>
    {
        public static TemplateOnlyEqualityComparer Instance { get; } = new();

        public bool Equals(HxRouteAttribute? x, HxRouteAttribute? y)
            => x is not null
            && y is not null
            && x.Template.Equals(y.Template, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode([DisallowNull] HxRouteAttribute obj) => obj.Template.GetHashCode();
    }
}