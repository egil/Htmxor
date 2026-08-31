using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Htmxor.TestAssets.Alba;

public sealed partial class SemanticHtmlContentBodyAssertion : IScenarioAssertion
{
	private readonly string? cssSelector;

	public string Expected { get; }

	public SemanticHtmlContentBodyAssertion(string? cssSelector, string expected)
	{
		this.cssSelector = cssSelector;
		Expected = expected;
	}

	public void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex)
	{
		var received = ex.ReadBody(context);

		try
		{
			AssertMatches(received, cssSelector, Expected);
		}
		catch (HtmlEqualException exception)
		{
			ex.Add($"Response body does not contain the expected HTML:{Environment.NewLine}{exception.Message}");
		}
	}

	internal static void AssertMatches(string received, string? cssSelector, string expected)
	{
		if (cssSelector is null)
		{
			RemoveLeadingDocumentType(received).MarkupMatches(RemoveLeadingDocumentType(expected));
			return;
		}

		var parser = new HtmlParser();
		using var document = parser.ParseDocument(received);
		document.QuerySelectorAll(cssSelector).MarkupMatches(expected);
	}

	private static string RemoveLeadingDocumentType(string markup)
	{
		return LeadingDocumentType().Replace(markup, string.Empty, count: 1);
	}

	[GeneratedRegex(@"\A\s*<!DOCTYPE[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex LeadingDocumentType();
}
