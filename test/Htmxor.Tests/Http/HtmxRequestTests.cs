using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public sealed class HtmxRequestTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("false")]
	[InlineData("TRUE")]
	[InlineData("invalid")]
	[InlineData("true,false")]
	public void Invalid_request_marker_ignores_dependent_context(string? marker)
	{
		var context = new DefaultHttpContext();
		if (marker is not null)
		{
			context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = marker;
		}

		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Boosted] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.HistoryRestoreRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.CurrentURL] = "/current";
		context.Request.Headers[HtmxRequestHeaderNames.Target] = "section#target";
		context.Request.Headers[HtmxRequestHeaderNames.Source] = "button#source";

		var request = new HtmxRequest(context);

		Assert.False(request.IsHtmxRequest);
		Assert.Equal(RoutingMode.Standard, request.RoutingMode);
		Assert.Null(request.RequestType);
		Assert.False(request.IsBoosted);
		Assert.False(request.IsHistoryRestoreRequest);
		Assert.Null(request.CurrentURL);
		Assert.Null(request.Target);
		Assert.Null(request.Source);
	}

	[Fact]
	public void Repeated_request_markers_ignore_dependent_context()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] =
			new Microsoft.Extensions.Primitives.StringValues(["true", "true"]);
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";

		var request = new HtmxRequest(context);

		Assert.False(request.IsHtmxRequest);
		Assert.Equal(RoutingMode.Standard, request.RoutingMode);
		Assert.Null(request.RequestType);
	}

	[Theory]
	[InlineData("true")]
	[InlineData(" true ")]
	public void One_normalized_true_marker_enables_dependent_context(string marker)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = marker;
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";

		var request = new HtmxRequest(context);

		Assert.True(request.IsHtmxRequest);
		Assert.Equal(RoutingMode.Direct, request.RoutingMode);
		Assert.Equal(HtmxRequestType.Partial, request.RequestType);
	}

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
}
