using Bunit;

namespace Htmxor.TestAssets.Alba;

public sealed class SemanticHtmlContentBodyAssertionTests
{
	[Fact]
	public void Equivalent_fragments_match()
	{
		SemanticHtmlContentBodyAssertion.AssertMatches(
			"<section><strong>ready</strong></section>",
			cssSelector: null,
			"<section> <strong>ready</strong> </section>");
	}

	[Fact]
	public void Document_type_is_ignored()
	{
		SemanticHtmlContentBodyAssertion.AssertMatches(
			"<!doctype html><html><body><main>ready</main></body></html>",
			cssSelector: null,
			"<html><body><main>ready</main></body></html>");
	}

	[Fact]
	public void Fragment_does_not_match_full_document()
	{
		var action = () => SemanticHtmlContentBodyAssertion.AssertMatches(
			"<html><body><main>ready</main></body></html>",
			cssSelector: null,
			"<main>ready</main>");

		action.Should().Throw<HtmlEqualException>();
	}

	[Fact]
	public void Selector_compares_only_matching_elements()
	{
		SemanticHtmlContentBodyAssertion.AssertMatches(
			"<main><p>ignored</p><section data-fragment>ready</section></main>",
			"[data-fragment]",
			"<section data-fragment>ready</section>");
	}

	[Fact]
	public void Semantic_mismatch_is_reported()
	{
		var action = () => SemanticHtmlContentBodyAssertion.AssertMatches(
			"<section>actual</section>",
			cssSelector: null,
			"<section>expected</section>");

		action.Should().Throw<HtmlEqualException>();
	}
}
