using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #269's own red-contract cases, compiled for both net10.0 and net11.0 (see the csproj). Every case pairs a
// stock host against an AddHtmxor candidate host built by Issue269Host, and every gate is a named Issue260Gate
// signal rather than a fixed sleep (the Issue260Gate pattern).
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

		// Stock's own oracle: the boundary re-descends into WriteComponentHtmlCore from this later <template>
		// update, exactly where the started-response guard matters (`!_httpContext.Response.HasStarted` at
		// EndpointHtmlRenderer.Streaming.cs, both upstream versions). A candidate missing that guard tears the
		// connection down instead of completing, which Issue264StreamingBodyReader's ReadToEndAsync surfaces as
		// a TimeoutException rather than a silent hang. webassembly never reaches the header-setting branch
		// (marker.Type is "server" or "auto" only), so it never tears the connection down -- but on net10.0 it is
		// not characterization either: a second, successful visit to the same boundary still silently drops this
		// fixture's own persisted "issue269-probe" entry from the final Blazor-WebAssembly-Component-State marker
		// on the candidate there (reproduced deterministically; stock always retains it). net10.0's own
		// GetComponentRenderMode resolves a render-mode-bound component's mode through the componentRenderModes
		// dictionary (HtmxorEndpointCandidate.cs); net11.0 walks the live ComponentState chain instead and does
		// not reproduce this loss, so webassembly is characterization only on net11.0.
		var stockWhole = await stockReader.ReadToEndAsync();
		var candidateWhole = await candidateReader.ReadToEndAsync();

		Assert.Contains("<template blazor-component-id=", stockWhole, StringComparison.Ordinal);
		Assert.Contains("data-issue-269-page=\"updated\"", stockWhole, StringComparison.Ordinal);

		// Whole-body parity after decoding: each host carries its own ephemeral data-protection key, so the raw
		// persisted-state payload bytes never match byte-for-byte across two separately keyed hosts even when
		// the underlying persisted content is identical; descriptor, prerenderId and the framework's own
		// antiforgery-token persisted value are per-render or per-host random tokens with no stock-vs-candidate
		// meaning of their own. All three are normalized, each with the rationale above.
		Assert.Equal(
			Issue269PersistedState.Decode(stockWhole, stock.Protection),
			Issue269PersistedState.Decode(candidateWhole, candidate.Protection));
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
		Assert.Equal(stockResult.Headers, candidateResult.Headers);

		// Re-execution never streams on either target (RenderComponentCore awaits full quiescence first), so no
		// later <template> update exists here to re-descend into the boundary -- this pairing instead proves
		// persisted-state suppression or emission for a *streaming* page's boundary under re-execution matches
		// each target's own stock, the untested combination the issue's own evidence named ("mirrored for
		// non-streaming responses, and unproved for streamed ones"). net10.0 stock's AddPendingTask does not
		// track re-executed work at all, so its response reflects the page's pre-release state; net11.0 stock
		// does track it. Whichever each target's stock does, the candidate must match that target, not a fixed
		// classification applied on both (Issue260ReexecutionParityTests' own rationale for the same split).
		Assert.Equal(
			Issue269PersistedState.Decode(stockResult.Body, stock.Protection),
			Issue269PersistedState.Decode(candidateResult.Body, candidate.Protection));
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
}

// A minimal, #269-owned persisted-state decoder: it only decodes the two marker shapes this issue's own fixture
// produces (Blazor-Server-Component-State and Blazor-WebAssembly-Component-State), rather than reusing
// Issue191PersistedStateParityTests' own NormalizeDynamicState (which blanks the payload instead of decoding
// it) or copying Issue214StateParityTests' own NormalizeBody wholesale. Every #191/#214/#260/#269 normalization
// helper is intentionally local to its own file, matching this repository's established per-issue-file
// convention, rather than a single shared type whose scope would grow with each new issue that needs a
// slightly different marker set.
internal static class Issue269PersistedState
{
	public static string Decode(string body, IDataProtectionProvider protection)
	{
		var normalized = Regex.Replace(body, "\"(prerenderId|descriptor)\":\"[^\"]+\"", "\"$1\":\"<dynamic>\"");
		return Regex.Replace(normalized, "<!--Blazor-(Server|WebAssembly)-Component-State:(.*?)-->", match =>
		{
			var bytes = Convert.FromBase64String(match.Groups[2].Value);
			if (match.Groups[1].Value == "Server")
			{
				bytes = protection.CreateProtector("Microsoft.AspNetCore.Components.Server.State").Unprotect(bytes);
			}

			var state = JsonSerializer.Deserialize<SortedDictionary<string, byte[]>>(bytes)!;
			var decoded = string.Join(";", state.Select(item => $"{item.Key}={NormalizeAntiforgeryToken(Encoding.UTF8.GetString(item.Value))}"));
			return $"<!--Blazor-{match.Groups[1].Value}-Component-State:{decoded}-->";
		});
	}

	// The framework's own AntiforgeryStateProvider registers an OnPersisting callback on every interactive
	// endpoint (not something this fixture's own component persists), carrying a token value that is random per
	// request and keyed per host, exactly like Issue191PersistedStateParityTests' own NormalizePersistedValue.
	private static string NormalizeAntiforgeryToken(string value)
		=> Regex.Replace(value, "\"value\":\"[^\"]+\"(?=,\"formFieldName\":\"__RequestVerificationToken\")", "\"value\":\"<antiforgery-token>\"");
}
