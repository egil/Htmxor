using Htmxor.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10;

// Hand-authored stand-in for future generated output. Issue #89 exercises only lifecycle composition.
internal static class Issue89GeneratedAction
{
	public const string DeleteHandlerIdentity = "Htmxor.AspNetCore10.Issue89Page.DELETE.DeleteItem";

	public static HtmxorComponentActionDescriptor DeleteDescriptor { get; } = new(
		typeof(Issue89Page),
		"/issue-89/{ItemId:int}",
		HttpMethods.Delete,
		DeleteHandlerIdentity);

	public static RazorComponentsEndpointConventionBuilder Register(
		RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints)
		=> builder.AddHtmxorComponentEndpoints(endpoints, [DeleteDescriptor]);
}
