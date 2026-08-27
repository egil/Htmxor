using Htmxor;
using Htmxor.Builder;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10;

// Hand-authored stand-in for future generated output. Issue #87 exercises only its runtime contract.
internal static class Issue87GeneratedActions
{
	public const string PutHandlerIdentity = "Htmxor.AspNetCore10.Issue87Page.PUT.PutItem";
	public const string PatchHandlerIdentity = "Htmxor.AspNetCore10.Issue87Page.PATCH.PatchItem";
	public const string DeleteHandlerIdentity = "Htmxor.AspNetCore10.Issue87Page.DELETE.DeleteItem";

	public static HtmxorComponentActionDescriptor PutDescriptor { get; } = new(
		typeof(Issue87Page),
		"/issue-87/{ItemId:int}",
		HttpMethods.Put,
		PutHandlerIdentity);

	public static HtmxorComponentActionDescriptor PatchDescriptor { get; } = new(
		typeof(Issue87Page),
		"/issue-87/{ItemId:int}",
		HttpMethods.Patch,
		PatchHandlerIdentity);

	public static HtmxorComponentActionDescriptor DeleteDescriptor { get; } = new(
		typeof(Issue87Page),
		"/issue-87/{ItemId:int}",
		HttpMethods.Delete,
		DeleteHandlerIdentity);

	public static RazorComponentsEndpointConventionBuilder Register(
		RazorComponentsEndpointConventionBuilder builder,
		IEndpointRouteBuilder endpoints)
		=> builder.AddHtmxorComponentEndpoints(
			endpoints,
			[PutDescriptor, PatchDescriptor, DeleteDescriptor]);

	public static HtmxorComponentActionDescriptor GetDescriptor(string method)
		=> method switch
		{
			"PUT" => PutDescriptor,
			"PATCH" => PatchDescriptor,
			"DELETE" => DeleteDescriptor,
			_ => throw new ArgumentOutOfRangeException(nameof(method), method, "Expected a declared unsafe method."),
		};
}

public partial class Issue87Page
{
	[Inject]
	private HtmxorComponentActionRequest ActionRequest { get; set; } = default!;

	public override async Task SetParametersAsync(ParameterView parameters)
	{
		await base.SetParametersAsync(parameters);
		if (ActionRequest.TryConsume(Issue87GeneratedActions.PutDescriptor))
		{
			await InvokeAsync(PutItem);
			return;
		}

		if (ActionRequest.TryConsume(Issue87GeneratedActions.PatchDescriptor))
		{
			await InvokeAsync(PatchItem);
			return;
		}

		if (ActionRequest.TryConsume(Issue87GeneratedActions.DeleteDescriptor))
		{
			await InvokeAsync(DeleteItem);
		}
	}

	private async Task InvokeAsync(Action<HtmxEventArgs> handler)
	{
		var callback = EventCallback.Factory.Create(this, handler);
		await callback.InvokeAsync(new HtmxEventArgs(HttpContext.GetHtmxContext()));
	}
}
