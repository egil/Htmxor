using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Builder;

internal class HtmxorComponentEndpointDataSource : EndpointDataSource
{
    public override IReadOnlyList<Endpoint> Endpoints { get; }

    public HtmxorComponentEndpointDataSource(IReadOnlyList<ComponentInfo> components)
    {
        Endpoints = UpdateEndpoints(components);
    }

    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

    private static List<Endpoint> UpdateEndpoints(IReadOnlyList<ComponentInfo> components)
    {
        var result = new List<Endpoint>();

        foreach (var componentInfo in components.Where(x => x.IsHtmxorCompatible))
        {
            foreach (var hxRoute in componentInfo.HxRoutes)
            {
                var builder = new RouteEndpointBuilder(
                    null,
                    RoutePatternFactory.Parse(hxRoute.Template),
                    order: 0);

                builder.Metadata.Add(new RequireAntiforgeryTokenAttribute());

                // All attributes defined for the type are included as metadata.
                foreach (var attribute in componentInfo.ComponentType.GetCustomAttributes())
                {
                    builder.Metadata.Add(attribute);
                }

                // We do not support link generation, so explicitly opt-out.
                builder.Metadata.Add(new SuppressLinkGenerationMetadata());
                builder.Metadata.Add(new HttpMethodMetadata(hxRoute.Methods, false));
                builder.Metadata.Add(new ComponentTypeMetadata(componentInfo.ComponentType));
                builder.Metadata.Add(new RootComponentMetadata(typeof(HtmxorComponentRequestHost)));
                builder.Metadata.Add(new HtmxorEndpointMetadata(hxRoute));

                if (componentInfo.ComponentLayoutType is not null)
                {
                    builder.Metadata.Add(new HtmxorLayoutComponentMetadata(componentInfo.ComponentLayoutType));
                }

                builder.RequestDelegate = static httpContext =>
                {
                    var invoker = httpContext.RequestServices.GetRequiredService<IHtmxorComponentEndpointInvoker>();
                    return invoker.Render(httpContext);
                };

                // Always override the order, since our client router does not support it.
                builder.Order = 0;

                // The display name is for debug purposes by endpoint routing.
                builder.DisplayName = $"{builder.RoutePattern.RawText} ({componentInfo.ComponentType.Name}) (HTMX route)";

                result.Add(builder.Build());
            }
        }

        return result;
    }
}
