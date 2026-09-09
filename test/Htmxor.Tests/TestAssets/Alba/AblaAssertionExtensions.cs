using System.Diagnostics.CodeAnalysis;

namespace Htmxor.TestAssets.Alba;

public static class AblaAssertionExtensions
{
	/// <summary>
	/// Assert that the HTTP response body is parsable as Html and is semantically equivalent to
	/// the <paramref name="expected"/> HTML string.
	/// </summary>
	/// <param name="expected">The expected HTML.</param>
	public static Scenario ContentShouldBeHtml(this Scenario scenario, [StringSyntax("Html")] string expected)
	{
		return scenario.AssertThat(new SemanticHtmlContentBodyAssertion(null, expected));
	}

	/// <summary>
	/// Assert that the HTTP response body has exactly one root element that matches <paramref name="cssSelector"/>.
	/// </summary>
	/// <param name="cssSelector">The CSS selector for the required root element.</param>
	public static Scenario ContentShouldHaveSingleRootElement(this Scenario scenario, string cssSelector)
	{
		return scenario.AssertThat(new SingleRootHtmlElementAssertion(cssSelector));
	}

	/// <summary>
	/// Assert that response elements matching <paramref name="cssSelector"/> are semantically equivalent to <paramref name="expected"/>.
	/// </summary>
	/// <param name="cssSelector">The CSS selector for response elements.</param>
	/// <param name="expected">The expected HTML.</param>
	public static Scenario ContentShouldHaveElementsEqualTo(
		this Scenario scenario,
		string cssSelector,
		[StringSyntax("Html")] string expected)
	{
		return scenario.AssertThat(new SemanticHtmlContentBodyAssertion(cssSelector, expected));
	}
}
