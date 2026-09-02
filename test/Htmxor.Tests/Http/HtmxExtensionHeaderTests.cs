using Htmxor.TestAssets;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public sealed class HtmxExtensionHeaderTests
{
	[Fact]
	public void Valid_extension_request_header_is_exposed_exactly()
	{
		var context = CreateHtmxContext(("HX-PTag", "order:42"));

		var found = new HtmxRequest(context).TryGetExtensionHeader("HX-PTag", out var value);

		Assert.True(found);
		Assert.Equal("order:42", value);
	}

	[Fact]
	public void Empty_extension_request_header_is_exposed_exactly()
	{
		var context = CreateHtmxContext(("HX-PTag", string.Empty));

		var found = new HtmxRequest(context).TryGetExtensionHeader("HX-PTag", out var value);

		Assert.True(found);
		Assert.Equal(string.Empty, value);
	}

	[Theory]
	[InlineData("HX-Request")]
	[InlineData("hx-trigger")]
	[InlineData("HXOR-Event-Handler-Id")]
	[InlineData("HXOR-Custom")]
	[InlineData("X-Extension")]
	[InlineData("HX Bad")]
	public void Protected_or_malformed_extension_request_names_are_not_exposed(string name)
	{
		var context = CreateHtmxContext((name, "value"));

		var found = new HtmxRequest(context).TryGetExtensionHeader(name, out var value);

		Assert.False(found);
		Assert.Equal(string.Empty, value);
	}

	[Fact]
	public void Repeated_or_unsafe_extension_request_values_are_not_exposed()
	{
		var repeated = CreateHtmxContext();
		repeated.Request.Headers.Append("HX-PTag", "first");
		repeated.Request.Headers.Append("HX-PTag", "second");
		var control = CreateHtmxContext(("HX-PTag", "unsafe\nvalue"));
		var malformed = CreateHtmxContext(("HX-PTag", "\uD800"));

		Assert.False(new HtmxRequest(repeated).TryGetExtensionHeader("HX-PTag", out _));
		Assert.False(new HtmxRequest(control).TryGetExtensionHeader("HX-PTag", out _));
		Assert.False(new HtmxRequest(malformed).TryGetExtensionHeader("HX-PTag", out _));
	}

	[Fact]
	public void Oversized_extension_request_value_is_not_exposed()
	{
		var context = CreateHtmxContext(("HX-PTag", new string('a', 4097)));

		Assert.False(new HtmxRequest(context).TryGetExtensionHeader("HX-PTag", out _));
	}

	[Fact]
	public void Oversized_extension_name_is_not_exposed_or_written()
	{
		var name = "HX-" + new string('a', 4094);
		var requestContext = CreateHtmxContext((name, "value"));
		var responseContext = CreateHtmxContext();

		Assert.False(new HtmxRequest(requestContext).TryGetExtensionHeader(name, out _));
		Assert.Throws<ArgumentException>(() => new HtmxResponse(responseContext).SetExtensionHeader(name, "value"));
		Assert.False(responseContext.Response.Headers.ContainsKey(name));
	}

	[Fact]
	public void Extension_response_header_replaces_only_its_own_header_and_preserves_response_state()
	{
		var context = CreateHtmxContext();
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers[HtmxResponseHeaderNames.Reswap] = "outerHTML";
		context.Response.Headers["X-Application"] = "retained";
		var response = new HtmxResponse(context);

		var returned = response.SetExtensionHeader("HX-PTag", "first");
		response.SetExtensionHeader("hx-ptag", "second");
		response.SetExtensionHeader("HX-Trace", "trace");

		Assert.Same(response, returned);
		Assert.Equal("second", context.Response.Headers["HX-PTag"]);
		Assert.Equal("trace", context.Response.Headers["HX-Trace"]);
		Assert.Equal("outerHTML", context.Response.Headers[HtmxResponseHeaderNames.Reswap]);
		Assert.Equal("retained", context.Response.Headers["X-Application"]);
		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
	}

	[Fact]
	public void Empty_extension_response_header_is_preserved_exactly()
	{
		var context = CreateHtmxContext();

		new HtmxResponse(context).SetExtensionHeader("HX-PTag", string.Empty);

		Assert.Equal(string.Empty, context.Response.Headers["HX-PTag"]);
	}

	[Theory]
	[InlineData("HX-Request", "value")]
	[InlineData("HXOR-Private", "value")]
	[InlineData("HX Bad", "value")]
	[InlineData("HX-PTag", "unsafe\rvalue")]
	[InlineData("HX-PTag", "\uD800")]
	public void Invalid_extension_response_input_does_not_mutate_response(string name, string value)
	{
		var context = CreateHtmxContext();
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers["X-Application"] = "retained";
		var response = new HtmxResponse(context);

		Assert.Throws<ArgumentException>(() => response.SetExtensionHeader(name, value));

		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		Assert.Equal("retained", context.Response.Headers["X-Application"]);
		Assert.False(context.Response.Headers.ContainsKey("HX-PTag"));
	}

	[Fact]
	public void Invalid_marker_rejects_response_after_validation_without_mutation()
	{
		var context = new HttpContextBuilder()
			.WithRequestHeader((HtmxRequestHeaderNames.HtmxRequest, "false"))
			.Build();
		context.Response.Headers["X-Application"] = "retained";
		var response = new HtmxResponse(context);

		Assert.Throws<InvalidOperationException>(() => response.SetExtensionHeader("HX-PTag", "value"));
		Assert.Throws<ArgumentException>(() => response.SetExtensionHeader("HX-PTag", "unsafe\nvalue"));
		Assert.Equal("retained", context.Response.Headers["X-Application"]);
		Assert.False(context.Response.Headers.ContainsKey("HX-PTag"));
	}

	[Fact]
	public void Oversized_extension_response_value_does_not_mutate_response()
	{
		var context = CreateHtmxContext();
		var response = new HtmxResponse(context);

		Assert.Throws<ArgumentException>(() => response.SetExtensionHeader("HX-PTag", new string('a', 4097)));
		Assert.False(context.Response.Headers.ContainsKey("HX-PTag"));
	}

	private static HttpContext CreateHtmxContext(params (string HeaderName, string Value)[] headers)
	{
		var context = new HttpContextBuilder()
			.WithRequestHeader((HtmxRequestHeaderNames.HtmxRequest, "true"))
			.Build();
		foreach (var (name, value) in headers)
		{
			context.Request.Headers[name] = value;
		}

		return context;
	}

}
