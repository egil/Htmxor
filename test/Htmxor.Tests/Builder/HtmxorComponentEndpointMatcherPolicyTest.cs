using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Htmxor.Builder;

public class HtmxorComponentEndpointMatcherPolicyTest
{
    private static CandidateSet CreateHxCandidateSet(HxRouteAttribute hxRouteAttribute)
    {
        var htmxorPointMetadata = new HtmxorEndpointMetadata(hxRouteAttribute);
        var endpoint = new Endpoint(null, new(htmxorPointMetadata), null);
        var candidates = new CandidateSet([endpoint], [new()], [1]);
        return candidates;
    }

    private static CandidateSet CreateRouteCandidateSet(RouteAttribute routeAttribute)
    {
        var endpoint = new Endpoint(null, new(routeAttribute), null);
        var candidates = new CandidateSet([endpoint], [new()], [1]);
        return candidates;
    }

    [Fact]
    public void AppliesToEndpoints_with_route_only_endpoint()
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();

        var result = cut.AppliesToEndpoints([new Endpoint(null, null, null)]);

        Assert.False(result);
    }

    [Fact]
    public void AppliesToEndpoints_with_hxroute_endpoint()
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();
        var htmxorPointMetadata = new HtmxorEndpointMetadata(new HxRouteAttribute("/"));

        var result = cut.AppliesToEndpoints([new Endpoint(null, new(htmxorPointMetadata), null)]);

        Assert.True(result);
    }

    [Fact]
    public void ApplyAsync_HxRequest_HxEndpoint()
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();
        var httpContext = new HttpContextBuilder()
            .WithRequestHeader((HtmxRequestHeaderNames.HtmxRequest, null))
            .Build();
        CandidateSet candidates = CreateHxCandidateSet(new HxRouteAttribute("/"));

        cut.ApplyAsync(httpContext, candidates);

        candidates.IsValidCandidate(0).Should().BeTrue();
    }

    [Fact]
    public void ApplyAsync_HxRequest_RouteEndpoint()
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();
        var httpContext = new HttpContextBuilder()
            .WithRequestHeader((HtmxRequestHeaderNames.HtmxRequest, null))
            .Build();
        CandidateSet candidates = CreateRouteCandidateSet(new RouteAttribute("/"));

        cut.ApplyAsync(httpContext, candidates);

        candidates.IsValidCandidate(0).Should().BeFalse();
    }

    [Fact]
    public void ApplyAsync_RouteRequest_HxEndpoint()
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();
        var httpContext = new HttpContextBuilder().Build();
        CandidateSet candidates = CreateHxCandidateSet(new HxRouteAttribute("/"));

        cut.ApplyAsync(httpContext, candidates);

        candidates.IsValidCandidate(0).Should().BeFalse();
    }

    public static TheoryData<HxRouteAttribute, (string HeaderName, string? Value)[]> MatchingHxRouteRequests = new TheoryData<HxRouteAttribute, (string HeaderName, string? Value)[]>
    {
        { new("/"), [] },
        { new("/") { CurrentURL = "/foo"}, [(HtmxRequestHeaderNames.CurrentURL, "/foo")] },
        { new("/") { CurrentURL = "/foo"}, [(HtmxRequestHeaderNames.CurrentURL, "/FOO")] },
        { new("/") { CurrentURL = "/FOO"}, [(HtmxRequestHeaderNames.CurrentURL, "/foo")] },
        { new("/") { Target = "#foo"}, [(HtmxRequestHeaderNames.Target, "#foo")] },
        { new("/") { Target = "#foo"}, [(HtmxRequestHeaderNames.Target, "#FOO")] },
        { new("/") { Targets = ["#foo", "#bar"]}, [(HtmxRequestHeaderNames.Target, "#foo")] },
        { new("/") { Targets = ["#foo", "#bar"]}, [(HtmxRequestHeaderNames.Target, "#BAR")] },
        { new("/") { Trigger = "#foo"}, [(HtmxRequestHeaderNames.Trigger, "#foo")] },
        { new("/") { Trigger = "#foo"}, [(HtmxRequestHeaderNames.Trigger, "#FOO")] },
        { new("/") { TriggerName = "#foo"}, [(HtmxRequestHeaderNames.TriggerName, "#foo")] },
        { new("/") { TriggerName = "#foo"}, [(HtmxRequestHeaderNames.TriggerName, "#FOO")] },
    };

    [Theory]
    [MemberData(nameof(MatchingHxRouteRequests))]
    public void ApplyAsync_HxRequest_HxEndpoint_matching(HxRouteAttribute hxRouteAttribute, (string HeaderName, string? Value)[] requestHeaders)
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();
        var httpContext = new HttpContextBuilder()
            .WithRequestHeader([(HtmxRequestHeaderNames.HtmxRequest, null), .. requestHeaders])
            .Build();
        var candidates = CreateHxCandidateSet(hxRouteAttribute);

        cut.ApplyAsync(httpContext, candidates);

        candidates.IsValidCandidate(0).Should().BeTrue();
    }

    public static TheoryData<HxRouteAttribute, (string HeaderName, string? Value)[]> NoneMatchingHxRouteRequests = new TheoryData<HxRouteAttribute, (string HeaderName, string? Value)[]>
    {
        { new("/") { CurrentURL = "/foo"}, [(HtmxRequestHeaderNames.CurrentURL, "/bar")] },
        { new("/") { Target = "#foo"}, [(HtmxRequestHeaderNames.Target, "#bar")] },
        { new("/") { Targets = ["#foo", "#bar"]}, [(HtmxRequestHeaderNames.Target, "#baz")] },
        { new("/") { Trigger = "#foo"}, [(HtmxRequestHeaderNames.Trigger, "#bar")] },
        { new("/") { TriggerName = "#foo"}, [(HtmxRequestHeaderNames.TriggerName, "#bar")] },
    };

    [Theory]
    [MemberData(nameof(NoneMatchingHxRouteRequests))]
    public void ApplyAsync_HxRequest_HxEndpoint_none_matching(HxRouteAttribute hxRouteAttribute, (string HeaderName, string? Value)[] requestHeaders)
    {
        var cut = new HtmxorComponentEndpointMatcherPolicy();
        var httpContext = new HttpContextBuilder()
            .WithRequestHeader([(HtmxRequestHeaderNames.HtmxRequest, null), .. requestHeaders])
            .Build();
        var candidates = CreateHxCandidateSet(hxRouteAttribute);

        cut.ApplyAsync(httpContext, candidates);

        candidates.IsValidCandidate(0).Should().BeFalse();
    }
}
