using System;

namespace Htmxor;

internal static class HtmxRouteRepresentationContract
{
	public static bool IsValidCurrentUrl(string? declaration)
	{
		if (declaration is null || string.IsNullOrWhiteSpace(declaration))
		{
			return true;
		}

		if (!IsWellFormedUtf16(declaration))
		{
			return false;
		}

		var declarationWithoutFragment = RemoveFragment(declaration);
		if (!Uri.TryCreate(declarationWithoutFragment, UriKind.RelativeOrAbsolute, out var declaredUrl) ||
			declaredUrl is null ||
			!declaredUrl.IsWellFormedOriginalString())
		{
			return false;
		}

		return !declaredUrl.IsAbsoluteUri || IsHttpUrl(declaredUrl);
	}

	public static bool IsValidOptionalTarget(string? declaration)
		=> string.IsNullOrWhiteSpace(declaration) || IsValidTarget(declaration);

	public static bool IsValidTarget(string? declaration)
	{
		if (declaration is null)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(declaration))
		{
			return false;
		}

		if (!IsWellFormedUtf16(declaration))
		{
			return false;
		}

		var separator = FindHash(declaration);
		if (!IsValidSeparator(declaration, separator))
		{
			return false;
		}

		var tagLength = separator < 0 ? declaration.Length : separator;
		return IsValidIdentityPart(declaration, 0, tagLength) &&
			(separator < 0 || IsValidIdentityPart(
				declaration,
				separator + 1,
				declaration.Length - separator - 1));
	}

	private static bool IsValidSeparator(string value, int separator)
		=> separator != 0 &&
			separator != value.Length - 1 &&
			(separator < 0 || FindHash(value, separator + 1) < 0);

	private static bool IsValidIdentityPart(string value, int start, int length)
	{
		if (length == 0)
		{
			return false;
		}

		for (var index = start; index < start + length; index++)
		{
			if (char.IsControl(value[index]) || char.IsWhiteSpace(value[index]))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsWellFormedUtf16(string value)
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

	private static string RemoveFragment(string value)
	{
		var fragmentStart = FindHash(value);
		return fragmentStart < 0 ? value : value.Substring(0, fragmentStart);
	}

	private static int FindHash(string value, int start = 0)
	{
		for (var index = start; index < value.Length; index++)
		{
			if (value[index] == '#')
			{
				return index;
			}
		}

		return -1;
	}

	private static bool IsHttpUrl(Uri uri)
		=> (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) &&
			!string.IsNullOrEmpty(uri.Host);
}
