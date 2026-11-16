namespace Htmxor.AspNetCore10;

/// <summary>
/// Issue #288's single intended difference from stock: a dual-representation page appends
/// <c>HX-Request, HX-Request-Type</c> to <c>Vary</c>. Stock-parity header snapshots read responses through
/// this view, which requires the pair to appear exactly once, in order, at the end of <c>Vary</c>, and then
/// removes it so the remaining headers still compare whole. <see cref="Issue288VaryHeaderTests"/> owns the
/// positive evidence that each response carries it.
/// </summary>
internal static class Issue288ParityHeaders
{
	private static readonly string[] RepresentationVary = ["HX-Request", "HX-Request-Type"];

	public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> Read(HttpResponseMessage response)
	{
		foreach (var header in response.Headers.Concat(response.Content.Headers))
		{
			if (!header.Key.Equals("Vary", StringComparison.OrdinalIgnoreCase))
			{
				yield return header;
				continue;
			}

			var remaining = WithoutRepresentationVary(header.Value);
			if (remaining.Length > 0)
			{
				yield return new(header.Key, remaining);
			}
		}
	}

	private static string[] WithoutRepresentationVary(IEnumerable<string> values)
	{
		var tokens = values
			.SelectMany(value => value.Split(','))
			.Select(token => token.Trim())
			.Where(token => token.Length > 0)
			.ToArray();
		var representationTokens = tokens.Where(IsRepresentationToken).ToArray();
		if (representationTokens.Length == 0)
		{
			return tokens;
		}

		Assert.Equal(RepresentationVary, tokens[^RepresentationVary.Length..]);
		Assert.Equal(RepresentationVary.Length, representationTokens.Length);
		return tokens[..^RepresentationVary.Length];
	}

	private static bool IsRepresentationToken(string token)
		=> RepresentationVary.Contains(token, StringComparer.OrdinalIgnoreCase);
}
