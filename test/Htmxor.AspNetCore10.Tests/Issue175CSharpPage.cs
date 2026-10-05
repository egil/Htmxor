using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10;

[Route("/issue-175/csharp")]
[DisableHtmxDirectRouting]
public sealed class Issue175CSharpPage : ComponentBase
{
	[Inject]
	private Issue175ApplicationProbe Probe { get; set; } = default!;

	[CascadingParameter]
	private HttpContext HttpContext { get; set; } = default!;

	private string? RouteMetadata
		=> HttpContext.GetEndpoint()?.Metadata.GetMetadata<Issue175MetadataSentinel>()?.Value;

	protected override void OnInitialized() => Probe.RecordInitialization();

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "h1");
		builder.AddAttribute(1, "data-issue-175-page", "csharp");
		builder.AddContent(2, "Normal-only all-C# page");
		builder.CloseElement();

		builder.OpenElement(3, "p");
		builder.AddAttribute(4, "data-route-metadata", RouteMetadata);
		builder.AddContent(5, RouteMetadata);
		builder.CloseElement();
	}
}
