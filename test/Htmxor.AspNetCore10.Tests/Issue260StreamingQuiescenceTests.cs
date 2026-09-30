using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #260's own red-contract cases, compiled for both net10.0 and net11.0 (see the csproj). Every case pairs a
// stock host against an AddHtmxor candidate host built by Issue260Host, and every gate is a named
// Issue260Gate signal rather than a fixed sleep.
public sealed class Issue260InheritedStreamingTests
{
	private const string Path = "/issue-260/inherited-streaming";

	[Fact]
	public async Task An_attribute_less_child_inside_a_streaming_page_streams_like_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);
		var stockGate = stock.Services.GetRequiredService<Issue260Gate>();
		var candidateGate = candidate.Services.GetRequiredService<Issue260Gate>();

		var stockResponseTask = stock.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		var candidateResponseTask = candidate.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(
			stockGate.WaitForReachedAsync("inherited-child"),
			candidateGate.WaitForReachedAsync("inherited-child"));

		// The protected behavior: an attribute-less child inheriting [StreamRendering] from its page starts the
		// response while its own pending work is still gated, exactly as stock's inherited EndpointComponentState
		// does. A candidate that instead classifies the child as non-streaming withholds response headers here
		// until the release below, which is the exact defect measured on #192 (comment 5694844384).
		using var stockResponse = await stockResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		await using var stockReader = await Issue264StreamingBodyReader.CreateAsync(stockResponse);
		var stockInitial = await stockReader.ReadUntilAsync(["data-issue-260-child=\"initial\""]);
		Assert.Contains("data-issue-260-child=\"initial\"", stockInitial, StringComparison.Ordinal);
		Assert.Contains("<!--bl:", stockInitial, StringComparison.Ordinal);

		using var candidateResponse = await candidateResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		await using var candidateReader = await Issue264StreamingBodyReader.CreateAsync(candidateResponse);
		var candidateInitial = await candidateReader.ReadUntilAsync(["data-issue-260-child=\"initial\""]);
		Assert.Contains("data-issue-260-child=\"initial\"", candidateInitial, StringComparison.Ordinal);

		Assert.Equal(Issue260Snapshot.NormalizeHeaders(stockResponse), Issue260Snapshot.NormalizeHeaders(candidateResponse));

		stockGate.Release("inherited-child");
		candidateGate.Release("inherited-child");

		// Whole-body parity: the bl markers, the streamed <template> update, ordering, and the final flush.
		var stockWhole = await stockReader.ReadToEndAsync();
		var candidateWhole = await candidateReader.ReadToEndAsync();
		Assert.Contains("<template blazor-component-id=", stockWhole, StringComparison.Ordinal);
		Assert.Equal(stockWhole, candidateWhole);
	}
}

public sealed class Issue260NonStreamingQuiescenceTests
{
	private const string Path = "/issue-260/late-discovery";

