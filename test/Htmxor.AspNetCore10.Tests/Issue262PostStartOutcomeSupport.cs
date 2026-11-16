using System.IO.Pipelines;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Htmxor.AspNetCore10;

// What Issue262StreamingOutcomePage and Issue262StreamingFormPage do once the test releases them: the only
// test-owned parts of a #262 case are this gate and the outcome it raises.
internal static class Issue262Outcome
{
	public const string Waypoint = "issue-262-outcome";
	public const string FormName = "issue-262-outcome";
	public const string Throw = "throw";
	public const string ThrowLarge = "throw-large";
	public const string Navigate = "navigate";
	public const string NavigateLarge = "navigate-large";
	public const string Destination = "/issue-262/destination?from=streamed&step=2";

	// Carries characters HtmlEncoder must encode, so a candidate that writes the message raw differs from stock.
	public const string Message = "issue-262 <streamed> & \"failed\"";

	// Larger than the 16 KiB character buffer of the response writer both invokers construct, so one streamed
	// update or one error template cannot fit in that buffer and has to pass through whatever sits above it.
	public static readonly string LargeText = new('x', 40_000);

	public static string UpdatedState(string? outcome)
		=> outcome == NavigateLarge ? "updated " + LargeText : "updated";

	public static string MessageFor(string? outcome)
		=> outcome == ThrowLarge ? Message + " " + LargeText : Message;

	public static void Raise(string? outcome, NavigationManager navigation)
	{
		if (outcome is Navigate or NavigateLarge)
		{
			// Throws NavigationException under the default switch setting, which is the path #262 owns.
			navigation.NavigateTo(Destination);
		}

		throw new InvalidOperationException(MessageFor(outcome));
	}
}

public enum Issue262Errors
{
	Generic,
	DevelopmentEnvironment,
	DetailedErrorsSetting,
}

internal sealed record Issue262Case(string Outcome, bool EnhancedNavigation, bool Post)
{
	public string Path => (Post ? "/issue-262/streaming-form" : "/issue-262/streaming") + "?Outcome=" + Outcome;
}

internal sealed record Issue262Result(
	HttpStatusCode StatusCode,
	IReadOnlyDictionary<string, string> Headers,
	string? FramingHeaderValue,
	string Body,
	string PipelineObservation);

// What an application-owned middleware in front of the endpoint observed for the one outcome request: whether
// the endpoint returned or threw, what it threw, whether the response had started by then, and whether the
// endpoint had completed the response body itself before that.
internal sealed class Issue262Journal
{
	private readonly TaskCompletionSource<string> observation = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private bool bodyCompleted;

	public Task<string> Observation => observation.Task.WaitAsync(TimeSpan.FromSeconds(20));

	public void RecordBodyCompleted() => bodyCompleted = true;

	public void Record(HttpContext context, Exception? exception)
	{
		var outcome = exception is null ? "returned" : $"threw {exception.GetType().FullName}: {Abbreviate(exception.Message)}";
		observation.TrySetResult(
			$"{context.Request.Method} {context.Request.Path} started={context.Response.HasStarted} completed={bodyCompleted} {outcome}");
	}

	// Enough to tell the cases' messages apart without printing the large case's 40,000 filler characters.
	private static string Abbreviate(string message) => message.Length <= 64 ? message : message[..64] + "...";
}

// Passes every member through to the server's own response body feature and only notes that the endpoint
// called CompleteAsync. TestServer's client cannot see that call (its body stream ends when the request does),
// so the call itself is the observable completion behavior at this boundary.
internal sealed class Issue262CompletionObserver(IHttpResponseBodyFeature inner, Issue262Journal journal) : IHttpResponseBodyFeature
{
	public Stream Stream => inner.Stream;

	public PipeWriter Writer => inner.Writer;

	public void DisableBuffering() => inner.DisableBuffering();

	public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);

	public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
		=> inner.SendFileAsync(path, offset, count, cancellationToken);

	public Task CompleteAsync()
	{
		journal.RecordBodyCompleted();
		return inner.CompleteAsync();
	}
}

// A stock or AddHtmxor host for #262's cases, over Issue260Host's TestServer and data-protection wiring. The
// real endpoint factory, antiforgery middleware, environment, RazorComponentsServiceOptions and response body
// features stay in charge; this adds only the pipeline observer below.
internal sealed class Issue262Host(Issue260Host host) : IAsyncDisposable
{
	private const string FormPath = "/issue-262/streaming-form";

	public HttpClient Client => host.Client;

	public Issue260Gate Gate { get; } = host.Services.GetRequiredService<Issue260Gate>();

	public Issue262Journal Journal { get; } = host.Services.GetRequiredService<Issue262Journal>();

	public static async Task<Issue262Host> CreateAsync(bool htmxor, Issue262Errors errors = Issue262Errors.Generic)
		=> new(await Issue260Host.CreateAsync<Issue260App>(
			htmxor,
			configurePipeline: ObserveEndpoint,
			configureServices: services => services.AddSingleton<Issue262Journal>(),
			configureBuilder: builder => ConfigureErrors(builder, errors)));

	// RazorComponentsServiceOptions.DetailedErrors is bound from the host's "detailedErrors" setting by the
	// framework's own post-configuration, so the setting is the real way an application turns it on.
	private static void ConfigureErrors(WebApplicationBuilder builder, Issue262Errors errors)
	{
		builder.Environment.EnvironmentName =
			errors == Issue262Errors.DevelopmentEnvironment ? Environments.Development : Environments.Production;
		builder.Configuration[WebHostDefaults.DetailedErrorsKey] =
			errors == Issue262Errors.DetailedErrorsSetting ? "true" : "false";
	}

