using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// The streaming framing marker is `<!--{Guid.NewGuid()}-->` (InitializeStreamingRenderingFraming): one identifier
// minted per response, written to both the `ssr-framing` header and every body marker the client's own splitting
// logic correlates with that header. Only that identifier's presence is comparable across two separately keyed
// hosts, not its literal value. Normalizing a body by that *same response's own* header value, instead of
// blanking every GUID-shaped comment independently, keeps a candidate whose header and body markers disagree
// observable: the header-to-marker identity is exactly what a client depends on to split the stream correctly.
internal static class Issue263FramingMarkup
{
	private static readonly Regex AnyMarker = new(
		"<!--[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}-->",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	// Replaces exactly the marker matching `headerValue` -- that same response's own "ssr-framing" header value,
	// or null when the header is absent -- with a fixed placeholder. A body marker that does not match the
	// header is left untouched, so it still fails whole-body parity instead of being hidden by this
	// normalization.
	public static string Normalize(string body, string? headerValue)
		=> headerValue is null ? body : body.Replace($"<!--{headerValue}-->", "<!--<framing-id>-->", StringComparison.Ordinal);

	// Used only where no header carries a value to correlate against, such as proving a response that never
	// frames also carries no stray framing-shaped marker in its body.
	public static bool ContainsAnyMarker(string body) => AnyMarker.IsMatch(body);
}

// #263's paired stock-against-candidate cases for streamed framing, compiled for both net10.0 and net11.0 (see
// the csproj). Every case pairs a stock host against an AddHtmxor candidate host built by Issue260Host, reusing
// Issue260App's existing inherited-streaming page and Issue260Gate's "inherited-child" waypoint instead of
// adding a third copy of a plain [StreamRendering] page/child pair.
public sealed class Issue263EnhancedNavigationStreamingTests
{
	private const string Path = "/issue-260/inherited-streaming";

	[Fact]
	public async Task An_enhanced_navigation_request_to_a_streaming_page_matches_stocks_framing_like_an_ordinary_one()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);

		var stockResult = await RunAsync(stock);
		var candidateResult = await RunAsync(candidate);

		// Stock's own oracle: an enhanced-navigation request is not re-executed and not Direct-routed, so stock
		// still allows framing for it, carries the header, and marks the streamed <template> update enhanced.
		Assert.NotNull(stockResult.FramingHeaderValue);
		Assert.Contains("<template blazor-component-id=", stockResult.Body, StringComparison.Ordinal);
		Assert.Contains("enhanced-nav=\"true\"", stockResult.Body, StringComparison.Ordinal);

		Assert.Equal(stockResult.FramingHeaderValue is not null, candidateResult.FramingHeaderValue is not null);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(
			Issue263FramingMarkup.Normalize(stockResult.Body, stockResult.FramingHeaderValue),
			Issue263FramingMarkup.Normalize(candidateResult.Body, candidateResult.FramingHeaderValue));
	}

	private static Task<Issue263FramingResult> RunAsync(Issue260Host host)
		=> Issue263FramingRun.RunReleasingAfterHeadersAsync(host, Path, enhancedNavigation: true);
}

// Stock allows streaming framing whenever a request is not status-code re-executed (IStatusCodeReExecuteFeature),
// independently of whether an exception handler is re-executing it, so an exception-handled enhanced-navigation
// response still carries the ssr-framing header. That response never streams on either host (an error-handler
// render waits for full quiescence), so the header is the only framing observable here: neither body carries
// framing comment markers.
public sealed class Issue263ExceptionHandlerFramingTests
{
	private const string OriginPath = "/issue-263/exception-origin";
	private const string ErrorPath = "/issue-260/inherited-streaming";

