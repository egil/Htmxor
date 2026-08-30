namespace Htmxor.Http;

internal static class HtmxElementIdentity
{
	public static bool Equals(string? expected, string? actual)
	{
		if (expected is null || actual is null)
		{
			return false;
		}

		var expectedSeparator = expected.IndexOf('#', StringComparison.Ordinal);
		var actualSeparator = actual.IndexOf('#', StringComparison.Ordinal);
		if (expectedSeparator < 0 || actualSeparator < 0)
		{
			return expectedSeparator == actualSeparator &&
				string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
		}

		return expected.AsSpan(0, expectedSeparator).Equals(
				actual.AsSpan(0, actualSeparator),
				StringComparison.OrdinalIgnoreCase) &&
			expected.AsSpan(expectedSeparator + 1).SequenceEqual(actual.AsSpan(actualSeparator + 1));
	}

	public static bool Matches(string tag, string id, string? actual)
	{
		if (actual is null || actual.Length != tag.Length + id.Length + 1)
		{
			return false;
		}

		return actual.AsSpan(0, tag.Length).Equals(tag.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
			actual[tag.Length] == '#' &&
			actual.AsSpan(tag.Length + 1).SequenceEqual(id.AsSpan());
	}
}