	// Records what the endpoint did and then handles the exception itself, as an application's own exception
	// middleware would. Everything the endpoint wrote, completed and threw has been observed by then, and the
	// request ending normally lets the client read every byte to a real end-of-stream on both hosts; an
	// exception left to reach TestServer would instead abort the client's stream at an arbitrary point.
	private static void ObserveEndpoint(WebApplication app) => app.Use(async (context, next) =>
	{
		if (!context.Request.Query.ContainsKey("Outcome"))
		{
			await next(context);
			return;
		}

		var journal = context.RequestServices.GetRequiredService<Issue262Journal>();
		context.Features.Set<IHttpResponseBodyFeature>(
			new Issue262CompletionObserver(context.Features.GetRequiredFeature<IHttpResponseBodyFeature>(), journal));
		try
		{
			await next(context);
			journal.Record(context, null);
		}
		catch (Exception exception)
		{
			journal.Record(context, exception);
		}
	});

	public async Task<Issue262Result> RunAsync(Issue262Case @case)
	{
		using var request = @case.Post ? await CreateFormPostAsync(@case.Path) : new HttpRequestMessage(HttpMethod.Get, @case.Path);
		if (@case.EnhancedNavigation)
		{
			request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		}

		using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(TimeSpan.FromSeconds(5));
		await using var reader = await Issue264StreamingBodyReader.CreateAsync(response);
		const string initialMarker = "data-issue-262=\"initial\"";
		Assert.Contains(initialMarker, await reader.ReadUntilAsync([initialMarker]), StringComparison.Ordinal);

		Gate.Release(Issue262Outcome.Waypoint);
		// A genuine end-of-stream: ReadToEndAsync throws on a timeout or a faulted read instead of returning.
		var body = await reader.ReadToEndAsync();
		var observation = await Journal.Observation;

		var framing = response.Headers.TryGetValues("ssr-framing", out var values) ? values.Single() : null;
		return new(response.StatusCode, Issue260Snapshot.NormalizeHeaders(response), framing, body, observation);
	}

	// The form page's own GET issues the real antiforgery cookie and request token the POST then presents.
	private async Task<HttpRequestMessage> CreateFormPostAsync(string path)
	{
		using var page = await Client.GetAsync(FormPath);
		var token = Regex.Match(
			await page.Content.ReadAsStringAsync(),
			"name=\"__RequestVerificationToken\" value=\"([^\"]+)\"",
			RegexOptions.CultureInvariant);
		Assert.True(token.Success, "The stock form must issue a real antiforgery request token.");

		var request = new HttpRequestMessage(HttpMethod.Post, path);
		request.Headers.Add("Cookie", string.Join("; ", page.Headers.GetValues("Set-Cookie").Select(cookie => cookie.Split(';')[0])));
		request.Headers.Add("Sec-Fetch-Site", "same-origin");
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = Issue262Outcome.FormName,
			["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
		});
		return request;
	}

	// The opaque-redirect payload is meaningful only to the host that protected it, so this asks that same
	// host to resolve it and reports where it redirects.
	public async Task<string?> ResolveRedirectionAsync(string body)
	{
		var match = Regex.Match(body, "<template type=\"redirection\"[^>]*>([^<]+)</template>", RegexOptions.CultureInvariant);
		Assert.True(match.Success, $"Expected a redirection template in: {body}");
		using var redirect = await Client.GetAsync(WebUtility.HtmlDecode(match.Groups[1].Value));
		Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
		return redirect.Headers.Location?.OriginalString;
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();
}

// The only three things a #262 body comparison does not compare byte for byte, each for a stated reason.
internal static partial class Issue262Body
{
	// 1. The streaming framing identifier is a fresh Guid per response; Issue263FramingMarkup replaces only the
	//    marker that equals that same response's own ssr-framing header, so a body marker that disagrees with
	//    its header still fails.
	// 2. Each host has its own ephemeral data-protection keys, so an antiforgery token and an opaque-redirect
	//    payload can never be equal across hosts. Their destinations are compared separately by resolving each
	//    payload on the host that protected it.
	public static string Normalize(Issue262Result result) => Issue264Snapshot.NormalizeProtectedPayloads(
		Issue263FramingMarkup.Normalize(result.Body, result.FramingHeaderValue));

	// 3. A detailed error is Exception.ToString(), whose stack frames name the invoker and renderer types of
	//    whichever host rendered it, so the frames below the page's own can never be equal. This keeps the
	//    template's exact position and everything outside it, and inside it keeps the exception's type and
	//    message plus every frame of the test page itself, dropping only the frames of other types.
	public static string NormalizeDetailedError(Issue262Result result)
		=> ErrorTemplate().Replace(Normalize(result), match => $"<template type=\"error\">{KeepOwnFrames(match.Groups[1].Value)}</template>");

	private static string KeepOwnFrames(string encodedText)
	{
		var lines = WebUtility.HtmlDecode(encodedText).Split('\n');
		return string.Join('\n', lines.Where((line, index) => index == 0 || line.Contains("Issue262", StringComparison.Ordinal)));
	}

	[GeneratedRegex("<template type=\"error\">([^<]*)</template>", RegexOptions.CultureInvariant)]
	private static partial Regex ErrorTemplate();
}