	[Fact]
	public async Task Non_streaming_work_discovered_after_the_first_wait_began_completes_before_initial_html_like_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);
		var stockGate = stock.Services.GetRequiredService<Issue260Gate>();
		var candidateGate = candidate.Services.GetRequiredService<Issue260Gate>();

		var stockResponseTask = stock.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		var candidateResponseTask = candidate.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(
			stockGate.WaitForReachedAsync("late-sibling"), stockGate.WaitForReachedAsync("late-child"),
			candidateGate.WaitForReachedAsync("late-sibling"), candidateGate.WaitForReachedAsync("late-child"));

		stockGate.Release("late-child");
		candidateGate.Release("late-child");
		await Task.WhenAll(
			stockGate.WaitForReachedAsync("late-grandchild"), candidateGate.WaitForReachedAsync("late-grandchild"));

		// Stock's own oracle: the non-streaming wait loops until no new non-streaming task appears, so the
		// grandchild discovered only once the child's own wait released is a new non-streaming task that must
		// itself be awaited before either host may respond at all. A single, one-shot WhenAll over the tasks
		// known when the wait began would let the response proceed here instead.
		await Assert.ThrowsAsync<TimeoutException>(() => stockResponseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));
		await Assert.ThrowsAsync<TimeoutException>(() => candidateResponseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));

		// Release only the late-discovered task, not the streaming sibling yet. Holding the sibling here, and
		// reading each host's initial HTML before releasing it below, forces the sibling's own completion to
		// land strictly after serialization: stock can then only ever reach the response as a streamed
		// <template> update, not as part of the initial write. Without this ordering, stock's own initial
		// write races the sibling's completion against the grandchild's continuation completing the
		// non-streaming wait, and stock itself produces either shape depending on which wins -- which would
		// make a correct fix fail this case's whole-body comparison as often as it passes. This ordering is
		// also what pins criterion 2's "no batch is missed between initial serialization and the start of
		// streaming": the sibling's batch can only ever reach the response from here as a streamed update.
		stockGate.Release("late-grandchild");
		candidateGate.Release("late-grandchild");

		using var stockResponse = await stockResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		await using var stockReader = await Issue264StreamingBodyReader.CreateAsync(stockResponse);
		var stockInitial = await stockReader.ReadUntilAsync(["data-issue-260-late-grandchild=\"grandchild-complete\""]);
		// The grandchild carries no [StreamRendering] of its own or through an inherited ancestor, so it can
		// never receive a later <template> update either: if the initial write had happened too early, the
		// wrong ("grandchild-pending") state would be frozen in the response for good.
		Assert.Contains("data-issue-260-late-grandchild=\"grandchild-complete\"", stockInitial, StringComparison.Ordinal);

		using var candidateResponse = await candidateResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		await using var candidateReader = await Issue264StreamingBodyReader.CreateAsync(candidateResponse);
		var candidateInitial = await candidateReader.ReadUntilAsync(["data-issue-260-late-grandchild=\"grandchild-complete\""]);
		Assert.Contains("data-issue-260-late-grandchild=\"grandchild-complete\"", candidateInitial, StringComparison.Ordinal);

		stockGate.Release("late-sibling");
		candidateGate.Release("late-sibling");

		var stockWhole = await stockReader.ReadToEndAsync();
		var candidateWhole = await candidateReader.ReadToEndAsync();

		Assert.Contains("<template blazor-component-id=", stockWhole, StringComparison.Ordinal);
		Assert.Equal(stockWhole, candidateWhole);
	}
}

// LR-ce7bd22-P001 / the corrected criterion-5 decision (https://github.com/egil/Htmxor/issues/260, "Correction
// to the criterion-5 decision"): a pending task with no owning ComponentState -- an async disposal still in
// flight for a component removed mid-render -- still counts as non-streaming work, exactly as stock's
// AddPendingTask counts it, regardless of whether any [StreamRendering] component exists anywhere on the page.
public sealed class Issue260UnownedPendingTaskTests
{
	private const string Path = "/issue-260/unowned-pending";

	[Fact]
	public async Task Pending_disposal_with_no_owning_component_completes_before_initial_html_like_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);
		var stockGate = stock.Services.GetRequiredService<Issue260Gate>();
		var candidateGate = candidate.Services.GetRequiredService<Issue260Gate>();

		var stockResponseTask = stock.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		var candidateResponseTask = candidate.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(
			stockGate.WaitForReachedAsync("unowned-pending-page"),
			candidateGate.WaitForReachedAsync("unowned-pending-page"));

		stockGate.Release("unowned-pending-page");
		candidateGate.Release("unowned-pending-page");
		await Task.WhenAll(
			stockGate.WaitForReachedAsync("unowned-pending-child-dispose"),
			candidateGate.WaitForReachedAsync("unowned-pending-child-dispose"));

		// Stock's own oracle: a pending task with no owning ComponentState still counts as non-streaming work,
		// so neither host may respond while the child's disposal is held.
		await Assert.ThrowsAsync<TimeoutException>(() => stockResponseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));
		await Assert.ThrowsAsync<TimeoutException>(() => candidateResponseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));

		stockGate.Release("unowned-pending-child-dispose");
		candidateGate.Release("unowned-pending-child-dispose");

		using var stockResponse = await stockResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		using var candidateResponse = await candidateResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(Issue260Snapshot.NormalizeHeaders(stockResponse), Issue260Snapshot.NormalizeHeaders(candidateResponse));

		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var candidateBody = await candidateResponse.Content.ReadAsStringAsync();
		Assert.Contains("data-issue-260-unowned-pending=\"child-removed\"", stockBody, StringComparison.Ordinal);
		Assert.Equal(stockBody, candidateBody);
	}
}

