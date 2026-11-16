using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Http;

/// <summary>
/// Declares that a response from an endpoint serving both a full and a direct representation
/// varies by the request headers that select between them.
/// </summary>
internal static class HtmxRepresentationVary
{
	internal static readonly ImmutableArray<string> RepresentationHeaders =
	[
		HtmxRequestHeaderNames.HtmxRequest,
		HtmxRequestHeaderNames.RequestType,
	];

	/// <summary>
	/// Appends the representation headers to <c>Vary</c> now and again when the response starts.
	/// Output caching captures headers before start callbacks run, while the exception handler clears
	/// headers before it re-executes; appending at both points covers each.
	/// </summary>
	public static void Append(HttpResponse response)
	{
		Append(response.Headers);
		response.OnStarting(static state =>
		{
			Append(((HttpResponse)state).Headers);
			return Task.CompletedTask;
		}, response);
	}

	private static void Append(IHeaderDictionary headers)
	{
		var existing = headers.Vary;
		var tokens = existing
			.SelectMany(static value => (value ?? string.Empty).Split(','))
			.Select(static token => token.Trim())
			.Where(static token => token.Length > 0)
			.ToArray();
		if (tokens.Contains("*", StringComparer.Ordinal))
		{
			return;
		}

		var missing = RepresentationHeaders
			.Where(name => !tokens.Contains(name, StringComparer.OrdinalIgnoreCase))
			.ToArray();
		if (missing.Length > 0)
		{
			headers.Vary = new StringValues(string.Join(", ", tokens.Concat(missing)));
		}
	}
}
