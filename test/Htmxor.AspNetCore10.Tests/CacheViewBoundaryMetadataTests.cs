#if NET11_0_OR_GREATER
using System.Reflection;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Components;

namespace Htmxor.AspNetCore10;

public sealed class CacheViewBoundaryMetadataTests
{
	[Fact]
	public void The_interactive_boundary_declares_stocks_rerender_cache_behavior()
	{
		var behavior = typeof(HtmxorEndpointCandidateRenderModeBoundary)
			.GetCustomAttribute<CacheBehaviorAttribute>(inherit: true);

		Assert.NotNull(behavior);
		Assert.Equal(CacheBehavior.Rerender, behavior.Behavior);
	}
}
#endif
