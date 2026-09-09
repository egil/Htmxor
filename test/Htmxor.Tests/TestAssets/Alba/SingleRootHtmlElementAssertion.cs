using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Http;

namespace Htmxor.TestAssets.Alba;

public sealed class SingleRootHtmlElementAssertion(string cssSelector) : IScenarioAssertion
{
	public void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex)
	{
		using var document = new HtmlParser().ParseDocument(ex.ReadBody(context));
		var rootElements = document.Body?.Children;
		if (rootElements?.Length != 1 || !rootElements[0].Matches(cssSelector))
		{
			ex.Add($"Response body must have exactly one root element matching '{cssSelector}'.");
		}
	}
}
