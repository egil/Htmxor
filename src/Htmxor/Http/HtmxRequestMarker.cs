using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

internal static class HtmxRequestMarker
{
	public static bool IsValid(IHeaderDictionary headers)
	{
		ArgumentNullException.ThrowIfNull(headers);

		return headers.TryGetValue(HtmxRequestHeaderNames.HtmxRequest, out var values)
			&& values.Count == 1
			&& string.Equals(values[0]?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
	}
}
