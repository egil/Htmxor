using Microsoft.AspNetCore.Routing.Patterns;

namespace Htmxor.Builder;

// Route groups replace the final endpoint pattern with a prefixed pattern.
// Stock Blazor invocation still needs the route authored by the component.
internal sealed record HtmxorComponentRoutePatternMetadata(RoutePattern RoutePattern);
