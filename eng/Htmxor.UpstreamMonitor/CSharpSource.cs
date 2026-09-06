using System.Text.RegularExpressions;

namespace Htmxor.UpstreamMonitor;

// Source is lexed as inert text. Literal and comment spans retain their offsets so declarations cannot arise from examples or executable bodies.
internal sealed partial class CSharpSource
{
	public CSharpSource(string source)
	{
		var characters = source.ToCharArray();
		var declarations = source.ToCharArray();
		foreach (Match match in Trivia().Matches(source))
		{
			for (var index = match.Index; index < match.Index + match.Length; index++)
			{
				if (characters[index] is not ('\n' or '\r'))
				{
					characters[index] = ' ';
					if (match.Value.StartsWith("/", StringComparison.Ordinal))
					{
						declarations[index] = ' ';
					}
				}
			}
		}
		Text = new string(characters);
		DeclarationText = new string(declarations);
		Types = TypeDeclaration().Matches(Text).Select(ParseType).ToArray();
	}

	public string Text { get; }
	private string DeclarationText { get; }
	public IReadOnlyList<SourceType> Types { get; }

	public static string Normalize(string value) => Whitespace().Replace(value.Trim(), " ");

	public static string NormalizeDeclaration(string value)
	{
		var literals = new List<string>();
		var normalized = Normalize(Trivia().Replace(value, match =>
		{
			literals.Add(match.Value);
			return $"\u0001{literals.Count - 1}\u0002";
		}));
		for (var index = 0; index < literals.Count; index++)
		{
			normalized = normalized.Replace($"\u0001{index}\u0002", literals[index], StringComparison.Ordinal);
		}
		return normalized;
	}

	public static int ClosingBrace(string text, int opening)
	{
		var depth = 1;
		for (var index = opening + 1; index < text.Length; index++)
		{
			depth += text[index] switch { '{' => 1, '}' => -1, _ => 0 };
			if (depth == 0)
			{
				return index;
			}
		}
		throw new MonitorFailure("Source declaration has an unmatched brace.");
	}

	private SourceType ParseType(Match match)
	{
		var opening = match.Index + match.Length - 1;
		var closing = Text[opening] == ';' ? opening : ClosingBrace(Text, opening);
		var bodyStart = Math.Min(opening + 1, closing);
		var name = Normalize(match.Groups["name"].Value);
		var tailGroup = match.Groups["tail"];
		var tail = tailGroup.Value.TrimStart();
		var parameterLength = PrimaryParameterLength(tail);
		var parameters = parameterLength == 0 ? null : NormalizeDeclaration(
			DeclarationText.Substring(tailGroup.Index + tailGroup.Length - tail.Length, parameterLength));
		tail = tail[parameterLength..].Trim();
		var constraints = Constraint().Matches(tail).Select(value => Normalize(value.Value)).ToArray();
		var bases = tail.Split("where ", StringSplitOptions.None)[0].Trim();
		return new(name, Normalize(match.Groups["modifiers"].Value + match.Groups["kind"].Value + " " + name),
			match.Groups["kind"].Value == "interface", bases.StartsWith(':') ? CSharpTypeName.SplitList(bases[1..]).Select(Normalize).ToArray() : [],
			constraints, Text[bodyStart..closing], DeclarationText[bodyStart..closing], parameters);
	}

	private static int PrimaryParameterLength(string tail)
	{
		if (!tail.StartsWith('('))
		{
			return 0;
		}
		var depth = 0;
		for (var index = 0; index < tail.Length; index++)
		{
			depth += tail[index] switch { '(' => 1, ')' => -1, _ => 0 };
			if (depth == 0)
			{
				return index + 1;
			}
		}
		throw new MonitorFailure("Source declaration has an unmatched parameter list.");
	}

	[GeneratedRegex("(?<raw>\"{3,})[\\s\\S]*?\\k<raw>|@\"(?:[^\"]|\"\")*\"|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])+'|//[^\\r\\n]*|/\\*[\\s\\S]*?\\*/")]
	private static partial Regex Trivia();
	[GeneratedRegex(@"\b(?<modifiers>(?:(?:public|protected|internal|private|abstract|sealed|static|partial|readonly|ref)\s+)*)(?<kind>class|interface|struct|record)\s+(?<name>\w+(?:\s*<[^>{}]+>)?)(?<tail>[^;{}]*)[;{]")]
	private static partial Regex TypeDeclaration();
	[GeneratedRegex(@"where\s+\w+\s*:\s*.*?(?=\bwhere\s|$)")]
	private static partial Regex Constraint();
	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}

internal sealed record SourceType(string Name, string Signature, bool IsInterface, IReadOnlyList<string> Bases,
	IReadOnlyList<string> Constraints, string Body, string DeclarationBody, string? PrimaryConstructorParameters);
