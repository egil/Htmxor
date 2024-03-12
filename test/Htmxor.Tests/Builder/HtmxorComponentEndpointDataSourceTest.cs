using Htmxor.TestAssets;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;
public class HtmxorComponentEndpointDataSourceTest
{
    [Fact]
    public void Registers_endpoints_based_on_HxRoute()
    {
        var cut = new HtmxorComponentEndpointDataSource(
            [
                new ComponentInfo(typeof(RouteOnly), null),
                new ComponentInfo(typeof(HxOnly), null),
                new ComponentInfo(typeof(HxAndRouteOnly), null),
            ]);

        var endpoints = cut.Endpoints;
        endpoints.Should().HaveCount(2);
        endpoints[0].Metadata.Should().Contain(new HxRouteAttribute("/hx-only"));
        endpoints[1].Metadata.Should().Contain(new HxRouteAttribute("/hx-and-route"));
    }

    [Route("/route-only")]
    private sealed class RouteOnly : NoopComponentBase
    {
    }

    [HxRoute("/hx-only")]
    private sealed class HxOnly : NoopComponentBase
    {
    }

    [Route("/hx-and-route")]
    [HxRoute("/hx-and-route")]
    private sealed class HxAndRouteOnly : NoopComponentBase
    {
    }
}
