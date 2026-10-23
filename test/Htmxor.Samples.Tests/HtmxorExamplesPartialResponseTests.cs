using System.Net;
using System.Text.RegularExpressions;

namespace Htmxor.Samples.Tests;

public sealed class HtmxorExamplesPartialResponseTests
{
	[Fact]
	public async Task Partial_get_archive_toggle_returns_the_selected_contact_button()
	{
		await using var host = await StartAsync();
		var page = await host.Client.GetStringAsync("/examples/archive-toggle");
		var path = Regex.Match(page, "hx-patch=\"(?<path>[^\"]+)\"", RegexOptions.CultureInvariant).Groups["path"].Value;
		Assert.False(string.IsNullOrWhiteSpace(path));

		var body = await GetPartialAsync(host, path);

		PartialResponseAssertions.IsSingleFragment(body, "button", "hx-patch=\"/examples/archive-toggle/", "<h1>Contact Archived Toggle</h1>");
	}

	[Fact]
	public async Task Partial_get_out_of_band_responses_returns_the_contact_form()
	{
		await using var host = await StartAsync();

		var body = await GetPartialAsync(host, "/examples/updating-other-content/out-of-band-responses");

		PartialResponseAssertions.IsSingleFragment(body, "form", "id=\"firstName\"", "<h1>Updating Other Content - Out of Band Responses</h1>");
	}

	[Fact]
	public async Task Partial_get_expand_the_target_returns_the_table_and_form_fragment()
	{
		await using var host = await StartAsync();

		var body = await GetPartialAsync(host, "/examples/updating-other-content/expand-the-target");

		PartialResponseAssertions.IsSingleFragment(body, "div", "<form", "<h1>Updating Other Content - Expand the Target</h1>", id: "table-and-form");
	}

	[Fact]
	public async Task Partial_get_infinite_scroll_returns_the_next_page_rows()
	{
		await using var host = await StartAsync();
		var page = await host.Client.GetStringAsync("/examples/infinite-scroll?page=0");
		var contact = PartialResponseAssertions.ContactIdentifier(page);

		var body = await GetPartialAsync(host, "/examples/infinite-scroll?page=0");

		PartialResponseAssertions.AreRowFragments(body, contact, "hx-get=\"/examples/infinite-scroll?page=1\"");
	}

	private static Task<SampleKestrelHost> StartAsync() => SampleKestrelHost.StartAsync("samples/HtmxorExamples/HtmxorExamples.csproj");

	private static async Task<string> GetPartialAsync(SampleKestrelHost host, string path)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}
}
