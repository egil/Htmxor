using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.PrimaryConstructor;

internal sealed class PrimaryConstructorInvoker(IRazorComponentEndpointInvoker inner)
	: IRazorComponentEndpointInvoker
{
	public Task Render(HttpContext context) => inner.Render(context);
}
