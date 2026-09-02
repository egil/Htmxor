using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

internal static class HtmxRequestHeaderParser
{
	public static HtmxRequestHeaderValues Parse(IHeaderDictionary headers)
	{
		ArgumentNullException.ThrowIfNull(headers);

		if (!IsHtmxRequest(headers))
		{
			return default;
		}

		return new(
			IsHtmxRequest: true,
			RequestType: ParseRequestType(headers),
			IsBoosted: ParseBoolean(headers, HtmxRequestHeaderNames.Boosted),
			IsHistoryRestoreRequest: ParseBoolean(headers, HtmxRequestHeaderNames.HistoryRestoreRequest),
			CurrentUrl: ParseCurrentUrl(headers),
			Target: ParseOpenValue(headers, HtmxRequestHeaderNames.Target),
			Source: ParseOpenValue(headers, HtmxRequestHeaderNames.Source),
			EventHandlerId: ParseOpenValue(headers, HtmxRequestHeaderNames.EventHandlerId));
	}

	public static bool IsHtmxRequest(IHeaderDictionary headers)
	{
		ArgumentNullException.ThrowIfNull(headers);
		return TryGetNormalizedValue(headers, HtmxRequestHeaderNames.HtmxRequest, out var value) &&
			string.Equals(value, "true", StringComparison.Ordinal);
	}

	private static HtmxRequestType? ParseRequestType(IHeaderDictionary headers)
	{
		if (!TryGetNormalizedValue(headers, HtmxRequestHeaderNames.RequestType, out var value))
		{
			return null;
		}

		return value switch
		{
			"full" => HtmxRequestType.Full,
			"partial" => HtmxRequestType.Partial,
			_ => null,
		};
	}

	private static bool ParseBoolean(IHeaderDictionary headers, string headerName)
		=> TryGetNormalizedValue(headers, headerName, out var value) &&
			string.Equals(value, "true", StringComparison.Ordinal);

	private static Uri? ParseCurrentUrl(IHeaderDictionary headers)
	{
		if (!TryGetNormalizedValue(headers, HtmxRequestHeaderNames.CurrentUrl, out var value) ||
			!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
			!uri.IsWellFormedOriginalString() ||
			!(string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
			string.IsNullOrEmpty(uri.Host))
		{
			return null;
		}

		return uri;
	}

	private static string? ParseOpenValue(IHeaderDictionary headers, string headerName)
		=> TryGetNormalizedValue(headers, headerName, out var value) ? value : null;

	private static bool TryGetNormalizedValue(
		IHeaderDictionary headers,
		string headerName,
		out string value)
	{
		value = string.Empty;
		if (!headers.TryGetValue(headerName, out var values) ||
			values.Count != 1 ||
			values[0] is not string rawValue ||
			!IsWellFormedUtf16(rawValue))
		{
			return false;
		}

		var normalized = TrimOptionalWhitespace(rawValue);
		if (normalized.Length == 0 ||
			string.IsNullOrWhiteSpace(normalized) ||
			normalized.Any(char.IsControl))
		{
			return false;
		}

		value = normalized;
		return true;
	}

	private static string TrimOptionalWhitespace(string value)
	{
		var start = 0;
		while (start < value.Length && IsOptionalWhitespace(value[start]))
		{
			start++;
		}

		var end = value.Length;
		while (end > start && IsOptionalWhitespace(value[end - 1]))
		{
			end--;
		}

		return value[start..end];
	}

	private static bool IsOptionalWhitespace(char value) => value is ' ' or '\t';

	internal static bool IsWellFormedUtf16(string value)
	{
		for (var index = 0; index < value.Length; index++)
		{
			if (char.IsHighSurrogate(value[index]))
			{
				if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
				{
					return false;
				}

				index++;
			}
			else if (char.IsLowSurrogate(value[index]))
			{
				return false;
			}
		}

		return true;
	}

	internal static bool IsAsciiHeaderSafe(string value)
		=> value.All(static character => !char.IsControl(character) && character <= '\u007e');
}

internal readonly record struct HtmxRequestHeaderValues(
	bool IsHtmxRequest,
	HtmxRequestType? RequestType,
	bool IsBoosted,
	bool IsHistoryRestoreRequest,
	Uri? CurrentUrl,
	string? Target,
	string? Source,
	string? EventHandlerId);
