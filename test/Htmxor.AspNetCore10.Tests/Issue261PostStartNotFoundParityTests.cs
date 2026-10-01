using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #261: a component that signals NotFound() after the response has already started streaming. Stock writes
// its not-found template through a fresh writer, flushes it, and stops the renderer
// (SignalRendererToFinishRendering), so ProcessPendingRender returns before building a new render tree for
// any later, quiescence-untracked work (Issue261StrayUpdateChild's detached continuation). A fix for this
// issue reuses that same stop, so the identical gate is expected to apply to it too.
//
// Both stock and the candidate check NotFoundEventArgs only once SendStreamingUpdatesAsync's quiescence wait
// has resolved; a render that races *inside* that wait streams on both sides identically, as its own
// <blazor-ssr> batch for the page's root component, and is not the gap. The stray is released only after the
// endpoint's own request-handling task -- including any stop the renderer set along the way -- has completed,
// signalled by Issue261NotFoundGate.EndpointFinished from a middleware that awaits the endpoint and then
// awaits release, identically on both hosts (see Issue261Host.CreateAsync). The request's own service scope,
// and with it the renderer and response writer, is still alive at that point, because the middleware that
// signals it has not itself returned yet.
//
// Two independent, component-level observations separate a genuine stop from a fix that merely avoids the
// resulting exception without stopping anything: Issue261StrayUpdateChild.RenderedState records the state
// its own render tree last carried, which only changes when ProcessPendingRender actually proceeds past the
// stop check to build a new one, regardless of what later happens to that render tree; the outcome of its own
// InvokeAsync(StateHasChanged) call separately records whether sending that render tree raised. A fix that
// gates the render keeps both unchanged from their initial values, on both hosts.
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
		// initial render), and the renderer's stop keeps Issue261StrayUpdateChild's own render attempt,
		// made once the endpoint had already finished, from building a new render tree or raising.
		Assert.Equal(HttpStatusCode.OK, stockResult.StatusCode);
		Assert.Contains(TemplateTypeMarker, stockResult.Body, StringComparison.Ordinal);
		Assert.Equal("stray-initial", stockResult.ObservedStrayRenderState);
		Assert.Null(stockResult.StrayOutcome);

		// The approved observation seam (status, headers, whole body): status, the antiforgery-normalized
		// headers, and the whole body (with the per-host opaque-redirect payload normalized, the same
		// Issue264Snapshot pattern this project already uses for a protected destination) must match stock
		// regardless of this case's own outcome, so a header-, status-, or body-shape regression cannot hide
		// behind the outcome assertions below.
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

		// The protected behavior, asserted against the candidate: the same render stays gated, so neither
		// its recorded render state nor its own render-attempt outcome differs from stock's.
		Assert.Equal("stray-initial", candidateResult.ObservedStrayRenderState);
		Assert.Null(candidateResult.StrayOutcome);
	}

	// Both gates are released under the identical EndpointFinished-driven precondition on both hosts: the
	// endpoint has already finished handling the request -- stop included, if one was set -- and only then is
	// the stray released, so no batch that only races *inside* quiescence can produce this case's red or its
	// green.
	private static async Task<Issue261Result> RunCaseAsync(Issue261Host host)
	{
		using var response = await host.Client.GetAsync(Path, HttpCompletionOption.ResponseHeadersRead);
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);

		var initial = await reader.ReadUntilAsync(["data-issue-261-stray=\"stray-initial\""]);
		Assert.Contains("data-issue-261-stray=\"stray-initial\"", initial, StringComparison.Ordinal);

		host.Gate.ReleaseNotFound();
		await host.Gate.EndpointFinished;

		host.Gate.ReleaseStray();
		var strayOutcome = await host.Gate.StrayOutcome;
		var observedStrayRenderState = host.Gate.LastObservedStrayRenderState;

		host.Gate.ReleaseResponse();
		var body = await reader.ReadToEndAsync();

		// Headers are read from the still-live HttpResponseMessage, not the body reader, so this must happen
		// before `response` goes out of scope; Issue260Snapshot.NormalizeHeaders replaces only the ephemeral
		// per-host antiforgery cookie value, the same normalization every other paired case in this project uses.
		return new(response.StatusCode, Issue260Snapshot.NormalizeHeaders(response), body, strayOutcome, observedStrayRenderState);
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
		Exception? StrayOutcome,
		string ObservedStrayRenderState);

	// A per-host wrapper over Issue260Host, carrying #261's own gate, so RunCaseAsync can observe both
	// without a second copy of Issue260Host's own TestServer/data-protection wiring.
	private sealed class Issue261Host(Issue260Host host, Issue261NotFoundGate gate) : IAsyncDisposable
	{
		public HttpClient Client => host.Client;

		public Issue261NotFoundGate Gate { get; } = gate;

		public static async Task<Issue261Host> CreateAsync(bool useHtmxor)
		{
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

					// Holds the response open, with its request scope -- and therefore the renderer and
					// response writer -- still alive, from the moment the endpoint's own request-handling
					// task completes until the test calls Issue261NotFoundGate.ReleaseResponse.
					app.Use(async (context, next) =>
					{
						await next(context);
						var gate = context.RequestServices.GetRequiredService<Issue261NotFoundGate>();
						gate.SignalEndpointFinished();
						await gate.ResponseReleased;
					});
				},
				configureServices: services => services.AddSingleton<Issue261NotFoundGate>());
			return new Issue261Host(host, host.Services.GetRequiredService<Issue261NotFoundGate>());
		}

		public ValueTask DisposeAsync() => host.DisposeAsync();
	}
}
