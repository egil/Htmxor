namespace Htmxor.UpstreamMonitor;

internal static class CSharpTypeName
{
	public static IEnumerable<string> SplitList(string text)
	{
		var start = 0;
		var nesting = new TypeNesting();
		for (var index = 0; index < text.Length; index++)
		{
			nesting.Advance(text[index]);
			if (text[index] == ',' && nesting.IsTopLevel)
			{
				yield return text[start..index];
				start = index + 1;
			}
		}
		yield return text[start..];
	}

	private sealed class TypeNesting
	{
		private int parentheses;
		private int brackets;
		private int arguments;

		public bool IsTopLevel => parentheses == 0 && brackets == 0 && arguments == 0;

		public void Advance(char character)
		{
			parentheses += character switch { '(' => 1, ')' => -1, _ => 0 };
			brackets += character switch { '[' => 1, ']' => -1, _ => 0 };
			// Parentheses already protect tuple and call commas; relational operators inside calls are not generic delimiters.
			if (parentheses == 0 && brackets == 0)
			{
				arguments += character switch { '<' => 1, '>' => -1, _ => 0 };
			}
		}
	}
}
