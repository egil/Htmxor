using Bunit;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Components;

public sealed class HtmxFragmentTests : BunitContext
{
	[Fact]
	public void Standard_request_is_wrapperless_by_default()
	{
		AddContext();

		var component = Render<HtmxFragment>(parameters => parameters
			.AddChildContent("<span data-fragment>content</span>"));

		component.MarkupMatches("<span data-fragment>content</span>");
	}

	[Fact]
	public void Supplied_element_id_and_attributes_wrap_child_content()
	{
		AddContext();
		var attributes = new Dictionary<string, object>
		{
			["hx-target"] = "#destination",
			["hx-swap"] = "outerHTML",
		};

		var component = Render<HtmxFragment>(parameters => parameters
			.Add(fragment => fragment.Element, " hx-partial ")
			.Add(fragment => fragment.Id, " envelope ")
			.Add(fragment => fragment.AdditionalAttributes, attributes)
			.AddChildContent("<span data-fragment>content</span>"));

		component.MarkupMatches(
			"""
			<hx-partial id="envelope" hx-target="#destination" hx-swap="outerHTML">
			  <span data-fragment>content</span>
			</hx-partial>
			""");
	}

	[Fact]
	public void Direct_request_does_not_let_target_choose_fragment_output()
	{
		AddContext("aside#unrelated");

		var component = Render<HtmxFragment>(parameters => parameters
			.Add(fragment => fragment.Element, "form")
			.Add(fragment => fragment.Id, "sidebar")
			.AddChildContent("<span data-fragment>content</span>"));

		component.MarkupMatches("<form id=\"sidebar\"><span data-fragment>content</span></form>");
	}

	private void AddContext(string? target = null)
	{
		var context = new DefaultHttpContext();
		if (target is not null)
		{
			context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
			context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
			context.Request.Headers[HtmxRequestHeaderNames.Target] = target;
		}

		Services.AddSingleton(context.GetHtmxContext());
	}
}
