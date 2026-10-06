using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

	private static readonly ISet<string> SupportedAttributeNames = new HashSet<string>(
		SupportedBindings.Select(static binding => binding.AttributeName),
		StringComparer.Ordinal);

	private HtmxorComponentActionDeclaration(
		string componentTypeName,
		string attributeName,
		string httpMethod,
		string? handlerName,
		RouteOwner owner,
		string path,
		TextSpan span,
		LinePositionSpan lineSpan,
		string? unsupportedReason)
	{
		ComponentTypeName = componentTypeName;
		AttributeName = attributeName;
		HttpMethod = httpMethod;
		HandlerName = handlerName;
		UsesStockRoute = owner.UsesStockRoute;
		RouteTemplate = owner.RouteTemplate;
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
		var scan = RazorMarkupScanner.Scan(source, SupportedAttributeNames);
		var owner = new RouteOwner(
			usesStockRoute: scan.PageDirectiveCount > 0,
			routeTemplate: scan.AttributeDirectives
				.Select(TryReadOmittedHtmxRoute)
				.FirstOrDefault(static template => template is not null));
		var declarations = ImmutableArray.CreateBuilder<HtmxorComponentActionDeclaration>();
		foreach (var binding in SupportedBindings)
		{
			var candidates = scan.Attributes
				.Where(attribute => attribute.Name == binding.AttributeName)
				.ToList();
			foreach (var candidate in candidates)
			{
				declarations.Add(Parse(
					componentTypeName,
					additionalFile.Path,
					text,
					source,
					binding,
					candidate,
					owner,
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
		RouteOwner owner,
		int methodDeclarationCount)
	{
		var attributeIndex = candidate.Index;
		var span = new TextSpan(attributeIndex, binding.AttributeName.Length);
		var placementReason = GetPlacementReason(binding, candidate, methodDeclarationCount);
		if (placementReason is not null)
		{
			return Unsupported(componentTypeName, binding, owner, path, text, span, placementReason);
		}

		var match = binding.SupportedBinding.Match(source, attributeIndex);
		return match.Success &&
			match.Index == attributeIndex &&
			IsBindingTerminator(source, match.Index + match.Length)
			? new HtmxorComponentActionDeclaration(
				componentTypeName,
				binding.AttributeName,
				binding.HttpMethod,
				match.Groups["handler"].Value,
				owner,
				path,
				span,
				text.Lines.GetLinePositionSpan(span),
				unsupportedReason: null)
			: Unsupported(
				componentTypeName,
				binding,
				owner,
				path,
				text,
				span,
				binding.AttributeName + " must use one double-quoted simple method-group name");
	}

	private static string? GetPlacementReason(
		ActionBinding binding,
		MarkupAttribute candidate,
		int methodDeclarationCount)
		=> !candidate.InOwnerMarkup
			? binding.AttributeName +
				" in a Razor template or @code markup is not supported; put the binding in the component's own markup"
			: methodDeclarationCount > 1
				? "at most one " + binding.AttributeName + " binding per component is supported"
				: null;

	private static string? TryReadOmittedHtmxRoute(string attributeDirective)
	{
		string[] prefixes =
		{
			"[HtmxRoute(\"",
			"[Htmxor.HtmxRoute(\"",
			"[global::Htmxor.HtmxRoute(\"",
		};
		const string suffix = "\")]";
		foreach (var prefix in prefixes)
		{
			if (!attributeDirective.StartsWith(prefix, StringComparison.Ordinal) ||
				!attributeDirective.EndsWith(suffix, StringComparison.Ordinal))
			{
				continue;
			}

			var value = attributeDirective.Substring(
				prefix.Length,
				attributeDirective.Length - prefix.Length - suffix.Length);
			if (value.Length > 0 && value.IndexOf('"') < 0)
			{
				return value;
			}
		}

		return null;
	}

	private static bool IsBindingTerminator(string source, int index)
		=> index == source.Length ||
			source[index] == '>' ||
			source[index] == '/' ||
			char.IsWhiteSpace(source[index]);

	private static HtmxorComponentActionDeclaration Unsupported(
		string componentTypeName,
		ActionBinding binding,
		RouteOwner owner,
		string path,
		SourceText text,
		TextSpan span,
		string reason)
		=> new(
			componentTypeName,
			binding.AttributeName,
			binding.HttpMethod,
			handlerName: null,
			owner,
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

	// The route owner is read from the whole file: a local @page, or an HtmxRoute literal without Methods.
	private sealed class RouteOwner
	{
		public RouteOwner(bool usesStockRoute, string? routeTemplate)
		{
			UsesStockRoute = usesStockRoute;
			RouteTemplate = routeTemplate;
		}

		public bool UsesStockRoute { get; }

		public string? RouteTemplate { get; }
	}
}
