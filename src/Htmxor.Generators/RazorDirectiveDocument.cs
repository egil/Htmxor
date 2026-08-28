using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class RazorDirectiveDocument
{
	private RazorDirectiveDocument(
		RazorSourceFile file,
		ImmutableArray<RazorAttributeDirective> attributes,
		ImmutableArray<string> usings,
		bool hasPage,
		string? declaredNamespace,
		bool hasMalformedDirective,
		bool hasMalformedAttribute,
		bool hasHtmxRouteAttributeText,
		bool hasHtmxRouteUsingText,
		Location htmxRouteTextLocation,
		Location attributeFallbackLocation,
		Location fallbackLocation)
	{
		File = file;
		Attributes = attributes;
		Usings = usings;
		HasPage = hasPage;
		DeclaredNamespace = declaredNamespace;
		HasMalformedDirective = hasMalformedDirective;
		HasMalformedAttribute = hasMalformedAttribute;
		HasHtmxRouteAttributeText = hasHtmxRouteAttributeText;
		HasHtmxRouteUsingText = hasHtmxRouteUsingText;
		HtmxRouteTextLocation = htmxRouteTextLocation;
		AttributeFallbackLocation = attributeFallbackLocation;
		FallbackLocation = fallbackLocation;
	}

	public RazorSourceFile File { get; }

	public string ComponentName => System.IO.Path.GetFileNameWithoutExtension(File.Path);

	public bool IsImports => string.Equals(ComponentName, "_Imports", StringComparison.OrdinalIgnoreCase);

	public ImmutableArray<RazorAttributeDirective> Attributes { get; }

	public ImmutableArray<string> Usings { get; }

	public bool HasPage { get; }

	public string? DeclaredNamespace { get; }

	public bool HasMalformedDirective { get; }

	public bool HasMalformedAttribute { get; }

	public bool HasHtmxRouteAttributeText { get; }

	public bool HasHtmxRouteUsingText { get; }

	public Location HtmxRouteTextLocation { get; }

	public Location AttributeFallbackLocation { get; }

	public Location FallbackLocation { get; }

	public static RazorDirectiveDocument Parse(RazorSourceFile file, CSharpParseOptions parseOptions)
	{
		var attributes = ImmutableArray.CreateBuilder<RazorAttributeDirective>();
		var usings = ImmutableArray.CreateBuilder<string>();
		var state = new ParseState(file);
		foreach (var directive in RazorDirectiveLocator.Find(file.Source))
		{
			state.Accept(directive, parseOptions, attributes, usings);
		}

		return new RazorDirectiveDocument(
			file,
			attributes.ToImmutable(),
			usings.ToImmutable(),
			state.HasPage,
			state.DeclaredNamespace,
			state.HasMalformedDirective,
			state.HasMalformedAttribute,
			state.HasHtmxRouteAttributeText,
			state.HasHtmxRouteUsingText,
			state.HtmxRouteTextLocation,
			state.AttributeFallbackLocation,
			state.FallbackLocation);
	}

	private sealed class ParseState(RazorSourceFile file)
	{
		public bool HasPage { get; private set; }

		public string? DeclaredNamespace { get; private set; }

		public bool HasMalformedDirective { get; private set; }

		public bool HasMalformedAttribute { get; private set; }

		public bool HasHtmxRouteAttributeText { get; private set; }

		public bool HasHtmxRouteUsingText { get; private set; }

		public Location HtmxRouteTextLocation { get; private set; } = Location.None;

		public Location AttributeFallbackLocation { get; private set; } = Location.None;

		public Location FallbackLocation { get; private set; } = Location.None;

		public void Accept(
			RazorDirective directive,
			CSharpParseOptions parseOptions,
			ImmutableArray<RazorAttributeDirective>.Builder attributes,
			ImmutableArray<string>.Builder usings)
		{
			SetFallbackLocation(directive.Span);
			switch (directive.Kind)
			{
				case RazorDirectiveKind.Attribute:
					AcceptAttribute(directive, parseOptions, attributes);
					break;
				case RazorDirectiveKind.Using:
					AcceptUsing(directive, parseOptions, usings);
					break;
				case RazorDirectiveKind.Page:
					HasPage = true;
					break;
				case RazorDirectiveKind.Namespace:
					AcceptNamespace(directive);
					break;
				default:
					throw new InvalidOperationException("Unrecognized Razor directive kind.");
			}
		}

		private void AcceptAttribute(
			RazorDirective directive,
			CSharpParseOptions parseOptions,
			ImmutableArray<RazorAttributeDirective>.Builder attributes)
		{
			if (AttributeFallbackLocation == Location.None)
			{
				AttributeFallbackLocation = CreateLocation(file, directive.Span);
			}

			var parsed = RazorAttributeDirective.TryParse(
				file,
				directive,
				parseOptions,
				out var inspectedSpan);
			if (RazorDirectiveLocator.ContainsOutsideComments(
				file.Source,
				inspectedSpan,
				"HtmxRoute"))
			{
				HasHtmxRouteAttributeText = true;
				if (HtmxRouteTextLocation == Location.None)
				{
					HtmxRouteTextLocation = CreateLocation(file, inspectedSpan);
				}
			}

			if (parsed is null)
			{
				HasMalformedDirective = true;
				HasMalformedAttribute = true;
				return;
			}

			attributes.Add(parsed);
		}

		private void AcceptUsing(
			RazorDirective directive,
			CSharpParseOptions parseOptions,
			ImmutableArray<string>.Builder usings)
		{
			var text = file.Source.ToString(directive.PayloadSpan).Trim();
			HasHtmxRouteUsingText |= ContainsHtmxRoute(text);
			var parsed = ParseUsing(text, parseOptions);
			if (parsed is null)
			{
				HasMalformedDirective = true;
				return;
			}

			usings.Add(parsed);
		}

		private void AcceptNamespace(RazorDirective directive)
		{
			var text = file.Source.ToString(directive.PayloadSpan).Trim();
			var parsed = SyntaxFactory.ParseName(text);
			if (text.Length == 0 || parsed.ContainsDiagnostics || parsed.Span.Length != text.Length)
			{
				HasMalformedDirective = true;
				return;
			}

			var resolved = parsed.WithoutTrivia().ToFullString();
			if (DeclaredNamespace is not null &&
				!string.Equals(DeclaredNamespace, resolved, StringComparison.Ordinal))
			{
				HasMalformedDirective = true;
				return;
			}

			DeclaredNamespace = resolved;
		}

		private void SetFallbackLocation(TextSpan span)
		{
			if (FallbackLocation == Location.None)
			{
				FallbackLocation = CreateLocation(file, span);
			}
		}

		private static bool ContainsHtmxRoute(string text)
			=> text.IndexOf("HtmxRoute", StringComparison.Ordinal) >= 0;
	}

	private static string? ParseUsing(string payload, CSharpParseOptions parseOptions)
	{
		if (payload.Length == 0)
		{
			return null;
		}

		var source = "using " + payload + (payload.EndsWith(";", StringComparison.Ordinal) ? string.Empty : ";");
		var root = SyntaxFactory.ParseCompilationUnit(source, options: parseOptions);
		var directive = root.Usings.SingleOrDefault();
		return directive is not null &&
			root.Members.Count == 0 &&
			!root.ContainsDiagnostics
			? directive.WithoutTrivia().ToFullString()
			: null;
	}

	internal static Location CreateLocation(RazorSourceFile file, TextSpan span)
		=> Location.Create(file.Path, span, file.Source.Lines.GetLinePositionSpan(span));
}

