using Htmxor.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10;

// Hand-authored stand-in for future generated output. Issue #91 exercises only route registration.
internal static class Issue91GeneratedRoute
{
	public const string NormalizedRoute = "/reports/{ReportId:int}";
	public const string PolicyName = "issue-91-policy";

	public static HtmxorComponentGetRouteDescriptor Descriptor { get; } = new(
		typeof(Issue91HtmxOnlyComponent),
		NormalizedRoute,
		[
			new HtmxRouteAttribute(NormalizedRoute) { Methods = [HttpMethods.Get] },
			new AuthorizeAttribute(PolicyName),
		]);

	public static IEndpointConventionBuilder Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapHtmxorComponentEndpoint(Descriptor);
}
