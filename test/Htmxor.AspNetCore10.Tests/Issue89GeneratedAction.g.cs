using Htmxor;
using Htmxor.Builder;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
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

public partial class Issue89Page : IComponent
{
	[Inject]
	private HtmxorComponentActionRequest ActionRequest { get; set; } = default!;

	async Task IComponent.SetParametersAsync(ParameterView parameters)
	{
		// Use virtual dispatch so an application-authored override remains the lifecycle owner.
		await SetParametersAsync(parameters);
		if (ActionRequest.TryConsume(Issue89GeneratedAction.DeleteDescriptor))
		{
			var callback = EventCallback.Factory.Create<HtmxEventArgs>(this, DeleteItem);
			await callback.InvokeAsync(new HtmxEventArgs(HttpContext.GetHtmxContext()));
		}
	}
}
