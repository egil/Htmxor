using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #266's own paired stock-against-candidate first-render redirect-representation cases, compiled for both
// net10.0 and net11.0 (see the csproj). The switch stays off throughout (this repository's default, throwing
// NavigationManager). These cases reuse Issue190's external and internal navigation pages and lifecycle
// journal -- now a both-target fixture, see Issue190NavigationFixtures.cs -- through Issue260Host/Issue260App
// rather than Issue190NavigationParityTests.cs's own net10.0-only Issue187ParityHost, and reuse Issue264's
// request-building, snapshot, and opaque-redirect-following helpers (Issue264SwitchOnSupport.cs), which
// already compile for both targets.
//
// Each paired case compares the whole Issue264Snapshot -- status, Location, HX-Redirect, Content-Type,
// ssr-framing presence, and the normalized body -- with the opaque-redirect header masked to null. That header
// is masked because each Issue260Host owns an independent ephemeral data-protection key (see Issue260Host), so
// its protected value is never equal across hosts even when both redirect to the same destination; it is
// instead compared by following each host's own token through its own "_framework/opaque-redirect" endpoint.
public sealed class Issue266ExternalFirstRenderRedirectTests
{
	// Issue190NavigationPage.razor's @page route. Issue190NavigationHost.Path holds the same value, but it lives in
	// the net10.0-only Issue190NavigationParityTests.cs.
	private const string InternalNavigationPath = "/issue-190/navigate";

	// Issue190NavigationPage.razor's relative destination. NavigationManager resolves it against the request's base
	// URI, so both stock and the candidate send an absolute Location whose path is this value.
	private const string InternalNavigationDestinationPath = "/issue-190/destination";

	// Stock answers an external first-render destination under progressively-enhanced navigation with 200 and an
	// opaque blazor-enhanced-nav-redirect-location header
	// (EndpointHtmlRenderer.Prerendering.HandleNavigationBeforeResponseStarted) instead of a 302.
	[Fact]
	public async Task External_destination_first_render_navigation_under_enhanced_navigation_has_stock_opaque_redirect_parity()
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		var stockSnapshot = await SendAsync(stock, Issue190ExternalNavigationContract.Path, enhancedNavigation: true);
		var candidateSnapshot = await SendAsync(candidate, Issue190ExternalNavigationContract.Path, enhancedNavigation: true);

		// Stock's own oracle, pinned before any parity comparison so this case never passes vacuously.
		Assert.Equal(HttpStatusCode.OK, stockSnapshot.StatusCode);
		Assert.Null(stockSnapshot.Location);
		Assert.NotNull(stockSnapshot.EnhancedNavigationLocation);
		Assert.Equal(
			new Uri(Issue190ExternalNavigationContract.Destination),
			await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(stock.Client, stockSnapshot.EnhancedNavigationLocation!));

		Assert.NotNull(candidateSnapshot.EnhancedNavigationLocation);
		AssertPairedSnapshotParity(stockSnapshot, candidateSnapshot);
		Assert.Equal(
			new Uri(Issue190ExternalNavigationContract.Destination),
			await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(candidate.Client, candidateSnapshot.EnhancedNavigationLocation!));
	}

	// Characterization: a same-origin destination never reaches stock's opaque-redirect branch
	// (IsPossibleExternalDestination is false), so enhanced navigation makes no difference and both stock and
	// the candidate answer the same ordinary 302.
	[Fact]
	public async Task Internal_destination_first_render_navigation_under_enhanced_navigation_has_stock_redirect_parity()
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		var stockSnapshot = await SendAsync(stock, InternalNavigationPath, enhancedNavigation: true);
		var candidateSnapshot = await SendAsync(candidate, InternalNavigationPath, enhancedNavigation: true);

		Assert.Equal(HttpStatusCode.Found, stockSnapshot.StatusCode);
		Assert.NotNull(stockSnapshot.Location);
		Assert.EndsWith(InternalNavigationDestinationPath, stockSnapshot.Location!.AbsolutePath, StringComparison.Ordinal);
		Assert.Null(stockSnapshot.EnhancedNavigationLocation);

		AssertPairedSnapshotParity(stockSnapshot, candidateSnapshot);
	}

	// Characterization: without progressively-enhanced navigation, IsProgressivelyEnhancedNavigation is false
	// regardless of the destination, so stock's HandleNavigationBeforeResponseStarted falls through to the same
	// ordinary 302 the candidate's bare catch produces.
	[Fact]
	public async Task External_destination_first_render_navigation_without_enhanced_navigation_has_stock_redirect_parity()
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		var stockSnapshot = await SendAsync(stock, Issue190ExternalNavigationContract.Path, enhancedNavigation: false);
		var candidateSnapshot = await SendAsync(candidate, Issue190ExternalNavigationContract.Path, enhancedNavigation: false);

		Assert.Equal(HttpStatusCode.Found, stockSnapshot.StatusCode);
		Assert.Equal(Issue190ExternalNavigationContract.Destination, stockSnapshot.Location?.OriginalString);
		Assert.Null(stockSnapshot.EnhancedNavigationLocation);

		AssertPairedSnapshotParity(stockSnapshot, candidateSnapshot);
	}

	// Guard: htmx has no stock counterpart (stock never inspects IsHtmxRequest), so, as #264 decided for its own
	// htmx cases, this pins the candidate's own output instead of comparing to stock. A first-render htmx
	// navigation keeps the bare redirect; sending it through HandleNavigationBeforeResponseStarted would answer
	// 200 with HX-Redirect instead, and this case catches that rather than leaving it to a browser.
	[Fact]
	public async Task Htmx_external_destination_first_render_navigation_keeps_the_candidates_bare_redirect()
	{
		await using var candidate = await CreateHostAsync(htmxor: true);

		var snapshot = await SendAsync(candidate, Issue190ExternalNavigationContract.Path, htmx: true);

		Assert.Equal(HttpStatusCode.Found, snapshot.StatusCode);
		Assert.Equal(Issue190ExternalNavigationContract.Destination, snapshot.Location?.OriginalString);
		Assert.Null(snapshot.HxRedirect);
		Assert.Null(snapshot.EnhancedNavigationLocation);
	}

	private static Task<Issue260Host> CreateHostAsync(bool htmxor) =>
		Issue260Host.CreateAsync<Issue260App>(
			htmxor,
			configureServices: services => services.AddSingleton<Issue190LifecycleJournal>());

	private static Task<Issue264Snapshot> SendAsync(
		Issue260Host host, string path, bool enhancedNavigation = false, bool htmx = false) =>
		Issue264SwitchOnRequests.SendAsync(
			host.Client, Issue264SwitchOnRequests.Create(HttpMethod.Get, path, htmx, enhancedNavigation));

	// The opaque-redirect header is masked before comparison; see this file's header comment for why.
	private static void AssertPairedSnapshotParity(Issue264Snapshot stock, Issue264Snapshot candidate) =>
		Assert.Equal(stock with { EnhancedNavigationLocation = null }, candidate with { EnhancedNavigationLocation = null });
}
