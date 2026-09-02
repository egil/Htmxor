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
	[InlineData("\rtrue\r")]
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
		context.Request.Headers[HtmxRequestHeaderNames.CurrentUrl] = "/current";
		context.Request.Headers[HtmxRequestHeaderNames.Target] = "section#target";
		context.Request.Headers[HtmxRequestHeaderNames.Source] = "button#source";

		var request = new HtmxRequest(context);

		Assert.False(request.IsHtmxRequest);
		Assert.Equal(RoutingMode.Standard, request.RoutingMode);
		Assert.Null(request.RequestType);
		Assert.False(request.IsBoosted);
		Assert.False(request.IsHistoryRestoreRequest);
		Assert.Null(request.CurrentUrl);
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
	[InlineData("\ttrue\t")]
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

	[Theory]
	[InlineData("")]
	[InlineData("false")]
	[InlineData("TRUE")]
	[InlineData("true,false")]
	public void Boolean_request_headers_require_one_lowercase_true_value(string value)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "full";
		context.Request.Headers[HtmxRequestHeaderNames.Boosted] = value;
		context.Request.Headers[HtmxRequestHeaderNames.HistoryRestoreRequest] = value;

		var request = new HtmxRequest(context);

		Assert.False(request.IsBoosted);
		Assert.False(request.IsHistoryRestoreRequest);
	}

	[Fact]
	public void Repeated_boolean_request_headers_fail_closed()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "full";
		context.Request.Headers[HtmxRequestHeaderNames.Boosted] =
			new Microsoft.Extensions.Primitives.StringValues(["true", "true"]);
		context.Request.Headers[HtmxRequestHeaderNames.HistoryRestoreRequest] =
			new Microsoft.Extensions.Primitives.StringValues(["true", "false"]);

		var request = new HtmxRequest(context);

		Assert.False(request.IsBoosted);
		Assert.False(request.IsHistoryRestoreRequest);
	}

	[Theory]
	[InlineData("https://example.test/current", "https://example.test/current")]
	[InlineData(" /current ", null)]
	[InlineData("ftp://example.test/current", null)]
	[InlineData("https://example.test/current\n", null)]
	public void Current_url_requires_one_absolute_http_uri(
		string value,
		string? expectedOriginalString)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "full";
		context.Request.Headers[HtmxRequestHeaderNames.CurrentUrl] = value;

		var request = new HtmxRequest(context);

		Assert.Equal(expectedOriginalString, request.CurrentUrl?.OriginalString);
	}

	[Fact]
	public void Ill_formed_utf16_request_header_values_fail_closed()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		var illFormed = new string('\uD800', 1);
		context.Request.Headers[HtmxRequestHeaderNames.CurrentUrl] = illFormed;
		context.Request.Headers[HtmxRequestHeaderNames.Source] = illFormed;
		context.Request.Headers[HtmxRequestHeaderNames.Target] = illFormed;

		var request = new HtmxRequest(context);

		Assert.Null(request.CurrentUrl);
		Assert.Null(request.Source);
		Assert.Null(request.Target);
	}

	[Fact]
	public void Repeated_current_url_fails_closed()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "full";
		context.Request.Headers[HtmxRequestHeaderNames.CurrentUrl] =
			new Microsoft.Extensions.Primitives.StringValues([
				"https://example.test/current",
				"https://example.test/current"]);

		var request = new HtmxRequest(context);

		Assert.Null(request.CurrentUrl);
	}

	[Theory]
	[InlineData("button#submit", "button#submit")]
	[InlineData(" button#submit\t", "button#submit")]
	[InlineData(" ", null)]
	[InlineData("button\n#submit", null)]
	public void Source_and_target_preserve_one_safe_open_value(
		string value,
		string? expectedValue)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Source] = value;
		context.Request.Headers[HtmxRequestHeaderNames.Target] = value;

		var request = new HtmxRequest(context);

		Assert.Equal(expectedValue, request.Source);
		Assert.Equal(expectedValue, request.Target);
	}

	[Fact]
	public void Repeated_source_and_target_fail_closed()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Source] =
			new Microsoft.Extensions.Primitives.StringValues(["button#source", "button#source"]);
		context.Request.Headers[HtmxRequestHeaderNames.Target] =
			new Microsoft.Extensions.Primitives.StringValues(["section#target", "section#target"]);

		var request = new HtmxRequest(context);

		Assert.Null(request.Source);
		Assert.Null(request.Target);
	}
}
