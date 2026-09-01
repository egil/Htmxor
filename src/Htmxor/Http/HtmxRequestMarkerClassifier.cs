using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

internal static class HtmxRequestMarkerClassifier
{
	public static bool IsHtmxRequest(IHeaderDictionary headers)
	{
		ArgumentNullException.ThrowIfNull(headers);

		return headers.TryGetValue(HtmxRequestHeaderNames.HtmxRequest, out var values)
			&& values.Count == 1
			&& values[0] is string value
			&& value.AsSpan().Trim(" \t").SequenceEqual("true");
	}
}