	[Fact]
	public async Task An_exception_handled_streaming_response_matches_stocks_enhanced_navigation_framing_header()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, ConfigurePipeline);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, ConfigurePipeline);

		var stockResult = await RunAsync(stock);
		var candidateResult = await RunAsync(candidate);

		// Stock's own oracle: UseExceptionHandler re-executes to the configured path without setting
		// IStatusCodeReExecuteFeature, so stock's own `!isReExecuted` condition still allows framing here, even
		// though the response never actually streams.
		Assert.Equal(HttpStatusCode.InternalServerError, stockResult.StatusCode);
		Assert.NotNull(stockResult.FramingHeaderValue);
		Assert.False(Issue263FramingMarkup.ContainsAnyMarker(stockResult.Body));

		// The framing header's presence first, so a mismatch is reported as that header rather than as a
		// whole-header difference.
		Assert.Equal(stockResult.FramingHeaderValue is not null, candidateResult.FramingHeaderValue is not null);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(
			Issue263FramingMarkup.Normalize(stockResult.Body, stockResult.FramingHeaderValue),
			Issue263FramingMarkup.Normalize(candidateResult.Body, candidateResult.FramingHeaderValue));
	}

	private static void ConfigurePipeline(WebApplication app)
	{
		app.UseExceptionHandler(ErrorPath);
		app.Use((context, next) =>
		{
			if (context.Request.Path == OriginPath)
			{
				throw new InvalidOperationException("issue-263 exception origin");
			}

			return next(context);
		});
		app.UseRouting();
	}

	private static Task<Issue263FramingResult> RunAsync(Issue260Host host)
		=> Issue263FramingRun.RunReleasingBeforeHeadersAsync(host, OriginPath, enhancedNavigation: true);
}

// Stock's framing condition has two halves: frame when a request is not status-code re-executed, and do not
// frame when it is. A fix that only adds the first half (for example, allowing framing unconditionally) would
// turn the exception-handler case above green while still disagreeing with stock here, since a status-code
// re-execution is not an exception-handler re-execution and carries no IExceptionHandlerFeature. Paired, because
// stock has its own status-code re-execution oracle (UseStatusCodePagesWithReExecute).
public sealed class Issue263ReexecutionFramingTests
{
	private const string OriginPath = "/issue-263/reexecution-origin";
	private const string ReexecutedPath = "/issue-260/inherited-streaming";

	[Fact]
	public async Task A_status_reexecuted_enhanced_navigation_request_matches_stocks_suppressed_framing()
	{
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, ConfigurePipeline);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, ConfigurePipeline);

		var stockResult = await RunAsync(stock);
		var candidateResult = await RunAsync(candidate);

		// Stock's own oracle: a status-code re-execution sets IStatusCodeReExecuteFeature, so stock's own
		// `!isReExecuted` condition suppresses framing here, unlike the exception-handler case above.
		Assert.Null(stockResult.FramingHeaderValue);
		Assert.False(Issue263FramingMarkup.ContainsAnyMarker(stockResult.Body));

		Assert.Equal(stockResult.FramingHeaderValue is not null, candidateResult.FramingHeaderValue is not null);
		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(
			Issue263FramingMarkup.Normalize(stockResult.Body, stockResult.FramingHeaderValue),
			Issue263FramingMarkup.Normalize(candidateResult.Body, candidateResult.FramingHeaderValue));
	}

	private static void ConfigurePipeline(WebApplication app)
	{
		app.UseStatusCodePagesWithReExecute(ReexecutedPath);
		app.Use((context, next) =>
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

	// Neither of Issue263FramingRun's two generic orderings is safe here: v10.0.11 stock's own AddPendingTask
	// (and the candidate's matching short-circuit) does not track a re-executed request's pending work at all,
	// so on net10.0 the response completes on its own -- with the child's pre-release "initial" state -- without
	// ever needing this gate's release, the same net10.0-only short-circuit Issue260ReexecutionParityTests'
	// own reexecuted-streaming-child case releases around identically. Awaiting the response before releasing on
	// net10.0 removes the race between that unprompted completion and this test's own release; net11.0 does
	// track the work, so releasing first is both safe and necessary there.
	private static async Task<Issue263FramingResult> RunAsync(Issue260Host host)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		using var request = new HttpRequestMessage(HttpMethod.Get, OriginPath);
		request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		var responseTask = host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");

#if NET11_0_OR_GREATER
		gate.Release("inherited-child");
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
#else
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		gate.Release("inherited-child");
#endif

		var framingHeaderValue = response.Headers.TryGetValues("ssr-framing", out var values) ? values.Single() : null;
		var headers = Issue260Snapshot.NormalizeHeaders(response);
		var body = await response.Content.ReadAsStringAsync();
		return new(response.StatusCode, headers, framingHeaderValue, body);
	}
}

