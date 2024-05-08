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

	public IReadOnlySet<HtmxRouteAttribute> HxRoutes { get; }

	// TODO: figure out if certain render modes are incompatible with htmxor
	public bool IsHtmxorCompatible
		=> HxRoutes.Count > 0;

	public ComponentInfo(Type componentType, IComponentRenderMode? renderMode)
	{
		ComponentType = componentType;
		RenderMode = renderMode;

		// Since GetCustomAttributes does not guarantee the order of attributes
		// returned, having a priority allows users to define a default HtmxLayout
		// e.g. in their _Imports.razor, and then override that on a component base,
		// just as they can with the @layout directive in razor files.
		ComponentLayoutType = componentType
			.GetCustomAttributes<HtmxLayoutAttribute>(true)
			.OrderBy(x => x.Priority)
			.LastOrDefault()
			?.LayoutType;

		var hxRoutes = componentType
			.GetCustomAttributes<HtmxRouteAttribute>(true)
			.ToHashSet();

		var routes = componentType
			.GetCustomAttributes<RouteAttribute>(true)
			.Select(x => new HtmxRouteAttribute(x.Template));

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

	private sealed class TemplateOnlyEqualityComparer : IEqualityComparer<HtmxRouteAttribute>
	{
		public static TemplateOnlyEqualityComparer Instance { get; } = new();

		public bool Equals(HtmxRouteAttribute? x, HtmxRouteAttribute? y)
			=> x is not null
			&& y is not null
			&& x.Template.Equals(y.Template, StringComparison.OrdinalIgnoreCase);

		public int GetHashCode([DisallowNull] HtmxRouteAttribute obj)
			=> obj.Template.GetHashCode(StringComparison.OrdinalIgnoreCase);
	}
}
