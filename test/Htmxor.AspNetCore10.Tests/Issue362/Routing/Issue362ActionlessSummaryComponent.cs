using Htmxor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10.Issue362.Routing;

[HtmxRoute(
	"/actionless-summaries/{SummaryId:int}",
	Methods = ["GET", "DELETE"],
	CurrentUrl = "/orders",
	Target = "section",
	Targets = null!)]
[Authorize(Policy = "issue-97-summary-policy")]
[Host("summaries.internal.example")]
public sealed class Issue362ActionlessSummaryComponent : ComponentBase
{
	private bool lifecycleReady;

	[Parameter]
	public int SummaryId { get; set; }

	[Parameter]
	[SupplyParameterFromQuery]
	public bool SuppressNavigationResponse { get; set; }

	[CascadingParameter]
	private HttpContext HttpContext { get; set; } = default!;

	[Inject]
	private Issue100RequestProbe RequestProbe { get; set; } = default!;

	private string? GroupMarker
		=> HttpContext.GetEndpoint()?.Metadata.GetMetadata<Issue97GroupMetadata>()?.Value;

	protected override void OnInitialized()
	{
		RequestProbe.RecordInitialization();
	}

	protected override void OnParametersSet()
	{
		lifecycleReady = true;
		RequestProbe.RecordBinding();
		if (SuppressNavigationResponse)
		{
			HttpContext.GetHtmxContext().Response.Location("/actionless-navigation-destination");
		}
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "hx-delete", "/issue-362-group/actionless-summaries/" + SummaryId);
		builder.AddAttribute(
			2,
			"ondelete",
			EventCallback.Factory.Create<HtmxEventArgs>(this, DeleteSummary));
		builder.AddContent(3, "No server action");
		builder.CloseElement();

		builder.OpenElement(4, "section");
		builder.AddAttribute(5, "data-issue-97-summary-component", string.Empty);
		builder.OpenElement(6, "p");
		builder.AddAttribute(7, "data-summary-request-values", string.Empty);
		builder.AddContent(
			8,
			$"{SummaryId}|{HttpContext.User.Identity?.Name}|{GroupMarker}|" +
			$"{RequestProbe.BindingCount}|{RequestProbe.InitializationCount}|" +
			lifecycleReady.ToString().ToLowerInvariant());
		builder.CloseElement();
		builder.CloseElement();
	}

	private void DeleteSummary(HtmxEventArgs _) => RequestProbe.RecordCallback();
}
