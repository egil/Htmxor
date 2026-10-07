using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class HtmxorComponentActionDeclaration
{
	private const string ImplicitMethod = "GET";

	// @onget is listed only so it is reported: Razor compiles it, but GET needs no declaration.
	private static readonly ActionBinding[] SupportedBindings =
	{
		new("@onget", ImplicitMethod),
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
		string? handlerAccess,
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
		HandlerAccess = handlerAccess;
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

	// The handler as the author wrote it (M or this.M), which is the expression Razor binds.
	public string? HandlerAccess { get; }

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
				.Where(attribute => attribute.Name == binding.AttributeName && !RazorBindingValue.IsEmpty(attribute.Value))
				.ToList();
			foreach (var candidate in candidates)
			{
				declarations.Add(Parse(
					componentTypeName,
					additionalFile.Path,
					text,
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
		ActionBinding binding,
		MarkupAttribute candidate,
		RouteOwner owner,
		int methodDeclarationCount)
	{
		var span = new TextSpan(candidate.Index, binding.AttributeName.Length);
		var handlerName = RazorBindingValue.TryReadHandler(candidate.Value);
		var reason = GetUnsupportedReason(binding, candidate, methodDeclarationCount, handlerName);
		return reason is null
			? new HtmxorComponentActionDeclaration(
				componentTypeName,
				binding.AttributeName,
				binding.HttpMethod,
				handlerName,
				RazorBindingValue.TryReadHandlerAccess(candidate.Value),
				owner,
				path,
				span,
				text.Lines.GetLinePositionSpan(span),
				unsupportedReason: null)
			: Unsupported(componentTypeName, binding, owner, path, text, span, reason);
	}

	private static string? GetUnsupportedReason(
		ActionBinding binding,
		MarkupAttribute candidate,
		int methodDeclarationCount,
		string? handlerName)
	{
		if (binding.HttpMethod == ImplicitMethod)
		{
			return binding.AttributeName + " declares nothing: GET is implicit, so remove the binding";
		}

		if (!candidate.InOwnerMarkup)
		{
			return binding.AttributeName +
				" in a Razor template or @code markup is not supported; put the binding in the component's own markup";
		}

		if (methodDeclarationCount > 1)
		{
			return "at most one " + binding.AttributeName + " binding per component is supported";
		}

		return handlerName is null
			? binding.AttributeName + " must name a handler method, not " +
				RazorBindingValue.GetUnsupportedCause(candidate.Value)
			: null;
	}

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
			handlerAccess: null,
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
		}

		public string AttributeName { get; }

		public string HttpMethod { get; }
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