internal sealed class RazorAttributeDirective
{
	private RazorAttributeDirective(string text, TextSpan span, Location location)
	{
		Text = text;
		Span = span;
		Location = location;
	}

	public string Text { get; }

	public TextSpan Span { get; }

	public Location Location { get; }

	public static RazorAttributeDirective? TryParse(
		RazorSourceFile file,
		RazorDirective directive,
		CSharpParseOptions parseOptions,
		out TextSpan inspectedSpan)
	{
		inspectedSpan = directive.Span;
		var bracketStart = SkipWhitespace(file.Source, directive.PayloadSpan.Start);
		if (bracketStart >= file.Source.Length || file.Source[bracketStart] != '[')
		{
			return null;
		}

		var remaining = file.Source.ToString(TextSpan.FromBounds(bracketStart, file.Source.Length));
		var member = SyntaxFactory.ParseMemberDeclaration(
			remaining,
			offset: 0,
			options: parseOptions,
			consumeFullText: false);
		var attributeList = member?.ChildNodes().OfType<AttributeListSyntax>().FirstOrDefault();
		if (attributeList is not null)
		{
			inspectedSpan = new TextSpan(
				bracketStart + attributeList.SpanStart,
				attributeList.Span.Length);
		}

		if (!IsComplete(attributeList))
		{
			return null;
		}

		var span = inspectedSpan;
		return new RazorAttributeDirective(
			file.Source.ToString(span),
			span,
			RazorDirectiveDocument.CreateLocation(file, span));
	}

