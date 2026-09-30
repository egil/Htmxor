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

	// Every host process, and its output queue, is shared across every test in this class (see
	// Issue264SwitchOnFixture). A test that fails on an assertion before it reaches its own drain-based check
	// (AssertNoUnobservedNavigationExceptionAsync, or an Error-log count) never drains the lines its own
	// request produced, and each host also has its own one-time startup noise (observed: a benign
	// "Hosting startup assembly exception" from Microsoft.AspNetCore.Hosting.Diagnostics). xUnit constructing
	// a fresh instance of this class per test is used here to discard all of that backlog before each test's
	// own request, on every host, rather than letting it be misattributed to a later, unrelated test.
	public async Task InitializeAsync()
	{
		await fixture.Stock.DrainRecentOutputAsync();
		await fixture.Candidate.DrainRecentOutputAsync();
		await fixture.CandidateSwitchOff.DrainRecentOutputAsync();
	}

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
		const string path = "/issue-264/streaming";
		var stock = await RunStreamingCaseAsync(fixture.Stock, path);
		var candidate = await RunStreamingCaseAsync(fixture.Candidate, path);

		AssertStreamingStopsAtRedirection(stock);
		AssertStreamingStopsAtRedirection(candidate);
		// The incremental checks above only look for the redirection template's text; they would pass a bare
		// template written outside stock's <blazor-ssr>/<blazor-ssr-end> frame, which blazor.web.js would never
		// act on. Comparing the whole normalized body catches that, per criterion 3's "whole body".
		Assert.Equal(stock.WholeBody, candidate.WholeBody);
		await AssertStreamingDestinationParityAsync(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	// LR-08402d5-P001: SignalRendererToFinishRendering is a *deferred* stop. When a child's synchronous
	// OnInitialized navigates while its parent's post-start render batch is still being built, that batch is
	// already past ProcessPendingRender's gate and reaches UpdateDisplayAsync with the stop flag already set;
	// only UpdateDisplayAsync's own "!rendererIsStopped" check keeps it from streaming after the redirection
	// template. Without that check this batch streams a <blazor-ssr> update after the template and this case
	// fails; the Issue264StreamingPage case never reaches that check.
	[Fact]
	public async Task Streaming_child_navigation_during_post_start_render_batch_has_stock_redirection_template_parity()
	{
		const string path = "/issue-264/streaming-child-navigate";
		const string initialMarker = "data-issue-264-streaming-child=\"initial\"";
		var stock = await RunStreamingCaseAsync(fixture.Stock, path, initialMarker);
		var candidate = await RunStreamingCaseAsync(fixture.Candidate, path, initialMarker);

		AssertStreamingStopsAtRedirection(stock);
		AssertStreamingStopsAtRedirection(candidate);
		Assert.Equal(stock.WholeBody, candidate.WholeBody);
		await AssertStreamingDestinationParityAsync(stock, candidate);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	// PR #265 review discussion_r4140656921: if OpaqueRedirection.CreateProtectedRedirectionUrl throws while
	// OnNavigateTo builds the after-start redirection template, no part of that template may reach the wire;
	// stock builds the whole template in a buffer first, so a failed attempt leaves nothing on the wire.
	// This needs a data-protection provider that throws only for the opaque-redirection purpose, which is
	// orthogonal to every other switch-on case's real protector, so it uses a dedicated pair of hosts started
	// and disposed only by this test rather than the shared fixture.
	[Fact]
	public async Task Streaming_navigation_with_failed_opaque_redirection_protection_has_no_malformed_template_parity()
	{
		await using var stockHost = await Issue264SwitchOnHostProcess.StartAsync(
			"stock", Issue264SwitchOnConstants.ThrowOnOpaqueRedirectionProtectFlag);
		await using var candidateHost = await Issue264SwitchOnHostProcess.StartAsync(
			"candidate", Issue264SwitchOnConstants.ThrowOnOpaqueRedirectionProtectFlag);

		var stockBody = await RunFaultedStreamingCaseAsync(stockHost);
		var candidateBody = await RunFaultedStreamingCaseAsync(candidateHost);

		// Stock's own oracle: even though this navigation fails, stock never emits a bare, unclosed
		// redirection template -- confirmed live (not just by source reading) against this exact fault.
		Assert.DoesNotContain("<template type=\"redirection\">", stockBody, StringComparison.Ordinal);
		Assert.Equal(stockBody, candidateBody);
	}

	// LR-08402d5-P002: neither oracle answers this case. Stock has no htmx concept, and the switch-off
	// (throwing) candidate gives 500 for pending-work navigation regardless of htmx, which is #260's own
	// defect (see #192 comment 5901861239), not a valid switch-off answer. This case therefore asserts the
	// owner's decided output directly, per
	// https://github.com/egil/Htmxor/issues/264#issuecomment-5902791174: htmx navigation from pending work
	// gets HX-Redirect, the same as a completed submit; only first-render navigation keeps the bare 302.
	[Fact]
	public async Task Htmx_pending_work_navigation_gets_hx_redirect_per_decision()
	{
		var on = await SendAsync(fixture.Candidate, "/issue-264/pending", htmx: true);

		Assert.Equal(HttpStatusCode.OK, on.StatusCode);
		Assert.Equal(Issue264SwitchOnConstants.InternalDestination, on.HxRedirect);
		Assert.Null(on.Location);
		await AssertNoUnobservedNavigationExceptionAsync(fixture.Candidate);
	}

	// LR-08402d5-P003: before #264, the throwing path could reach HandleNavigationBeforeResponseStarted at
	// most once per request, so a second external NavigateTo was unreachable. With the switch on, both calls
	// run OnNavigateTo. Stock's HandleNavigationBeforeResponseStarted uses Headers.Add, so the second call
	// throws (logged by GetErrorHandledTask) and the header keeps stock's first value. This reads the raw
	// header values directly, rather than through Issue264Snapshot.EnhancedNavigationLocation, because that
	// property assumes exactly one value and a second value is exactly the defect under test.
	[Fact]
	public async Task Double_external_navigation_under_enhanced_navigation_has_stock_single_header_value_parity()
	{
		const string path = "/issue-264/sync-double-external";
		using var stockRequest = Issue264SwitchOnRequests.Create(HttpMethod.Get, path, enhancedNavigation: true);
		using var stockResponse = await fixture.Stock.Client.SendAsync(stockRequest);
		using var candidateRequest = Issue264SwitchOnRequests.Create(HttpMethod.Get, path, enhancedNavigation: true);
		using var candidateResponse = await fixture.Candidate.Client.SendAsync(candidateRequest);

		var stockValues = EnhancedNavigationRedirectHeaderValues(stockResponse);
		var candidateValues = EnhancedNavigationRedirectHeaderValues(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
		Assert.Single(stockValues); // stock's own oracle: exactly one value, even though NavigateTo ran twice
		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(stockValues.Count, candidateValues.Count);
		// Stock's own oracle: GetErrorHandledTask logs the second Headers.Add's exception at Error level, the
		// only surviving signal that a conflicting redirect occurred once the header itself shows only one
		// value. See PR #265 review discussion_r4140656885.
		await AssertExactlyOneErrorLoggedAsync(fixture.Stock);
		await AssertExactlyOneErrorLoggedAsync(fixture.Candidate);
		var stockDestination = await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(fixture.Stock, stockValues[0]);
		var candidateDestination = await Issue264SwitchOnRequests.FollowOpaqueRedirectAsync(fixture.Candidate, candidateValues[0]);
		Assert.Equal(new Uri("https://example.invalid/issue-264/one"), stockDestination); // stock keeps the first
		Assert.Equal(stockDestination, candidateDestination);
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

	private static Task<Issue264StreamingSnapshot> RunStreamingCaseAsync(Issue264SwitchOnHostProcess host, string path) =>
		RunStreamingCaseAsync(host, path, initialMarker: "data-issue-264-streaming=\"initial\"");

	private static async Task<Issue264StreamingSnapshot> RunStreamingCaseAsync(
		Issue264SwitchOnHostProcess host, string path, string initialMarker)
	{
		using var response = await host.Client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);

		var initial = await reader.ReadUntilAsync([initialMarker]);
		// A missed precondition (the initial render never arriving) must fail here, not be reported later as a
		// missing redirection: ReadUntilAsync returns whatever it has on timeout, so an empty or partial
		// `initial` would otherwise blame the protected behavior for a broken setup.
		Assert.Contains(initialMarker, initial, StringComparison.Ordinal);
		await ReleaseAndRequireSuccessAsync(host.ReleaseStreamingNavigateAsync);

		var beforeResume = await reader.ReadUntilAsync(["<template type=\"redirection\">", "<template blazor-component-id"]);
		await ReleaseAndRequireSuccessAsync(host.ReleaseStreamingResumeAsync);

		// Only a genuine end-of-stream counts as completion here (see Issue264StreamingBodyReader.ReadToEndAsync):
		// a failed release, a delayed continuation, or a hung response now fails this case instead of silently
		// passing with an empty tail.
		var wholeBody = await reader.ReadToEndAsync(TimeSpan.FromSeconds(2));
		return new(
			response.StatusCode,
			BeforeResume: beforeResume[initial.Length..],
			AfterResume: wholeBody[beforeResume.Length..],
			WholeBody: Issue264Snapshot.NormalizeProtectedPayloads(wholeBody));
	}

	// Unlike RunStreamingCaseAsync, this does not wait for a marker between the two gate releases: under fault
	// injection, stock never emits anything in that window (the failed attempt writes nothing at all), so
	// waiting for a marker that will never arrive there would only cost a real timeout for no benefit. Both
	// gates are released back-to-back and the whole body is read only once, to a genuine end-of-stream.
	private static async Task<string> RunFaultedStreamingCaseAsync(Issue264SwitchOnHostProcess host)
	{
		using var response = await host.Client.GetAsync("/issue-264/streaming", HttpCompletionOption.ResponseHeadersRead);
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);

		var initial = await reader.ReadUntilAsync(["data-issue-264-streaming=\"initial\""]);
		Assert.Contains("data-issue-264-streaming=\"initial\"", initial, StringComparison.Ordinal);
		await ReleaseAndRequireSuccessAsync(host.ReleaseStreamingNavigateAsync);
		await ReleaseAndRequireSuccessAsync(host.ReleaseStreamingResumeAsync);

		var wholeBody = await reader.ReadToEndAsync(TimeSpan.FromSeconds(2));
		return Issue264Snapshot.NormalizeProtectedPayloads(wholeBody);
	}

	// A release call that itself failed must not be silently treated as "nothing more will ever arrive": that
	// would let a broken release endpoint pass through to ReadToEndAsync's genuine-EOF wait and (once that
	// eventually times out) report a confusing stream failure instead of the actual release failure.
	private static async Task ReleaseAndRequireSuccessAsync(Func<Task<HttpResponseMessage>> release)
	{
		using var response = await release();
		Assert.True(response.IsSuccessStatusCode, $"Streaming gate release failed with {response.StatusCode}.");
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

	// The host relays every Error-or-above log entry to its own stdout as "ERROR <category>|<message>" (see
	// Issue264ErrorLogRelay), since the host otherwise clears every logging provider. This counts them per
	// request the same way AssertNoUnobservedNavigationExceptionAsync counts "UNOBSERVED " lines.
	private static async Task AssertExactlyOneErrorLoggedAsync(Issue264SwitchOnHostProcess host)
	{
		var lines = await host.DrainRecentOutputAsync();
		var errors = lines.Where(line => line.StartsWith("ERROR ", StringComparison.Ordinal)).ToArray();
		Assert.True(
			errors.Length == 1,
			$"Expected exactly one Error-level log entry for this request; got {errors.Length}: {string.Join("; ", errors)}");
	}

	private static IReadOnlyList<string> EnhancedNavigationRedirectHeaderValues(HttpResponseMessage response) =>
		response.Headers.TryGetValues("blazor-enhanced-nav-redirect-location", out var values) ? values.ToArray() : [];

	private sealed record Issue264StreamingSnapshot(HttpStatusCode StatusCode, string BeforeResume, string AfterResume, string WholeBody);
}
