using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #261: a component that signals NotFound() after the response has already started streaming. Stock writes
// its not-found template through a fresh writer, flushes it, and stops the renderer
// (SignalRendererToFinishRendering), so a later render from work the renderer never tracked for quiescence
// (Issue261StrayUpdateChild's detached continuation) is gated by ProcessPendingRender before it ever touches
// the response again. At 8b4a905 the candidate (HtmxorEndpointCandidateRenderer.Streaming.cs's
// WriteNotFoundAfterResponseStarted) writes the same template but never calls that stop, so the same deferred
// render is let through to SendBatchAsStreamingUpdate.
//
// Both stock and the candidate check NotFoundEventArgs only once SendStreamingUpdatesAsync's quiescence wait
// has resolved; a render that races *inside* that wait streams on both sides identically, as its own
// <blazor-ssr> batch for the page's root component, and is not the gap. Issue261ResponseHold makes "after the
// not-found write, and only then" an observed precondition instead of an assumption: it holds the response
// open at the first write/flush/completion attempt once the not-found template has already been written,
// identically on both hosts, and only once held does the test release the stray.
//
// HttpResponseStreamWriter's own FlushAsync and DisposeAsync only ever reach the underlying stream through a
// WriteAsync call, and only when its char buffer is non-empty. For this page, nothing is buffered into either
// host's writer after the not-found content, so neither host performs a further body operation once that
// content is written, and Issue261ResponseHold's own backstop is what engages the hold, immediately after the
// endpoint's request-handling task has finished -- by which point stock's stop has already run, and the
// candidate's writer is already disposed either way. The two hosts stay identical on the wire (whole body,
// status, headers); what differs is whether Issue261StrayUpdateChild's own render attempt, made while the
// response is held, reaches SendBatchAsStreamingUpdate at all. Stock's StrayOutcome is always null
// (ProcessPendingRender returns before touching the writer); the candidate's is an ObjectDisposedException,
// because nothing stopped it.
public sealed class Issue261PostStartNotFoundParityTests
{
	private const string Path = "/issue-261/post-start-not-found";
	private const string NotFoundDestinationPath = "/issue-261/not-found-destination";
	private const string TemplateTypeMarker = "<template type=\"not-found\"";

	[Fact]
	public async Task AddHtmxor_suppresses_an_untracked_update_after_a_post_start_not_found_like_stock()
	{
		await using var stock = await Issue261Host.CreateAsync(useHtmxor: false);
		await using var candidate = await Issue261Host.CreateAsync(useHtmxor: true);

		var stockResult = await RunCaseAsync(stock);
		var candidateResult = await RunCaseAsync(candidate);

		// Stock's own oracle, pinned before the comparison the test cares about: it actually reached the
		// not-found template with a normal 200 (streaming never changes the status already sent with the
		// initial render), and the renderer's stop means Issue261StrayUpdateChild's own render attempt,
		// made while the response was held, never throws.
		Assert.Equal(HttpStatusCode.OK, stockResult.StatusCode);
		Assert.Contains(TemplateTypeMarker, stockResult.Body, StringComparison.Ordinal);
		Assert.Null(stockResult.StrayOutcome);

		// The approved observation seam (status, headers, whole body): status, the antiforgery-normalized
		// headers, and the whole body (with the per-host opaque-redirect payload normalized, the same
		// Issue264Snapshot pattern this project already uses for a protected destination) must match stock
		// regardless of this case's own outcome, so a header-, status-, or body-shape regression cannot hide
		// behind the outcome assertion below.
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(
			Issue264Snapshot.NormalizeProtectedPayloads(stockResult.Body),
			Issue264Snapshot.NormalizeProtectedPayloads(candidateResult.Body));

		// Destination parity: the opaque-redirect payload itself can never be equal byte-for-byte (each host
		// has its own ephemeral data-protection key), so this follows it on each host and compares what it
		// actually resolves to instead.
		var stockDestination = await FollowNotFoundDestinationAsync(stock, stockResult.Body);
		var candidateDestination = await FollowNotFoundDestinationAsync(candidate, candidateResult.Body);
		Assert.Equal(NotFoundDestinationPath, stockDestination.AbsolutePath);
		Assert.Equal(stockDestination.AbsolutePath, candidateDestination.AbsolutePath);

		// Nothing stops the candidate's renderer for a post-start NotFound, so Issue261StrayUpdateChild's
		// render attempt -- made under the identical hold stock was also driven through -- is let through to
		// SendBatchAsStreamingUpdate instead of being gated first, and throws against the response
		// HtmxorEndpointCandidate.RenderComponentCore has already finished with.
		Assert.Null(candidateResult.StrayOutcome);
	}

