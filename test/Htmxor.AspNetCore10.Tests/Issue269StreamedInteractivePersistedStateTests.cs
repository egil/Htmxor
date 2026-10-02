using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #269's streamed interactive-boundary parity cases, compiled for both net10.0 and net11.0 (see the csproj).
// Every case pairs a stock host against an AddHtmxor candidate host built by Issue269Host, and every gate is a
// named Issue260Gate signal rather than a fixed sleep (the Issue260Gate pattern).
public sealed class Issue269StreamedBoundaryTests
{
	[Theory]
	[InlineData("server")]
	[InlineData("webassembly")]
	[InlineData("auto")]
	public async Task A_later_streamed_update_that_re_renders_an_interactive_boundary_completes_like_stock(string mode)
	{
		var path = $"/issue-269/streaming/{mode}";
		var gateName = $"issue-269-page-{mode}";
		await using var stock = await Issue269Host.CreateAsync(htmxor: false);
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true);
		var stockGate = stock.Services.GetRequiredService<Issue260Gate>();
		var candidateGate = candidate.Services.GetRequiredService<Issue260Gate>();

		var stockResponseTask = stock.Client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
		var candidateResponseTask = candidate.Client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(
			stockGate.WaitForReachedAsync(gateName),
			candidateGate.WaitForReachedAsync(gateName));

		// Response headers only arrive this early because each page's own pending OnInitializedAsync task is
		// streaming work (the page carries [StreamRendering] directly), so the invoker starts the response while
		// that task is still gated below, exactly as stock's own quiescence split does. Release only happens
		// after this, so the boundary's later re-render is guaranteed to land after the response has started.
		using var stockResponse = await stockResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		await using var stockReader = await Issue264StreamingBodyReader.CreateAsync(stockResponse);
		var stockInitial = await stockReader.ReadUntilAsync(["data-issue-269-page=\"initial\""]);
		Assert.Contains("data-issue-269-page=\"initial\"", stockInitial, StringComparison.Ordinal);
		Assert.Contains("<!--bl:", stockInitial, StringComparison.Ordinal);

		using var candidateResponse = await candidateResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
		await using var candidateReader = await Issue264StreamingBodyReader.CreateAsync(candidateResponse);
		var candidateInitial = await candidateReader.ReadUntilAsync(["data-issue-269-page=\"initial\""]);
		Assert.Contains("data-issue-269-page=\"initial\"", candidateInitial, StringComparison.Ordinal);

		Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(Issue260Snapshot.NormalizeHeaders(stockResponse), Issue260Snapshot.NormalizeHeaders(candidateResponse));

		stockGate.Release(gateName);
		candidateGate.Release(gateName);

		// Stock's own oracle: the page's later <template> update re-renders the render-mode boundary after the
		// response has started, where stock writes the boundary marker again but sets the cache header only while
		// the response has not started. ReadToEndAsync reports a torn-down connection as a bounded failure rather
		// than a hang, and the decoded whole-body comparison below covers the persisted state written after that
		// update for every render mode.
		var stockWhole = await stockReader.ReadToEndAsync();
		Assert.Contains("<template blazor-component-id=", stockWhole, StringComparison.Ordinal);
		Assert.Contains("data-issue-269-page=\"updated\"", stockWhole, StringComparison.Ordinal);
		var stockUpdate = stockWhole[stockWhole.IndexOf("<template blazor-component-id=", StringComparison.Ordinal)..];
		Assert.Contains($"<!--Blazor:{{\"type\":\"{mode}\"", stockUpdate, StringComparison.Ordinal);
		var stockDecoded = Issue272PersistedState.Decode(stockWhole, stock.Protection);
		AssertEntryInModeStore(stockDecoded, mode, Entry);

		var candidateWhole = await candidateReader.ReadToEndAsync();

		// Whole-body parity on content, not bytes: the shared Issue272PersistedState decoder owns which
		// per-host or per-render values are normalized and why.
		Assert.Equal(stockDecoded, Issue272PersistedState.Decode(candidateWhole, candidate.Protection));
	}

	[Theory]
	[InlineData("server")]
	[InlineData("webassembly")]
	[InlineData("auto")]
	public async Task Status_code_reexecution_of_a_streaming_boundary_page_matches_each_targets_stock(string mode)
	{
		var originPath = $"/issue-269/reexecuted-origin/{mode}";
		var reexecutedPath = $"/issue-269/streaming/{mode}";
		var gateName = $"issue-269-page-{mode}";
		await using var stock = await Issue269Host.CreateAsync(
			htmxor: false, app => ConfigureReexecutionPipeline(app, originPath, reexecutedPath));
		await using var candidate = await Issue269Host.CreateAsync(
			htmxor: true, app => ConfigureReexecutionPipeline(app, originPath, reexecutedPath));

		var stockResult = await RunReexecutedAsync(stock, originPath, gateName);
		var candidateResult = await RunReexecutedAsync(candidate, originPath, gateName);

		Assert.Equal(HttpStatusCode.NotFound, stockResult.Status);
		Assert.Equal(stockResult.Status, candidateResult.Status);
		Assert.Contains("data-issue-269-child=\"rendered\"", stockResult.Body, StringComparison.Ordinal);
		Assert.Contains($"<!--Blazor:{{\"type\":\"{mode}\"", stockResult.Body, StringComparison.Ordinal);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);

		// Re-execution never streams on either target (RenderComponentCore awaits full quiescence first), so no
		// later <template> update re-renders the boundary here. This pairing instead pins each target's own stock
		// persisted-state rule for a re-executed request: net10.0 stock writes no persisted state for any
		// re-executed response (the rule Issue191PersistedStateParityTests pins for a page without pending work),
		// while net11.0 stock writes the re-executed boundary's state. The candidate must match each target's own
		// stock rather than one classification on both.
		var stockDecoded = Issue272PersistedState.Decode(stockResult.Body, stock.Protection);
#if NET11_0_OR_GREATER
		AssertEntryInModeStore(stockDecoded, mode, Entry);
#else
		Assert.DoesNotContain("Component-State:", stockResult.Body, StringComparison.Ordinal);
#endif
		Assert.Equal(stockDecoded, Issue272PersistedState.Decode(candidateResult.Body, candidate.Protection));
	}

	private static void ConfigureReexecutionPipeline(WebApplication app, string originPath, string reexecutedPath)
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

	private static async Task<(HttpStatusCode Status, string Body, IReadOnlyDictionary<string, string> Headers)> RunReexecutedAsync(
		Issue269Host host, string originPath, string gateName)
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
		// Awaiting the response before releasing removes the race, following Issue260ReexecutionParityTests'
		// own ordering for the same target split.
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		gate.Release(gateName);
#endif

		var body = await response.Content.ReadAsStringAsync();
		return (response.StatusCode, body, Issue260Snapshot.NormalizeHeaders(response));
	}

	private const string Entry = "issue269-probe=\"issue269-persisted\"";

	// Translates this fixture's own "server"/"webassembly"/"auto" mode route value into the expected-stores
	// shape the shared Issue272PersistedState.AssertEntryInStores takes, so the placement oracle itself does
	// not need to agree with this fixture's mode vocabulary.
	private static void AssertEntryInModeStore(string decodedBody, string mode, string entry)
	{
		var (expectServer, expectWebAssembly) = mode switch
		{
			"server" => (true, false),
			"webassembly" => (false, true),
			_ => (true, true),
		};
		Issue272PersistedState.AssertEntryInStores(decodedBody, entry, expectServer, expectWebAssembly);
	}
}
