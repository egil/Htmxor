using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.Endpoints;

public static class HtmxorComponentEndpointRouteBuilderExtensions
{
    public static RazorComponentsEndpointConventionBuilder AddHtmxorComponentEndpoints(
        this RazorComponentsEndpointConventionBuilder builder,
        IEndpointRouteBuilder endpoints)
    {
        //builder.Finally(x =>
        //{
        //    if (!x.Metadata.Any(md => md is HxRouteAttribute))
        //    {
        //        return;
        //    }

        //    // Razor components only supports HTTP GET and HTTP POST,
        //    // so if one or more HxRoutes are also defined on a page, 
        //    // this will expand the support http methods supported by the
        //    // endpoint to include the methods defined in the HxRouteAttributes.
        //    var httpMethodNames = x.Metadata.OfType<HxRouteAttribute>().SelectMany(x => x.Methods).Distinct();
        //    var httpMethodMetadata = new HttpMethodMetadata(httpMethodNames, false);

        //    if (x.Metadata.SingleOrDefault(x => x is HttpMethodMetadata) is { } existingHttpMethodMetadata)
        //    {
        //        x.Metadata.Remove(existingHttpMethodMetadata);
        //    }

        //    x.Metadata.Add(httpMethodMetadata);

        //    // Add to mark as requiring overriding the default Razor component endpoint.
        //    x.Metadata.Add(HtmxorOverrideRequiredRazorComponentEndpointMetadata.Instance);
        //});

        var componentTypes = GetDiscoveredComponentTypes(builder)
            .Where(candidate => candidate.GetCustomAttributes<HxRouteAttribute>() is { } hxRoutes && hxRoutes.Any());

        endpoints.DataSources.Add(new HtmxorComponentEndpointDataSource(componentTypes));

        return builder;
    }

    // Instead of reimplementing the discovery logic from Blazor with all the configuration options it provides,
    // lets just steal the gather components. Can perhaps be refactored to use UnsafeAccessor for better perf.
    private static List<Type> GetDiscoveredComponentTypes(RazorComponentsEndpointConventionBuilder builder)
    {
        var builderType = builder.GetType();
        var appBuilder = builderType.GetProperty("ApplicationBuilder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder);
        var appBuilderType = appBuilder!.GetType();
        var componentCollectionBuilder = appBuilderType.GetProperty("Components", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(appBuilder);
        var componentCollectionBuilderType = componentCollectionBuilder!.GetType();
        var componentInfos = componentCollectionBuilderType.GetMethod("ToComponentCollection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(componentCollectionBuilder, null);
        var componentInfoType = componentInfos!.GetType().GetElementType();
        var getComponentTypeProperty = componentInfoType!.GetProperty("ComponentType", BindingFlags.Instance | BindingFlags.Public);

        var componentTypes = new List<Type>();
        foreach (var componentInfo in (Array)componentInfos)
        {
            componentTypes.Add((Type)getComponentTypeProperty!.GetValue(componentInfo)!);
        }

        return componentTypes;
    }
}