using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #266's own paired stock-against-candidate first-render redirect-representation cases, compiled for both
// net10.0 and net11.0 (see the csproj). The switch stays off throughout (this repository's default, throwing
// NavigationManager); #264's separately hosted switch-on evidence is not reused here. These cases reuse
// Issue190's external and internal navigation pages and lifecycle journal -- now a both-target fixture, see
// Issue190NavigationFixtures.cs -- through Issue260Host/Issue260App rather than
// Issue190NavigationParityTests.cs's own net10.0-only Issue187ParityHost, and reuse Issue264's
// request-building, header-normalizing, and opaque-redirect-following helpers (Issue264SwitchOnSupport.cs),
// which already compile for both targets.
//
// The observation seam is deliberately narrow: HTTP status plus the Location, blazor-enhanced-nav-redirect-
// location, and HX-Redirect headers. Stock's own first-render NavigationException handling does not return
// early (RazorComponentEndpointInvoker.RenderComponentCore keeps running after
// EndpointHtmlRenderer.HandleNavigationException returns empty content), while the candidate's current catch
// does. That gives stock a chance to write further body content and headers the candidate's early return never
// reaches; matching those is #230's "continue the pipeline" work, not this issue's redirect-representation
// split, and is called out in #266's own residual risk. Comparing the whole body or the whole header set here
// would therefore fail for a reason unrelated to the protected behavior, on both sides of the real fix.
public sealed class Issue266ExternalFirstRenderRedirectTests
{
	// Issue190NavigationPage.razor's own hardcoded @page route; there is no shared constant for it because,
	// unlike the external contract, no other test currently needs one.
	private const string InternalNavigationPath = "/issue-190/navigate";

	// Blazor's NavigationManager resolves the page's relative "/issue-190/destination" literal to an absolute
	// URI before it ever reaches Response.Redirect, so both stock and the candidate redirect to this exact
	// absolute form, not the page's own relative literal.
	private const string InternalNavigationDestinationPath = "/issue-190/destination";

	// The red case: stock answers an external first-render destination under progressively-enhanced navigation
	// with 200 and an opaque blazor-enhanced-nav-redirect-location header
	// (EndpointHtmlRenderer.Prerendering.HandleNavigationBeforeResponseStarted); at HEAD the candidate's first-
	// render catch always calls the bare context.Response.Redirect, so it answers 302 with no such header.
	[Fact]
	public async Task External_destination_first_render_navigation_under_enhanced_navigation_has_stock_opaque_redirect_parity()
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		using var stockResponse = await SendAsync(stock, Issue190ExternalNavigationContract.Path, enhancedNavigation: true);
		using var candidateResponse = await SendAsync(candidate, Issue190ExternalNavigationContract.Path, enhancedNavigation: true);

