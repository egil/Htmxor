using System.Net;
using System.Text.RegularExpressions;

namespace Htmxor.AspNetCore10;

// With Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException on,
// HttpNavigationManager.NavigateToCore never throws NavigationException; it instead invokes the
// endpoint-based navigation callback the three-argument IHostEnvironmentNavigationManager.Initialize supplies.
// At 965886b, HtmxorEndpointCandidate.InitializeStandardComponentServicesAsync calls the two-argument overload,
// so that callback is never supplied: every switch-on navigation is silently dropped, the component finishes
// rendering as if NavigateTo had never been called, and the abandoned callback invocation raises an unobserved
// InvalidOperationException. See https://github.com/egil/Htmxor/issues/264 and its reproduction at
// https://github.com/egil/Htmxor/issues/192#issuecomment-5901461146.
//
// Every case here proves the paired stock host actually observed switch-on behavior (a 302 with no exception)
// before trusting the candidate's parity, per the issue's Verification contract.
public sealed class Issue264SwitchOnTests : IClassFixture<Issue264SwitchOnFixture>
{
	private readonly Issue264SwitchOnFixture fixture;

	public Issue264SwitchOnTests(Issue264SwitchOnFixture fixture) => this.fixture = fixture;

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Synchronous_OnInitialized_navigation_before_start_has_stock_redirect_parity(bool forceLoad)
	{
		var path = $"/issue-264/sync?ForceLoad={forceLoad}";

		using var stockResponse = await fixture.Stock.Client.GetAsync(path);
		AssertStockObservedSwitchOn(stockResponse);

		using var candidateResponse = await fixture.Candidate.Client.GetAsync(path);

		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(stockResponse.Headers.Location, candidateResponse.Headers.Location);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Non_streaming_pending_work_navigation_before_start_has_stock_redirect_parity()
	{
		const string path = "/issue-264/pending";

		using var stockResponse = await fixture.Stock.Client.GetAsync(path);
		AssertStockObservedSwitchOn(stockResponse);

		using var candidateResponse = await fixture.Candidate.Client.GetAsync(path);

		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(stockResponse.Headers.Location, candidateResponse.Headers.Location);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Completed_form_submit_navigation_has_stock_redirect_parity()
	{
		var stockSubmit = await SubmitFormAsync(fixture.Stock.Client);
		AssertStockObservedSwitchOn(stockSubmit);

		var candidateSubmit = await SubmitFormAsync(fixture.Candidate.Client);

		Assert.Equal(stockSubmit.StatusCode, candidateSubmit.StatusCode);
		Assert.Equal(stockSubmit.Headers.Location, candidateSubmit.Headers.Location);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task External_navigation_under_enhanced_navigation_has_stock_opaque_redirect_parity()
	{
		var path = "/issue-264/sync?Destination=" +
			Uri.EscapeDataString("https://example.invalid/issue-264/destination");
		using var stockRequest = new HttpRequestMessage(HttpMethod.Get, path);
		stockRequest.Headers.Add("Accept", "text/html; blazor-enhanced-nav=on");
		using var stockResponse = await fixture.Stock.Client.SendAsync(stockRequest);
		AssertStockObservedSwitchOn(stockResponse, expectOpaqueRedirect: true);

		using var candidateRequest = new HttpRequestMessage(HttpMethod.Get, path);
		candidateRequest.Headers.Add("Accept", "text/html; blazor-enhanced-nav=on");
		using var candidateResponse = await fixture.Candidate.Client.SendAsync(candidateRequest);

		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Null(candidateResponse.Headers.Location);
		Assert.True(
			candidateResponse.Headers.TryGetValues("blazor-enhanced-nav-redirect-location", out _),
			"The candidate must ask for an opaque redirection, the same way stock does for an external " +
			"destination under progressively enhanced navigation.");
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Streaming_navigation_after_response_started_has_stock_redirection_template_parity()
	{
		using var stockResponse = await fixture.Stock.Client.GetAsync(
			"/issue-264/streaming", HttpCompletionOption.ResponseHeadersRead);
		var stockBody = NormalizeProtectedPayloads(await stockResponse.Content.ReadAsStringAsync());
		Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
		Assert.Contains("<template type=\"redirection\">", stockBody, StringComparison.Ordinal);
		Assert.DoesNotContain("updated-after-navigate", stockBody, StringComparison.Ordinal);

		using var candidateResponse = await fixture.Candidate.Client.GetAsync(
			"/issue-264/streaming", HttpCompletionOption.ResponseHeadersRead);
		var candidateBody = NormalizeProtectedPayloads(await candidateResponse.Content.ReadAsStringAsync());

		// The candidate must stop rendering at the same point stock does: no component update is allowed to
		// stream after the redirection template, so the whole normalized body matches stock's exactly.
		Assert.Equal(stockBody, candidateBody);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	private static async Task<HttpResponseMessage> SubmitFormAsync(HttpClient client)
	{
		using var page = await client.GetAsync("/issue-264/form");
		var body = await page.Content.ReadAsStringAsync();
		var token = Regex.Match(body, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
		Assert.True(token.Success, "The switch-on host must issue a real antiforgery request token.");
		using var request = new HttpRequestMessage(HttpMethod.Post, "/issue-264/form");
		if (page.Headers.TryGetValues("Set-Cookie", out var cookies))
		{
			request.Headers.Add("Cookie", string.Join("; ", cookies.Select(cookie => cookie.Split(';')[0])));
		}

		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "issue-264-go",
			["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
		});
		return await client.SendAsync(request);
	}

	// A stock host that never observed switch-on behavior (still throwing, so still 302 but for the wrong
	// reason if the runtimeconfig switch failed to apply) would silently invalidate every candidate comparison
	// in the same case. Every switch-on assertion here goes through this oracle first.
	private static void AssertStockObservedSwitchOn(HttpResponseMessage stockResponse, bool expectOpaqueRedirect = false)
	{
		if (expectOpaqueRedirect)
		{
			Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
			Assert.True(
				stockResponse.Headers.TryGetValues("blazor-enhanced-nav-redirect-location", out _),
				"Stock must show switch-on, opaque-redirect behavior for this external, enhanced-navigation " +
				"case; otherwise this pairing proves nothing about the candidate.");
			return;
		}

		Assert.Equal(HttpStatusCode.Found, stockResponse.StatusCode);
		Assert.NotNull(stockResponse.Headers.Location);
	}

	private static async Task AssertNoUnobservedNavigationExceptionAsync(Issue264SwitchOnHostProcess host)
	{
		var lines = await host.DrainRecentOutputAsync();
		var unobserved = lines.Where(line => line.StartsWith("UNOBSERVED ", StringComparison.Ordinal)).ToArray();
		Assert.True(
			unobserved.Length == 0,
			"No InvalidOperationException about uninitialized endpoint-based navigation may go unobserved: " +
			string.Join("; ", unobserved));
	}

	private static string NormalizeProtectedPayloads(string body) => Regex.Replace(
		body,
		"(_framework/opaque-redirect\\?url=|value=\")CfDJ8[^\"<]+",
		"$1<protected>",
		RegexOptions.CultureInvariant);
}
