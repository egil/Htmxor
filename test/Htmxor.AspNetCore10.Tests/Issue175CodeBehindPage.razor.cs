using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

[DisableHtmxDirectRouting]
public partial class Issue175CodeBehindPage
{
	[CascadingParameter]
	private HttpContext HttpContext { get; set; } = default!;

	private string? RouteMetadata
		=> HttpContext.GetEndpoint()?.Metadata.GetMetadata<Issue175MetadataSentinel>()?.Value;

	protected override void OnInitialized() => Probe.RecordInitialization();
}
