using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// The streaming framing marker is `<!--{Guid.NewGuid()}-->` (InitializeStreamingRenderingFraming), minted fresh
// per host per response, so only its shape -- not its literal value -- is comparable across two separately
// keyed hosts. Shared by every #263 case that reads a streamed body, instead of a per-file copy of the pattern.
internal static class Issue263FramingMarkup
{
	private static readonly Regex Pattern = new(
		"<!--[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}-->",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static string Normalize(string body) => Pattern.Replace(body, "<!--<framing-id>-->");
}

// #263's own red-contract cases for streamed framing, compiled for both net10.0 and net11.0 (see the csproj).
// Every case pairs a stock host against an AddHtmxor candidate host built by Issue260Host, reusing Issue260App's
// existing inherited-streaming page and Issue260Gate's "inherited-child" waypoint instead of adding a third copy
// of a plain [StreamRendering] page/child pair.
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
		Assert.True(stockResult.HasFramingHeader);
		Assert.Contains("<template blazor-component-id=", stockResult.Body, StringComparison.Ordinal);
		Assert.Contains("enhanced-nav=\"true\"", stockResult.Body, StringComparison.Ordinal);

		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(Issue263FramingMarkup.Normalize(stockResult.Body), Issue263FramingMarkup.Normalize(candidateResult.Body));
	}

	private static async Task<Issue263FramingResult> RunAsync(Issue260Host host)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		using var request = new HttpRequestMessage(HttpMethod.Get, Path);
		request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		var responseTask = host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var hasFramingHeader = response.Headers.Contains("ssr-framing");
		var headers = Issue260Snapshot.NormalizeHeaders(response);
		gate.Release("inherited-child");

		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);
		var body = await reader.ReadToEndAsync();
		return new(response.StatusCode, headers, hasFramingHeader, body);
	}

	private sealed record Issue263FramingResult(
		HttpStatusCode StatusCode,
		IReadOnlyDictionary<string, string> Headers,
		bool HasFramingHeader,
		string Body);
}

// v10.0.11 stock allows streaming framing whenever a request is not re-executed via IStatusCodeReExecuteFeature,
// regardless of whether an exception handler is also re-executing it (RazorComponentEndpointInvoker.Render
// passes isErrorHandler and isReExecuted to InitializeStreamingRenderingFraming as two independent flags, and
// that method's own condition is `!isReExecuted`). The candidate's net10.0 framing condition is instead
// `!waitForQuiescence`, where waitForQuiescence already includes isErrorHandler
// (HtmxorEndpointCandidate.RenderComponentCore), so an exception-handler response that is not itself re-executed
// loses both the header and its comment markers on net10.0 even though stock keeps them. #214 aligned net11.0's
// own condition to `!isReexecuted` directly, with no isErrorHandler term, so net11.0 already matches stock here.
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
		// IStatusCodeReExecuteFeature, so stock's own `!isReExecuted` condition still allows framing here.
		Assert.Equal(HttpStatusCode.InternalServerError, stockResult.StatusCode);
		Assert.True(stockResult.HasFramingHeader);

		Assert.Equal(stockResult.StatusCode, candidateResult.StatusCode);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(Issue263FramingMarkup.Normalize(stockResult.Body), Issue263FramingMarkup.Normalize(candidateResult.Body));

		// The direct, target-agnostic form of the defect above: the candidate must carry the framing header
		// exactly when stock does. On net10.0 the candidate's header is absent while stock's is present; on
		// net11.0 both already agree.
		Assert.Equal(stockResult.HasFramingHeader, candidateResult.HasFramingHeader);
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

	// Unlike the ordinary enhanced-navigation case above, an exception-handler response never actually streams
	// on either host: both stock's own RazorComponentEndpointInvoker and the candidate's RenderComponentCore
	// compute `isErrorHandler` into a full-quiescence wait, so no header reaches the client until the gated
	// child's release lets that wait resolve. Releasing before awaiting the response (not after, as the
	// ordinary case does) is what Issue260NavigationEmptyBodyTests' own error-handler cases already rely on for
	// the same reason.
	private static async Task<Issue263FramingResult> RunAsync(Issue260Host host)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		using var request = new HttpRequestMessage(HttpMethod.Get, OriginPath);
		request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		var responseTask = host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");
		gate.Release("inherited-child");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		var hasFramingHeader = response.Headers.Contains("ssr-framing");
		var headers = Issue260Snapshot.NormalizeHeaders(response);
		var body = await response.Content.ReadAsStringAsync();
		return new(response.StatusCode, headers, hasFramingHeader, body);
	}

	private sealed record Issue263FramingResult(
		HttpStatusCode StatusCode,
		IReadOnlyDictionary<string, string> Headers,
		bool HasFramingHeader,
		string Body);
}
