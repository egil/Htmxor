using Bunit;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Components;

public sealed class HtmxAsyncLoadTests : BunitContext
{
	[Fact]
	public void Standard_request_renders_the_native_htmx_load_contract()
	{
		var context = new DefaultHttpContext();
		context.Request.Path = "/reports/slow";
		Services.AddSingleton(context.GetHtmxContext());

		var component = Render<HtmxAsyncLoad>(parameters => parameters
			.Add(component => component.Id, "lazy")
			.Add(component => component.Loading, "<span data-loading>loading</span>")
			.AddChildContent("<span data-lazy-child>loaded</span>"));

		var element = component.Find("div#lazy");
		Assert.Equal("/reports/slow", element.GetAttribute("hx-get"));
		Assert.Equal("load", element.GetAttribute("hx-trigger"));
		Assert.Equal("#lazy", element.GetAttribute("hx-target"));
		Assert.Equal("outerHTML", element.GetAttribute("hx-swap"));
		Assert.Single(component.FindAll("[data-loading]"));
		Assert.Empty(component.FindAll("[data-lazy-child]"));
	}

	[Theory]
	[InlineData("div#lazy", "div#lazy", true)]
	[InlineData("DIV#lazy", "DIV#lazy", true)]
	[InlineData("div#Lazy", "div#Lazy", false)]
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

		var component = Render<HtmxAsyncLoad>(parameters => parameters
			.Add(component => component.Id, "lazy")
			.AddChildContent("<span data-lazy-child>loaded</span>"));

		Assert.Equal(expectedChild ? 1 : 0, component.FindAll("[data-lazy-child]").Count);
	}
}
