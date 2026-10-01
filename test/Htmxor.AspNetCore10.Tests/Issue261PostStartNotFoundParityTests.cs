using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

// #261: a component that signals NotFound() after the response has already started streaming. Stock writes
// its not-found template through a fresh writer, flushes it immediately, and stops the renderer
// (SignalRendererToFinishRendering), so a later render from work the renderer never tracked for quiescence
// (Issue261StrayUpdateChild's detached continuation) never reaches the wire. At 8b4a905 the candidate
// (HtmxorEndpointCandidateRenderer.WriteNotFoundAfterResponseStarted) writes the same template into the
// shared streaming writer but never calls the stop, so that later render still streams as its own
// <blazor-ssr> batch.
//
// Per the issue's own evidence ("Delivery checkpoint: red contract started"), both stock and the candidate
// check NotFoundEventArgs only once SendStreamingUpdatesAsync's quiescence wait has resolved; a render that
// races *inside* that wait streams on both sides and is not the gap. Issue261StrayUpdateChild's update is
// instead kicked off from a detached (never awaited, never tracked) continuation, so it can only run once the
// component's own quiescence-contributing work (NotFound()) has already finished -- the one case stock's stop
// and the candidate's missing stop actually diverge on.
//
// Issue261NotFoundGate.StrayOutcome -- resolved by the component's own render attempt, not inferred from
// response timing -- is what lets the test wait for that attempt to finish before reading the rest of the
// body, on both sides, without a sleep. Reading the body to completion before that attempt has had a chance
// to run would race the server's own response completion against it and can turn a genuine streamed update
// into an unobserved, already-disposed-writer failure instead (see the exception this case's own early
// drafts observed, kept here only as the comment explaining why the order below is load-bearing).
public sealed class Issue261PostStartNotFoundParityTests
{
	[Fact]
	public async Task AddHtmxor_suppresses_an_untracked_update_after_a_post_start_not_found_like_stock()
	{
		await using var stock = await Issue261PostStartNotFoundHost.CreateAsync(useHtmxor: false);
		await using var candidate = await Issue261PostStartNotFoundHost.CreateAsync(useHtmxor: true);

		var stockResult = await RunCaseAsync(stock, releaseStrayOnlyAfterTemplateOnWire: true);
		var candidateResult = await RunCaseAsync(candidate, releaseStrayOnlyAfterTemplateOnWire: false);

		// Stock's own oracle, pinned before the comparison the test cares about: it actually reached the
		// not-found template with a normal 200 (streaming never changes the status already sent with the
		// initial render), and the renderer's stop means Issue261StrayUpdateChild's deferred update never
		// becomes a streamed batch at all.
		Assert.Equal(HttpStatusCode.OK, stockResult.StatusCode);
		Assert.Contains("<template type=\"not-found\"", stockResult.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("stray-updated", stockResult.Body, StringComparison.Ordinal);

		// The approved observation seam (status, headers, whole body): status and the antiforgery-normalized
		// headers must match stock regardless of this case's own outcome, so a header- or status-level
		// regression cannot hide behind the content assertion below.
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);

		// The protected behavior: the candidate should match stock and never stream an update once a
		// post-start NotFound has been raised. At 8b4a905 nothing stops the candidate's renderer for that
		// case, so the same deferred update still reaches the wire as its own <blazor-ssr> batch -- this is
		// the meaningful red; it must stop doing that once the renderer-stop is added.
		Assert.Contains("<template type=\"not-found\"", candidateResult.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("stray-updated", candidateResult.Body, StringComparison.Ordinal);
	}

	// `releaseStrayOnlyAfterTemplateOnWire` is true only for stock: stock flushes its not-found template
	// through a fresh writer immediately, so waiting for it on the wire before releasing the stray gate is a
	// genuine causal signal there (see Issue264's own RunStreamingCaseAsync, the same shape). The candidate
	// never flushes its not-found template ahead of its own final flush (this issue's second, independent
	// gap), so that same wait would hang; both gates are released back-to-back for it instead.
	//
	// Both branches await Issue261NotFoundGate.StrayOutcome -- resolved by Issue261StrayUpdateChild's own
	// render attempt -- before reading the rest of the body. That ordering, not a sleep, is what lets the
	// stray update's own batch (when the renderer does not stop it) actually land before this method reads to
	// a genuine end-of-stream.
	private static async Task<Issue261Snapshot> RunCaseAsync(Issue261PostStartNotFoundHost host, bool releaseStrayOnlyAfterTemplateOnWire)
	{
		using var response = await host.Client.GetAsync(
			Issue261PostStartNotFoundHost.Path, HttpCompletionOption.ResponseHeadersRead);
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);

		var initial = await reader.ReadUntilAsync(["data-issue-261-stray=\"stray-initial\""]);
		Assert.Contains("data-issue-261-stray=\"stray-initial\"", initial, StringComparison.Ordinal);

		host.Gate.ReleaseNotFound();
		if (releaseStrayOnlyAfterTemplateOnWire)
		{
			var beforeStray = await reader.ReadUntilAsync(["<template type=\"not-found\""]);
			Assert.Contains("<template type=\"not-found\"", beforeStray, StringComparison.Ordinal);
		}

		host.Gate.ReleaseStray();
		await host.Gate.StrayOutcome;

		var body = await reader.ReadToEndAsync();

		// Headers are read from the still-live HttpResponseMessage, not the body reader, so this must happen
		// before `response` goes out of scope; Issue260Snapshot.NormalizeHeaders replaces only the ephemeral
		// per-host antiforgery cookie value, the same normalization every other paired case in this project uses.
		return new(response.StatusCode, Issue260Snapshot.NormalizeHeaders(response), body);
	}

