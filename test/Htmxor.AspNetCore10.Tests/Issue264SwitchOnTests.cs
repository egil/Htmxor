using System.Net;

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
// Every host proves it actually observed the switch value its mode requires before Issue264SwitchOnFixture
// hands it out, and htmx cases compare the switch-on candidate to Htmxor's own switch-off (throwing-path)
// candidate rather than to stock, per the owner's decision:
// https://github.com/egil/Htmxor/issues/264#issuecomment-5901861030.
public sealed class Issue264SwitchOnTests : IClassFixture<Issue264SwitchOnFixture>, IAsyncLifetime
{
	private readonly Issue264SwitchOnFixture fixture;

	public Issue264SwitchOnTests(Issue264SwitchOnFixture fixture) => this.fixture = fixture;

	// The candidate host process, and its output queue, are shared across every test in this class (see
	// Issue264SwitchOnFixture). A test that fails on an assertion before it reaches its own
	// AssertNoUnobservedNavigationExceptionAsync call never drains the lines its own request produced, so
	// xUnit constructing a fresh instance of this class per test is used here to discard that backlog before
	// each test's own request, rather than letting an earlier test's failure be misattributed to a later one.
	public async Task InitializeAsync() => await fixture.Candidate.DrainRecentOutputAsync();

	public Task DisposeAsync() => Task.CompletedTask;