public sealed class Issue260NonStreamingNavigationTests
{
	[Fact]
	public async Task A_navigation_exception_from_the_first_non_streaming_wait_is_handled_before_the_response_starts_like_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);

		var stockResult = await RunPendingNavigationAsync(stock);
		var candidateResult = await RunPendingNavigationAsync(candidate);

		// Stock's own oracle: a NavigationException raised while waiting for non-streaming pending work is
		// caught before the response starts and answered with an ordinary redirect, never a 500. This is the
		// defect from #192 comment 5901861239 (measured with the switch that lets NavigateTo throw off, i.e. the
		// default production path, which neither host in this test overrides).
		AssertRedirect(stockResult);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Location, candidateResult.Location);
	}

	[Fact]
	public async Task A_navigation_exception_from_a_later_non_streaming_wait_iteration_is_handled_before_the_response_starts_like_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);

		var stockResult = await RunLateNavigationAsync(stock);
		var candidateResult = await RunLateNavigationAsync(candidate);

		// Same oracle as above, but the exception is raised by a task discovered only after the first
		// non-streaming wait already began (see Issue260LateNavigationChild): a fix that wraps only the first
		// wait in a try/catch, rather than every iteration of a loop, would still let this one escape unhandled
		// instead of becoming stock's ordinary redirect.
		AssertRedirect(stockResult);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Location, candidateResult.Location);
	}

	// No stock oracle for either case below: htmx, and Direct routing mode, are Htmxor's own concepts. Both
	// assert the owner's decided output directly, per the #260 checkpoint's carried decision
	// (https://github.com/egil/Htmxor/issues/264#issuecomment-5902791174) and the #260 test-contract review
	// decision (https://github.com/egil/Htmxor/issues/260, "Decisions from the test-contract review"): a
	// NavigationException raised by non-streaming pending work before the response starts answers an htmx
	// request with HX-Redirect, the same as #264's switch-on
	// Issue264SwitchOnTests.Htmx_pending_work_navigation_gets_hx_redirect_per_decision, instead of a bare
	// redirect or an unhandled exception.
	[Fact]
	public async Task An_htmx_request_whose_pending_work_navigates_before_start_gets_hx_redirect_per_the_carried_decision()
	{
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);
		var gate = candidate.Services.GetRequiredService<Issue260Gate>();

		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-260/pending-navigation");
		request.Headers.Add("HX-Request", "true");
		var responseTask = candidate.Client.SendAsync(request);
		await gate.WaitForReachedAsync("pending-navigation");
		gate.Release("pending-navigation");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("/issue-260/destination", SingleHeaderOrNull(response, "HX-Redirect"));
		Assert.Null(response.Headers.Location);
	}

	// Direct mode (RoutingMode.Direct, selected by "HX-Request-Type: partial") forces full quiescence, so this
	// NavigationException is raised from the invoker's await quiesceTask, not from the non-streaming wait --
	// the same "NavigationException raised by pending work before the response starts" mechanism as criterion
	// 3, reached through a different invoker path. In scope per the review decision above.
	[Fact]
	public async Task An_htmx_direct_mode_partial_request_whose_pending_work_navigates_before_start_gets_hx_redirect_per_the_carried_decision()
	{
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);
		var gate = candidate.Services.GetRequiredService<Issue260Gate>();

		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-260/pending-navigation");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		var responseTask = candidate.Client.SendAsync(request);
		await gate.WaitForReachedAsync("pending-navigation");
		gate.Release("pending-navigation");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("/issue-260/destination", SingleHeaderOrNull(response, "HX-Redirect"));
		Assert.Null(response.Headers.Location);
	}

	// LR-ce7bd22-P003 / the corrected criterion-5 decision: an htmx Direct-mode POST whose submit handler
	// navigates only after an await (not synchronously from the click handler itself) reaches the invoker's
	// full-quiescence wait through a different site (the POST branch's own await quiesceTask) than the GET
	// case above. No stock oracle: Direct mode is Htmxor's own concept. Reuses #213's own host and awaited-
	// submit page (Issue213RedirectHost.SubmitAsync), since this is the POST half of the same mechanism the
	// GET case above pins, rather than duplicating that harness here.
	[Fact]
	public async Task An_htmx_direct_mode_submit_whose_handler_navigates_after_an_await_gets_hx_redirect_per_the_carried_decision()
	{
		await using var candidate = await Issue213SubmitRedirectTests.Issue213RedirectHost.StartAsync(htmxor: true);

		var snapshot = await candidate.SubmitAsync(htmx: true, awaited: true);

		Assert.Equal(HttpStatusCode.OK, snapshot.Status);
		Assert.Equal("/issue-213/after-submit", snapshot.HxRedirect);
		Assert.Null(snapshot.Location);
	}

	private static string? SingleHeaderOrNull(HttpResponseMessage response, string name)
		=> response.Headers.TryGetValues(name, out var values) ? values.Single() : null;

	private static void AssertRedirect(Issue260NavigationResult result)
	{
		Assert.Equal(HttpStatusCode.Found, result.StatusCode);
		Assert.NotNull(result.Location);
	}

	private static async Task<Issue260NavigationResult> RunPendingNavigationAsync(Issue260Host host)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync("/issue-260/pending-navigation");
		await gate.WaitForReachedAsync("pending-navigation");
		gate.Release("pending-navigation");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		return new(response.StatusCode, response.Headers.Location);
	}

	private static async Task<Issue260NavigationResult> RunLateNavigationAsync(Issue260Host host)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync("/issue-260/late-navigation", HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(gate.WaitForReachedAsync("late-sibling"), gate.WaitForReachedAsync("late-nav-child"));

		gate.Release("late-nav-child");
		await gate.WaitForReachedAsync("late-nav-grandchild");

		// Bounded, deterministic checkpoint: the response must not have started yet. A non-streaming wait that does
		// not loop (criterion 2's defect) has already started the response once "late-nav-child" completed, so the
		// grandchild's navigation below would then land after the start and race between an in-body redirect and an
		// escaped exception. Failing here makes that red deterministic, and once this passes the navigation below is
		// raised on a later wait iteration before the response starts, so only per-iteration handling can pass.
		await Assert.ThrowsAsync<TimeoutException>(() => responseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));

		gate.Release("late-nav-grandchild");
		gate.Release("late-sibling");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		return new(response.StatusCode, response.Headers.Location);
	}

	private sealed record Issue260NavigationResult(HttpStatusCode StatusCode, Uri? Location);
}

