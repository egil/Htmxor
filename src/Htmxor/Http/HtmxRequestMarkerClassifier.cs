using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

internal static class HtmxRequestMarkerClassifier
{
	public static bool IsHtmxRequest(IHeaderDictionary headers)
	{
		ArgumentNullException.ThrowIfNull(headers);

		return HtmxRequestHeaderParser.IsHtmxRequest(headers);
	}
}
