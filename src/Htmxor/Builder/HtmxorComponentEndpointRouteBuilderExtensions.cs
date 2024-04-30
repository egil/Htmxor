using System.Reflection;
using Htmxor.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;

#pragma warning disable IDE0130 
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130 

public static class HtmxorComponentEndpointRouteBuilderExtensions
{
    public static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
        this RazorComponentsEndpointConventionBuilder builder,
        IEndpointRouteBuilder endpoints)
    {
        var componentTypes = builder.GetDiscoveredComponents();
        endpoints.DataSources.Add(new HtmxorComponentEndpointDataSource(componentTypes));

        return builder;
    }

    // Instead of reimplementing the discovery logic from Blazor with all the configuration options it provides,
    // lets just steal the gather components. Can perhaps be refactored to use UnsafeAccessor for better perf.
    private static List<ComponentInfo> GetDiscoveredComponents(this RazorComponentsEndpointConventionBuilder builder)
    {
        var builderType = builder.GetType();
        var appBuilder = builderType.GetProperty("ApplicationBuilder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder);
        var appBuilderType = appBuilder!.GetType();
        var componentCollectionBuilder = appBuilderType.GetProperty("Components", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(appBuilder);
        var componentCollectionBuilderType = componentCollectionBuilder!.GetType();
        var componentInfos = componentCollectionBuilderType.GetMethod("ToComponentCollection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(componentCollectionBuilder, null);
        var componentInfoType = componentInfos!.GetType().GetElementType();
        var getComponentTypeProperty = componentInfoType!.GetProperty("ComponentType", BindingFlags.Instance | BindingFlags.Public);
        var getRenderModeProperty = componentInfoType!.GetProperty("RenderMode", BindingFlags.Instance | BindingFlags.Public);

        var componentTypes = new List<ComponentInfo>();
        foreach (var componentInfo in (Array)componentInfos)
        {
            var type = (Type)getComponentTypeProperty!.GetValue(componentInfo)!;
            var renderMode = (IComponentRenderMode?)getRenderModeProperty!.GetValue(componentInfo);
            componentTypes.Add(new ComponentInfo(type, renderMode));
        }

        return componentTypes;
    }
}
