using Bunit;
using Htmxor.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Htmxor.Http.Mock;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Http;

public class HtmxResponseTests : TestContext
{
    [Fact]
    public void Location_AddsLocationHeader()
    {
        // Arrange
        var context = new MockHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Location("/new-location");

        // Assert
        Assert.Equal("/new-location", context.Response.Headers[HtmxResponseHeaderNames.Location]);
    }

    [Fact]
    public void PushUrl_AddsPushUrlHeader()
    {
        // Arrange
        var context = new MockHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.PushUrl("/new-url");

        // Assert
        Assert.Equal("/new-url", context.Response.Headers[HtmxResponseHeaderNames.PushUrl]);
    }

    [Fact]
    public void Redirect_AddsRedirectHeader()
    {
        // Arrange
        var context = new MockHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Redirect("/new-redirect");

        // Assert
        Assert.Equal("/new-redirect", context.Response.Headers[HtmxResponseHeaderNames.Redirect]);
    }

    [Fact]
    public void Refresh_AddsRefreshHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Refresh();

        // Assert
        Assert.Equal("true", context.Response.Headers[HtmxResponseHeaderNames.Refresh]);
    }

    [Fact]
    public void ReplaceUrl_AddsReplaceUrlHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.ReplaceUrl("/new-replace-url");

        // Assert
        Assert.Equal("/new-replace-url", context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl]);
    }

    [Fact]
    public void Reswap_AddsReswapHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Reswap(SwapStyle.InnerHTML);

        // Assert
        Assert.Equal("innerHTML", context.Response.Headers[HtmxResponseHeaderNames.Reswap]);
    }

    [Fact]
    public void Retarget_AddsRetargetHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Retarget(".new-target");

        // Assert
        Assert.Equal(".new-target", context.Response.Headers[HtmxResponseHeaderNames.Retarget]);
    }

    [Fact]
    public void Reselect_AddsReselectHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Reselect(".new-selection");

        // Assert
        Assert.Equal(".new-selection", context.Response.Headers[HtmxResponseHeaderNames.Reselect]);
    }

    [Fact]
    public void Trigger_AfterSwap_AddsTriggerAfterSwapHeader()
    {
        // Arrange
        var context = new MockHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Trigger("event1", timing: TriggerStyle.AfterSwap);

        // Assert
        var result = context.Response.Headers[HtmxResponseHeaderNames.TriggerAfterSwap];
        Assert.Contains("event1", result.ToList());
    }

    [Fact]
    public void Trigger_AfterSettle_AddsTriggerAfterSettleHeader()
    {
        // Arrange
        var context = new MockHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Trigger("event2", timing: TriggerStyle.AfterSettle);

        // Assert
        var result = context.Response.Headers[HtmxResponseHeaderNames.TriggerAfterSettle];
        Assert.Contains("event2", result.ToList());
    }

    [Fact]
    public void Trigger_Default_AddsTriggerHeader()
    {
        // Arrange
        var context = new MockHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Trigger("event3");

        // Assert
        var result = context.Response.Headers[HtmxResponseHeaderNames.Trigger];
        Assert.Contains("event3", result.ToList());
    }

    [Fact]
    public void Trigger_DefaultObject_AddsTriggerHeaderWithJsonString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);
        var triggerObject = new { level = "info", message = "Here Is A Message" };

        // Act
        response.Trigger("showMessage", triggerObject);

        // Assert
        var result = context.Response.Headers[HtmxResponseHeaderNames.Trigger];
        Assert.Contains("{\"showMessage\":{\"level\":\"info\",\"message\":\"Here Is A Message\"}}", result.ToString());
    }

    [Fact]
    public void Trigger_DefaultObjectWithTiming_AddsCorrectTriggerHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);
        var triggerObject = new { level = "info", message = "Here Is A Message" };

        // Act
        response.Trigger("showMessage", triggerObject, timing: TriggerStyle.AfterSettle);

        // Assert
        var result = context.Response.Headers[HtmxResponseHeaderNames.TriggerAfterSettle];
        Assert.Contains("{\"showMessage\":{\"level\":\"info\",\"message\":\"Here Is A Message\"}}", result.ToString());
    }

    [Fact]
    public void Trigger_DefaultObjectWithoutDetail_AddsCorrectTriggerHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);
        var triggerObject = new { level = "info", message = "Here Is A Message" };

        // Act
        response.Trigger("showMessage", triggerObject);

        // Assert
        var result = context.Response.Headers[HtmxResponseHeaderNames.Trigger];
        Assert.Contains("{\"showMessage\":{\"level\":\"info\",\"message\":\"Here Is A Message\"}}", result.ToList());
    }

    [Fact]
    public void Trigger_CanUseExistingTriggerWithTrigger_AddsCorrectTriggerHeader()
    {
        const string expected = @"cool,neat";

        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Trigger("cool")
            .Trigger("neat");

        // Assert
        Assert.Equal(expected, context.Response.Headers[HtmxResponseHeaderNames.Trigger]);
    }

    [Fact]
    public void Trigger_CanUseExistingTriggerWithMultipleTriggersWithDetail_AddsCorrectTriggerHeader()
    {
        const string expected = @"{""wow"":"""",""cool"":{""magic"":""something""},""neat"":{""moremagic"":false}}";

        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Trigger("wow");
        response.Trigger("cool", new { magic = "something" })
            .Trigger("neat", new { moremagic = false });

        // Assert
        Assert.Equal(expected, context.Response.Headers[HtmxResponseHeaderNames.Trigger]);
    }

    [Fact]
    public void Trigger_CanUseExistingTriggerWithDetailWithMultipleTriggersWithDetail_AddsCorrectTriggerHeader()
    {
        const string expected = @"{""wow"":{""display"":true},""cool"":{""magic"":""something""},""neat"":{""moremagic"":false}}";

        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        // Act
        response.Trigger("wow", new { display = true });
        response.Trigger("cool", new { magic = "something" })
            .Trigger("neat", new { moremagic = false });

        // Assert
        Assert.Equal(expected, context.Response.Headers[HtmxResponseHeaderNames.Trigger]);
    }

    [Fact]
    public void Trigger_CanUseExistingSimpleExternalTriggerWithMultipleTriggersWithDetail()
    {
        const string expected = @"{""event1"":"""",""event2"":"""",""event3"":"""",""wow"":{""display"":true},""cool"":{""magic"":""something""},""neat"":{""moremagic"":false}}";

        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        context.Response.Headers[HtmxRequestHeaderNames.Trigger] = new StringValues("event1,event2,event3".Split(","));

        // Act
        response.Trigger("wow", new { display = true });
        response.Trigger("cool", new { magic = "something" })
            .Trigger("neat", new { moremagic = false });

        // Assert
        Assert.Equal(expected, context.Response.Headers[HtmxResponseHeaderNames.Trigger]);
    }

    [Fact]
    public void Trigger_CanUseExistingMixedSimpleAndComplexExternalTriggerWithMultipleTriggersWithDetail()
    {
        const string expected = @"{""event1"":"""",""event2"":"""",""event3"":"""",""event4"":"""",""showMessage"":{""level"":""info"",""message"":""Here Is A Message""},""wow"":{""display"":true},""cool"":{""magic"":""something""},""neat"":{""moremagic"":false}}";

        // Arrange
        var context = new DefaultHttpContext();
        var response = new HtmxResponse(context);

        string[] mixed = ["event1",
            "event2",
            "event3,event4",
            "{\"showMessage\":{\"level\":\"info\",\"message\":\"Here Is A Message\"}}"
        ];

        context.Response.Headers[HtmxRequestHeaderNames.Trigger] = new StringValues(mixed);

        // Act
        response.Trigger("wow", new { display = true });
        response.Trigger("cool", new { magic = "something" })
            .Trigger("neat", new { moremagic = false });

        // Assert
        Assert.Equal(expected, context.Response.Headers[HtmxResponseHeaderNames.Trigger]);
    }
}
