using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public sealed class HtmxRequestTests
{
	public static TheoryData<string[]?> InvalidRequestMarkers =>
		new()
		{
			{ null },
			{ [""] },
			{ ["false"] },
			{ ["truthy"] },
			{ ["true, true"] },
			{ ["true", "true"] },
		};

	[Theory]
	[InlineData("full", HtmxRequestType.Full, RoutingMode.Standard)]
	[InlineData("partial", HtmxRequestType.Partial, RoutingMode.Direct)]
	[InlineData("unknown", null, RoutingMode.Standard)]
	[InlineData("", null, RoutingMode.Standard)]
	public void Request_type_controls_representation(
		string value,
		HtmxRequestType? expectedType,
		RoutingMode expectedMode)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = value;

		var request = new HtmxRequest(context);

		Assert.Equal(expectedType, request.RequestType);
		Assert.Equal(expectedMode, request.RoutingMode);
	}

	[Fact]
	public void Request_preserves_complete_source_and_target_identities()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Source] = "button#submit";
		context.Request.Headers[HtmxRequestHeaderNames.Target] = "section";

		var request = new HtmxRequest(context);

		Assert.Equal("button#submit", request.Source);
		Assert.Equal("section", request.Target);
	}

	[Fact]
	public void Contradictory_request_types_fail_closed()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] =
			new Microsoft.Extensions.Primitives.StringValues(["partial", "full"]);

		var request = new HtmxRequest(context);

		Assert.Null(request.RequestType);
		Assert.Equal(RoutingMode.Standard, request.RoutingMode);
	}

	[Theory]
	[MemberData(nameof(InvalidRequestMarkers))]
	public void Invalid_request_marker_ignores_dependent_context_and_keeps_standard_routing(
		string[]? markerValues)
	{
		var context = new DefaultHttpContext();
		if (markerValues is not null)
		{
			context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = markerValues;
		}
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Boosted] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.HistoryRestoreRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.CurrentURL] = "/current";
		context.Request.Headers[HtmxRequestHeaderNames.Source] = "button#source";
		context.Request.Headers[HtmxRequestHeaderNames.Target] = "section#target";

		var request = new HtmxRequest(context);

		Assert.False(request.IsHtmxRequest);
		Assert.Equal(RoutingMode.Standard, request.RoutingMode);
		Assert.Null(request.RequestType);
		Assert.False(request.IsBoosted);
		Assert.False(request.IsHistoryRestoreRequest);
		Assert.Null(request.CurrentURL);
		Assert.Null(request.Source);
		Assert.Null(request.Target);
	}

	[Theory]
	[InlineData("true")]
	[InlineData(" True ")]
	[InlineData("TRUE")]
	public void One_trimmed_case_insensitive_true_marker_enables_htmx_context(string markerValue)
	{
		var partialContext = new DefaultHttpContext();
		partialContext.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = markerValue;
		partialContext.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		var fullContext = new DefaultHttpContext();
		fullContext.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = markerValue;
		fullContext.Request.Headers[HtmxRequestHeaderNames.RequestType] = "full";

		var partialRequest = new HtmxRequest(partialContext);
		var fullRequest = new HtmxRequest(fullContext);

		Assert.True(partialRequest.IsHtmxRequest);
		Assert.Equal(HtmxRequestType.Partial, partialRequest.RequestType);
		Assert.Equal(RoutingMode.Direct, partialRequest.RoutingMode);
		Assert.True(fullRequest.IsHtmxRequest);
		Assert.Equal(HtmxRequestType.Full, fullRequest.RequestType);
		Assert.Equal(RoutingMode.Standard, fullRequest.RoutingMode);
	}
}