// #263 acceptance criterion 4's "direct htmx responses remain unframed" clause: Direct routing is Htmxor's own
// concept (selected by HX-Request-Type: partial) with no stock oracle, so this is candidate-only, the same
// convention #260's own Direct-mode cases use. Guards against a framing-condition fix that also widens framing
// to Direct responses, which the issue's own Evidence section rules out ("Direct htmx responses deliberately
// never frame, and stay that way").
public sealed class Issue263DirectRoutingFramingTests
{
	private const string Path = "/issue-260/inherited-streaming";

	[Fact]
	public async Task A_direct_htmx_enhanced_navigation_request_never_frames()
	{
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true);
		var gate = candidate.Services.GetRequiredService<Issue260Gate>();

		using var request = new HttpRequestMessage(HttpMethod.Get, Path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		var responseTask = candidate.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");

		// Direct routing forces full quiescence (RoutingMode.Direct is one of waitForQuiescence's own terms on
		// both targets), so -- like the exception-handler case above -- no header reaches the
		// client until the gated child's release lets that wait resolve.
		gate.Release("inherited-child");
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.False(response.Headers.Contains("ssr-framing"));
		var body = await response.Content.ReadAsStringAsync();
		Assert.Contains("data-issue-260-page", body, StringComparison.Ordinal);
		Assert.False(Issue263FramingMarkup.ContainsAnyMarker(body));
	}
}

internal sealed record Issue263FramingResult(
	HttpStatusCode StatusCode,
	IReadOnlyDictionary<string, string> Headers,
	string? FramingHeaderValue,
	string Body);

// Shared request/response plumbing for every #263 framing case: both release orderings this file needs, against
// the same Issue260Host and Issue260Gate "inherited-child" waypoint.
internal static class Issue263FramingRun
{
	// For a request whose response streams immediately (an ordinary or enhanced-navigation GET with no
	// full-quiescence term set): release only once headers have arrived, so the child stays pending at the
	// initial write and the response always takes the streamed path instead of racing Reached against the
	// child's own await.
	public static async Task<Issue263FramingResult> RunReleasingAfterHeadersAsync(
		Issue260Host host, string path, bool enhancedNavigation)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		using var request = CreateRequest(path, enhancedNavigation);
		var responseTask = host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var framingHeaderValue = GetFramingHeaderValue(response);
		var headers = Issue260Snapshot.NormalizeHeaders(response);
		gate.Release("inherited-child");

		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);
		var body = await reader.ReadToEndAsync();
		return new(response.StatusCode, headers, framingHeaderValue, body);
	}

	// For a request that forces full quiescence on both hosts (an exception-handler re-execution), both hosts
	// withhold every header until that wait resolves, so releasing must happen before awaiting the response
	// rather than after. A status-code re-execution does not qualify: net10.0 does not track its pending work.
	public static async Task<Issue263FramingResult> RunReleasingBeforeHeadersAsync(
		Issue260Host host, string path, bool enhancedNavigation)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		using var request = CreateRequest(path, enhancedNavigation);
		var responseTask = host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");
		gate.Release("inherited-child");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var framingHeaderValue = GetFramingHeaderValue(response);
		var headers = Issue260Snapshot.NormalizeHeaders(response);
		var body = await response.Content.ReadAsStringAsync();
		return new(response.StatusCode, headers, framingHeaderValue, body);
	}

	private static HttpRequestMessage CreateRequest(string path, bool enhancedNavigation)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (enhancedNavigation)
		{
			request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		}

		return request;
	}

	private static string? GetFramingHeaderValue(HttpResponseMessage response)
		=> response.Headers.TryGetValues("ssr-framing", out var values) ? values.Single() : null;
}
