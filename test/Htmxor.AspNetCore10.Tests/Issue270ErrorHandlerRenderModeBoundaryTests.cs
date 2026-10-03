using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #270's paired stock-against-candidate exception-handler cases, compiled for both net10.0 and net11.0 (see
// the csproj). UseExceptionHandler re-executes each request onto #269's own [StreamRendering] page with an
// interactive child (Issue269StreamingPage / Issue269InteractiveChild, hosted by Issue269Host), where stock's
// ResolveComponentForRenderMode ignores the render-mode boundary ("Ignore the render mode boundary in error
// scenarios") and the candidate must render the same tree.
public sealed class Issue270ErrorHandlerRenderModeBoundaryTests
{
	private const string MiddlewareOriginPath = "/issue-270/exception-origin";
	private const string ComponentOriginPath = "/issue-270/component-exception-origin";

	// The origin throws from a plain middleware rather than a routed component, mirroring
	// Issue269StreamedBoundaryTests' ConfigureReexecutionPipeline origin. Rendering a component origin
	// initializes the request's scoped NavigationManager, and UseExceptionHandler re-executes in that same scope
	// unless createScopeForErrors is true (the samples and docs/index.md opt in; this pipeline keeps the
	// default), so a component origin here would fail the re-executed page's own render with "RemoteNavigationManager
	// already initialized" on stock itself -- a scoping detail orthogonal to the render-mode boundary this file
	// protects. The paired case below exercises a component origin under the configuration that avoids it.
	[Theory]
	[InlineData("server")]
	[InlineData("webassembly")]
	[InlineData("auto")]
	public async Task An_exception_handler_response_for_a_streaming_page_with_an_interactive_component_matches_stock(string mode)
	{
		var streamingPath = $"/issue-269/streaming/{mode}";
		var gateName = $"issue-269-page-{mode}";
		await using var stock = await Issue269Host.CreateAsync(htmxor: false, app => ConfigureMiddlewareOriginPipeline(app, streamingPath));
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true, app => ConfigureMiddlewareOriginPipeline(app, streamingPath));

		var stockResult = await RunExceptionHandledAsync(stock, MiddlewareOriginPath, gateName);
		var candidateResult = await RunExceptionHandledAsync(candidate, MiddlewareOriginPath, gateName);

