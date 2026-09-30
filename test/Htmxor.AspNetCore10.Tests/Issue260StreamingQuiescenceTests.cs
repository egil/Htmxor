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

		stockGate.Release("late-grandchild");
		stockGate.Release("late-sibling");
		candidateGate.Release("late-grandchild");
		candidateGate.Release("late-sibling");

		using var stockResponse = await stockResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		using var candidateResponse = await candidateResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var candidateBody = await candidateResponse.Content.ReadAsStringAsync();

		// The grandchild carries no [StreamRendering] of its own or through an inherited ancestor, so it can
		// never receive a later <template> update either: if the initial write had happened too early, the
		// wrong ("grandchild-pending") state would be frozen in the response for good.
		Assert.Contains("data-issue-260-late-grandchild=\"grandchild-complete\"", stockBody, StringComparison.Ordinal);
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
		var responseTask = host.Client.GetAsync("/issue-260/late-navigation");
		await Task.WhenAll(gate.WaitForReachedAsync("late-sibling"), gate.WaitForReachedAsync("late-nav-child"));

		gate.Release("late-nav-child");
		await gate.WaitForReachedAsync("late-nav-grandchild");
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

	[Fact]
	public async Task A_status_reexecuted_request_with_non_streaming_pending_work_matches_stock()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, ConfigurePipeline);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, ConfigurePipeline);

		var stockResult = await RunAsync(stock);
		var candidateResult = await RunAsync(candidate);

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

	private static void ConfigurePipeline(WebApplication app)
	{
		app.UseStatusCodePagesWithReExecute(ReexecutedPath);
		app.Use(static (context, next) =>
		{
			if (context.Request.Path == OriginPath)
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				return Task.CompletedTask;
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static async Task<(HttpStatusCode StatusCode, string Body, IReadOnlyDictionary<string, string> Headers)> RunAsync(Issue260Host host)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync(OriginPath, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("reexecuted-pending");
		gate.Release("reexecuted-pending");

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
