using Htmxor.DependencyInjection;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Rendering;

public class HtmxorRendererNavigationTests
{
	[Fact]
	public async Task Forced_replacement_navigation_emits_one_redirect_and_suppresses_component_output()
	{
		const string requestedLocation = "/orders/%7E42?view=full";
		var context = new DefaultHttpContext();
		context.Request.Scheme = "https";
		context.Request.Host = new HostString("app.example");
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers[HtmxResponseHeaderNames.Location] = "/existing-location";
		context.Response.Headers[HtmxResponseHeaderNames.PushUrl] = "/existing-push";
		context.Response.Headers[HtmxResponseHeaderNames.Redirect] = "/existing-redirect";
		context.Response.Headers[HtmxResponseHeaderNames.Refresh] = "true";
		context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl] = "/existing-replace";
		var options = new NavigationOptions
		{
			ForceLoad = true,
			ReplaceHistoryEntry = true,
		};
		var navigation = new HtmxorNavigationException(
			requestedLocation,
			$"https://app.example{requestedLocation}",
			in options);

		var rendered = await HtmxorRenderer.HandleNavigationException(context, navigation);

		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		Assert.Equal(
			requestedLocation,
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Redirect]));
		Assert.False(context.Response.Headers.ContainsKey(HtmxResponseHeaderNames.Location));
		Assert.False(context.Response.Headers.ContainsKey(HtmxResponseHeaderNames.PushUrl));
		Assert.False(context.Response.Headers.ContainsKey(HtmxResponseHeaderNames.Refresh));
		Assert.False(context.Response.Headers.ContainsKey(HtmxResponseHeaderNames.ReplaceUrl));
		Assert.True(context.GetHtmxContext().Response.EmptyResponseBodyRequested);
		using var writer = new StringWriter();
		await rendered.WriteToAsync(writer);
		Assert.Equal(string.Empty, writer.ToString());
	}
}
