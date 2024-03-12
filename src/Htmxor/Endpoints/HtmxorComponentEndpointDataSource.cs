using System.Reflection;
using Htmxor.Configuration;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Endpoints;

internal class HtmxorComponentEndpointDataSource : EndpointDataSource
{
    private readonly IEnumerable<Type> componentTypes;
    private IReadOnlyList<Endpoint>? endpoints;

    public override IReadOnlyList<Endpoint> Endpoints => endpoints ??= UpdateEndpoints();

    public HtmxorComponentEndpointDataSource(IEnumerable<Type> componentTypes)
    {
        this.componentTypes = componentTypes;
        UpdateEndpoints();
    }

    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

    private IReadOnlyList<Endpoint> UpdateEndpoints()
    {
        var endpoints = new List<Endpoint>();
        foreach (var componentType in componentTypes)
        {
            foreach (var hxRoute in componentType.GetCustomAttributes<HxRouteAttribute>(true))
            {
                var builder = new RouteEndpointBuilder(
                    null,
                    RoutePatternFactory.Parse(hxRoute.Template),
                    order: 0);

                builder.Metadata.Add(new RequireAntiforgeryTokenAttribute());

                // All attributes defined for the type are included as metadata.
                foreach (var attribute in componentType.GetCustomAttributes())
                {
                    builder.Metadata.Add(attribute);
                }

                // We do not support link generation, so explicitly opt-out.
                builder.Metadata.Add(new SuppressLinkGenerationMetadata());
                builder.Metadata.Add(new HttpMethodMetadata(hxRoute.Methods, false));
                builder.Metadata.Add(new ComponentTypeMetadata(componentType));
                builder.Metadata.Add(new HtmxorEndpointMetadata(hxRoute));

                builder.RequestDelegate = static httpContext =>
                {
                    var invoker = httpContext.RequestServices.GetRequiredService<IHtmxorComponentEndpointInvoker>();
                    return invoker.Render(httpContext);
                };

                // Always override the order, since our client router does not support it.
                builder.Order = 0;

                // The display name is for debug purposes by endpoint routing.
                builder.DisplayName = $"{builder.RoutePattern.RawText} ({componentType.Name}) (HTMX route)";

                endpoints.Add(builder.Build());
            }
        }

        return endpoints;
    }
}

internal sealed class HtmxorEndpointMetadata(HxRouteAttribute hxRoute)
{
    private readonly Uri? currentUrl = string.IsNullOrWhiteSpace(hxRoute.CurrentURL)
        ? null
        : new Uri(hxRoute.CurrentURL, UriKind.RelativeOrAbsolute);

    public bool IsValidFor(HtmxRequest htmxRequest)
    {
        if (htmxRequest is null)
            return false;

        if (!htmxRequest.IsHtmxRequest)
            return false;

        if (!string.IsNullOrWhiteSpace(hxRoute.CurrentURL) && currentUrl != htmxRequest.CurrentURL)
            return false;

        if (!string.IsNullOrWhiteSpace(hxRoute.Target) && !hxRoute.Target.Equals(htmxRequest.Target, StringComparison.OrdinalIgnoreCase))
            return false;

        if (hxRoute.Targets.Count > 0 && !hxRoute.Targets.Contains(htmxRequest.Target, StringComparer.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(hxRoute.Trigger) && !hxRoute.Trigger.Equals(htmxRequest.Trigger, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(hxRoute.TriggerName) && !hxRoute.TriggerName.Equals(htmxRequest.TriggerName, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}