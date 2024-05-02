using System.Globalization;
using Htmxor.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Htmxor;

internal class HtmxorComponentRequestHost : IComponent
{
    private RenderHandle renderHandle;

    [Inject]
    public HtmxorEndpointRoutingStateProvider RoutingStateProvider { get; set; } = default!;

    public void Attach(RenderHandle renderHandle)
        => this.renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        renderHandle.Render(BuildRenderTree);
        return Task.CompletedTask;
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RoutingStateProvider.LayoutType is not null)
        {
            builder.OpenComponent(0, RoutingStateProvider.LayoutType);
            builder.AddAttribute(1, "Body", RenderRouteComponent(RoutingStateProvider));
            builder.CloseComponent();
        }
        else
        {
            RenderRouteComponent(RoutingStateProvider)(builder);
        }
    }

    private static RenderFragment RenderRouteComponent(HtmxorEndpointRoutingStateProvider routingStateProvider) => builder =>
    {
        ArgumentNullException.ThrowIfNull(routingStateProvider.RoutePattern);
        ArgumentNullException.ThrowIfNull(routingStateProvider.RouteData);

        builder.OpenComponent(0, routingStateProvider.RouteData.PageType);
        foreach (var (name, value) in GetRouteParameters(routingStateProvider.RoutePattern, routingStateProvider.RouteData.RouteValues))
        {
            builder.AddComponentParameter(1, name, value);
        }
        builder.CloseComponent();
    };

	private static IEnumerable<(string Name, object? Value)> GetRouteParameters(RoutePattern routePattern, IReadOnlyDictionary<string, object?> routeValues)
    {
        foreach (var parameter in routePattern.Parameters)
        {
            // Add null values for optional route parameters that weren't provided.
            if (!routeValues.TryGetValue(parameter.Name, out var parameterValue))
            {
                yield return (parameter.Name, null);
            }

            if (parameterValue is string value)
            {
                // At this point the values have already been URL decoded, but we might not have decoded '/' characters.
                // as that can cause issues when routing the request (You wouldn't be able to accept parameters that contained '/').
                // To be consistent with existing Blazor quirks that used Uri.UnescapeDataString, we'll replace %2F with /.
                // We don't want to call Uri.UnescapeDataString here as that would decode other characters that we don't want to decode,
                // for example, any value that was "double" encoded (for whatever reason) within the original URL.
                parameterValue = value.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
            }

            if (parameter.ParameterPolicies.Count > 0 && !parameter.IsCatchAll)
            {
                // If the parameter has some well-known set of route constraints, then we need to convert the value
                // to the target type.
                for (var i = 0; i < parameter.ParameterPolicies.Count; i++)
                {
                    var policy = parameter.ParameterPolicies[i];
                    switch (policy.Content)
                    {
                        case "bool":
                            yield return (parameter.Name, bool.Parse((string)parameterValue!));
                            break;
                        case "datetime":
                            yield return (parameter.Name, DateTime.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        case "decimal":
                            yield return (parameter.Name, decimal.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        case "double":
                            yield return (parameter.Name, double.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        case "float":
                            yield return (parameter.Name, float.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        case "guid":
                            yield return (parameter.Name, Guid.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        case "int":
                            yield return (parameter.Name, int.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        case "long":
                            yield return (parameter.Name, long.Parse((string)parameterValue!, CultureInfo.InvariantCulture));
                            break;
                        default:
                            continue;
                    }
                }
            }
            else
            {
                yield return (parameter.Name, parameterValue);
            }
        }
    }
}
