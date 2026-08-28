using Htmxor.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10;

// Issue #89 retains its hand-authored endpoint descriptor while consuming the generated callback hook.
internal static class Issue89GeneratedAction
{
	public const string DeleteHandlerIdentity = "Htmxor.AspNetCore10.Issue89Page.DELETE.DeleteItem";

	public static HtmxorComponentActionDescriptor DeleteDescriptor { get; } = new(
		typeof(Issue89Page),
		"/issue-89/{ItemId:int}",
		HttpMethods.Delete,
		DeleteHandlerIdentity,
		Issue89Page.__HtmxorGeneratedDeleteAction);

	public static RazorComponentsEndpointConventionBuilder Register(
		RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints)
		=> builder.AddHtmxorComponentEndpoints(endpoints, [DeleteDescriptor]);
}
