using Htmxor.Http;

namespace Htmxor.Builder;

internal sealed record class EndpointMetadata(HtmxRouteAttribute HxRoute)
{
	private readonly string? currentUrl = string.IsNullOrWhiteSpace(HxRoute.CurrentUrl)
		? null
		: HxRoute.CurrentUrl;

	public bool IsValidFor(HtmxRequest htmxRequest)
		=> IsValidFor(htmxRequest, out _);

	public bool IsValidFor(HtmxRequest htmxRequest, out RepresentationFilterRejection? rejection)
	{
		rejection = null;
		if (htmxRequest is null)
			return false;

		if (htmxRequest.RoutingMode is not RoutingMode.Direct)
			return false;

		if (currentUrl is not null && !HtmxCurrentUrlMatcher.Matches(currentUrl, htmxRequest.CurrentUrl))
			rejection = new(nameof(HxRoute.CurrentUrl), currentUrl, htmxRequest.CurrentUrl is null);

		else if (!string.IsNullOrWhiteSpace(HxRoute.Target) && !HtmxElementIdentity.Equals(HxRoute.Target, htmxRequest.Target))
			rejection = new(nameof(HxRoute.Target), HxRoute.Target, htmxRequest.Target is null);

		else if (HxRoute.Targets.Length > 0 && !HxRoute.Targets.Any(target => HtmxElementIdentity.Equals(target, htmxRequest.Target)))
			rejection = new(nameof(HxRoute.Targets), string.Join(", ", HxRoute.Targets), htmxRequest.Target is null);

		return rejection is null;
	}
}
