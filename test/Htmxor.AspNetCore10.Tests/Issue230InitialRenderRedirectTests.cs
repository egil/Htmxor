using System.Net;

namespace Htmxor.AspNetCore10;

// When a component redirects during its initial render, Htmxor preserves the stock TempData and Session
// write-back and the stock redirect representation. Each case runs one browser's journey against a real hosted
// pipeline and reads what survived on a fresh following request, because an early return that skips persistence
// is invisible on the redirect response itself. TempData and Session parameters exist only on .NET 11; on
// .NET 10 the same cases prove the representation and the survivors are "missing" on both hosts.
public sealed class Issue230InitialRenderRedirectTests
{
	private const string CrossOrigin = "https://idp.example/login";

	// "sync" throws inside the first render; "async" throws from the pending work awaited before the response
	// (a GET) or before submit dispatch (a POST); "abandoned" throws inside the first render from a child whose
	// parent has pending work that later fails, which stock never waits for. A synchronous POST leaves no form
	// to dispatch to, so stock answers 400 and returns before write-back; an asynchronous POST still dispatches
	// the submit, whose write is the one that survives.
	[Theory]
	[InlineData("GET", "sync", HttpStatusCode.Found, "sync")]
	[InlineData("GET", "async", HttpStatusCode.Found, "async")]
	[InlineData("GET", "abandoned", HttpStatusCode.Found, "abandoned")]
	[InlineData("POST", "sync", HttpStatusCode.BadRequest, null)]
	[InlineData("POST", "async", HttpStatusCode.Found, "submit")]
	public async Task Ordinary_initial_render_redirect_has_stock_response_and_write_back_parity(
		string method, string mode, HttpStatusCode stockStatus, string? survivingWrite)
	{
		await using var stock = await Issue230Host.StartAsync(htmxor: false);
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var stockResponse = await stock.SendAsync(method, mode, htmx: false);
		var stockSurvivors = await stock.ReadAsync();
		var candidateResponse = await candidate.SendAsync(method, mode, htmx: false);
		var candidateSurvivors = await candidate.ReadAsync();

		// Stock's own oracle, pinned so the comparison never passes vacuously.
		Assert.Equal(stockStatus, stockResponse.Status);
		Assert.Equal(Issue230Host.ReadPath, stockResponse.Location);
		Assert.Equal(Written(survivingWrite), stockSurvivors);
		Assert.Equal(stockResponse, candidateResponse);
		Assert.Equal(stockSurvivors, candidateSurvivors);
	}

	// htmx has no stock counterpart for the representation: an htmx request cannot follow a 302 for a partial
	// response, so every entry point answers 200 with HX-Redirect. What survives must still equal what stock
	// keeps for the same journey.
	[Theory]
	[InlineData("GET", "sync", "sync")]
	[InlineData("GET", "async", "async")]
	[InlineData("GET", "abandoned", "abandoned")]
	[InlineData("POST", "async", "submit")]
	public async Task Htmx_initial_render_redirect_asks_the_client_to_navigate_and_keeps_stock_write_back(
		string method, string mode, string survivingWrite)
	{
		await using var stock = await Issue230Host.StartAsync(htmxor: false);
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		await stock.SendAsync(method, mode, htmx: false);
		var stockSurvivors = await stock.ReadAsync();
		var response = await candidate.SendAsync(method, mode, htmx: true);
		var survivors = await candidate.ReadAsync();

		Assert.Equal(HttpStatusCode.OK, response.Status);
		Assert.Equal(Issue230Host.ReadPath, response.HxRedirect);
		Assert.Null(response.Location);
		Assert.Empty(response.Body);
		Assert.Equal(Written(survivingWrite), stockSurvivors);
		Assert.Equal(stockSurvivors, survivors);
	}

