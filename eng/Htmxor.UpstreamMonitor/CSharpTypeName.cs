using System.Text;

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

	public static string MetadataIdentity(string text)
	{
		text = Compact(text).Replace("global::", string.Empty, StringComparison.Ordinal);
		var identity = new StringBuilder();
		for (var index = 0; index < text.Length && text[index] != '('; index++)
		{
			if (text[index] == '<')
			{
				var closing = GenericEnd(text, index);
				identity.Append('`').Append(SplitList(text[(index + 1)..closing]).Count());
				index = closing;
			}
			else
			{
				identity.Append(text[index]);
			}
		}
		return identity.ToString();
	}

	public static string Compact(string text) => string.Concat(text.Where(character => !char.IsWhiteSpace(character)));

	private static int GenericEnd(string text, int opening)
	{
		var depth = 1;
		for (var index = opening + 1; index < text.Length; index++)
		{
			depth += text[index] switch { '<' => 1, '>' => -1, _ => 0 };
			if (depth == 0)
			{
				return index;
			}
		}
		throw new MonitorFailure("Source type has unmatched generic arguments.");
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