	private sealed record Issue261Snapshot(HttpStatusCode StatusCode, IReadOnlyDictionary<string, string> Headers, string Body);
}

// A minimal in-process TestServer host for #261's post-start-not-found case, compiled for both net10.0 and
// net11.0 (see the csproj's net11.0 Compile/RazorComponent Include). It deliberately does not reuse
// Issue187ParityHost: that host's own file compiles only for net10.0 today, and widening its much larger
// surface (session, authentication, forms, TempData) to net11.0 would pull in behavior this issue's case
// never touches (see Issue260Host's own comment, the same shape and reasoning). It reuses Issue260App as its
// root component -- a plain Router with no NotFoundPage, so NotFound() never triggers the Router's own
// content swap -- rather than adding a near-duplicate App.razor, and reuses Issue264StreamingBodyReader for
// incremental reading rather than adding a second copy of that logic.
internal sealed class Issue261PostStartNotFoundHost(WebApplication app, Issue261NotFoundGate gate) : IAsyncDisposable
{
	public const string Path = "/issue-261/post-start-not-found";

	// Never resolved by an actual component in this host: WriteNotFoundAfterResponseStarted (and stock's
	// GetNotFoundUrl) only needs a path string to build the not-found template's destination URL from, and
	// falls back to this item when NotFoundEventArgs itself carries no Path (see
	// HtmxorEndpointCandidateRenderer.Streaming.cs's WriteNotFoundAfterResponseStarted and the upstream
	// EndpointHtmlRenderer.EventDispatch.cs GetNotFoundUrl it mirrors). Supplying it this way, rather than
	// through Router.NotFoundPage, keeps the Router from swapping its own rendered content on NotFound() --
	// that swap is itself an additional, already-non-divergent render this case does not need.
	public const string NotFoundDestinationPath = "/issue-261/not-found-destination";

	public HttpClient Client { get; } = app.GetTestClient();

	public Issue261NotFoundGate Gate { get; } = gate;

	public static async Task<Issue261PostStartNotFoundHost> CreateAsync(bool useHtmxor)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue260App).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddSingleton<Issue261NotFoundGate>();
		var razorComponents = builder.Services.AddRazorComponents();
		if (useHtmxor)
		{
			razorComponents.AddHtmxor();
		}

		var app = builder.Build();
		app.Use(SupplyNotFoundDestinationAsync);
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue260App>();
		if (useHtmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new Issue261PostStartNotFoundHost(app, app.Services.GetRequiredService<Issue261NotFoundGate>());
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await app.DisposeAsync();
	}

	private static Task SupplyNotFoundDestinationAsync(HttpContext context, RequestDelegate next)
	{
		if (context.Request.Path == Path)
		{
			context.Items["StatusCodePagesOptions"] = NotFoundDestinationPath;
		}

		return next(context);
	}
}
