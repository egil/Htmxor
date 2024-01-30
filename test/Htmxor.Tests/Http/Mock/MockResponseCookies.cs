using Microsoft.AspNetCore.Http;

namespace Htmxor.Http.Mock;

/// <summary>
///     Mock implementation of the <see cref="IResponseCookies" /> interface for testing purposes.
/// </summary>
public class MockResponseCookies : IResponseCookies
{
	private readonly Dictionary<string, CookieOptions> cookies = new();

	/// <inheritdoc />
	public void Append(string key, string value)
	{
		Append(key, value, new CookieOptions());
	}

	/// <inheritdoc />
	public void Append(string key, string value, CookieOptions options)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		// Create or update the cookie
		cookies[key] = options;
	}

	/// <inheritdoc />
	public void Append(ReadOnlySpan<KeyValuePair<string, string>> keyValuePairs, CookieOptions options)
	{
		foreach (var keyValuePair in keyValuePairs) Append(keyValuePair.Key, keyValuePair.Value, options);
	}

	/// <inheritdoc />
	public void Delete(string key)
	{
		Delete(key, new CookieOptions());
	}

	/// <inheritdoc />
	public void Delete(string key, CookieOptions options)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		// Expire the cookie by setting MaxAge to a past value
		options.Expires = DateTime.Now.AddYears(-1);

		// Create or update the cookie
		cookies[key] = options;
	}
}