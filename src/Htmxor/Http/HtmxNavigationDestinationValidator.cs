using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

internal static class HtmxNavigationDestinationValidator
{
	public static void ValidateLocal(string destination, HttpRequest request, string argumentName)
		=> Validate(destination, request, argumentName, allowCrossOrigin: false);

	public static void ValidateLocalHistory(string destination, HttpRequest request, string argumentName)
	{
		ValidateSyntax(destination, argumentName);
		if (IsReservedHistoryLiteral(destination))
		{
			throw new ArgumentException(
				"The reserved history literals 'true' and 'false' are not URL destinations.",
				argumentName);
		}

		ValidatePolicy(destination, request, argumentName, allowCrossOrigin: false);
	}

	public static void ValidateRedirect(string destination, HttpRequest request, string argumentName)
		=> Validate(destination, request, argumentName, allowCrossOrigin: true);

	private static void Validate(
		string destination,
		HttpRequest request,
		string argumentName,
		bool allowCrossOrigin)
	{
		ValidateSyntax(destination, argumentName);
		ValidatePolicy(destination, request, argumentName, allowCrossOrigin);
	}

	private static void ValidateSyntax(string destination, string argumentName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(destination, argumentName);
		if (char.IsWhiteSpace(destination[0]) || char.IsWhiteSpace(destination[^1]))
		{
			throw new ArgumentException("The destination cannot have surrounding whitespace.", argumentName);
		}

		if (!HtmxRequestHeaderParser.IsAsciiHeaderSafe(destination))
		{
			throw new ArgumentException(
				"The destination must contain only ASCII HTTP header characters.",
				argumentName);
		}

		var uriKind = destination.StartsWith("//", StringComparison.Ordinal)
			? UriKind.Relative
			: UriKind.RelativeOrAbsolute;
		if (!Uri.TryCreate(destination, uriKind, out var parsed) || !parsed.IsWellFormedOriginalString())
		{
			throw new ArgumentException("The destination must be a well-formed URI reference.", argumentName);
		}
	}

	private static void ValidatePolicy(
		string destination,
		HttpRequest request,
		string argumentName,
		bool allowCrossOrigin)
	{
		var requestOrigin = GetRequestOrigin(request);
		if (!Uri.TryCreate(requestOrigin, destination, out var resolved) || !IsHttpScheme(resolved))
		{
			throw new ArgumentException("The destination must use the HTTP or HTTPS scheme.", argumentName);
		}

		if (!allowCrossOrigin && !HasSameOrigin(requestOrigin, resolved))
		{
			throw new ArgumentException("The destination must resolve to the active request origin.", argumentName);
		}
	}

	private static Uri GetRequestOrigin(HttpRequest request)
	{
		var originText = $"{request.Scheme}://{request.Host.ToUriComponent()}/";
		if (!Uri.TryCreate(originText, UriKind.Absolute, out var origin) || !IsHttpScheme(origin))
		{
			throw new InvalidOperationException("The active request does not have a valid HTTP(S) origin.");
		}

		return origin;
	}

	private static bool HasSameOrigin(Uri requestOrigin, Uri destination)
		=> string.Equals(requestOrigin.Scheme, destination.Scheme, StringComparison.OrdinalIgnoreCase)
		&& string.Equals(requestOrigin.IdnHost, destination.IdnHost, StringComparison.OrdinalIgnoreCase)
		&& requestOrigin.Port == destination.Port;

	private static bool IsHttpScheme(Uri destination)
		=> destination.Scheme is "http" or "https";

	private static bool IsReservedHistoryLiteral(string destination)
		=> destination.Equals("true", StringComparison.Ordinal)
		|| destination.Equals("false", StringComparison.Ordinal);
}
