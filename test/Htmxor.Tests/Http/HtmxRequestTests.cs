using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public sealed class HtmxRequestTests
{
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
