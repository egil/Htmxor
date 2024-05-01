using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Htmxor.Builder;

internal class HtmxorComponentEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    // This executes very early in the routing pipeline so that other
    // policies can see the resulting dynamicComponentEndpoint.
    public override int Order => int.MinValue + 150;

    /// <inheritdoc/>
    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.Any(endpoint => endpoint.Metadata.GetMetadata<HtmxorEndpointMetadata>() is not null);
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(candidates);

        var htmxContext = httpContext.GetHtmxContext();
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var endpoint = candidates[i].Endpoint;
            var htmxorEndpointMetadata = endpoint.Metadata.GetMetadata<HtmxorEndpointMetadata>();

            if (htmxorEndpointMetadata is null && !htmxContext.Request.IsFullPageRequest)
            {
                candidates.SetValidity(i, false);
                continue;
            }

            if (htmxorEndpointMetadata is not null && !htmxorEndpointMetadata.IsValidFor(htmxContext.Request))
            {
                candidates.SetValidity(i, false);
            }
        }

        return Task.CompletedTask;
    }
}