// LR-ce7bd22-P002 / LR-ce7bd22-S001 / the corrected criterion-5 decision: a navigation caught by the invoker's
// full-quiescence wait on an error-handler or a re-executed request -- the two paths this class verifies --
// must answer with no rendered page body, exactly as stock's own NavigationException catch in
// RenderEndpointComponent returns PrerenderedComponentHtmlContent.Empty instead of whatever was already
// rendered before the navigation. Direct-mode POST is outside this class: its navigation is answered with
// HX-Redirect and has no stock oracle for its body (Issue260NonStreamingNavigationTests' own Direct POST case
// asserts only the status and redirect headers).
public sealed class Issue260NavigationEmptyBodyTests
{
	private const string PendingNavigationPath = "/issue-260/pending-navigation";
	private const string ErrorOriginPath = "/issue-260/error-origin";
	private const string ReexecutionOriginPath = "/issue-260/reexecuted-origin-pending-navigation";

	[Fact]
	public async Task A_navigation_from_pending_work_during_an_error_handler_response_has_stock_empty_body_parity()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, ConfigureErrorHandlerPipeline);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, ConfigureErrorHandlerPipeline);

		var stockResult = await RunAsync(stock, ErrorOriginPath);
		var candidateResult = await RunAsync(candidate, ErrorOriginPath);

		Assert.Equal(HttpStatusCode.Found, stockResult.StatusCode);
		Assert.NotNull(stockResult.Location);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Location, candidateResult.Location);
		// Stock's own oracle: RenderEndpointComponent's NavigationException catch returns
		// PrerenderedComponentHtmlContent.Empty, so the redirect carries no rendered page content.
		Assert.Equal(string.Empty, stockResult.Body);
		Assert.Equal(stockResult.Body, candidateResult.Body);
	}

	// net11.0 only: v10.0.11 stock's AddPendingTask does not track re-executed work at all
	// (Issue260ReexecutedPendingPage's own comment), so on net10.0 this page's pending navigation is never
	// observed by a re-executed request -- the response answers with the page's pre-release state and no
	// navigation, which #260's existing reexecution cases already cover.
