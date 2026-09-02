using Htmxor.Http;

namespace Htmxor.Builder;

internal static class HtmxCurrentUrlMatcher
{
	public static bool Matches(string declaration, Uri? currentUrl)
	{
		if (currentUrl is null ||
			!HtmxRequestHeaderParser.IsWellFormedUtf16(declaration) ||
			!Uri.TryCreate(declaration, UriKind.RelativeOrAbsolute, out var declaredUrl) ||
			!declaredUrl.IsWellFormedOriginalString())
		{
			return false;
		}

		if (declaredUrl.IsAbsoluteUri)
		{
			return IsHttpUrl(declaredUrl) && AreEquivalent(declaredUrl, currentUrl);
		}

		try
		{
			var resolvedUrl = new Uri(currentUrl, declaredUrl);
			return AreEquivalent(resolvedUrl, currentUrl);
		}
		catch (UriFormatException)
		{
			return false;
		}
	}

	private static bool AreEquivalent(Uri expected, Uri actual)
		=> IsHttpUrl(expected) &&
			IsHttpUrl(actual) &&
			string.Equals(expected.Scheme, actual.Scheme, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(expected.IdnHost, actual.IdnHost, StringComparison.OrdinalIgnoreCase) &&
			expected.Port == actual.Port &&
			string.Equals(expected.UserInfo, actual.UserInfo, StringComparison.Ordinal) &&
			string.Equals(expected.AbsolutePath, actual.AbsolutePath, StringComparison.Ordinal) &&
			string.Equals(expected.Query, actual.Query, StringComparison.Ordinal);

	private static bool IsHttpUrl(Uri uri)
		=> (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) &&
			!string.IsNullOrEmpty(uri.Host);
}
