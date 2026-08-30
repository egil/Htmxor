using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Htmxor.Builder;

public class HtmxorComponentEndpointMatcherPolicyTest
{
	private static CandidateSet CreateHxCandidateSet(HtmxRouteAttribute hxRouteAttribute)
	{
		var htmxorPointMetadata = new EndpointMetadata(hxRouteAttribute);
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
		var cut = new ComponentEndpointMatcherPolicy();

		var result = cut.AppliesToEndpoints([new Endpoint(null, null, null)]);

		Assert.False(result);
	}

	[Fact]
	public void AppliesToEndpoints_with_hxroute_endpoint()
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var htmxorPointMetadata = new EndpointMetadata(new HtmxRouteAttribute("/"));

		var result = cut.AppliesToEndpoints([new Endpoint(null, new(htmxorPointMetadata), null)]);

		Assert.True(result);
	}

	[Fact]
	public void ApplyAsync_HxRequest_HxEndpoint()
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var httpContext = new HttpContextBuilder()
			.WithRequestHeader(
				(HtmxRequestHeaderNames.HtmxRequest, "true"),
				(HtmxRequestHeaderNames.RequestType, "partial"))
			.Build();
		CandidateSet candidates = CreateHxCandidateSet(new HtmxRouteAttribute("/"));

		cut.ApplyAsync(httpContext, candidates);

		candidates.IsValidCandidate(0).Should().BeTrue();
	}

	[Fact]
	public void ApplyAsync_HxRequest_RouteEndpoint()
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var httpContext = new HttpContextBuilder()
			.WithRequestHeader(
				(HtmxRequestHeaderNames.HtmxRequest, "true"),
				(HtmxRequestHeaderNames.RequestType, "partial"))
			.Build();
		CandidateSet candidates = CreateRouteCandidateSet(new RouteAttribute("/"));

		cut.ApplyAsync(httpContext, candidates);

		candidates.IsValidCandidate(0).Should().BeFalse();
	}

	[Fact]
	public void ApplyAsync_RouteRequest_HxEndpoint()
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var httpContext = new HttpContextBuilder().Build();
		CandidateSet candidates = CreateHxCandidateSet(new HtmxRouteAttribute("/"));

		cut.ApplyAsync(httpContext, candidates);

		candidates.IsValidCandidate(0).Should().BeFalse();
	}

	public static TheoryData<HtmxRouteAttribute, (string HeaderName, string? Value)[]> MatchingHxRouteRequests = new TheoryData<HtmxRouteAttribute, (string HeaderName, string? Value)[]>
	{
		{ new("/"), [] },
		{ new("/") { CurrentURL = "/foo"}, [(HtmxRequestHeaderNames.CurrentURL, "/foo")] },
		{ new("/") { CurrentURL = "/foo"}, [(HtmxRequestHeaderNames.CurrentURL, "/FOO")] },
		{ new("/") { CurrentURL = "/FOO"}, [(HtmxRequestHeaderNames.CurrentURL, "/foo")] },
		{ new("/") { Target = "div#foo"}, [(HtmxRequestHeaderNames.Target, "div#foo")] },
		{ new("/") { Targets = ["div#foo", "section"]}, [(HtmxRequestHeaderNames.Target, "div#foo")] },
		{ new("/") { Targets = ["div#foo", "section"]}, [(HtmxRequestHeaderNames.Target, "section")] },
	};

	[Theory]
	[MemberData(nameof(MatchingHxRouteRequests))]
	public void ApplyAsync_HxRequest_HxEndpoint_matching(HtmxRouteAttribute hxRouteAttribute, (string HeaderName, string? Value)[] requestHeaders)
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var httpContext = new HttpContextBuilder()
			.WithRequestHeader([
				(HtmxRequestHeaderNames.HtmxRequest, "true"),
				(HtmxRequestHeaderNames.RequestType, "partial"),
				.. requestHeaders])
			.Build();
		var candidates = CreateHxCandidateSet(hxRouteAttribute);

		cut.ApplyAsync(httpContext, candidates);

		candidates.IsValidCandidate(0).Should().BeTrue();
	}

	public static TheoryData<HtmxRouteAttribute, (string HeaderName, string? Value)[]> NoneMatchingHxRouteRequests = new TheoryData<HtmxRouteAttribute, (string HeaderName, string? Value)[]>
	{
		{ new("/") { CurrentURL = "/foo"}, [(HtmxRequestHeaderNames.CurrentURL, "/bar")] },
		{ new("/") { Target = "div#foo"}, [(HtmxRequestHeaderNames.Target, "div#bar")] },
		{ new("/") { Target = "div#foo"}, [(HtmxRequestHeaderNames.Target, "div#FOO")] },
		{ new("/") { Targets = ["div#foo", "section"]}, [(HtmxRequestHeaderNames.Target, "div#baz")] },
	};

	[Theory]
	[MemberData(nameof(NoneMatchingHxRouteRequests))]
	public void ApplyAsync_HxRequest_HxEndpoint_none_matching(HtmxRouteAttribute hxRouteAttribute, (string HeaderName, string? Value)[] requestHeaders)
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var httpContext = new HttpContextBuilder()
			.WithRequestHeader([
				(HtmxRequestHeaderNames.HtmxRequest, "true"),
				(HtmxRequestHeaderNames.RequestType, "partial"),
				.. requestHeaders])
			.Build();
		var candidates = CreateHxCandidateSet(hxRouteAttribute);

		cut.ApplyAsync(httpContext, candidates);

		candidates.IsValidCandidate(0).Should().BeFalse();
	}
}
