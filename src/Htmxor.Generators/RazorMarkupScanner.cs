using System;
using System.Collections.Generic;

namespace Htmxor.Generators;

// A small Razor lexer: it follows markup, Razor transitions and enough C# lexing (strings, comments and
// brackets) to find every attribute the Razor compiler would bind, and it reads the route-owner directives
// from the whole file. Attributes in @code/@functions markup and Razor templates (@<tag>) are reported as
// outside the component's own markup. Incomplete text, as the IDE sends while typing, never throws.
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
	private string? namespaceDirective;
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
		scanner.ScanMarkup(ownerMarkup: true, island: false);
		return new RazorMarkupScan(
			scanner.attributes,
			scanner.pageDirectiveCount,
			scanner.attributeDirectives,
			scanner.namespaceDirective);
	}

	// Scans to the end of the source, or for a markup island inside C# until its outermost element closes.
	private void ScanMarkup(bool ownerMarkup, bool island)
	{
		var depth = 0;
		while (index < source.Length)
		{
			if (source[index] == '<')
			{
				depth += ScanTag(ownerMarkup);
				if (island && depth <= 0)
				{
					return;
				}
			}
			else if (source[index] == '@')
			{
				ScanMarkupTransition(ownerMarkup, island);
			}
			else
			{
				index++;
			}
		}
	}

	private int ScanTag(bool ownerMarkup)
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
		var selfClosing = ScanAttributes(ownerMarkup);
		if (Array.Exists(RawTextElements, element => string.Equals(element, name, StringComparison.OrdinalIgnoreCase)))
		{
			SkipRawText(name);
			return 0;
		}

		return selfClosing || Contains(VoidElements, name) ? 0 : 1;
	}

	private bool ScanAttributes(bool ownerMarkup)
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

			if (StartsWith("@*"))
			{
				SkipBlockComment("*@");
				continue;
			}

			ScanAttribute(ownerMarkup);
		}

		return false;
	}

	private void ScanAttribute(bool ownerMarkup)
	{
		var nameStart = index;
		while (index < source.Length && !IsAttributeNameEnd(source[index]))
		{
			index++;
		}

		if (index == nameStart)
		{
			Advance(1);
			return;
		}

		var name = source.Substring(nameStart, index - nameStart);
		var value = ScanAttributeValue();
		if (attributeNames.Contains(name))
		{
			attributes.Add(new MarkupAttribute(nameStart, name, value, ownerMarkup));
		}
	}

	// The raw value text, quotes included, or null for an attribute without a value.
	private string? ScanAttributeValue()
	{
		SkipWhitespace();
		if (Peek(0) != '=')
		{
			return null;
		}

		index++;
		SkipWhitespace();
		var valueStart = index;
		SkipAttributeValue();
		return source.Substring(valueStart, index - valueStart);
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
			// Inside a quoted value Razor still runs "@*" to the next "*@", across quotes, and emits that span
			// as literal text, so the value cannot end inside it.
			if (source[index] == '@' && (Peek(1) == '*' || !FollowsLetterOrDigit()))
			{
				SkipExpressionTransition();
			}
			else
			{
				index++;
			}
		}

		Advance(1);
	}

	// An implicit or explicit Razor expression used as markup or attribute content.
	private void SkipExpressionTransition()
	{
		if (StartsWith("@*"))
		{
			SkipBlockComment("*@");
			return;
		}

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

	private void ScanMarkupTransition(bool ownerMarkup, bool island)
	{
		// A Razor comment starts even right after text or an expression, unlike an email-like '@'.
		if (Peek(1) is '*' or '@' or '(' || FollowsLetterOrDigit())
		{
			SkipExpressionTransition();
			return;
		}

		if (Peek(1) == '{')
		{
			index++;
			ScanCodeBlock(ownerMarkup);
			return;
		}

		ScanKeywordTransition(ownerMarkup, island);
	}

	private void ScanKeywordTransition(bool ownerMarkup, bool island)
	{
		var transitionStart = index;
		index++;
		var keyword = ReadIdentifier();
		if (Contains(MemberBlockKeywords, keyword) && SkipWhitespaceTo('{'))
		{
			ScanCodeBlock(ownerMarkup: false);
		}
		else if (IsStatement(keyword))
		{
			ScanStatement(ownerMarkup);
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
	private void ScanStatement(bool ownerMarkup)
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
				ScanCodeBlock(ownerMarkup);
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
		else if (keyword == "namespace")
		{
			namespaceDirective ??= body;
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
	// outermost element closes; a Razor template (@<tag>) is never the component's own markup.
	private void ScanCodeBlock(bool ownerMarkup)
	{
		var depth = 0;
		var atStatementStart = true;
		while (index < source.Length)
		{
			if (TrySkipComment(atStatementStart))
			{
				continue;
			}

			if (TrySkipCSharpLiteralOrComment())
			{
				atStatementStart = false;
				continue;
			}

			if (TryScanCodeMarkup(ownerMarkup, atStatementStart))
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

	// A comment is not a statement token, so the statement-start state before it still holds after it.
	// A Razor comment may appear anywhere in a code block; an HTML comment, like an element, starts a statement.
	private bool TrySkipComment(bool atStatementStart)
	{
		if (!atStatementStart || !StartsWith("<!--"))
		{
			return TrySkipCodeComment();
		}

		SkipPast("-->");
		return true;
	}

	// C# line and block comments, and Razor comments, which Razor accepts inside C# too.
	private bool TrySkipCodeComment()
	{
		if (StartsWith("//"))
		{
			SkipTo('\n');
		}
		else if (StartsWith("/*") || StartsWith("@*"))
		{
			SkipBlockComment(source[index] == '/' ? "*/" : "*@");
		}
		else
		{
			return false;
		}

		return true;
	}

	private bool TryScanCodeMarkup(bool ownerMarkup, bool atStatementStart)
	{
		if (source[index] == '<' && atStatementStart && IsNameStart(Peek(1)))
		{
			ScanMarkup(ownerMarkup, island: true);
			return true;
		}

		if (source[index] == '@' && Peek(1) == '<')
		{
			index++;
			ScanMarkup(ownerMarkup: false, island: true);
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

	// Balanced C#, such as an expression or a statement header. A Razor template inside it is markup.
	private void SkipBalanced(char open, char close)
	{
		var depth = 0;
		while (index < source.Length)
		{
			if (TrySkipCSharpLiteralOrComment())
			{
				continue;
			}

			if (StartsWith("@<"))
			{
				index++;
				ScanMarkup(ownerMarkup: false, island: true);
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
		=> TrySkipCodeComment() || TrySkipCSharpString() || TrySkipCharLiteral();

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
				Advance(source[index] == '\\' && !verbatim ? 2 : 1);
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
			Advance(source[index] == '\\' ? 2 : 1);
		}

		Advance(1);
		return true;
	}

	// HTML ends script and style text only at their closing tag, and plaintext never ends.
	private void SkipRawText(string elementName)
	{
		var closingTag = string.Equals(elementName, PlaintextElement, StringComparison.OrdinalIgnoreCase)
			? -1
			: FindClosingTag("</" + elementName);
		index = closingTag < 0 ? source.Length : closingTag;
		SkipPast(">");
	}

	// "</script" only closes script when the name ends there, so "</scripture>" stays raw text.
	private int FindClosingTag(string prefix)
	{
		var found = source.IndexOf(prefix, index, StringComparison.OrdinalIgnoreCase);
		while (found >= 0 && !IsTagNameEnd(found + prefix.Length))
		{
			found = source.IndexOf(prefix, found + 1, StringComparison.OrdinalIgnoreCase);
		}

		return found;
	}

	private bool IsTagNameEnd(int position)
		=> position >= source.Length || char.IsWhiteSpace(source[position]) || source[position] is '/' or '>';

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

	private void SkipPast(string value, int openingLength = 1)
	{
		var found = source.IndexOf(value, Math.Min(index + openingLength, source.Length), StringComparison.Ordinal);
		index = found < 0 ? source.Length : found + value.Length;
	}

	// The closing marker of "@*" or "/*" cannot overlap its two-character opening marker.
	private void SkipBlockComment(string closing)
		=> SkipPast(closing, openingLength: 2);

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

	private void Advance(int count)
		=> index = Math.Min(index + count, source.Length);

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
		IReadOnlyList<string> attributeDirectives,
		string? namespaceDirective)
	{
		Attributes = attributes;
		PageDirectiveCount = pageDirectiveCount;
		AttributeDirectives = attributeDirectives;
		NamespaceDirective = namespaceDirective;
	}

	public IReadOnlyList<MarkupAttribute> Attributes { get; }

	public int PageDirectiveCount { get; }

	public IReadOnlyList<string> AttributeDirectives { get; }

	public string? NamespaceDirective { get; }
}

internal sealed class MarkupAttribute
{
	public MarkupAttribute(int index, string name, string? value, bool inOwnerMarkup)
	{
		Index = index;
		Name = name;
		Value = value;
		InOwnerMarkup = inOwnerMarkup;
	}

	public int Index { get; }

	public string Name { get; }

	public string? Value { get; }

	public bool InOwnerMarkup { get; }
}