	// Stock's 400 for a POST whose abandoned first render left no form is kept for htmx too; the navigation
	// itself was still answered in the htmx representation before dispatch rejected the submit.
	[Fact]
	public async Task Htmx_post_whose_first_render_navigated_keeps_stock_bad_request()
	{
		await using var stock = await Issue230Host.StartAsync(htmxor: false);
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var stockResponse = await stock.SendAsync("POST", "sync", htmx: false);
		var response = await candidate.SendAsync("POST", "sync", htmx: true);

		Assert.Equal(HttpStatusCode.BadRequest, stockResponse.Status);
		Assert.Equal(stockResponse.Status, response.Status);
		Assert.Equal(Issue230Host.ReadPath, response.HxRedirect);
		Assert.Null(response.Location);
		Assert.Equal(await stock.ReadAsync(), await candidate.ReadAsync());
	}

	// A generated action is not a POST, so its redirect never reaches the submit-dispatch guard. Stock maps no
	// PUT, so the oracle is the issue's representation rule and the written values themselves.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Generated_action_redirect_uses_the_request_representation_and_keeps_written_values(bool htmx)
	{
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var response = await candidate.SendAsync("PUT", "action", htmx);
		var survivors = await candidate.ReadAsync();

		Assert.Equal(htmx ? HttpStatusCode.OK : HttpStatusCode.Found, response.Status);
		Assert.Equal(Issue230Host.ReadPath, htmx ? response.HxRedirect : response.Location);
		Assert.Null(htmx ? response.Location : response.HxRedirect);
		Assert.Empty(response.Body);
		Assert.Equal(Written("action"), survivors);
	}

	// HX-Redirect carries any http(s) destination, so a cross-origin sign-in redirect reaches an htmx client
	// from the first render and from a generated action alike.
	[Theory]
	[InlineData("GET", "sync")]
	[InlineData("PUT", "action")]
	public async Task Htmx_cross_origin_redirect_asks_the_client_to_navigate(string method, string mode)
	{
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var response = await candidate.SendAsync(method, mode, htmx: true, destination: CrossOrigin);
		var survivors = await candidate.ReadAsync();

		Assert.Equal(HttpStatusCode.OK, response.Status);
		Assert.Equal(CrossOrigin, response.HxRedirect);
		Assert.Null(response.Location);
		Assert.Equal(Written(mode), survivors);
	}

	// #266's enhanced-navigation representation must survive continuing past the navigation into write-back.
	[Fact]
	public async Task Enhanced_navigation_external_redirect_keeps_stock_opaque_representation_and_write_back()
	{
		await using var stock = await Issue230Host.StartAsync(htmxor: false);
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var stockResponse = await stock.SendAsync("GET", "sync", htmx: false, CrossOrigin, enhancedNavigation: true);
		var stockSurvivors = await stock.ReadAsync();
		var candidateResponse = await candidate.SendAsync("GET", "sync", htmx: false, CrossOrigin, enhancedNavigation: true);
		var candidateSurvivors = await candidate.ReadAsync();

		Assert.Equal(HttpStatusCode.OK, stockResponse.Status);
		Assert.True(stockResponse.HasEnhancedNavigationLocation);
		Assert.Equal(Written("sync"), stockSurvivors);
		Assert.Equal(stockResponse, candidateResponse);
		Assert.Equal(stockSurvivors, candidateSurvivors);
	}

	// .NET 11's Uri does not accept the request-relative form of a percent-encoded non-ASCII path as well formed,
	// so Htmxor's own destination validator rejects it there. The absolute form then carries the destination,
	// so the htmx request still gets a redirect it can follow on both frameworks.
	[Fact]
	public async Task Htmx_redirect_to_a_non_ascii_destination_still_asks_the_client_to_navigate()
	{
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var response = await candidate.SendAsync("GET", "sync", htmx: true, destination: "/issue-230/é");

		Assert.Equal(HttpStatusCode.OK, response.Status);
		Assert.EndsWith("/issue-230/%C3%A9", response.HxRedirect, StringComparison.Ordinal);
		Assert.Null(response.Location);
	}

	private static Issue230Survivors Written(string? write)
	{
#if NET11_0_OR_GREATER
		var written = write is null ? Issue230Carrier.Missing : $"written-{write}";
#else
		// No TempData or Session parameter exists on .NET 10, so nothing a page writes can survive there.
		_ = write;
		var written = Issue230Carrier.Missing;
#endif
		return new(written, written);
	}
}
