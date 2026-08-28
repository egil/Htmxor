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
	private static readonly Regex PageDirective = new(
		"^\\s*@page(?:\\s|$)",
		RegexOptions.CultureInvariant | RegexOptions.Multiline);
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
		if (PageDirective.IsMatch(source))
		{
			return null;
		}

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
			!IsInsideAttributeValue(source, tagStart, attributeIndex) &&
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
		if (line.IndexOf('<') >= 0 ||
			line.IndexOf('>') >= 0 ||
			line.IndexOf("\"\"\"", StringComparison.Ordinal) >= 0)
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

	private static bool IsInsideAttributeValue(string source, int tagStart, int attributeIndex)
	{
		var quote = '\0';
		for (var index = tagStart + 1; index < attributeIndex; index++)
		{
			var current = source[index];
			if (quote == '\0' && (current == '\'' || current == '"'))
			{
				quote = current;
			}
			else if (current == quote)
			{
				quote = '\0';
			}
		}

		return quote != '\0';
	}

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
