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
			var methodDeclarations = scan.Attributes
				.Where(attribute => attribute.Name == binding.AttributeName && !RazorBindingValue.IsEmpty(attribute.Value))
				.Select(candidate => Parse(componentTypeName, additionalFile.Path, text, binding, candidate, owner))
				.ToList();
			declarations.AddRange(RejectDifferentHandlers(methodDeclarations, binding, owner));
		}

		return declarations.ToImmutable();
	}

	// One HTTP method maps to one handler. Bindings that name the same handler are one action; when they name
	// different handlers, every binding that names one fails, so no handler is chosen silently.
	private static IEnumerable<HtmxorComponentActionDeclaration> RejectDifferentHandlers(
		IReadOnlyList<HtmxorComponentActionDeclaration> declarations,
		ActionBinding binding,
		RouteOwner owner)
	{
		var handlers = declarations
			.Select(static declaration => declaration.HandlerName)
			.OfType<string>()
			.Distinct(StringComparer.Ordinal)
			.Select(static handler => "'" + handler + "'")
			.ToList();
		if (handlers.Count < 2)
		{
			return declarations;
		}

		var reason = binding.AttributeName + " binds different handlers " +
			string.Join(", ", handlers.Take(handlers.Count - 1)) + " and " + handlers[handlers.Count - 1] +
			"; bind one handler per HTTP method";
		return declarations.Select(declaration => declaration.HandlerName is null
			? declaration
			: Unsupported(declaration.ComponentTypeName, binding, owner, declaration.Path, declaration.Span, declaration.LineSpan, reason));
	}

	private static HtmxorComponentActionDeclaration Parse(
		string componentTypeName,
		string path,
		SourceText text,
		ActionBinding binding,
		MarkupAttribute candidate,
		RouteOwner owner)
	{
		var span = new TextSpan(candidate.Index, binding.AttributeName.Length);
		var lineSpan = text.Lines.GetLinePositionSpan(span);
		var handlerName = RazorBindingValue.TryReadHandler(candidate.Value);
		var reason = GetUnsupportedReason(binding, candidate, handlerName);
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
				lineSpan,
				unsupportedReason: null)
			: Unsupported(componentTypeName, binding, owner, path, span, lineSpan, reason);
	}

	private static string? GetUnsupportedReason(
		ActionBinding binding,
		MarkupAttribute candidate,
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
		TextSpan span,
		LinePositionSpan lineSpan,
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
			lineSpan,
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