		AssertStockOracleAndParity(stockResult, candidateResult, stock, candidate);
	}

	// The paired component-thrown case: a routed page throws instead of a middleware, under
	// UseExceptionHandler(path, createScopeForErrors: true) -- the configuration this repository's own samples
	// and docs/index.md use -- so the re-executed page renders in a fresh DI scope rather than the origin's own.
	[Theory]
	[InlineData("server")]
	[InlineData("webassembly")]
	[InlineData("auto")]
	public async Task A_component_thrown_exception_handler_response_with_a_fresh_error_scope_matches_stock(string mode)
	{
		var streamingPath = $"/issue-269/streaming/{mode}";
		var gateName = $"issue-269-page-{mode}";
		await using var stock = await Issue269Host.CreateAsync(htmxor: false, app => ConfigureComponentOriginPipeline(app, streamingPath));
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true, app => ConfigureComponentOriginPipeline(app, streamingPath));

		var stockResult = await RunExceptionHandledAsync(stock, ComponentOriginPath, gateName);
		var candidateResult = await RunExceptionHandledAsync(candidate, ComponentOriginPath, gateName);

		AssertStockOracleAndParity(stockResult, candidateResult, stock, candidate);
	}

	private static void AssertStockOracleAndParity(
		(HttpStatusCode Status, string Body, IReadOnlyDictionary<string, string> Headers) stockResult,
		(HttpStatusCode Status, string Body, IReadOnlyDictionary<string, string> Headers) candidateResult,
		Issue269Host stock,
		Issue269Host candidate)
	{
		// Stock's own oracle: UseExceptionHandler answers 500 for the origin's exception and renders the
		// re-executed page as an ordinary component tree -- the interactive child directly under the page, with
		// no `<!--Blazor:{` render-mode-boundary marker, because ResolveComponentForRenderMode ignores the
		// boundary while handling errors -- and the invoker writes no persisted state while
		// IExceptionHandlerFeature is present. The page's own post-release "updated" state pins the
		// full-quiescence premise RunExceptionHandledAsync's release-then-await ordering relies on. The child's
		// own AssignedRenderMode reads "none", because stock assigns no render mode to a component outside any
		// render-mode boundary.
		Assert.Equal(HttpStatusCode.InternalServerError, stockResult.Status);
		Assert.Contains("data-issue-269-child=\"rendered\"", stockResult.Body, StringComparison.Ordinal);
		Assert.Contains("data-issue-269-page=\"updated\"", stockResult.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("<!--Blazor:{", stockResult.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("Component-State:", stockResult.Body, StringComparison.Ordinal);
		Assert.Equal("none", ExtractChildRenderMode(stockResult.Body));

		Assert.Equal(stockResult.Status, candidateResult.Status);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);

		// The render mode the component itself observes (ComponentBase.AssignedRenderMode, surfaced by
		// Issue269InteractiveChild's own always-on marker) is a distinct, component-visible risk from the
		// streaming-marker shape: a fix could remove the extra marker pair while still leaving the component
		// able to see an assigned render mode it should not have outside any boundary. Comparing it directly,
		// not only inside the whole-body assertion below, keeps that risk visible on its own.
		Assert.Equal(ExtractChildRenderMode(stockResult.Body), ExtractChildRenderMode(candidateResult.Body));

		// Whole-body parity on content, not bytes: the shared Issue272PersistedState decoder owns which
		// per-host or per-render values are normalized and why. Neither host writes persisted state on this
		// path, so this is a pass-through content comparison that still catches a render-mode boundary's own
		// component id shifting every id after it, not only an extra marker pair.
		var stockDecoded = Issue272PersistedState.Decode(stockResult.Body, stock.Protection);
		Assert.Equal(stockDecoded, Issue272PersistedState.Decode(candidateResult.Body, candidate.Protection));
	}

	private static void ConfigureMiddlewareOriginPipeline(WebApplication app, string streamingPath)
	{
		app.UseExceptionHandler(streamingPath);
		app.Use((context, next) =>
		{
			if (context.Request.Path == MiddlewareOriginPath)
			{
				throw new InvalidOperationException("issue-270 exception-handler boundary probe");
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static void ConfigureComponentOriginPipeline(WebApplication app, string streamingPath)
	{
		// createScopeForErrors: true -- the configuration this repository's own samples and docs/index.md use --
		// gives the re-executed page a fresh DI scope, so the component origin's own already-initialized
		// NavigationManager (see the sibling case's header comment) never collides with the re-executed page's.
		app.UseExceptionHandler(streamingPath, createScopeForErrors: true);
		app.UseRouting();
	}

	private static async Task<(HttpStatusCode Status, string Body, IReadOnlyDictionary<string, string> Headers)> RunExceptionHandledAsync(
		Issue269Host host, string originPath, string gateName)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		var responseTask = host.Client.GetAsync(originPath);
		await gate.WaitForReachedAsync(gateName);

		// Handling an exception forces a full-quiescence wait before any output (confirmed against both
		// upstream invokers), so nothing streams before this release lands: release-then-await is deterministic
		// here, with no response bytes racing the release the way a streamed response's partial body would. The
		// oracle's own "updated" assertion checks this premise directly instead of only relying on it.
		gate.Release(gateName);
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));

		var body = await response.Content.ReadAsStringAsync();
		return (response.StatusCode, body, Issue260Snapshot.NormalizeHeaders(response));
	}

	private static string ExtractChildRenderMode(string body)
	{
		var match = Regex.Match(body, "data-issue-269-child-render-mode=\"([^\"]*)\"");
		return match.Success ? match.Groups[1].Value : "<missing>";
	}
}