	private static bool IsComplete(AttributeListSyntax? attributeList)
		=> attributeList is not null &&
		attributeList.SpanStart == 0 &&
		!attributeList.CloseBracketToken.IsMissing &&
		!attributeList.ContainsDiagnostics;

	private static int SkipWhitespace(SourceText source, int start)
	{
		var position = start;
		while (position < source.Length && char.IsWhiteSpace(source[position]))
		{
			position++;
		}

		return position;
	}
}

internal enum RazorDirectiveKind
{
	Attribute,
	Using,
	Page,
	Namespace,
}

internal readonly struct RazorDirective(
	RazorDirectiveKind kind,
	TextSpan span,
	TextSpan payloadSpan)
{
	public RazorDirectiveKind Kind { get; } = kind;

	public TextSpan Span { get; } = span;

	public TextSpan PayloadSpan { get; } = payloadSpan;
}

internal static class RazorDirectiveLocator
{
	private static readonly ImmutableArray<DirectiveKeyword> Keywords =
	[
		new("@attribute", RazorDirectiveKind.Attribute),
		new("@namespace", RazorDirectiveKind.Namespace),
		new("@using", RazorDirectiveKind.Using),
		new("@page", RazorDirectiveKind.Page),
	];

	public static ImmutableArray<RazorDirective> Find(SourceText source)
	{
		var directives = ImmutableArray.CreateBuilder<RazorDirective>();
		var commentDepth = 0;
		foreach (var line in source.Lines)
		{
			commentDepth = ScanLine(source, line, commentDepth, directives);
		}

		return directives.ToImmutable();
	}

	public static bool ContainsOutsideComments(SourceText source, TextSpan span, string value)
	{
		var commentDepth = 0;
		var position = span.Start;
		while (position < span.End)
		{
			if (TryReadComment(source, ref position, ref commentDepth))
			{
				continue;
			}

			if (commentDepth == 0 &&
				position + value.Length <= span.End &&
				Matches(source, position, value))
			{
				return true;
			}

			position++;
		}

		return false;
	}

	private static int ScanLine(
		SourceText source,
		TextLine line,
		int commentDepth,
		ImmutableArray<RazorDirective>.Builder directives)
	{
		var canStartDirective = true;
		var position = line.Start;
		while (position < line.End)
		{
			if (TryReadComment(source, ref position, ref commentDepth))
			{
				continue;
			}

			if (commentDepth > 0)
			{
				position++;
				continue;
			}

			if (canStartDirective && char.IsWhiteSpace(source[position]))
			{
				position++;
				continue;
			}

			if (canStartDirective && TryReadDirective(source, line, position, out var directive))
			{
				directives.Add(directive);
			}

			canStartDirective = false;
			position++;
		}

		return commentDepth;
	}

	private static bool TryReadComment(SourceText source, ref int position, ref int depth)
	{
		if (Matches(source, position, "@*"))
		{
			depth++;
			position += 2;
			return true;
		}

		if (depth > 0 && Matches(source, position, "*@"))
		{
			depth--;
			position += 2;
			return true;
		}

		return false;
	}

	private static bool TryReadDirective(
		SourceText source,
		TextLine line,
		int start,
		out RazorDirective directive)
	{
		foreach (var keyword in Keywords)
		{
			if (!Matches(source, start, keyword.Text) || !HasKeywordBoundary(source, start + keyword.Text.Length))
			{
				continue;
			}

			var keywordEnd = start + keyword.Text.Length;
			directive = new RazorDirective(
				keyword.Kind,
				TextSpan.FromBounds(start, line.End),
				TextSpan.FromBounds(keywordEnd, line.End));
			return true;
		}

		directive = default;
		return false;
	}

	private static bool HasKeywordBoundary(SourceText source, int position)
		=> position >= source.Length || char.IsWhiteSpace(source[position]);

	private static bool Matches(SourceText source, int start, string value)
	{
		if (start + value.Length > source.Length)
		{
			return false;
		}

		for (var index = 0; index < value.Length; index++)
		{
			if (source[start + index] != value[index])
			{
				return false;
			}
		}

		return true;
	}

	private readonly struct DirectiveKeyword(string text, RazorDirectiveKind kind)
	{
		public string Text { get; } = text;

		public RazorDirectiveKind Kind { get; } = kind;
	}
}
