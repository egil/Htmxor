using System;
using System.Collections.Generic;

namespace Htmxor.Generators;

// A small Razor lexer: it follows markup, Razor transitions and enough C# lexing (strings, comments and
// brackets) to find every attribute the Razor compiler would bind on the component's own elements, and it
// reads the route-owner directives from the whole file. Markup inside @code/@functions blocks and Razor
// templates (@<tag>) is lexed so it cannot desynchronize the scan, but its attributes are not reported.
internal sealed class RazorMarkupScanner
{
	private const string PlaintextElement = "plaintext";
	private static readonly string[] RawTextElements = { "script", "style", PlaintextElement };
	private static readonly string[] VoidElements =
		{ "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };
	private static readonly string[] StatementKeywords = { "if", "for", "foreach", "while", "switch", "lock", "do", "try" };
	private static readonly string[] ContinuationKeywords = { "else", "catch", "finally", "while" };
	private static readonly string[] MemberBlockKeywords = { "code", "functions" };
	private static readonly string[] Directives =
	{
		"page", "attribute", "using", "inject", "inherits", "layout", "implements",
		"typeparam", "namespace", "rendermode", "preservewhitespace", "model",
	};

	private readonly string source;
	private readonly ISet<string> attributeNames;
	private readonly List<MarkupAttribute> attributes = new();
	private readonly List<string> attributeDirectives = new();
	private int pageDirectiveCount;
	private int index;

	private RazorMarkupScanner(string source, ISet<string> attributeNames)
	{
		this.source = source;
		this.attributeNames = attributeNames;
	}

	public static RazorMarkupScan Scan(string source, ISet<string> attributeNames)
	{
		var scanner = new RazorMarkupScanner(source, attributeNames);
		scanner.ScanMarkup(record: true, island: false);
		return new RazorMarkupScan(scanner.attributes, scanner.pageDirectiveCount, scanner.attributeDirectives);
	}

	// Scans to the end of the source, or for a markup island inside C# until its outermost element closes.
	private void ScanMarkup(bool record, bool island)
	{
		var depth = 0;
		while (index < source.Length)
		{
			if (source[index] == '<')
			{
				depth += ScanTag(record);
				if (island && depth <= 0)
				{
					return;
				}
			}
			else if (source[index] == '@')
			{
				ScanMarkupTransition(record, island);
			}
			else
			{
				index++;
			}
		}
	}

	private int ScanTag(bool record)
	{
		if (StartsWith("<!--"))
		{
			SkipPast("-->");
			return 0;
		}

		if (Peek(1) == '/')
		{
			SkipPast(">");
			return -1;
		}

		if (!IsNameStart(Peek(1)))
		{
			index++;
			return 0;
		}

		index++;
		var name = ReadName();
		var selfClosing = ScanAttributes(record);
		if (Array.Exists(RawTextElements, element => string.Equals(element, name, StringComparison.OrdinalIgnoreCase)))
		{
			SkipRawText(name);
			return 0;
		}

		return selfClosing || Contains(VoidElements, name) ? 0 : 1;
	}

	private bool ScanAttributes(bool record)
	{
		while (index < source.Length)
		{
			SkipWhitespace();
			if (Peek(0) == '>')
			{
				index++;
				return false;
			}

			if (StartsWith("/>"))
			{
				index += 2;
				return true;
			}

			ScanAttribute(record);
		}

		return false;
	}

	private void ScanAttribute(bool record)
	{
		var nameStart = index;
		while (index < source.Length && !IsAttributeNameEnd(source[index]))
		{
			index++;
		}

		if (index == nameStart)
		{
			index++;
			return;
		}

		var name = source.Substring(nameStart, index - nameStart);
		if (record && attributeNames.Contains(name))
		{
			attributes.Add(new MarkupAttribute(nameStart, name));
		}

		SkipWhitespace();
		if (Peek(0) == '=')
		{
			index++;
			SkipWhitespace();
			SkipAttributeValue();
		}
	}

	private void SkipAttributeValue()
	{
		var quote = Peek(0);
		if (quote is '"' or '\'')
		{
			index++;
			SkipQuotedAttributeValue(quote);
			return;
		}

		if (quote == '@')
		{
			SkipExpressionTransition();
			return;
		}

		while (index < source.Length && !char.IsWhiteSpace(source[index]) && source[index] != '>' && !StartsWith("/>"))
		{
			index++;
		}
	}

	private void SkipQuotedAttributeValue(char quote)
	{
		while (index < source.Length && source[index] != quote)
		{
			if (source[index] == '@' && !FollowsLetterOrDigit())
			{
				SkipExpressionTransition();
			}
			else
			{
				index++;
			}
		}

		index++;
	}

	// An implicit or explicit Razor expression used as markup or attribute content.
	private void SkipExpressionTransition()
	{
		index++;
		if (Peek(0) == '@')
		{
			index++;
		}
		else if (Peek(0) == '(')
		{
			SkipBalanced('(', ')');
		}
		else if (IsIdentifierStart(Peek(0)))
		{
			SkipImplicitExpression();
		}
	}

	private void SkipImplicitExpression()
	{
		while (IsIdentifierStart(Peek(0)))
		{
			ReadIdentifier();
			while (Peek(0) is '(' or '[')
			{
				SkipBalanced(Peek(0), Peek(0) == '(' ? ')' : ']');
			}

			if (Peek(0) != '.' || !IsIdentifierStart(Peek(1)))
			{
				return;
			}

			index++;
		}
	}

	private void ScanMarkupTransition(bool record, bool island)
	{
		if (FollowsLetterOrDigit() || Peek(1) == '@' || Peek(1) == '(')
		{
			SkipExpressionTransition();
			return;
		}

		if (Peek(1) == '*')
		{
			SkipPast("*@");
			return;
		}

		if (Peek(1) == '{')
		{
			index++;
			ScanCodeBlock(record);
			return;
		}

		ScanKeywordTransition(record, island);
	}

	private void ScanKeywordTransition(bool record, bool island)
	{
		var transitionStart = index;
		index++;
		var keyword = ReadIdentifier();
		if (Contains(MemberBlockKeywords, keyword) && SkipWhitespaceTo('{'))
		{
			ScanCodeBlock(record: false);
		}
		else if (IsStatement(keyword))
		{
			ScanStatement(record);
		}
		else if (!island && Contains(Directives, keyword) && StartsLine(transitionStart))
		{
			ScanDirective(keyword);
		}
		else
		{
			index = transitionStart;
			SkipExpressionTransition();
		}
	}

	private bool IsStatement(string keyword)
	{
		if (Contains(StatementKeywords, keyword))
		{
			return true;
		}

		var afterKeyword = index;
		SkipWhitespace();
		var isUsingStatement = keyword == "using" && Peek(0) == '(';
		index = afterKeyword;
		return isUsingStatement;
	}

	// A control-flow statement written in markup: its header is C#, its body is a code block, and an
	// else/catch/finally/while continuation belongs to the same statement.
	private void ScanStatement(bool record)
	{
		while (true)
		{
			SkipWhitespace();
			if (Peek(0) == '(')
			{
				SkipBalanced('(', ')');
				SkipWhitespace();
			}

			if (Peek(0) == '{')
			{
				ScanCodeBlock(record);
			}

			if (!TryReadContinuation())
			{
				return;
			}
		}
	}

	private bool TryReadContinuation()
	{
		var afterStatement = index;
		SkipWhitespace();
		var keyword = ReadIdentifier();
		if (!Contains(ContinuationKeywords, keyword))
		{
			index = afterStatement;
			return false;
		}

		var afterKeyword = index;
		SkipWhitespace();
		if (keyword != "else" || ReadIdentifier() != "if")
		{
			index = afterKeyword;
		}

		return true;
	}

	private void ScanDirective(string keyword)
	{
		var bodyStart = index;
		SkipDirectiveBody();
		var body = source.Substring(bodyStart, index - bodyStart).Trim();
		if (keyword == "page")
		{
			pageDirectiveCount++;
		}
		else if (keyword == "attribute")
		{
			attributeDirectives.Add(body);
		}
	}

	private void SkipDirectiveBody()
	{
		var depth = 0;
		while (index < source.Length && (depth > 0 || source[index] != '\n'))
		{
			if (TrySkipCSharpLiteralOrComment())
			{
				continue;
			}

			depth += source[index] switch
			{
				'(' or '[' or '{' => 1,
				')' or ']' or '}' => -1,
				_ => 0,
			};
			index++;
		}
	}

	// C# statements between braces. Markup that starts a statement switches to markup until its
	// outermost element closes; a Razor template (@<tag>) is lexed the same way but never reported.
	private void ScanCodeBlock(bool record)
	{
		var depth = 0;
		var atStatementStart = true;
		while (index < source.Length)
		{
			if (TrySkipCSharpLiteralOrComment())
			{
				atStatementStart = false;
				continue;
			}

			if (TryScanCodeMarkup(record, atStatementStart))
			{
				atStatementStart = true;
				continue;
			}

			depth += source[index] switch
			{
				'{' => 1,
				'}' => -1,
				_ => 0,
			};
			atStatementStart = UpdateStatementStart(atStatementStart, source[index]);
			index++;
			if (depth == 0)
			{
				return;
			}
		}
	}

	private bool TryScanCodeMarkup(bool record, bool atStatementStart)
	{
		if (source[index] == '<' && atStatementStart && IsNameStart(Peek(1)))
		{
			ScanMarkup(record, island: true);
			return true;
		}

		if (source[index] == '@' && Peek(1) == '<')
		{
			index++;
			ScanMarkup(record: false, island: true);
			return true;
		}

		if (StartsWith("@:"))
		{
			SkipPast("\n");
			return true;
		}

		return false;
	}

	private static bool UpdateStatementStart(bool atStatementStart, char value)
		=> char.IsWhiteSpace(value)
			? atStatementStart
			: value is '{' or '}' or ';' or ':' or ')';

	private void SkipBalanced(char open, char close)
	{
		var depth = 0;
		while (index < source.Length)
		{
			if (TrySkipCSharpLiteralOrComment())
			{
				continue;
			}

			var value = source[index];
			index++;
			depth += value == open ? 1 : value == close ? -1 : 0;
			if (depth == 0)
			{
				return;
			}
		}
	}

	private bool TrySkipCSharpLiteralOrComment()
	{
		if (StartsWith("//"))
		{
			SkipTo('\n');
			return true;
		}

		if (StartsWith("/*"))
		{
			SkipPast("*/");
			return true;
		}

		return TrySkipCSharpString() || TrySkipCharLiteral();
	}

	private bool TrySkipCSharpString()
	{
		var prefixEnd = index;
		while (prefixEnd < source.Length && source[prefixEnd] is '$' or '@')
		{
			prefixEnd++;
		}

		if (prefixEnd >= source.Length || source[prefixEnd] != '"')
		{
			return false;
		}

		var prefix = source.Substring(index, prefixEnd - index);
		index = prefixEnd;
		if (StartsWith("\"\"\""))
		{
			SkipRawString();
		}
		else
		{
			index++;
			SkipStringBody(verbatim: prefix.IndexOf('@') >= 0, interpolated: prefix.IndexOf('$') >= 0);
		}

		return true;
	}

	private void SkipRawString()
	{
		var quoteStart = index;
		while (Peek(0) == '"')
		{
			index++;
		}

		var delimiter = source.Substring(quoteStart, index - quoteStart);
		SkipPast(delimiter);
	}

	private void SkipStringBody(bool verbatim, bool interpolated)
	{
		while (index < source.Length)
		{
			if (source[index] == '"')
			{
				if (!verbatim || Peek(1) != '"')
				{
					index++;
					return;
				}

				index += 2;
			}
			else if (interpolated && source[index] == '{')
			{
				SkipInterpolationHole();
			}
			else
			{
				index += source[index] == '\\' && !verbatim ? 2 : 1;
			}
		}
	}

	private void SkipInterpolationHole()
	{
		if (Peek(1) == '{')
		{
			index += 2;
			return;
		}

		SkipBalanced('{', '}');
	}

	private bool TrySkipCharLiteral()
	{
		if (Peek(0) != '\'')
		{
			return false;
		}

		index++;
		while (index < source.Length && source[index] is not '\'' and not '\n')
		{
			index += source[index] == '\\' ? 2 : 1;
		}

		index++;
		return true;
	}

	// HTML ends script and style text only at their closing tag, and plaintext never ends.
	private void SkipRawText(string elementName)
	{
		var closingTag = string.Equals(elementName, PlaintextElement, StringComparison.OrdinalIgnoreCase)
			? -1
			: source.IndexOf("</" + elementName, index, StringComparison.OrdinalIgnoreCase);
		index = closingTag < 0 ? source.Length : closingTag;
		SkipPast(">");
	}

	private string ReadName()
	{
		var start = index;
		while (index < source.Length && (char.IsLetterOrDigit(source[index]) || source[index] is '-' or '_' or ':' or '.'))
		{
			index++;
		}

		return source.Substring(start, index - start);
	}

	private string ReadIdentifier()
	{
		var start = index;
		while (index < source.Length && (char.IsLetterOrDigit(source[index]) || source[index] == '_'))
		{
			index++;
		}

		return source.Substring(start, index - start);
	}

	private bool SkipWhitespaceTo(char value)
	{
		SkipWhitespace();
		return Peek(0) == value;
	}

	private void SkipWhitespace()
	{
		while (index < source.Length && char.IsWhiteSpace(source[index]))
		{
			index++;
		}
	}

	private void SkipTo(char value)
	{
		var found = source.IndexOf(value, index);
		index = found < 0 ? source.Length : found;
	}

	private void SkipPast(string value)
	{
		var found = source.IndexOf(value, Math.Min(index + 1, source.Length), StringComparison.Ordinal);
		index = found < 0 ? source.Length : found + value.Length;
	}

	private bool StartsLine(int position)
	{
		var lineStart = source.LastIndexOf('\n', Math.Max(position - 1, 0));
		for (var current = lineStart + 1; current < position; current++)
		{
			if (!char.IsWhiteSpace(source[current]))
			{
				return false;
			}
		}

		return true;
	}

	private bool FollowsLetterOrDigit()
		=> index > 0 && char.IsLetterOrDigit(source[index - 1]);

	private bool StartsWith(string value)
		=> string.CompareOrdinal(source, index, value, 0, value.Length) == 0;

	private char Peek(int offset)
		=> index + offset < source.Length ? source[index + offset] : '\0';

	private static bool IsNameStart(char value)
		=> char.IsLetter(value);

	private static bool IsIdentifierStart(char value)
		=> char.IsLetter(value) || value == '_';

	private static bool IsAttributeNameEnd(char value)
		=> char.IsWhiteSpace(value) || value is '=' or '>' or '/' or '"' or '\'';

	private static bool Contains(string[] values, string value)
		=> Array.IndexOf(values, value) >= 0;
}

internal sealed class RazorMarkupScan
{
	public RazorMarkupScan(
		IReadOnlyList<MarkupAttribute> attributes,
		int pageDirectiveCount,
		IReadOnlyList<string> attributeDirectives)
	{
		Attributes = attributes;
		PageDirectiveCount = pageDirectiveCount;
		AttributeDirectives = attributeDirectives;
	}

	public IReadOnlyList<MarkupAttribute> Attributes { get; }

	public int PageDirectiveCount { get; }

	public IReadOnlyList<string> AttributeDirectives { get; }
}

internal sealed class MarkupAttribute
{
	public MarkupAttribute(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public int Index { get; }

	public string Name { get; }
}
