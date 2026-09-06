using Microsoft.Extensions.Logging;

namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.QualifiedRenderer;

internal abstract class QualifiedRenderer : Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure.StaticHtmlRenderer
{
	protected QualifiedRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
		: base(services, loggerFactory)
	{
	}
}
