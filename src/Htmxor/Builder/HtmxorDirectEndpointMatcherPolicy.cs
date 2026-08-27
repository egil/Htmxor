using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Htmxor.Builder;

internal sealed class HtmxorDirectEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
	public override int Order => int.MinValue + 150;

	public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
	{
		ArgumentNullException.ThrowIfNull(endpoints);
		return endpoints.Any(endpoint =>
			endpoint.Metadata.GetMetadata<HtmxorDirectEndpointMetadata>() is not null);
	}

	public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
	{
		ArgumentNullException.ThrowIfNull(httpContext);
		ArgumentNullException.ThrowIfNull(candidates);
		if (httpContext.GetHtmxContext().Request.RoutingMode is RoutingMode.Direct)
		{
			return Task.CompletedTask;
		}

		for (var index = 0; index < candidates.Count; index++)
		{
			if (candidates.IsValidCandidate(index) &&
				candidates[index].Endpoint.Metadata.GetMetadata<HtmxorDirectEndpointMetadata>() is not null)
			{
				candidates.SetValidity(index, false);
			}
		}

		return Task.CompletedTask;
	}
}
