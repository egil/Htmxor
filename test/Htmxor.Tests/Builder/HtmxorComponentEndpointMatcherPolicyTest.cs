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
		{ new("/") { CurrentUrl = "/foo"}, [(HtmxRequestHeaderNames.CurrentUrl, "https://localhost/foo")] },
		{ new("/") { CurrentUrl = "/foo?Filter=Open"}, [(HtmxRequestHeaderNames.CurrentUrl, "https://localhost/foo?Filter=Open")] },
		{ new("/") { CurrentUrl = "HTTPS://LOCALHOST/foo"}, [(HtmxRequestHeaderNames.CurrentUrl, "https://localhost/foo")] },
		{ new("/") { CurrentUrl = "https://localhost:443/foo"}, [(HtmxRequestHeaderNames.CurrentUrl, "https://localhost/foo")] },
		{ new("/") { Target = "div#foo"}, [(HtmxRequestHeaderNames.Target, "div#foo")] },
		{ new("/") { Target = "div#foo"}, [(HtmxRequestHeaderNames.Target, "DIV#foo")] },
		{ new("/") { Targets = ["div#foo", "section"]}, [(HtmxRequestHeaderNames.Target, "div#foo")] },
		{ new("/") { Targets = ["div#foo", "section"]}, [(HtmxRequestHeaderNames.Target, "SECTION")] },
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
		{ new("/") { CurrentUrl = "/foo"}, [(HtmxRequestHeaderNames.CurrentUrl, "https://localhost/bar")] },
		{ new("/") { CurrentUrl = "https://localhost/foo"}, [(HtmxRequestHeaderNames.CurrentUrl, "http://localhost/foo")] },
		{ new("/") { CurrentUrl = "ftp://localhost/foo"}, [(HtmxRequestHeaderNames.CurrentUrl, "https://localhost/foo")] },
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

	[Fact]
	public void ApplyAsync_current_url_matching_keeps_path_and_query_case_sensitive()
	{
		var cut = new ComponentEndpointMatcherPolicy();
		var httpContext = new HttpContextBuilder()
			.WithRequestHeader(
				(HtmxRequestHeaderNames.HtmxRequest, "true"),
				(HtmxRequestHeaderNames.RequestType, "partial"),
				(HtmxRequestHeaderNames.CurrentUrl, "https://example.test/Items?Filter=Open"))
			.Build();
		var candidates = CreateHxCandidateSet(new HtmxRouteAttribute("/")
		{
			CurrentUrl = "https://example.test/items?filter=open",
		});

		cut.ApplyAsync(httpContext, candidates);

		candidates.IsValidCandidate(0).Should().BeFalse();
	}
}
