using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Http;

namespace Htmxor.TestAssets.Alba;

public sealed class SemanticHtmlContentBodyAssertion : IScenarioAssertion
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
			var parser = new HtmlParser();
			using var document = parser.ParseDocument(received);

			if (cssSelector is null)
			{
				using var expectedDocument = parser.ParseDocument(Expected);
				document.DocumentElement.MarkupMatches(expectedDocument.DocumentElement.OuterHtml);
				return;
			}

			document.QuerySelectorAll(cssSelector).MarkupMatches(Expected);
		}
		catch (HtmlEqualException exception)
		{
			ex.Add($"Response body does not contain the expected HTML:{Environment.NewLine}{exception.Message}");
		}
	}
}