		// Stock's own oracle, pinned before any parity comparison so this case never passes vacuously.
		Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
		Assert.Null(stockResponse.Headers.Location);
		var stockOpaqueLocation = SingleOpaqueRedirectHeader(stockResponse);
		Assert.NotNull(stockOpaqueLocation);
		Assert.Equal(
			new Uri(Issue190ExternalNavigationContract.Destination),
			await FollowOpaqueRedirectAsync(stock.Client, stockOpaqueLocation));

		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(stockResponse.Headers.Location, candidateResponse.Headers.Location);
		var candidateOpaqueLocation = SingleOpaqueRedirectHeader(candidateResponse);
		Assert.NotNull(candidateOpaqueLocation);
		// The protected value is host-specific (each host owns its own ephemeral data-protection key, see
		// Issue260Host), so it is compared after unprotecting it through each host's own
		// "_framework/opaque-redirect" endpoint rather than by string equality on the token itself.
		Assert.Equal(
			new Uri(Issue190ExternalNavigationContract.Destination),
			await FollowOpaqueRedirectAsync(candidate.Client, candidateOpaqueLocation));
	}

	// Characterization: a same-origin destination never reaches stock's opaque-redirect branch
	// (IsPossibleExternalDestination is false), so enhanced navigation makes no difference and both stock and
	// the candidate already answer the same ordinary 302.
	[Fact]
	public async Task Internal_destination_first_render_navigation_under_enhanced_navigation_already_matches_stock()
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		using var stockResponse = await SendAsync(stock, InternalNavigationPath, enhancedNavigation: true);
		using var candidateResponse = await SendAsync(candidate, InternalNavigationPath, enhancedNavigation: true);

		Assert.Equal(HttpStatusCode.Found, stockResponse.StatusCode);
		Assert.NotNull(stockResponse.Headers.Location);
		Assert.EndsWith(InternalNavigationDestinationPath, stockResponse.Headers.Location!.AbsolutePath, StringComparison.Ordinal);
		Assert.Null(SingleOpaqueRedirectHeader(stockResponse));

		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(stockResponse.Headers.Location, candidateResponse.Headers.Location);
		Assert.Null(SingleOpaqueRedirectHeader(candidateResponse));
	}

	// Characterization: without progressively-enhanced navigation, IsProgressivelyEnhancedNavigation is false
	// regardless of the destination, so stock's HandleNavigationBeforeResponseStarted falls through to the same
	// ordinary 302 the candidate's current bare catch already produces.
	[Fact]
	public async Task External_destination_first_render_navigation_without_enhanced_navigation_already_matches_stock()
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		using var stockResponse = await SendAsync(stock, Issue190ExternalNavigationContract.Path, enhancedNavigation: false);
		using var candidateResponse = await SendAsync(candidate, Issue190ExternalNavigationContract.Path, enhancedNavigation: false);

		Assert.Equal(HttpStatusCode.Found, stockResponse.StatusCode);
		Assert.Equal(Issue190ExternalNavigationContract.Destination, stockResponse.Headers.Location?.OriginalString);
		Assert.Null(SingleOpaqueRedirectHeader(stockResponse));

		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(stockResponse.Headers.Location, candidateResponse.Headers.Location);
		Assert.Null(SingleOpaqueRedirectHeader(candidateResponse));
	}

	// Guard, not a change: htmx has no stock counterpart (stock never inspects IsHtmxRequest), so -- as #264
	// decided for its own htmx cases -- this pins the candidate's own current output instead of comparing to
	// stock. It must still pass unchanged once #266 splits the first-render catch, because an htmx request
	// keeps context.Response.Redirect; it exists so a wrong split -- routing htmx through
	// HandleNavigationBeforeResponseStarted too (producing HX-Redirect instead), or always using that method
	// regardless of IsHtmxRequest (producing the opaque-redirect header instead) -- fails here instead of only
	// being caught by a browser.
	[Fact]
	public async Task Htmx_external_destination_first_render_navigation_keeps_the_candidates_bare_redirect()
	{
		await using var candidate = await CreateHostAsync(htmxor: true);

		using var response = await candidate.Client.SendAsync(
			Issue264SwitchOnRequests.Create(HttpMethod.Get, Issue190ExternalNavigationContract.Path, htmx: true));

		Assert.Equal(HttpStatusCode.Found, response.StatusCode);
		Assert.Equal(Issue190ExternalNavigationContract.Destination, response.Headers.Location?.OriginalString);
		Assert.False(response.Headers.Contains("HX-Redirect"));
		Assert.Null(SingleOpaqueRedirectHeader(response));
	}

	private static Task<Issue260Host> CreateHostAsync(bool htmxor) =>
		Issue260Host.CreateAsync<Issue260App>(
			htmxor,
			configureServices: services => services.AddSingleton<Issue190LifecycleJournal>());

	private static Task<HttpResponseMessage> SendAsync(Issue260Host host, string path, bool enhancedNavigation) =>
		host.Client.SendAsync(Issue264SwitchOnRequests.Create(HttpMethod.Get, path, enhancedNavigation: enhancedNavigation));

	private static string? SingleOpaqueRedirectHeader(HttpResponseMessage response) =>
		response.Headers.TryGetValues("blazor-enhanced-nav-redirect-location", out var values) ? values.Single() : null;

	// Follows the opaque redirect on the same host that protected it: each Issue260Host owns its own ephemeral
	// data-protection key (see Issue260Host), so the token is meaningful only against the host that issued it.
	private static async Task<Uri?> FollowOpaqueRedirectAsync(HttpClient client, string? opaqueUrl)
	{
		Assert.NotNull(opaqueUrl);
		using var response = await client.GetAsync(opaqueUrl);
		return response.Headers.Location;
	}
}
