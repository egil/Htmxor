using Microsoft.Extensions.Logging;
using FrameworkRenderer = Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure.StaticHtmlRenderer;

namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.AliasedRenderer;

internal abstract class AliasedRenderer
	: FrameworkRenderer
{
	protected AliasedRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
		: base(services, loggerFactory)
	{
	}
}
