using Bunit;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Components;

public sealed class HtmxAsyncLoadTests : TestContext
{
	[Theory]
	[InlineData("div#lazy", "div#lazy", true)]
	[InlineData("button#other", "div#lazy", false)]
	[InlineData("div#lazy", "section#other", false)]
	public void Partial_request_loads_child_only_for_its_complete_source_and_target_identities(
		string source,
		string target,
		bool expectedChild)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Source] = source;
		context.Request.Headers[HtmxRequestHeaderNames.Target] = target;
		Services.AddSingleton(context.GetHtmxContext());

		var component = RenderComponent<HtmxAsyncLoad>(parameters => parameters
			.Add(component => component.Id, "lazy")
			.AddChildContent("<span data-lazy-child>loaded</span>"));

		Assert.Equal(expectedChild ? 1 : 0, component.FindAll("[data-lazy-child]").Count);
	}
}
