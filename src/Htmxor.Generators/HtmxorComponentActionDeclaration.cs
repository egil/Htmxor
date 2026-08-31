using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class HtmxorComponentActionDeclaration
{
	private static readonly ActionBinding[] SupportedBindings =
	{
		new("@onpost", "POST"),
		new("@onput", "PUT"),
		new("@onpatch", "PATCH"),
		new("@ondelete", "DELETE"),
		new("@onquery", "QUERY"),
	};

	private HtmxorComponentActionDeclaration(
		string componentTypeName,
		string attributeName,
		string httpMethod,
		string? handlerName,
		bool usesStockRoute,
		string? routeTemplate,
		string path,
		TextSpan span,
		LinePositionSpan lineSpan,
		string? unsupportedReason)
	{
		ComponentTypeName = componentTypeName;
		AttributeName = attributeName;
		HttpMethod = httpMethod;
		HandlerName = handlerName;
		UsesStockRoute = usesStockRoute;
		RouteTemplate = routeTemplate;
		Path = path;
		Span = span;
		LineSpan = lineSpan;
		UnsupportedReason = unsupportedReason;
	}

	public string ComponentTypeName { get; }

	public string AttributeName { get; }

	public string HttpMethod { get; }

	public string? HandlerName { get; }

	public bool UsesStockRoute { get; }

	public string? RouteTemplate { get; }

	public string Path { get; }

	public TextSpan Span { get; }

	public LinePositionSpan LineSpan { get; }

	public string? UnsupportedReason { get; }

	public static ImmutableArray<HtmxorComponentActionDeclaration> ParseAll(
		AdditionalText additionalFile,
		string? componentTypeName,
		CancellationToken cancellationToken)
	{
		if (componentTypeName is null)
		{
			return ImmutableArray<HtmxorComponentActionDeclaration>.Empty;
		}

		var text = additionalFile.GetText(cancellationToken);
		if (text is null)
		{
			return ImmutableArray<HtmxorComponentActionDeclaration>.Empty;
		}

		var source = text.ToString();
		var declarations = ImmutableArray.CreateBuilder<HtmxorComponentActionDeclaration>();
		foreach (var binding in SupportedBindings)
		{
			var candidates = FindMarkupAttributes(source, binding.AttributeName);
			foreach (var candidate in candidates)
			{
				declarations.Add(Parse(
					componentTypeName,
					additionalFile.Path,
					text,
					source,
					binding,
					candidate,
					candidates.Count));
			}
		}

		return declarations.ToImmutable();
	}

	private static HtmxorComponentActionDeclaration Parse(
		string componentTypeName,
		string path,
		SourceText text,
		string source,
		ActionBinding binding,
		MarkupAttribute candidate,
		int methodDeclarationCount)
	{
		var span = new TextSpan(candidate.Index, binding.AttributeName.Length);
		if (methodDeclarationCount > 1)
		{
			return Unsupported(
				componentTypeName,
				binding,
				candidate.UsesStockRoute,
				candidate.RouteTemplate,
				path,
				text,
				span,
				"at most one " + binding.AttributeName + " binding per component is supported");
		}

		var match = binding.SupportedBinding.Match(source, candidate.Index);
		return match.Success &&
			match.Index == candidate.Index &&
			IsBindingTerminator(source, match.Index + match.Length)
			? new HtmxorComponentActionDeclaration(
				componentTypeName,
				binding.AttributeName,
				binding.HttpMethod,
				match.Groups["handler"].Value,
				candidate.UsesStockRoute,
				candidate.RouteTemplate,
				path,
				span,
				text.Lines.GetLinePositionSpan(span),
				unsupportedReason: null)
			: Unsupported(
				componentTypeName,
				binding,
				candidate.UsesStockRoute,
				candidate.RouteTemplate,
				path,
				text,
				span,
				binding.AttributeName + " must use one double-quoted simple method-group name");
	}

	private static IReadOnlyList<MarkupAttribute> FindMarkupAttributes(
		string source,
		string attributeName)
	{
		var attributes = new List<MarkupAttribute>();
		var searchIndex = 0;
		while ((searchIndex = source.IndexOf(attributeName, searchIndex, StringComparison.Ordinal)) >= 0)
		{
			if (IsMarkupAttribute(
				source,
				searchIndex,
				attributeName,
				out var usesStockRoute,
				out var routeTemplate))
			{
				attributes.Add(new MarkupAttribute(searchIndex, usesStockRoute, routeTemplate));
			}

			searchIndex += attributeName.Length;
		}

		return attributes;
	}

	private static bool IsMarkupAttribute(
		string source,
		int attributeIndex,
		string attributeName,
		out bool usesStockRoute,
		out string? routeTemplate)
	{
		usesStockRoute = false;
		routeTemplate = null;
		var tagStart = source.LastIndexOf('<', attributeIndex);
		return tagStart >= 0 &&
			TryGetRouteOwner(source, tagStart, out usesStockRoute, out routeTemplate) &&
			source.LastIndexOf('>', attributeIndex) < tagStart &&
			!IsInsideDelimitedRegion(source, attributeIndex, "@*", "*@") &&
			!IsInsideDelimitedRegion(source, attributeIndex, "<!--", "-->") &&
			HasSupportedTagPrefix(source, tagStart, attributeIndex) &&
			attributeIndex > 0 &&
			char.IsWhiteSpace(source[attributeIndex - 1]) &&
			IsAttributeNameTerminator(source, attributeIndex + attributeName.Length);
	}

	private static bool TryGetRouteOwner(
		string source,
		int tagStart,
		out bool usesStockRoute,
		out string? routeTemplate)
	{
		usesStockRoute = false;
		routeTemplate = null;
		var tagLineStart = source.LastIndexOf('\n', tagStart);
		tagLineStart = tagLineStart < 0 ? 0 : tagLineStart + 1;
		if (!IsWhitespace(source, tagLineStart, tagStart))
		{
			return false;
		}

		var lines = source
			.Substring(0, tagLineStart)
			.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		var pageDirectiveCount = 0;
		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (trimmed.Length == 0)
			{
				continue;
			}

			if (IsSupportedPageDirectiveLine(trimmed))
			{
				pageDirectiveCount++;
				continue;
			}

			if (IsSupportedMarkupLine(trimmed))
			{
				continue;
			}

			if (!IsSupportedDirectiveLine(trimmed))
			{
				return false;
			}

			if (routeTemplate is null && TryReadOmittedHtmxRoute(trimmed, out var declaredRoute))
			{
				routeTemplate = declaredRoute;
			}
		}

		usesStockRoute = pageDirectiveCount == 1;
		return pageDirectiveCount <= 1;
	}

	private static bool TryReadOmittedHtmxRoute(string line, out string? routeTemplate)
	{
		string[] prefixes =
		{
			"@attribute [HtmxRoute(\"",
			"@attribute [Htmxor.HtmxRoute(\"",
			"@attribute [global::Htmxor.HtmxRoute(\"",
		};
		const string suffix = "\")]";
		foreach (var prefix in prefixes)
		{
			if (!line.StartsWith(prefix, StringComparison.Ordinal) ||
				!line.EndsWith(suffix, StringComparison.Ordinal))
			{
				continue;
			}

			var value = line.Substring(prefix.Length, line.Length - prefix.Length - suffix.Length);
			if (value.Length > 0 && value.IndexOf('"') < 0)
			{
				routeTemplate = value;
				return true;
			}
		}

		routeTemplate = null;
		return false;
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

	private static bool IsSupportedMarkupLine(string line)
	{
		if (!HasSupportedMarkupBounds(line))
		{
			return false;
		}

		const int nameStart = 1;
		var nameEnd = SkipName(line, nameStart, line.Length - 1, allowRazorPrefix: false);
		if (!HasSupportedMarkupNameBoundary(line, nameStart, nameEnd))
		{
			return false;
		}

		var openingTagEnd = line.IndexOf('>', nameEnd);
		return openingTagEnd >= 0 &&
			(IsSelfClosingMarkupLine(line, nameStart, nameEnd, openingTagEnd) ||
			IsPlainMarkupElementLine(line, nameStart, nameEnd, openingTagEnd));
	}

	private static bool HasSupportedMarkupNameBoundary(
		string line,
		int nameStart,
		int nameEnd)
		=> nameEnd > nameStart &&
			(nameEnd == line.Length - 1 ||
			char.IsWhiteSpace(line[nameEnd]) ||
			line[nameEnd] == '>');

	private static bool IsSelfClosingMarkupLine(
		string line,
		int nameStart,
		int nameEnd,
		int openingTagEnd)
	{
		if (openingTagEnd != line.Length - 1 ||
			!IsVoidHtmlElement(line, nameStart, nameEnd))
		{
			return false;
		}

		var index = openingTagEnd - 1;
		while (index >= 0 && char.IsWhiteSpace(line[index]))
		{
			index--;
		}

		return index >= 0 && line[index] == '/';
	}

	private static bool IsVoidHtmlElement(string line, int nameStart, int nameEnd)
	{
		const string voidElementNames = "|area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr|";
		var name = "|" + line.Substring(nameStart, nameEnd - nameStart).ToLowerInvariant() + "|";
		return voidElementNames.IndexOf(name, StringComparison.Ordinal) >= 0;
	}

	private static bool IsPlainMarkupElementLine(
		string line,
		int nameStart,
		int nameEnd,
		int openingTagEnd)
	{
		var name = line.Substring(nameStart, nameEnd - nameStart);
		if (string.Equals(name, "plaintext", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var closingTag = "</" + name + ">";
		var closingTagStart = line.Length - closingTag.Length;
		return closingTagStart > openingTagEnd &&
			line.EndsWith(closingTag, StringComparison.OrdinalIgnoreCase) &&
			line.IndexOf('<', openingTagEnd + 1) == closingTagStart &&
			line.IndexOf('>', openingTagEnd + 1) == line.Length - 1;
	}

	private static bool HasSupportedMarkupBounds(string line)
		=> HasOnlySingleLineLexicalContent(line) &&
			line.Length >= 3 &&
			line[0] == '<' &&
			line[line.Length - 1] == '>' &&
			line[1] != '!' &&
			line[1] != '?' &&
			line[1] != '/';

	private static bool IsSupportedPageDirectiveLine(string line)
	{
		const string prefix = "@page \"";
		return HasOnlySingleLineLexicalContent(line) &&
			line.StartsWith(prefix, StringComparison.Ordinal) &&
			line.EndsWith("\"", StringComparison.Ordinal) &&
			line.IndexOf('\"', prefix.Length) == line.Length - 1;
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
		var index = SkipName(source, tagStart + 1, attributeIndex, allowRazorPrefix: false);
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

			var attributeNameStart = index;
			index = SkipName(source, index, attributeIndex, allowRazorPrefix: true);
			if (index < 0)
			{
				return false;
			}

			if (index < attributeIndex && source[index] == '=')
			{
				index = SkipSupportedAttributeValue(
					source,
					index + 1,
					attributeIndex,
					attributeNameStart,
					index);
				if (index < 0)
				{
					return false;
				}
			}
		}

		return true;
	}

	private static int SkipName(string source, int start, int end, bool allowRazorPrefix)
	{
		var index = start;
		if (allowRazorPrefix && index < end && source[index] == '@')
		{
			index++;
		}

		var nameStart = index;
		while (index < end && IsNameCharacter(source[index]))
		{
			index++;
		}

		return index == nameStart ? -1 : index;
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

	private static int SkipSupportedAttributeValue(
		string source,
		int start,
		int end,
		int attributeNameStart,
		int attributeNameEnd)
	{
		if (start >= end || source[start] != '"')
		{
			return -1;
		}

		if (IsStaticIdTarget(source, start, end, attributeNameStart, attributeNameEnd))
		{
			return SkipStaticIdSelector(source, start + 2, end);
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

	private static bool IsStaticIdTarget(
		string source,
		int valueStart,
		int valueEnd,
		int attributeNameStart,
		int attributeNameEnd)
		=> attributeNameEnd - attributeNameStart == "hx-target".Length &&
			string.CompareOrdinal(
				source,
				attributeNameStart,
				"hx-target",
				0,
				"hx-target".Length) == 0 &&
			valueStart + 1 < valueEnd &&
			source[valueStart + 1] == '#';

	private static int SkipStaticIdSelector(string source, int start, int end)
	{
		if (start >= end || !IsIdentifierStart(source[start]))
		{
			return -1;
		}

		var index = start + 1;
		while (index < end &&
			(IsIdentifierPart(source[index]) || source[index] == '-'))
		{
			index++;
		}

		return index < end && source[index] == '"' ? index + 1 : -1;
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

	private static HtmxorComponentActionDeclaration Unsupported(
		string componentTypeName,
		ActionBinding binding,
		bool usesStockRoute,
		string? routeTemplate,
		string path,
		SourceText text,
		TextSpan span,
		string reason)
		=> new(
			componentTypeName,
			binding.AttributeName,
			binding.HttpMethod,
			handlerName: null,
			usesStockRoute,
			routeTemplate,
			path,
			span,
			text.Lines.GetLinePositionSpan(span),
			reason);

	private sealed class ActionBinding
	{
		public ActionBinding(string attributeName, string httpMethod)
		{
			AttributeName = attributeName;
			HttpMethod = httpMethod;
			SupportedBinding = new Regex(
				Regex.Escape(attributeName) + "\\s*=\\s*\"(?<handler>[A-Za-z_][A-Za-z0-9_]*)\"",
				RegexOptions.CultureInvariant);
		}

		public string AttributeName { get; }

		public string HttpMethod { get; }

		public Regex SupportedBinding { get; }
	}

	private sealed class MarkupAttribute
	{
		public MarkupAttribute(int index, bool usesStockRoute, string? routeTemplate)
		{
			Index = index;
			UsesStockRoute = usesStockRoute;
			RouteTemplate = routeTemplate;
		}

		public int Index { get; }

		public bool UsesStockRoute { get; }

		public string? RouteTemplate { get; }
	}
}
