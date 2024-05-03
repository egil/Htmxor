using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;
public class HtmxorComponentEndpointDataSourceTest
{
	[Fact]
	public void Registers_endpoints_based_on_HxRoute()
	{
		var cut = new ComponentEndpointDataSource(
			[
				new ComponentInfo(typeof(RouteOnly), null),
				new ComponentInfo(typeof(HxOnly), null),
				new ComponentInfo(typeof(HxAndRouteOnly), null),
			]);

		var endpoints = cut.Endpoints;
		endpoints.Should().HaveCount(3);
		endpoints[0].Metadata.Should().Contain(new EndpointMetadata(new HtmxRouteAttribute("/route-only")));
		endpoints[1].Metadata.Should().Contain(new EndpointMetadata(new HtmxRouteAttribute("/hx-only")));
		endpoints[2].Metadata.Should().Contain(new EndpointMetadata(new HtmxRouteAttribute("/hx-and-route") { Target = "target" }));
	}

	[Route("/route-only")]
	private sealed class RouteOnly : NoopComponentBase
	{
	}

	[HtmxRoute("/hx-only")]
	private sealed class HxOnly : NoopComponentBase
	{
	}

	[Route("/hx-and-route")]
	[HtmxRoute("/hx-and-route", Target = "target")]
	private sealed class HxAndRouteOnly : NoopComponentBase
	{
	}
}
