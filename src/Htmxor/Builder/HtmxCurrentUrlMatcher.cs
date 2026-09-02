using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;

namespace Htmxor.Builder;

internal static class HtmxCurrentUrlMatcher
{
	public static bool AreEquivalentDeclarations(string? left, string? right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}

		if (left is null || right is null)
		{
			return false;
		}

		if (TryCreateAbsoluteHttpUrl(left, out var leftUrl) &&
			TryCreateAbsoluteHttpUrl(right, out var rightUrl))
		{
			return AreEquivalent(leftUrl, rightUrl);
		}

		return string.Equals(RemoveFragment(left), RemoveFragment(right), StringComparison.Ordinal);
	}

	public static int GetDeclarationHashCode(string? declaration)
	{
		if (declaration is null)
		{
			return 0;
		}

		if (!TryCreateAbsoluteHttpUrl(declaration, out var url))
		{
			return StringComparer.Ordinal.GetHashCode(RemoveFragment(declaration));
		}

		var hash = new HashCode();
		hash.Add(url.Scheme, StringComparer.OrdinalIgnoreCase);
		hash.Add(url.IdnHost, StringComparer.OrdinalIgnoreCase);
		hash.Add(url.Port);
		hash.Add(url.UserInfo, StringComparer.Ordinal);
		hash.Add(url.AbsolutePath, StringComparer.Ordinal);
		hash.Add(url.Query, StringComparer.Ordinal);
		return hash.ToHashCode();
	}

	public static bool Matches(string declaration, Uri? currentUrl)
	{
		if (currentUrl is null ||
			!HtmxRequestHeaderParser.IsWellFormedUtf16(declaration))
		{
			return false;
		}

		var declarationWithoutFragment = RemoveFragment(declaration);
		if (!Uri.TryCreate(declarationWithoutFragment, UriKind.RelativeOrAbsolute, out var declaredUrl) ||
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

	private static bool TryCreateAbsoluteHttpUrl(string value, [NotNullWhen(true)] out Uri? url)
	{
		url = null;

		if (!HtmxRequestHeaderParser.IsWellFormedUtf16(value) ||
			!Uri.TryCreate(value, UriKind.Absolute, out var parsedUrl) ||
			parsedUrl is null ||
			!parsedUrl.IsWellFormedOriginalString() ||
			!IsHttpUrl(parsedUrl))
		{
			return false;
		}

		url = parsedUrl;
		return true;
	}

	private static string RemoveFragment(string value)
	{
		var fragmentStart = value.IndexOf('#', StringComparison.Ordinal);
		return fragmentStart < 0 ? value : value[..fragmentStart];
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
