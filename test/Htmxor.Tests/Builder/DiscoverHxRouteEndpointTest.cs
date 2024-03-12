using Htmxor.Http;

namespace Htmxor.Builder;

public class DiscoverHxRouteEndpointTest
{
    [Fact]
    public async Task NormalRequest_HxOnlyPage()
    {
        await using var host = await AlbaHost.For<global::Program>();

        await host.Scenario(s =>
        {
            s.Get.Url("/hx/test");
            s.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task HxRequest_HxOnlyPage()
    {
        await using var host = await AlbaHost.For<global::Program>();

        await host.Scenario(s =>
        {
            s.Get.Url("/hx/test");
            s.WithRequestHeader(HtmxRequestHeaderNames.HtmxRequest, string.Empty);
            s.ContentShouldBe("<h1>Test</h1>");
        });
    }

    [Fact]
    public async Task NormalPageWithHxRoutePage()
    {
        await using var host = await AlbaHost.For<global::Program>();

        await host.Scenario(s =>
        {
            s.Get.Url("/normal-and-hx");
            s.WithRequestHeader(HtmxRequestHeaderNames.HtmxRequest, string.Empty);
            s.ContentShouldBe("<h1>Hello, world!</h1>");
        });
    }
}