	// Both gates are released under the identical Issue261ResponseHold-driven precondition on both hosts: the
	// not-found write has already happened (confirmed per-host, not assumed), and only then is the stray
	// released, so no batch that only races *inside* quiescence can produce this case's red or its green.
	private static async Task<Issue261Result> RunCaseAsync(Issue261Host host)
	{
		var responseTask = host.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		var hold = await host.Hold;
		using var response = await responseTask;
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);

		var initial = await reader.ReadUntilAsync(["data-issue-261-stray=\"stray-initial\""]);
		Assert.Contains("data-issue-261-stray=\"stray-initial\"", initial, StringComparison.Ordinal);

		host.Gate.ReleaseNotFound();
		await hold.HoldReached;

		host.Gate.ReleaseStray();
		var strayOutcome = await host.Gate.StrayOutcome;

		hold.Release();
		var body = await reader.ReadToEndAsync();

		// Headers are read from the still-live HttpResponseMessage, not the body reader, so this must happen
		// before `response` goes out of scope; Issue260Snapshot.NormalizeHeaders replaces only the ephemeral
		// per-host antiforgery cookie value, the same normalization every other paired case in this project uses.
		return new(response.StatusCode, Issue260Snapshot.NormalizeHeaders(response), body, strayOutcome);
	}

	// The not-found template's own opaque-redirect payload is meaningful only against the host that protected
	// it, so this requests it back from that same host and reads where it actually redirects to.
	private static async Task<Uri> FollowNotFoundDestinationAsync(Issue261Host host, string body)
	{
		var match = Regex.Match(body, "<template type=\"not-found\"[^>]*>([^<]+)</template>", RegexOptions.CultureInvariant);
		Assert.True(match.Success, $"Expected a stock-shaped not-found template in: {body}");
		using var redirectResponse = await host.Client.GetAsync(match.Groups[1].Value);
		Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
		return redirectResponse.Headers.Location ?? throw new InvalidOperationException(
			$"Expected the opaque redirect to carry a Location header. Status: {redirectResponse.StatusCode}");
	}

	private sealed record Issue261Result(
		HttpStatusCode StatusCode,
		IReadOnlyDictionary<string, string> Headers,
		string Body,
		Exception? StrayOutcome);

	// A per-request wrapper over Issue260Host, carrying #261's own gate and the Issue261ResponseHold the
	// request's own pipeline creates (see Issue261ResponseHoldMiddleware), so RunCaseAsync can observe both
	// without a second copy of Issue260Host's own TestServer/data-protection wiring (S4: reuse, not copy).
	private sealed class Issue261Host(Issue260Host host, Issue261NotFoundGate gate, TaskCompletionSource<Issue261ResponseHold> holdSource)
		: IAsyncDisposable
	{
		public HttpClient Client => host.Client;

		public Issue261NotFoundGate Gate { get; } = gate;

		// Resolves once this request's own middleware instance has been created, which happens as soon as the
		// request starts being processed -- well before the not-found write, let alone the hold itself.
		public Task<Issue261ResponseHold> Hold => holdSource.Task;

		public static async Task<Issue261Host> CreateAsync(bool useHtmxor)
		{
			var holdSource = new TaskCompletionSource<Issue261ResponseHold>(TaskCreationOptions.RunContinuationsAsynchronously);
			var host = await Issue260Host.CreateAsync<Issue260App>(
				useHtmxor,
				configurePipeline: app =>
				{
					// Supplies WriteNotFoundAfterResponseStarted's (and stock's GetNotFoundUrl's) destination
					// fallback through the real re-execution middleware applications use, rather than writing
					// context.Items["StatusCodePagesOptions"] by hand. The response has already started with a
					// 200 by the time NotFound() is raised, so this never actually re-executes the request; it
					// only supplies the same item both renderers read.
					app.UseStatusCodePagesWithReExecute(NotFoundDestinationPath);
					Issue261ResponseHoldMiddleware.Use(app, TemplateTypeMarker, created => holdSource.TrySetResult(created));
				},
				configureServices: services => services.AddSingleton<Issue261NotFoundGate>());
			return new Issue261Host(host, host.Services.GetRequiredService<Issue261NotFoundGate>(), holdSource);
		}

		public ValueTask DisposeAsync() => host.DisposeAsync();
	}
}
