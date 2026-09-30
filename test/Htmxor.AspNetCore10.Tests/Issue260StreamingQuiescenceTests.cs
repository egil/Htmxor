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

		// Bounded, deterministic checkpoint: the response must not have started yet. Without this, a fix that
		// only wraps the *first* WhenAll in a try/catch -- and so already began writing the response as soon as
		// "late-nav-child" completed -- only sometimes loses the race with the grandchild's navigation below,
		// which would make this case's red intermittent instead of guaranteed.
		await Assert.ThrowsAsync<TimeoutException>(() => responseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));

		gate.Release("late-nav-grandchild");
		gate.Release("late-sibling");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		return new(response.StatusCode, response.Headers.Location);
	}

	private sealed record Issue260NavigationResult(HttpStatusCode StatusCode, Uri? Location);
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
	// variant: re-execution forces full quiescence (isReexecuted, HtmxorEndpointCandidate.cs), which is a
	// different invoker path from criterion 1's ordinary case, and the inherited-streaming child's markers
	// must survive it too. Not covered by the non-streaming case above, whose page has nothing to inherit
	// streaming from.
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
		gate.Release(gateName);

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var headers = Issue260Snapshot.NormalizeHeaders(response);
		var body = await response.Content.ReadAsStringAsync();
		return (response.StatusCode, body, headers);
	}
}

// A read-once normalized header snapshot shared by every #260 case in this file, following the same
// per-file convention as Issue186StreamingParityTests' own NormalizeHeaders and Issue187ResponseSnapshot: each
// host has its own ephemeral data-protection key, so only the antiforgery Set-Cookie value is host-specific,
// and only Date is otherwise time-varying.
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
					_ when group.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) => "<dynamic-antiforgery-cookie>",
					_ => string.Join(",", group.SelectMany(header => header.Value)),
				},
				StringComparer.OrdinalIgnoreCase);
}
