using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.PartialRenderer;

internal abstract partial class PartialRenderer : Renderer
{
	protected PartialRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
		: base(services, loggerFactory)
	{
	}
}
