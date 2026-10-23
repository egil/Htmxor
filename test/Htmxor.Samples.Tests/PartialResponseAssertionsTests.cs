namespace Htmxor.Samples.Tests;

public sealed class PartialResponseAssertionsTests
{
	private const string Contact = "833d4663-229f-4079-a121-05597498e90d";
	private const string Fragment = "<div id=\"counter\"><p role=\"status\">0</p><button hx-put=\"/counter\">Update</button></div>";

	[Theory]
	[InlineData("form", "sidebar")]
	[InlineData("div", "table-and-form")]
	[InlineData("div", "counter")]
	public void Root_identity_allows_attributes_before_id(string tag, string id)
	{
		var body = $"<{tag} class=\"sample\" data-kind=\"selected\" id=\"{id}\">selected</{tag}>";

		PartialResponseAssertions.IsSingleFragment(body, tag, "selected", "unselected", id);
	}

	[Theory]
	[InlineData("<div id=\"wrong\">selected</div>")]
	[InlineData("<div>selected</div>")]
	[InlineData("<div data-id=\"counter\">selected</div>")]
	[InlineData("<div><span id=\"counter\">selected</span></div>")]
	[InlineData("<form id=\"counter\">selected</form>")]
	public void Different_root_identity_is_rejected(string body)
	{
		var failure = Record.Exception(() => PartialResponseAssertions.IsSingleFragment(body, "div", "selected", "unselected", id: "counter"));

		Assert.NotNull(failure);
		Assert.Contains("Assert.Equal()", failure.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Outlet_fragments_allow_attributes_before_ids()
	{
		var body = "<div class=\"sample\" id=\"counter\"><p role=\"status\">1</p><button hx-put=\"/examples/out-of-band-outlets\">Update</button></div>"
			+ "<div hx-swap-oob=\"afterbegin:#toast-container\">Counter is now 1</div>"
			+ "<span class=\"sample\" id=\"message\" hx-swap-oob=\"outerHTML\">The count is odd!</span>";

		PartialResponseAssertions.AreCounterToastAndMessageFragments(body, "The count is odd!");
	}

	[Theory]
	[InlineData("")]
	[InlineData("\n")]
	[InlineData(" \r\n\t")]
	public void Selected_fragment_allows_surrounding_whitespace(string whitespace)
	{
		PartialResponseAssertions.IsSingleFragment(whitespace + Fragment + whitespace, "div", "role=\"status\"", "<h1>", id: "counter");
	}

	[Theory]
	[InlineData("prefix<div>selected</div>", "Only selected top-level elements may appear in")]
	[InlineData("<div>selected</div>suffix", "Only selected top-level elements may appear after")]
	[InlineData("<div>selected</div><aside></aside>", "Assert.Single()")]
	[InlineData("<div>selected", "Assert.Empty()")]
	[InlineData("<div>selected</aside>", "Assert.Equal()")]
	[InlineData("<div>selected</div", "complete HTML tags")]
	[InlineData("<!DOCTYPE html><div>selected</div>", "document envelope")]
	[InlineData("<>selected</>", "unnamed HTML tag")]
	public void Unselected_or_incomplete_output_is_rejected(string body, string diagnostic)
	{
		var failure = Record.Exception(() => PartialResponseAssertions.IsSingleFragment(body, "div", "selected", "<h1>"));

		Assert.NotNull(failure);
		Assert.Contains(diagnostic, failure.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Contact_observation_uses_the_data_row_after_the_heading()
	{
		var page = $"<table><thead><tr><th>Id</th></tr></thead><tbody>\n<tr><th>{Contact}</th><td>Ada</td></tr></tbody></table>";

		Assert.Equal(Contact, PartialResponseAssertions.ContactIdentifier(page));
		PartialResponseAssertions.AreRowFragments($"<tr><th>{Contact}</th><td>Ada</td></tr>\n<tr hx-get=\"?page=1\"><td>Loading</td></tr>\n", Contact, "?page=1");
	}

	[Fact]
	public void Heading_is_not_a_contact_identifier()
	{
		var failure = Record.Exception(() => PartialResponseAssertions.ContactIdentifier("<tbody><tr><th>Id</th></tr></tbody>"));

		Assert.NotNull(failure);
		Assert.Contains("contact GUID", failure.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Identifier_on_loader_attributes_is_not_a_contact_row()
	{
		var failure = Record.Exception(() => PartialResponseAssertions.AreRowFragments($"<tr data-id=\"{Contact}\" hx-get=\"?page=1\"><td>Loading</td></tr>", Contact, "?page=1"));

		Assert.NotNull(failure);
		Assert.Contains("Assert.Contains()", failure.Message, StringComparison.Ordinal);
	}
}
