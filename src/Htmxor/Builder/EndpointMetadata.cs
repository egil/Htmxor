using Htmxor.Http;

namespace Htmxor.Builder;

internal sealed record class EndpointMetadata(HtmxRouteAttribute HxRoute)
{
	private readonly Uri? currentUrl = string.IsNullOrWhiteSpace(HxRoute.CurrentURL)
		? null
		: new Uri(HxRoute.CurrentURL, UriKind.RelativeOrAbsolute);

	public bool IsValidFor(HtmxRequest htmxRequest)
	{
		if (htmxRequest is null)
			return false;

		if (!htmxRequest.IsHtmxRequest)
			return false;

		if (htmxRequest.IsBoosted)
			return false;

		if (currentUrl is not null && Uri.Compare(currentUrl, htmxRequest.CurrentURL, UriComponents.HttpRequestUrl, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
			return false;

		if (!string.IsNullOrWhiteSpace(HxRoute.Target) && !HxRoute.Target.Equals(htmxRequest.Target, StringComparison.OrdinalIgnoreCase))
			return false;

		if (HxRoute.Targets.Length > 0 && !HxRoute.Targets.Contains(htmxRequest.Target, StringComparer.OrdinalIgnoreCase))
			return false;

		if (!string.IsNullOrWhiteSpace(HxRoute.Trigger) && !HxRoute.Trigger.Equals(htmxRequest.Trigger, StringComparison.OrdinalIgnoreCase))
			return false;

		if (!string.IsNullOrWhiteSpace(HxRoute.TriggerName) && !HxRoute.TriggerName.Equals(htmxRequest.TriggerName, StringComparison.OrdinalIgnoreCase))
			return false;

		return true;
	}
}
