using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

public sealed class HtmxContext
{
	public HtmxRequest Request { get; }

	public HtmxResponse Response { get; }

	internal bool UsesCompletedFragmentSelection { get; set; }

	public HtmxContext(HttpContext context)
	{
		Request = new HtmxRequest(context);
		Response = new HtmxResponse(context);
	}
}
