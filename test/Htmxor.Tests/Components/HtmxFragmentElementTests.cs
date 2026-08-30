using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Components;

public sealed class HtmxFragmentElementTests
{
	[Fact]
	public void Default_match_requires_complete_target_element_identity_on_partial_request()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		context.Request.Headers[HtmxRequestHeaderNames.RequestType] = "partial";
		context.Request.Headers[HtmxRequestHeaderNames.Target] = "div#results";
		var fragment = new HtmxFragmentElement
		{
			Id = "results",
			ChildContent = _ => { },
			RenderDuringStandardRequest = false,
		};

		var shouldOutput = fragment.ShouldOutput(
			context.GetHtmxContext(),
			directConditionalChildren: 0,
			conditionalChildren: 0);

		Assert.True(shouldOutput);
	}
}
