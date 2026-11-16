using System.Net;
using System.Text.Encodings.Web;

namespace Htmxor.AspNetCore10;

// #262: a streamed response that ends, after it has started, in an exception or in a thrown navigation.
// Every case pairs a stock host against an AddHtmxor host and compares status, headers, the whole body read
// to a real end-of-stream, and what a middleware in front of the endpoint observed: whether the endpoint
// completed the response body, and whether it then returned or threw. Compiled for
// net10.0 and net11.0 (see the csproj).
public sealed class Issue262PostStartExceptionParityTests
{
	private const string GenericMessage =
		"There was an unhandled exception on the current request. For more details turn on detailed exceptions " +
		"by setting &#x27;DetailedErrors: true&#x27; in &#x27;appSettings.Development.json&#x27;";

	// Where Exception.ToString() moves from the message to its first stack frame, as HtmlEncoder writes it.
	private static readonly string StackStart = HtmlEncoder.Default.Encode(Environment.NewLine) + "   at ";

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task A_post_start_exception_matches_stocks_generic_error_response(bool enhancedNavigation, bool post)
	{
		var @case = new Issue262Case(Issue262Outcome.Throw, enhancedNavigation, post);
		await using var stock = await Issue262Host.CreateAsync(htmxor: false);
		await using var candidate = await Issue262Host.CreateAsync(htmxor: true);

		var stockResult = await stock.RunAsync(@case);
		var candidateResult = await candidate.RunAsync(@case);

		// Stock's own oracle: the status already sent stays 200, the generic text hides the exception, and the
		// template is the last thing in the body.
		Assert.Equal(HttpStatusCode.OK, stockResult.StatusCode);
		Assert.EndsWith(
			$"<blazor-ssr><template type=\"error\">{GenericMessage}</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
			stockResult.Body,
			StringComparison.Ordinal);
		Assert.DoesNotContain("issue-262 ", stockResult.Body, StringComparison.Ordinal);
		Assert.EndsWith($"started=True completed=True threw System.InvalidOperationException: {Issue262Outcome.Message}", stockResult.PipelineObservation, StringComparison.Ordinal);

		Issue262Assert.SameResponse(stockResult, candidateResult, Issue262Body.Normalize);
	}

	[Theory]
	[InlineData(Issue262Errors.DevelopmentEnvironment, false, false)]
	[InlineData(Issue262Errors.DevelopmentEnvironment, true, false)]
	[InlineData(Issue262Errors.DevelopmentEnvironment, false, true)]
	[InlineData(Issue262Errors.DetailedErrorsSetting, false, false)]
	[InlineData(Issue262Errors.DetailedErrorsSetting, true, true)]
	public async Task A_post_start_exception_matches_stocks_detailed_error_response(
		Issue262Errors errors, bool enhancedNavigation, bool post)
	{
		var @case = new Issue262Case(Issue262Outcome.Throw, enhancedNavigation, post);
		await using var stock = await Issue262Host.CreateAsync(htmxor: false, errors);
		await using var candidate = await Issue262Host.CreateAsync(htmxor: true, errors);

		var stockResult = await stock.RunAsync(@case);
		var candidateResult = await candidate.RunAsync(@case);

		// Stock's own oracle: the encoded exception text, including its stack, replaces the generic message.
		Assert.Equal(HttpStatusCode.OK, stockResult.StatusCode);
		Assert.Contains(
			"<template type=\"error\">System.InvalidOperationException: issue-262 &lt;streamed&gt; &amp; &quot;failed&quot;" + StackStart,
			stockResult.Body,
			StringComparison.Ordinal);
		Assert.Contains("Issue262Outcome.Raise", candidateResult.Body, StringComparison.Ordinal);

		Issue262Assert.SameResponse(stockResult, candidateResult, Issue262Body.NormalizeDetailedError);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task A_post_start_exception_larger_than_the_response_buffer_matches_stock(bool post)
	{
		var @case = new Issue262Case(Issue262Outcome.ThrowLarge, EnhancedNavigation: false, post);
		await using var stock = await Issue262Host.CreateAsync(htmxor: false, Issue262Errors.DetailedErrorsSetting);
		await using var candidate = await Issue262Host.CreateAsync(htmxor: true, Issue262Errors.DetailedErrorsSetting);

		var stockResult = await stock.RunAsync(@case);
		var candidateResult = await candidate.RunAsync(@case);

		// Stock's own oracle: the whole oversized message arrives inside one error template.
		Assert.Contains(Issue262Outcome.LargeText + StackStart, stockResult.Body, StringComparison.Ordinal);
		Assert.EndsWith("</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>", stockResult.Body, StringComparison.Ordinal);

		Issue262Assert.SameResponse(stockResult, candidateResult, Issue262Body.NormalizeDetailedError);
	}
}

public sealed class Issue262PostStartNavigationParityTests
{
	[Theory]
	[InlineData(Issue262Outcome.Navigate, false, false, "<template type=\"redirection\">")]
	[InlineData(Issue262Outcome.Navigate, true, false, "<template type=\"redirection\" enhanced=\"true\">")]
	[InlineData(Issue262Outcome.Navigate, false, true, "<template type=\"redirection\" from=\"form-post\">")]
	[InlineData(Issue262Outcome.Navigate, true, true, "<template type=\"redirection\" from=\"form-post\" enhanced=\"true\">")]
	[InlineData(Issue262Outcome.NavigateLarge, false, false, "<template type=\"redirection\">")]
	[InlineData(Issue262Outcome.NavigateLarge, false, true, "<template type=\"redirection\" from=\"form-post\">")]
	public async Task A_post_start_navigation_matches_stocks_whole_response(
		string outcome, bool enhancedNavigation, bool post, string stockTemplate)
	{
		var @case = new Issue262Case(outcome, enhancedNavigation, post);
		await using var stock = await Issue262Host.CreateAsync(htmxor: false);
		await using var candidate = await Issue262Host.CreateAsync(htmxor: true);

		var stockResult = await stock.RunAsync(@case);
		var candidateResult = await candidate.RunAsync(@case);

		// Stock's own oracle: the streamed update precedes the redirection template, the template carries the
		// request's attributes, and the endpoint returns normally instead of throwing.
		Assert.Equal(HttpStatusCode.OK, stockResult.StatusCode);
		Assert.Contains(Issue262Outcome.UpdatedState(outcome) + "</main>", stockResult.Body, StringComparison.Ordinal);
		Assert.Contains(stockTemplate, stockResult.Body, StringComparison.Ordinal);
		Assert.EndsWith("started=True completed=False returned", stockResult.PipelineObservation, StringComparison.Ordinal);

		Issue262Assert.SameResponse(stockResult, candidateResult, Issue262Body.Normalize);

		// The protected payloads above are normalized, so the destinations they carry are compared here.
		var stockDestination = await stock.ResolveRedirectionAsync(stockResult.Body);
		Assert.EndsWith(Issue262Outcome.Destination, stockDestination, StringComparison.Ordinal);
		Assert.Equal(stockDestination, await candidate.ResolveRedirectionAsync(candidateResult.Body));
	}
}

internal static class Issue262Assert
{
	public static void SameResponse(Issue262Result stock, Issue262Result candidate, Func<Issue262Result, string> normalizeBody)
	{
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(stock.Headers, candidate.Headers);
		Assert.Equal(normalizeBody(stock), normalizeBody(candidate));
		Assert.Equal(stock.PipelineObservation, candidate.PipelineObservation);
	}
}