	// Acceptance criterion 4, pinned on its own: every other case below also checks this, but each of them
	// fails on an earlier assertion at 965886b, so without this case the drain-and-attribute path itself has
	// no committed red.
	[Fact]
	public async Task No_unobserved_navigation_exception_after_a_switch_on_navigation()
	{
		using var response = await fixture.Candidate.Client.GetAsync("/issue-264/sync?ForceLoad=false");
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Synchronous_OnInitialized_navigation_before_start_has_stock_redirect_parity(bool forceLoad)
	{
		var path = $"/issue-264/sync?ForceLoad={forceLoad}";
		var stock = await SendAsync(fixture.Stock, path);
		var candidate = await SendAsync(fixture.Candidate, path);

		AssertRedirectParity(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Non_streaming_pending_work_navigation_before_start_has_stock_redirect_parity()
	{
		var stock = await SendAsync(fixture.Stock, "/issue-264/pending");
		var candidate = await SendAsync(fixture.Candidate, "/issue-264/pending");

		AssertRedirectParity(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Completed_form_submit_navigation_has_stock_redirect_parity()
	{
		var stock = await Issue264SwitchOnRequests.SubmitFormAsync(fixture.Stock.Client, destination: null);
		var candidate = await Issue264SwitchOnRequests.SubmitFormAsync(fixture.Candidate.Client, destination: null);

		AssertRedirectParity(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Theory]
	[InlineData(false)] // internal destination: the ordinary redirect branch, plus the ssr-framing header
	[InlineData(true)] // external destination: the opaque-redirect branch
	public async Task Enhanced_navigation_sync_before_start_has_stock_parity(bool external)
	{
		var destination = external
			? Issue264SwitchOnConstants.ExternalDestination
			: Issue264SwitchOnConstants.InternalDestination;
		var path = $"/issue-264/sync?Destination={Uri.EscapeDataString(destination)}";
		var stock = await SendAsync(fixture.Stock, path, enhancedNavigation: true);
		var candidate = await SendAsync(fixture.Candidate, path, enhancedNavigation: true);

		if (external)
		{
			await AssertOpaqueRedirectParityAsync(stock, candidate, destination);
		}
		else
		{
			AssertRedirectParity(stock, candidate);
			Assert.True(stock.HasSsrFraming, "Stock must frame an enhanced-navigation redirect with ssr-framing.");
			Assert.True(candidate.HasSsrFraming, "The candidate must match stock's ssr-framing under enhanced navigation.");
		}

		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Enhanced_navigation_form_submit_external_destination_has_stock_opaque_redirect_parity()
	{
		var destination = Issue264SwitchOnConstants.ExternalDestination;
		var stock = await Issue264SwitchOnRequests.SubmitFormAsync(fixture.Stock.Client, destination, enhancedNavigation: true);
		var candidate = await Issue264SwitchOnRequests.SubmitFormAsync(fixture.Candidate.Client, destination, enhancedNavigation: true);

		await AssertOpaqueRedirectParityAsync(stock, candidate, destination);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Enhanced_navigation_pending_work_before_start_has_stock_redirect_parity()
	{
		var stock = await SendAsync(fixture.Stock, "/issue-264/pending", enhancedNavigation: true);
		var candidate = await SendAsync(fixture.Candidate, "/issue-264/pending", enhancedNavigation: true);

		AssertRedirectParity(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	// htmx has no stock counterpart: the switch-on candidate's oracle is Htmxor's own switch-off
	// (throwing-path) candidate, for the same request. See the owner's decision on #264 linked above.
	[Fact]
	public async Task Htmx_first_render_get_has_switch_off_candidate_parity()
	{
		const string path = "/issue-264/sync?ForceLoad=false";
		var off = await SendAsync(fixture.CandidateSwitchOff, path, htmx: true);
		var on = await SendAsync(fixture.Candidate, path, htmx: true);

		AssertHtmxSwitchOffOracleIsBareRedirect(off);
		Assert.Equal(off.StatusCode, on.StatusCode);
		Assert.Equal(RelativeToHost(fixture.CandidateSwitchOff, off.Location), RelativeToHost(fixture.Candidate, on.Location));
		Assert.Null(on.HxRedirect);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Htmx_form_submit_has_switch_off_candidate_parity()
	{
		var off = await Issue264SwitchOnRequests.SubmitFormAsync(fixture.CandidateSwitchOff.Client, destination: null, htmx: true);
		var on = await Issue264SwitchOnRequests.SubmitFormAsync(fixture.Candidate.Client, destination: null, htmx: true);

		AssertHtmxSwitchOffOracleIsHxRedirect(off);
		Assert.Equal(off.StatusCode, on.StatusCode);
		Assert.Equal(off.HxRedirect, on.HxRedirect); // already request-relative on both sides; no host math needed
		Assert.Null(on.Location);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	[Fact]
	public async Task Streaming_navigation_after_response_started_has_stock_redirection_template_parity()
	{
		var stock = await RunStreamingCaseAsync(fixture.Stock);
		var candidate = await RunStreamingCaseAsync(fixture.Candidate);

		AssertStreamingStopsAtRedirection(stock);
		AssertStreamingStopsAtRedirection(candidate);
		// The incremental checks above only look for the redirection template's text; they would pass a bare
		// template written outside stock's <blazor-ssr>/<blazor-ssr-end> frame, which blazor.web.js would never
		// act on. Comparing the whole normalized body catches that, per criterion 3's "whole body".
		Assert.Equal(stock.WholeBody, candidate.WholeBody);
		await AssertStreamingDestinationParityAsync(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	private static Task<Issue264Snapshot> SendAsync(
		Issue264SwitchOnHostProcess host, string path, bool htmx = false, bool enhancedNavigation = false)
		=> Issue264SwitchOnRequests.SendAsync(host.Client, Issue264SwitchOnRequests.Create(HttpMethod.Get, path, htmx, enhancedNavigation));

	// A candidate that has exact stock parity redirects on its own ephemeral port, so only the host-relative
	// part of a redirect Location is comparable across two independent processes.
	private static string? RelativeToHost(Issue264SwitchOnHostProcess host, Uri? location) =>
		location is null ? null : host.BaseAddress.MakeRelativeUri(location).ToString();

	private void AssertRedirectParity(Issue264Snapshot stock, Issue264Snapshot candidate)
	{
		AssertStockShowsSwitchOnRedirect(stock);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(RelativeToHost(fixture.Stock, stock.Location), RelativeToHost(fixture.Candidate, candidate.Location));
		Assert.Equal(stock.ContentType, candidate.ContentType);
		Assert.Equal(stock.Body, candidate.Body);
	}

	// Stock with the switch off answers the same request with an empty body (Content-Length: 0); a rendered
	// body alongside the 302 is itself part of the proof that this stock host observed the switch as on, in
	// addition to the fixture-level ObservedSwitch check every case already relies on.
	private static void AssertStockShowsSwitchOnRedirect(Issue264Snapshot stock)
	{
		Assert.Equal(HttpStatusCode.Found, stock.StatusCode);
		Assert.NotNull(stock.Location);
		Assert.Contains("data-issue-264", stock.Body, StringComparison.Ordinal);
	}

	private static void AssertHtmxSwitchOffOracleIsBareRedirect(Issue264Snapshot off)
	{
		Assert.Equal(HttpStatusCode.Found, off.StatusCode);
		Assert.NotNull(off.Location);
		Assert.Null(off.HxRedirect);
	}

	private static void AssertHtmxSwitchOffOracleIsHxRedirect(Issue264Snapshot off)
	{
		Assert.Equal(HttpStatusCode.OK, off.StatusCode);
		Assert.NotNull(off.HxRedirect);
		Assert.Null(off.Location);
	}

	private async Task AssertOpaqueRedirectParityAsync(Issue264Snapshot stock, Issue264Snapshot candidate, string expectedDestination)
	{
		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.NotNull(stock.EnhancedNavigationLocation);
		Assert.Null(stock.Location);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.NotNull(candidate.EnhancedNavigationLocation);
		Assert.Null(candidate.Location);

		var stockDestination = await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(fixture.Stock, stock.EnhancedNavigationLocation!);
		var candidateDestination = await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(fixture.Candidate, candidate.EnhancedNavigationLocation!);
		Assert.Equal(new Uri(expectedDestination), stockDestination);
		Assert.Equal(new Uri(expectedDestination), candidateDestination);
	}

	private static async Task<Issue264StreamingSnapshot> RunStreamingCaseAsync(Issue264SwitchOnHostProcess host)
	{
		using var response = await host.Client.GetAsync("/issue-264/streaming", HttpCompletionOption.ResponseHeadersRead);
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);

		var initial = await reader.ReadUntilAsync(["data-issue-264-streaming=\"initial\""]);
		// A missed precondition (the initial render never arriving) must fail here, not be reported later as a
		// missing redirection: ReadUntilAsync returns whatever it has on timeout, so an empty or partial
		// `initial` would otherwise blame the protected behavior for a broken setup.
		Assert.Contains("data-issue-264-streaming=\"initial\"", initial, StringComparison.Ordinal);
		await host.ReleaseStreamingNavigateAsync();

		var beforeResume = await reader.ReadUntilAsync(["<template type=\"redirection\">", "<template blazor-component-id"]);
		await host.ReleaseStreamingResumeAsync();

		var wholeBody = await reader.ReadToEndOrTimeoutAsync(TimeSpan.FromSeconds(1));
		return new(
			response.StatusCode,
			BeforeResume: beforeResume[initial.Length..],
			AfterResume: wholeBody[beforeResume.Length..],
			WholeBody: Issue264Snapshot.NormalizeProtectedPayloads(wholeBody));
	}

	// Stock's post-start navigation writes the redirection template and stops the renderer without needing the
	// test to release the second gate at all; a candidate that silently dropped the navigation instead reaches
	// this point with nothing new, then only streams the forbidden component update once resume is released.
	private static void AssertStreamingStopsAtRedirection(Issue264StreamingSnapshot snapshot)
	{
		Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
		Assert.Contains("<template type=\"redirection\">", snapshot.BeforeResume, StringComparison.Ordinal);
		Assert.DoesNotContain("updated-after-navigate", snapshot.BeforeResume, StringComparison.Ordinal);
		Assert.Equal(string.Empty, snapshot.AfterResume);
	}

	private async Task AssertStreamingDestinationParityAsync(Issue264StreamingSnapshot stock, Issue264StreamingSnapshot candidate)
	{
		var stockUrl = Issue264SwitchOnRequests.ExtractRedirectionTemplateUrl(stock.BeforeResume);
		var candidateUrl = Issue264SwitchOnRequests.ExtractRedirectionTemplateUrl(candidate.BeforeResume);
		var stockDestination = await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(fixture.Stock, stockUrl);
		var candidateDestination = await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(fixture.Candidate, candidateUrl);

		Assert.Equal("issue-264/streaming-destination", RelativeToHost(fixture.Stock, stockDestination));
		Assert.Equal(RelativeToHost(fixture.Stock, stockDestination), RelativeToHost(fixture.Candidate, candidateDestination));
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

	private sealed record Issue264StreamingSnapshot(HttpStatusCode StatusCode, string BeforeResume, string AfterResume, string WholeBody);
}
