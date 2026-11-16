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
	// (a GET) or before submit dispatch (a POST). The synchronous POST pins whatever stock answers once the
	// abandoned first render has left no form to dispatch to.
	[Theory]
	[InlineData("GET", "sync")]
	[InlineData("GET", "async")]
	[InlineData("POST", "sync")]
	[InlineData("POST", "async")]
	public async Task Ordinary_initial_render_redirect_has_stock_response_and_write_back_parity(string method, string mode)
	{
		await using var stock = await Issue230Host.StartAsync(htmxor: false);
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var stockResponse = await stock.SendAsync(method, mode, htmx: false);
		var stockSurvivors = await stock.ReadAsync();
		var candidateResponse = await candidate.SendAsync(method, mode, htmx: false);
		var candidateSurvivors = await candidate.ReadAsync();

		// Stock's own oracle, pinned so the comparison never passes vacuously.
		Assert.Equal(Issue230Host.ReadPath, stockResponse.Location);
		if (method == "GET")
		{
			Assert.Equal(HttpStatusCode.Found, stockResponse.Status);
			Assert.Equal(Written(mode), stockSurvivors);
		}

		Assert.Equal(stockResponse, candidateResponse);
		Assert.Equal(stockSurvivors, candidateSurvivors);
	}

	// htmx has no stock counterpart for the representation: an htmx request cannot follow a 302 for a partial
	// response, so every entry point answers 200 with HX-Redirect. What survives must still equal what stock
	// keeps for the same journey.
	[Theory]
	[InlineData("GET", "sync")]
	[InlineData("GET", "async")]
	[InlineData("POST", "async")]
	public async Task Htmx_initial_render_redirect_asks_the_client_to_navigate_and_keeps_stock_write_back(string method, string mode)
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
		Assert.Equal(stockSurvivors, survivors);
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

	// A percent-encoded non-ASCII path is not a well-formed URI reference to .NET 11's Uri, so Htmxor's own
	// destination validator rejects it there and HX-Redirect cannot carry it. Such a destination keeps the stock
	// representation rather than failing the request, as a non-HTTP destination does; .NET 10 accepts it.
	[Fact]
	public async Task Htmx_redirect_to_a_non_ascii_destination_never_fails_the_request()
	{
		await using var stock = await Issue230Host.StartAsync(htmxor: false);
		await using var candidate = await Issue230Host.StartAsync(htmxor: true);

		var stockResponse = await stock.SendAsync("GET", "sync", htmx: true, destination: "/issue-230/é");
		var candidateResponse = await candidate.SendAsync("GET", "sync", htmx: true, destination: "/issue-230/é");

		Assert.Equal(HttpStatusCode.Found, stockResponse.Status);
#if NET11_0_OR_GREATER
		Assert.Equal(stockResponse, candidateResponse);
#else
		Assert.Equal(HttpStatusCode.OK, candidateResponse.Status);
		Assert.Equal(stockResponse.Location, candidateResponse.HxRedirect);
		Assert.Null(candidateResponse.Location);
#endif
	}

	private static Issue230Survivors Written(string mode)
	{
#if NET11_0_OR_GREATER
		var written = $"written-{mode}";
#else
		var written = mode.Length > 0 ? Issue230Carrier.Missing : mode;
#endif
		return new(written, written);
	}
}
