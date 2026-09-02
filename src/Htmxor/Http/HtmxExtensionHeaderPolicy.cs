using System.Text;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Http;

internal static class HtmxExtensionHeaderPolicy
{
	internal const int MaximumEncodedBytes = 4096;

	private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
	{
		HtmxRequestHeaderNames.Boosted,
		HtmxRequestHeaderNames.CurrentUrl,
		HtmxRequestHeaderNames.HistoryRestoreRequest,
		HtmxRequestHeaderNames.HtmxRequest,
		HtmxRequestHeaderNames.RequestType,
		HtmxRequestHeaderNames.Source,
		HtmxRequestHeaderNames.Target,
		HtmxRequestHeaderNames.EventHandlerId,
		HtmxResponseHeaderNames.Location,
		HtmxResponseHeaderNames.PushUrl,
		HtmxResponseHeaderNames.Redirect,
		HtmxResponseHeaderNames.Refresh,
		HtmxResponseHeaderNames.ReplaceUrl,
		HtmxResponseHeaderNames.Reswap,
		HtmxResponseHeaderNames.Retarget,
		HtmxResponseHeaderNames.Reselect,
		HtmxResponseHeaderNames.Trigger,
	};

	public static bool TryGetRequestValue(StringValues values, out string value)
	{
		value = string.Empty;
		if (values.Count != 1 || values[0] is not string candidate || !IsValidValue(candidate))
		{
			return false;
		}

		value = candidate;
		return true;
	}

	public static bool IsAllowedName(string? name)
		=> name is not null &&
			name.StartsWith("HX-", StringComparison.OrdinalIgnoreCase) &&
			!name.StartsWith("HXOR-", StringComparison.OrdinalIgnoreCase) &&
			!ProtectedNames.Contains(name) &&
			IsFieldName(name) &&
			GetEncodedByteCount(name) <= MaximumEncodedBytes;

	public static void ValidateResponseInput(string name, string value)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(value);
		if (!IsAllowedName(name))
		{
			throw new ArgumentException(
				"The extension header name must be an unprotected HX-* HTTP field name within the size limit.",
				nameof(name));
		}

		if (!IsValidValue(value))
		{
			throw new ArgumentException(
				"The extension header value must be header-safe, well-formed UTF-16, and within the size limit.",
				nameof(value));
		}
	}

	private static bool IsFieldName(string value)
	{
		foreach (var character in value)
		{
			if (!(character is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' ||
				"!#$%&'*+-.^_`|~".Contains(character, StringComparison.Ordinal)))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsValidValue(string value)
		=> HtmxRequestHeaderParser.IsWellFormedUtf16(value) &&
			HtmxRequestHeaderParser.IsAsciiHeaderSafe(value) &&
			GetEncodedByteCount(value) <= MaximumEncodedBytes;

	private static int GetEncodedByteCount(string value) => Encoding.UTF8.GetByteCount(value);
}