#if NET11_0_OR_GREATER
	[Fact]
	public async Task A_navigation_from_pending_work_during_a_status_reexecuted_response_has_stock_empty_body_parity()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, ConfigureReexecutionPipeline);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, ConfigureReexecutionPipeline);

		var stockResult = await RunAsync(stock, ReexecutionOriginPath);
		var candidateResult = await RunAsync(candidate, ReexecutionOriginPath);

		Assert.Equal(HttpStatusCode.Found, stockResult.StatusCode);
		Assert.NotNull(stockResult.Location);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Location, candidateResult.Location);
		Assert.Equal(string.Empty, stockResult.Body);
		Assert.Equal(stockResult.Body, candidateResult.Body);
	}
#endif

	private static void ConfigureErrorHandlerPipeline(WebApplication app)
	{
		app.UseExceptionHandler(PendingNavigationPath);
		app.Use((context, next) =>
		{
			if (context.Request.Path == ErrorOriginPath)
			{
				throw new InvalidOperationException("issue-260 error origin");
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static void ConfigureReexecutionPipeline(WebApplication app)
	{
		app.UseStatusCodePagesWithReExecute(PendingNavigationPath);
		app.Use((context, next) =>
		{
			if (context.Request.Path == ReexecutionOriginPath)
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				return Task.CompletedTask;
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static async Task<(HttpStatusCode StatusCode, Uri? Location, string Body)> RunAsync(Issue260Host host, string originPath)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync(originPath, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("pending-navigation");
		gate.Release("pending-navigation");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var body = await response.Content.ReadAsStringAsync();
		return (response.StatusCode, response.Headers.Location, body);
	}
}

public sealed class Issue260ReexecutionParityTests
{
	private const string OriginPath = "/issue-260/reexecuted-origin";
	private const string ReexecutedPath = "/issue-260/reexecuted-pending";
	private const string StreamingOriginPath = "/issue-260/reexecuted-origin-streaming";
	private const string StreamingReexecutedPath = "/issue-260/reexecuted-streaming";

	[Fact]
	public async Task A_status_reexecuted_request_with_non_streaming_pending_work_matches_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(
			htmxor: false, app => ConfigurePipeline(app, OriginPath, ReexecutedPath));
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(
			htmxor: true, app => ConfigurePipeline(app, OriginPath, ReexecutedPath));

		var stockResult = await RunAsync(stock, OriginPath, "reexecuted-pending");
		var candidateResult = await RunAsync(candidate, OriginPath, "reexecuted-pending");

		// Whatever stock itself does with a re-executed request's non-streaming pending work on this target --
		// including a target where stock's own AddPendingTask does not track it at all, so the response reflects
		// the component's pre-release state -- the candidate must match, per target, instead of applying the
		// same (untargeted) classification on every target regardless of what that target's own stock does.
		Assert.Equal(HttpStatusCode.NotFound, stockResult.StatusCode);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.DoesNotContain("data-issue-260-reexecution-origin", stockResult.Body, StringComparison.Ordinal);
		Assert.Equal(stockResult.Body, candidateResult.Body);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
	}

	// Work item 1's "status-code re-executed page with pending asynchronous work" also needs a *streaming*
	// variant: the inherited-streaming child's markers must survive re-execution too, on both targets, and
	// nothing streams during re-execution either way. Not covered by the non-streaming case above, whose page
	// has nothing to inherit streaming from.
	[Fact]
	public async Task A_status_reexecuted_streaming_page_with_an_inherited_pending_child_matches_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(
			htmxor: false, app => ConfigurePipeline(app, StreamingOriginPath, StreamingReexecutedPath));
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(
			htmxor: true, app => ConfigurePipeline(app, StreamingOriginPath, StreamingReexecutedPath));

		var stockResult = await RunAsync(stock, StreamingOriginPath, "reexecuted-streaming-child");
		var candidateResult = await RunAsync(candidate, StreamingOriginPath, "reexecuted-streaming-child");

		Assert.Equal(HttpStatusCode.NotFound, stockResult.StatusCode);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		// Stock's own oracle: the inherited child still carries its bl markers even though re-execution never
		// streams anything -- markers are decided by whether a component is a streaming component, not by
		// whether the response actually streams.
		Assert.Contains("<!--bl:", stockResult.Body, StringComparison.Ordinal);
		Assert.Equal(stockResult.Body, candidateResult.Body);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
	}

	private static void ConfigurePipeline(WebApplication app, string originPath, string reexecutedPath)
	{
		app.UseStatusCodePagesWithReExecute(reexecutedPath);
		app.Use((context, next) =>
		{
			if (context.Request.Path == originPath)
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				return Task.CompletedTask;
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static async Task<(HttpStatusCode StatusCode, string Body, IReadOnlyDictionary<string, string> Headers)> RunAsync(
		Issue260Host host, string originPath, string gateName)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync(originPath, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync(gateName);

#if NET11_0_OR_GREATER
		// net11.0 stock's AddPendingTask does track re-executed pending work, so its response only ever arrives
		// once this release lands: release-then-read is deterministic here.
		gate.Release(gateName);
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
#else
		// net10.0 stock's AddPendingTask does not track re-executed pending work at all (the same v10.0.11
		// short-circuit Issue260ReexecutedPendingPage's own comment describes), so its response can complete
		// without this gate ever being released -- racing this test's own release against that completion.
		// Awaiting the response headers before releasing removes the race: stock starts its response on its own,
		// with a body already serialized from the pre-release state, and a candidate that (wrongly) still waits
		// on this target fails deterministically on this bound instead of on a race.
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		gate.Release(gateName);
#endif

		var headers = Issue260Snapshot.NormalizeHeaders(response);
		var body = await response.Content.ReadAsStringAsync();
		return (response.StatusCode, body, headers);
	}
}

// A read-once normalized header snapshot shared by every #260 case in this file, following the same
// per-file convention as Issue186StreamingParityTests' own NormalizeHeaders and Issue187ResponseSnapshot: each
// host has its own ephemeral data-protection key, so only the antiforgery Set-Cookie *value* is host-specific,
// and only Date is otherwise time-varying. Whole-header parity (#260's own contract) still requires the
// cookie's name, count, order, and attributes (Path, SameSite, Secure, HttpOnly, ...) to match as the
// framework emits them; only the per-cookie value is replaced.
internal static class Issue260Snapshot
{
	public static IReadOnlyDictionary<string, string> NormalizeHeaders(HttpResponseMessage response) =>
		response.Headers.Concat(response.Content.Headers)
			.GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				group => group.Key,
				group => group.Key switch
				{
					_ when group.Key.Equals("Date", StringComparison.OrdinalIgnoreCase) => "<dynamic-date>",
					_ when group.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
						=> string.Join(",", group.SelectMany(header => header.Value).Select(NormalizeSetCookieValue)),
					_ => string.Join(",", group.SelectMany(header => header.Value)),
				},
				StringComparer.OrdinalIgnoreCase);

	// Replaces only the antiforgery cookie's value (the ephemeral, per-host token) with a fixed placeholder; any
	// other cookie is compared exactly as received. The cookie name and every attribute -- path, samesite, secure,
	// httponly, and any others the framework emits -- are left exactly as received, so a candidate that adds,
	// drops, or changes one of them still fails whole-header parity instead of being hidden behind this
	// normalization.
	private static string NormalizeSetCookieValue(string cookie)
	{
		const string antiforgeryCookiePrefix = ".AspNetCore.Antiforgery.";
		var nameSeparator = cookie.IndexOf('=');
		if (nameSeparator < 0 || !cookie.StartsWith(antiforgeryCookiePrefix, StringComparison.Ordinal))
		{
			return cookie;
		}

		var name = cookie[..nameSeparator];
		var valueAndAttributes = cookie[(nameSeparator + 1)..];
		var attributesSeparator = valueAndAttributes.IndexOf(';');
		var attributes = attributesSeparator < 0 ? string.Empty : valueAndAttributes[attributesSeparator..];
		return $"{name}=<dynamic-cookie-value>{attributes}";
	}
}
