using System.Diagnostics.CodeAnalysis;

namespace Htmxor.TestAssets;

public static class AblaAssertionExtensions
{
    /// <summary>
    /// Assert that the HTTP response body is parsable as JSON and is semantically equivalent to
    /// the <paramref name="expectedJson"/> JSON string.
    /// </summary>
    /// <param name="expectedJson">The expected JSON string</param>
    public static Scenario ContentShouldBeHtml(this Scenario scenario, [StringSyntax("Html")] string expected)
    {
        return scenario.AssertThat(new SemanticHtmlContentBodyAssertion(expected));
    }
}
