using Bunit;
using Htmxor.TestAssets.FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Http;

public class HtmxResponseTests : TestContext
{
	private static HttpContext CreateHttpContext(Action<JsonOptions>? configureJsonOptions = null)
	{
		var services = new ServiceCollection();
		if (configureJsonOptions is not null)
		{
			services.Configure(configureJsonOptions);
		}

		var result = new DefaultHttpContext()
		{
			RequestServices = services.BuildServiceProvider()
		};
		result.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "";
		result.GetHtmxContext();
		return result;
	}

	[Fact]
	public void Location_AddsLocationHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Location("/new-location");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Location]
			.Should()
			.Equal(["/new-location"]);
	}

	[Fact]
	public void Location_AddsLocationWIthAjaxContextHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		var locationTarget = new LocationTarget
		{
			Path = "/new-location",
			Target = "#testdiv"
		};

		// Act
		response.Location(locationTarget);

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Location]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "path": "/new-location", "target": "#testdiv" }
                """);
	}

	[Fact]
	public void PushUrl_AddsPushUrlHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.PushUrl("/new-url");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.PushUrl].Should().Equal(["/new-url"]);
	}

	[Fact]
	public void Redirect_AddsRedirectHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Redirect("/new-redirect");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Redirect].Should().Equal(["/new-redirect"]);
	}

	[Fact]
	public void Refresh_AddsRefreshHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Refresh();

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Refresh].Should().Equal(["true"]);
	}

	[Fact]
	public void ReplaceUrl_AddsReplaceUrlHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.ReplaceUrl("/new-replace-url");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl].Should().Equal(["/new-replace-url"]);
	}

	[Fact]
	public void PushUrl_AddsPushUrlBrowserHistoryHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.PreventBrowserHistoryUpdate();

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.PushUrl].Should().Equal(["false"]);
	}

	[Fact]
	public void ReplaceUrl_AddsReplaceUrlBrowserCUrrentUrlHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.PreventBrowserCurrentUrlUpdate();

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl].Should().Equal(["false"]);
	}

	[Fact]
	public void Reswap_AddsReswapHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Reswap(SwapStyle.innerHTML);

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Reswap].Should().Equal(["innerHTML"]);
	}

	[Fact]
	public void Retarget_AddsRetargetHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Retarget(".new-target");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Retarget].Should().Equal([".new-target"]);
	}

	[Fact]
	public void Reselect_AddsReselectHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Reselect(".new-selection");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Reselect].Should().Equal([".new-selection"]);
	}

	[Fact]
	public void Trigger_without_details_adds_trigger_header()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("event1");
	}

	[Fact]
	public void Multiple_trigger_events_without_details_share_trigger_header()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");
		response.Trigger("event2");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("event1,event2");
	}

	[Fact]
	public void Duplicate_trigger_event_is_emitted_once()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");
		response.Trigger("event1");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("event1");
	}

	[Fact]
	public void Trigger_with_detail_adds_json_trigger_header()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		var triggerObject = new { level = "info", message = "Here Is A Message" };

		// Act
		response.Trigger("showMessage", triggerObject);

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "showMessage": { "level": "info", "message": "Here Is A Message" } }
                """);
	}

	[Fact]
	public void Trigger_combines_events_with_and_without_details()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");
		response.Trigger("event2", new { magic = "something" });
		response.Trigger("event3", new { moremagic = false });

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "event1": null, "event2": { "magic": "something" }, "event3": { "moremagic": false } }
                """);
	}

	[Fact]
	public void Trigger_uses_application_json_options_for_event_details()
	{
		var context = CreateHttpContext(options =>
			options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);
		var response = context.GetHtmxContext().Response;

		response.Trigger("showMessage", new { MessageLevel = "info" });

		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "showMessage": { "message_level": "info" } }
                """);
	}

	[Fact]
	public void Htmx4_response_trigger_surface_does_not_expose_removed_timing_api()
	{
		var assembly = typeof(HtmxResponse).Assembly;

		assembly.GetType("Htmxor.TriggerTiming").Should().BeNull();
		typeof(HtmxResponseHeaderNames).GetField("TriggerAfterSwap").Should().BeNull();
		typeof(HtmxResponseHeaderNames).GetField("TriggerAfterSettle").Should().BeNull();
		Assert.All(
			typeof(HtmxResponse).GetMethods().Where(method => method.Name == nameof(HtmxResponse.Trigger)),
			method => Assert.DoesNotContain(
				method.GetParameters(),
				parameter => parameter.ParameterType.FullName == "Htmxor.TriggerTiming"));
	}
}
