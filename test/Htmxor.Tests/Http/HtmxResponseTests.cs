using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Http;

public class HtmxResponseTests : TestContext
{
    private static HttpContext CreateHttpContext()
    {
        var result = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
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
        Assert.Equal("/new-location", context.Response.Headers[HtmxResponseHeaderNames.Location]);
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
        Assert.Equal("/new-url", context.Response.Headers[HtmxResponseHeaderNames.PushUrl]);
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
        Assert.Equal("/new-redirect", context.Response.Headers[HtmxResponseHeaderNames.Redirect]);
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
        Assert.Equal("true", context.Response.Headers[HtmxResponseHeaderNames.Refresh]);
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
        Assert.Equal("/new-replace-url", context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl]);
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
        Assert.Equal("false", context.Response.Headers[HtmxResponseHeaderNames.PushUrl]);
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
        Assert.Equal("false", context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl]);
    }

    [Fact]
    public void Reswap_AddsReswapHeader()
    {
        // Arrange
        var context = CreateHttpContext();
        var response = context.GetHtmxContext().Response;

        // Act
        response.Reswap(SwapStyle.InnerHTML);

        // Assert
        Assert.Equal("innerHTML", context.Response.Headers[HtmxResponseHeaderNames.Reswap]);
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
        Assert.Equal(".new-target", context.Response.Headers[HtmxResponseHeaderNames.Retarget]);
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
        Assert.Equal(".new-selection", context.Response.Headers[HtmxResponseHeaderNames.Reselect]);
    }

    [Theory]
    [InlineData(TriggerTiming.Default, HtmxResponseHeaderNames.Trigger)]
    [InlineData(TriggerTiming.AfterSwap, HtmxResponseHeaderNames.TriggerAfterSwap)]
    [InlineData(TriggerTiming.AfterSettle, HtmxResponseHeaderNames.TriggerAfterSettle)]
    public void Trigger_without_details(TriggerTiming triggerTiming, string expectedHeaderKey)
    {
        // Arrange
        var context = CreateHttpContext();
        var response = context.GetHtmxContext().Response;

        // Act
        response.Trigger("event1", timing: triggerTiming);

        // Assert
        context.Response.Headers[expectedHeaderKey]
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("event1");
    }

    [Theory]
    [InlineData(TriggerTiming.Default, HtmxResponseHeaderNames.Trigger)]
    [InlineData(TriggerTiming.AfterSwap, HtmxResponseHeaderNames.TriggerAfterSwap)]
    [InlineData(TriggerTiming.AfterSettle, HtmxResponseHeaderNames.TriggerAfterSettle)]
    public void Multiple_trigger_events_without_details(TriggerTiming triggerTiming, string expectedHeaderKey)
    {
        // Arrange
        var context = CreateHttpContext();
        var response = context.GetHtmxContext().Response;

        // Act
        response.Trigger("event1", timing: triggerTiming);
        response.Trigger("event2", timing: triggerTiming);

        // Assert
        context.Response.Headers[expectedHeaderKey]
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("event1,event2");
    }

    [Theory]
    [InlineData(TriggerTiming.Default, HtmxResponseHeaderNames.Trigger)]
    [InlineData(TriggerTiming.AfterSwap, HtmxResponseHeaderNames.TriggerAfterSwap)]
    [InlineData(TriggerTiming.AfterSettle, HtmxResponseHeaderNames.TriggerAfterSettle)]
    public void Same_trigger_event_twice_without_details(TriggerTiming triggerTiming, string expectedHeaderKey)
    {
        // Arrange
        var context = CreateHttpContext();
        var response = context.GetHtmxContext().Response;

        // Act
        response.Trigger("event1", timing: triggerTiming);
        response.Trigger("event1", timing: triggerTiming);

        // Assert
        context.Response.Headers[expectedHeaderKey]
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("event1");
    }

    [Theory]
    [InlineData(TriggerTiming.Default, HtmxResponseHeaderNames.Trigger)]
    [InlineData(TriggerTiming.AfterSwap, HtmxResponseHeaderNames.TriggerAfterSwap)]
    [InlineData(TriggerTiming.AfterSettle, HtmxResponseHeaderNames.TriggerAfterSettle)]
    public void Trigger_DefaultObject_AddsTriggerHeaderWithJsonString(TriggerTiming triggerTiming, string expectedHeaderKey)
    {
        // Arrange
        var context = CreateHttpContext();
        var response = context.GetHtmxContext().Response;
        var triggerObject = new { level = "info", message = "Here Is A Message" };

        // Act
        response.Trigger("showMessage", triggerObject, triggerTiming);

        // Assert
        context.Response.Headers[expectedHeaderKey]
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeJsonSemanticallyEqualTo("""
                { "showMessage": { "level": "info", "message": "Here Is A Message" } }
                """);
    }

    [Theory]
    [InlineData(TriggerTiming.Default, HtmxResponseHeaderNames.Trigger)]
    [InlineData(TriggerTiming.AfterSwap, HtmxResponseHeaderNames.TriggerAfterSwap)]
    [InlineData(TriggerTiming.AfterSettle, HtmxResponseHeaderNames.TriggerAfterSettle)]
    public void Trigger_CanUseExistingTriggerWithMultipleTriggersWithDetail_AddsCorrectTriggerHeader(TriggerTiming triggerTiming, string expectedHeaderKey)
    {
        // Arrange
        var context = CreateHttpContext();
        var response = context.GetHtmxContext().Response;

        // Act
        response.Trigger("event1", triggerTiming);
        response.Trigger("event2", new { magic = "something" }, triggerTiming);
        response.Trigger("event3", new { moremagic = false }, triggerTiming);

        // Assert
        context.Response.Headers[expectedHeaderKey]
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeJsonSemanticallyEqualTo("""
                { "event1": null, "event2": { "magic": "something" }, "event3": { "moremagic": false } }
                """);
    }
}
