using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Htmxor.AspNetCore10;

public sealed class Issue287GuardTests
{
	private const string GuardMessage = "The active request is not an htmx request. This response operation requires the HX-Request: true header.";

	[Theory]
	[InlineData("Trigger")]
	[InlineData("Location")]
	[InlineData("EmptyBody")]
	public void Ordinary_request_rejects_htmx_only_operations_with_an_accurate_message_and_no_changes(string operation)
	{
		var context = new DefaultHttpContext();
		context.Request.Scheme = "https";
		context.Request.Host = new HostString("app.example");
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.ContentType = "text/html";
		context.Response.Headers["HX-Trigger"] = "existing-event";
		context.Response.Headers["HX-Location"] = "/existing-location";
		using var body = new MemoryStream("existing body"u8.ToArray());
		context.Response.Body = body;
		context.Response.ContentLength = body.Length;
		var headers = context.Response.Headers.ToArray();
		var response = context.GetHtmxContext().Response;
		var items = context.Items.ToArray();
		Action apply = operation switch
		{
			"Trigger" => () => response.Trigger("new-event"),
			"Location" => () => response.Location("/new-location"),
			"EmptyBody" => () => response.EmptyBody(),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};

		var exception = Assert.Throws<InvalidOperationException>(apply);

		Assert.Multiple(
			() => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
			() => Assert.Equal(headers, context.Response.Headers.ToArray()),
			() => Assert.Equal(items, context.Items.ToArray()),
			() => Assert.Same(body, context.Response.Body),
			() => Assert.Equal(0, body.Position),
			() => Assert.Equal("existing body"u8.ToArray(), body.ToArray()),
			() => Assert.Equal(GuardMessage, exception.Message)
		);
	}

	[Theory]
	[InlineData("Location")]
	[InlineData("EmptyBody")]
	public async Task Rejected_body_suppressing_operation_preserves_component_output(string operation)
	{
		await using var app = await Issue287StatusCodeTests.StartAsync();
		using var client = app.GetTestClient();

		using var response = await client.GetAsync("/issue-287/guard?operation=" + operation);
		var html = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.False(response.Headers.Contains("HX-Location")),
			() => Assert.Contains("<p data-guard-output>Component output survives rejection.</p>", html, StringComparison.Ordinal),
			() => Assert.Contains("<output>" + GuardMessage + "</output>", html, StringComparison.Ordinal)
		);
	}
}
