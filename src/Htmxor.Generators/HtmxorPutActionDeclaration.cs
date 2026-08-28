using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class HtmxorPutActionDeclaration
{
	private const string AttributeName = "@onput";
	private static readonly Regex SupportedBinding = new(
		"@onput\\s*=\\s*\"(?<handler>[A-Za-z_][A-Za-z0-9_]*)\"",
		RegexOptions.CultureInvariant);

	private HtmxorPutActionDeclaration(
		string componentTypeName,
		string? handlerName,
		string path,
		TextSpan span,
		LinePositionSpan lineSpan,
		string? unsupportedReason)
	{
		ComponentTypeName = componentTypeName;
		HandlerName = handlerName;
		Path = path;
		Span = span;
		LineSpan = lineSpan;
		UnsupportedReason = unsupportedReason;
	}

	public string ComponentTypeName { get; }

	public string? HandlerName { get; }

	public string Path { get; }

	public TextSpan Span { get; }

	public LinePositionSpan LineSpan { get; }

	public string? UnsupportedReason { get; }

	public static HtmxorPutActionDeclaration? Parse(
		AdditionalText additionalFile,
		string? componentTypeName,
		CancellationToken cancellationToken)
	{
		if (componentTypeName is null)
		{
			return null;
		}

		var text = additionalFile.GetText(cancellationToken);
		if (text is null)
		{
			return null;
		}

		var source = text.ToString();
		var attributeIndices = FindMarkupAttributeIndices(source);
		if (attributeIndices.Count == 0)
		{
			return null;
		}

		var attributeIndex = attributeIndices[0];
		var span = new TextSpan(attributeIndex, AttributeName.Length);
		if (attributeIndices.Count > 1)
		{
			return Unsupported(
				componentTypeName,
				additionalFile.Path,
				text,
				span,
				"exactly one @onput declaration is supported");
		}

		var match = SupportedBinding.Match(source, attributeIndex);
		return match.Success &&
			match.Index == attributeIndex &&
			IsBindingTerminator(source, match.Index + match.Length)
			? new HtmxorPutActionDeclaration(
				componentTypeName,
				match.Groups["handler"].Value,
				additionalFile.Path,
				span,
				text.Lines.GetLinePositionSpan(span),
				unsupportedReason: null)
			: Unsupported(
				componentTypeName,
				additionalFile.Path,
				text,
				span,
				"@onput must use one double-quoted simple method-group name");
	}

	private static IReadOnlyList<int> FindMarkupAttributeIndices(string source)
	{
		var indices = new List<int>();
		var searchIndex = 0;
		while ((searchIndex = source.IndexOf(AttributeName, searchIndex, StringComparison.Ordinal)) >= 0)
		{
			if (IsMarkupAttribute(source, searchIndex))
			{
				indices.Add(searchIndex);
			}

			searchIndex += AttributeName.Length;
		}

		return indices;
	}

	private static bool IsMarkupAttribute(string source, int attributeIndex)
	{
		var tagStart = source.LastIndexOf('<', attributeIndex);
		return tagStart >= 0 &&
			HasSupportedPreamble(source, tagStart) &&
			source.LastIndexOf('>', attributeIndex) < tagStart &&
			!IsInsideDelimitedRegion(source, attributeIndex, "@*", "*@") &&
			!IsInsideDelimitedRegion(source, attributeIndex, "<!--", "-->") &&
			HasSupportedTagPrefix(source, tagStart, attributeIndex) &&
			attributeIndex > 0 &&
			char.IsWhiteSpace(source[attributeIndex - 1]) &&
			IsAttributeNameTerminator(source, attributeIndex + AttributeName.Length);
	}

	private static bool HasSupportedPreamble(string source, int tagStart)
	{
		var tagLineStart = source.LastIndexOf('\n', tagStart);
		tagLineStart = tagLineStart < 0 ? 0 : tagLineStart + 1;
		if (!IsWhitespace(source, tagLineStart, tagStart))
		{
			return false;
		}

		var lines = source
			.Substring(0, tagLineStart)
			.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (trimmed.Length > 0 && !IsSupportedDirectiveLine(trimmed))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsWhitespace(string source, int start, int end)
	{
		for (var index = start; index < end; index++)
		{
			if (!char.IsWhiteSpace(source[index]))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsSupportedDirectiveLine(string line)
	{
		if (!HasOnlySingleLineLexicalContent(line) ||
			line.IndexOf('<') >= 0 ||
			line.IndexOf('>') >= 0)
		{
			return false;
		}

		var isAttributeDirective = line.StartsWith("@attribute [", StringComparison.Ordinal) &&
			line.EndsWith("]", StringComparison.Ordinal);
		var isUsingDirective = line.StartsWith("@using ", StringComparison.Ordinal) &&
			line.IndexOf('(') < 0;
		var isInjectDirective = line.StartsWith("@inject ", StringComparison.Ordinal) &&
			line.IndexOf('(') < 0;
		return isAttributeDirective || isUsingDirective || isInjectDirective;
	}

	private static bool HasOnlySingleLineLexicalContent(string line)
		=> line.IndexOf("/*", StringComparison.Ordinal) < 0 &&
			line.IndexOf("*/", StringComparison.Ordinal) < 0 &&
			line.IndexOf("//", StringComparison.Ordinal) < 0 &&
			line.IndexOf("\"\"\"", StringComparison.Ordinal) < 0 &&
			line.IndexOf('$') < 0 &&
			line.IndexOf('@', 1) < 0;

	private static bool HasSupportedTagPrefix(string source, int tagStart, int attributeIndex)
	{
		var index = SkipName(source, tagStart + 1, attributeIndex);
		if (index < 0)
		{
			return false;
		}

		while (index < attributeIndex)
		{
			index = SkipWhitespace(source, index, attributeIndex);
			if (index == attributeIndex)
			{
				return true;
			}

			index = SkipName(source, index, attributeIndex);
			if (index < 0)
			{
				return false;
			}

			if (index < attributeIndex && source[index] == '=')
			{
				index = SkipSupportedAttributeValue(source, index + 1, attributeIndex);
				if (index < 0)
				{
					return false;
				}
			}
		}

		return true;
	}

	private static int SkipName(string source, int start, int end)
	{
		var index = start;
		while (index < end && IsNameCharacter(source[index]))
		{
			index++;
		}

		return index == start ? -1 : index;
	}

	private static int SkipWhitespace(string source, int start, int end)
	{
		var index = start;
		while (index < end && char.IsWhiteSpace(source[index]))
		{
			index++;
		}

		return index;
	}

	private static int SkipSupportedAttributeValue(string source, int start, int end)
	{
		if (start >= end || source[start] != '"')
		{
			return -1;
		}

		var index = start + 1;
		while (index < end)
		{
			if (source[index] == '"')
			{
				return index + 1;
			}

			if (source[index] == '@')
			{
				index = SkipSimpleRazorIdentifier(source, index + 1, end);
				if (index < 0)
				{
					return -1;
				}

				continue;
			}

			if (!IsSupportedAttributeValueCharacter(source[index]))
			{
				return -1;
			}

			index++;
		}

		return -1;
	}

	private static int SkipSimpleRazorIdentifier(string source, int start, int end)
	{
		if (start >= end || !IsIdentifierStart(source[start]))
		{
			return -1;
		}

		var index = start + 1;
		while (index < end &&
			(IsIdentifierPart(source[index]) || source[index] == '.'))
		{
			index++;
		}

		return index;
	}

	private static bool IsNameCharacter(char value)
		=> char.IsLetterOrDigit(value) || value == '-' || value == '_' || value == ':';

	private static bool IsIdentifierStart(char value)
		=> char.IsLetter(value) || value == '_';

	private static bool IsIdentifierPart(char value)
		=> char.IsLetterOrDigit(value) || value == '_';

	private static bool IsSupportedAttributeValueCharacter(char value)
		=> char.IsLetterOrDigit(value) ||
			value == '/' ||
			value == '-' ||
			value == '_' ||
			value == '.' ||
			value == '?' ||
			value == '=' ||
			value == '&' ||
			value == ':' ||
			value == '%';

	private static bool IsInsideDelimitedRegion(
		string source,
		int index,
		string openingDelimiter,
		string closingDelimiter)
		=> source.LastIndexOf(openingDelimiter, index, StringComparison.Ordinal) >
			source.LastIndexOf(closingDelimiter, index, StringComparison.Ordinal);

	private static bool IsAttributeNameTerminator(string source, int index)
		=> index == source.Length || source[index] == '=' || char.IsWhiteSpace(source[index]);

	private static bool IsBindingTerminator(string source, int index)
		=> index == source.Length ||
			source[index] == '>' ||
			source[index] == '/' ||
			char.IsWhiteSpace(source[index]);

	private static HtmxorPutActionDeclaration Unsupported(
		string componentTypeName,
		string path,
		SourceText text,
		TextSpan span,
		string reason)
		=> new(
			componentTypeName,
			handlerName: null,
			path,
			span,
			text.Lines.GetLinePositionSpan(span),
			reason);
}
