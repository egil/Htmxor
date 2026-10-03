using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #270's paired stock-against-candidate exception-handler cases, compiled for both net10.0 and net11.0 (see
// the csproj). Each case re-executes UseExceptionHandler's path re-execution onto #269's own [StreamRendering]
// page with an interactive child (Issue269StreamingPage / Issue269InteractiveChild), so the candidate's
// render-mode boundary renders exactly where stock's own ResolveComponentForRenderMode ignores it ("Ignore the
// render mode boundary in error scenarios"). This reuses Issue269Host rather than a new wrapper: #270 is "not
// safely parallel" with #269 because both share this same fixture, and #269 owns it first.
//
// The origin throws from a plain middleware rather than a routed component, mirroring
// Issue269StreamedBoundaryTests' own ConfigureReexecutionPipeline origin shape. A component origin was tried
// and rejected: rendering any component (even one that immediately throws) initializes the request's scoped
// NavigationManager, and UseExceptionHandler's path re-execution reuses that same scope by default (its
// CreateScopeForErrors option defaults to false), so the re-executed page's own component render then fails
// with "RemoteNavigationManager already initialized" on stock itself -- a framework scoping detail orthogonal
// to the render-mode boundary this issue protects.
public sealed class Issue270ErrorHandlerRenderModeBoundaryTests
{
	private const string OriginPath = "/issue-270/exception-origin";

	[Theory]
	[InlineData("server")]
	[InlineData("webassembly")]
	[InlineData("auto")]
	public async Task An_exception_handler_response_for_a_streaming_page_with_an_interactive_component_matches_stock(string mode)
	{
		var streamingPath = $"/issue-269/streaming/{mode}";
		var gateName = $"issue-269-page-{mode}";
		await using var stock = await Issue269Host.CreateAsync(htmxor: false, app => ConfigureExceptionHandlerPipeline(app, streamingPath));
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true, app => ConfigureExceptionHandlerPipeline(app, streamingPath));

		var stockResult = await RunExceptionHandledAsync(stock, gateName);
		var candidateResult = await RunExceptionHandledAsync(candidate, gateName);

		// Stock's own oracle: UseExceptionHandler's path re-execution always answers 500 for an unhandled
		// component exception, and it renders the re-executed page as an ordinary component tree -- the
		// interactive child directly under the page, with no `<!--Blazor:{` render-mode-boundary marker,
		// because ResolveComponentForRenderMode ignores the boundary while handling errors -- and it suppresses
		// persisted state on this path exactly as it does for a pre-commit exception handler.
		Assert.Equal(HttpStatusCode.InternalServerError, stockResult.Status);
		Assert.Contains("data-issue-269-child=\"rendered\"", stockResult.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("<!--Blazor:{", stockResult.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("Component-State:", stockResult.Body, StringComparison.Ordinal);

		Assert.Equal(stockResult.Status, candidateResult.Status);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);

		// Whole-body parity on content, not bytes: the shared Issue272PersistedState decoder owns which
		// per-host or per-render values are normalized and why. Neither host has persisted state to decode
		// here, so this is a pass-through content comparison that still catches a render-mode boundary's own
		// component id shifting every id after it, not only an extra marker pair.
		var stockDecoded = Issue272PersistedState.Decode(stockResult.Body, stock.Protection);
		Assert.Equal(stockDecoded, Issue272PersistedState.Decode(candidateResult.Body, candidate.Protection));
	}

	private static void ConfigureExceptionHandlerPipeline(WebApplication app, string streamingPath)
	{
		app.UseExceptionHandler(streamingPath);
		app.Use((context, next) =>
		{
			if (context.Request.Path == OriginPath)
			{
				throw new InvalidOperationException("issue-270 exception-handler boundary probe");
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static async Task<(HttpStatusCode Status, string Body, IReadOnlyDictionary<string, string> Headers)> RunExceptionHandledAsync(
		Issue269Host host, string gateName)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync(OriginPath);
		await gate.WaitForReachedAsync(gateName);

		// Handling an exception forces a full-quiescence wait before any output (confirmed against both
		// upstream invokers), so nothing streams before this release lands: release-then-await is deterministic
		// here, with no response bytes racing the release the way a streamed response's partial body would.
		gate.Release(gateName);
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));

		var body = await response.Content.ReadAsStringAsync();
		return (response.StatusCode, body, Issue260Snapshot.NormalizeHeaders(response));
	}
}
