using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using ImportedRenderer = Microsoft.AspNetCore.Components.RenderTree.Renderer;

namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.ImportsAndUsages;

internal sealed class Usages
{
	public StaticHtmlRenderer? Renderer { get; init; }
	public IRazorComponentEndpointInvoker? Invoker { get; init; }
	public ImportedRenderer? Imported { get; init; }
}
