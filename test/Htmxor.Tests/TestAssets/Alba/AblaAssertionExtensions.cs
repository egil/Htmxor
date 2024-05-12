using System.Diagnostics.CodeAnalysis;

namespace Htmxor.TestAssets.Alba;

public static class AblaAssertionExtensions
{
	/// <summary>
	/// Assert that the HTTP response body is parsable as Html and is semantically equivalent to
	/// the <paramref name="expectedJson"/> JSON string.
	/// </summary>
	/// <param name="expectedJson">The expected JSON string</param>
	public static Scenario ContentShouldBeHtml(this Scenario scenario, [StringSyntax("Html")] string expected)
	{
		return scenario.AssertThat(new SemanticHtmlContentBodyAssertion(null, expected));
	}

	/// <summary>
	/// Assert that the HTTP response body that matches the <paramref name="cssSelector"/> is parsable as Html and is semantically equivalent to
	/// the <paramref name="expectedJson"/> JSON string.
	/// </summary>
	/// <param name="expectedJson">The expected JSON string</param>
	public static Scenario ContentShouldHaveElementsEqualTo(
		this Scenario scenario,
		string cssSelector,
		[StringSyntax("Html")] string expected)
	{
		return scenario.AssertThat(new SemanticHtmlContentBodyAssertion(cssSelector, expected));
	}
}
