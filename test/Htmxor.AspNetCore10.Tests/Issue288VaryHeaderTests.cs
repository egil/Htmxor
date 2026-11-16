using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

/// <summary>
/// When a component endpoint can serve both a full and a direct representation, every response from it
/// declares <c>Vary: HX-Request, HX-Request-Type</c> while preserving application-supplied <c>Vary</c>
/// values; endpoints with one representation are unchanged.
/// </summary>
public sealed class Issue288VaryHeaderTests
{
	private const string AppVaryHeader = "X-Issue-288-App-Vary";
	private static readonly string[] HtmxVary = ["HX-Request", "HX-Request-Type"];

	public static TheoryData<bool> OutputCacheModes => new() { false, true };

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Dual_page_varies_its_normal_and_partial_responses(bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normal = await client.GetAsync("/issue-288/dual");
		using var partial = await client.SendAsync(Partial(HttpMethod.Get, "/issue-288/dual"));

		await AssertVaryAsync(normal, HttpStatusCode.OK, HtmxVary, "data-issue-288-dual");
		await AssertVaryAsync(partial, HttpStatusCode.OK, HtmxVary, "data-issue-288-dual");
		Assert.DoesNotContain("<html", await partial.Content.ReadAsStringAsync(), StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Dual_page_varies_its_normal_and_htmx_action_responses(bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normal = await client.SendAsync(await ActionRequestAsync(client, htmx: false));
		using var htmx = await client.SendAsync(await ActionRequestAsync(client, htmx: true));

		await AssertVaryAsync(normal, HttpStatusCode.OK, HtmxVary, "saved");
		await AssertVaryAsync(htmx, HttpStatusCode.OK, HtmxVary, "saved");
	}

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Dual_page_varies_its_302_and_HX_Redirect_responses(bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normal = await client.GetAsync("/issue-288/dual?mode=redirect");
		using var htmx = await client.SendAsync(Partial(HttpMethod.Get, "/issue-288/dual?mode=redirect"));

		await AssertVaryAsync(normal, HttpStatusCode.Redirect, HtmxVary);
		Assert.Equal("http://localhost/issue-288/plain", normal.Headers.Location?.OriginalString);
		await AssertVaryAsync(htmx, HttpStatusCode.OK, HtmxVary);
		Assert.Equal("http://localhost/issue-288/plain", Assert.Single(htmx.Headers.GetValues("HX-Redirect")));
	}

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Dual_page_keeps_its_vary_on_a_500_reexecuted_onto_an_error_endpoint_that_is_not_dual(
		bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normal = await client.GetAsync("/issue-288/dual?mode=throw");
		using var htmx = await client.SendAsync(Partial(HttpMethod.Get, "/issue-288/dual?mode=throw"));
		using var errorEndpoint = await client.GetAsync("/issue-288/error");

		await AssertVaryAsync(normal, HttpStatusCode.InternalServerError, HtmxVary, "issue-288 error endpoint");
		await AssertVaryAsync(htmx, HttpStatusCode.InternalServerError, HtmxVary, "issue-288 error endpoint");
		await AssertVaryAsync(errorEndpoint, HttpStatusCode.InternalServerError, [], "issue-288 error endpoint");
	}

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Dual_page_varies_its_own_404(bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normal = await client.GetAsync("/issue-288/dual?mode=not-found");
		using var htmx = await client.SendAsync(Partial(HttpMethod.Get, "/issue-288/dual?mode=not-found"));

		await AssertVaryAsync(normal, HttpStatusCode.NotFound, HtmxVary);
		await AssertVaryAsync(htmx, HttpStatusCode.NotFound, HtmxVary);
	}

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Dual_page_varies_its_own_antiforgery_400(bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normal = await client.PostAsync("/issue-288/dual", new FormUrlEncodedContent([]));
		using var htmxRequest = Partial(HttpMethod.Post, "/issue-288/dual");
		htmxRequest.Content = new FormUrlEncodedContent([]);
		using var htmx = await client.SendAsync(htmxRequest);

		await AssertVaryAsync(normal, HttpStatusCode.BadRequest, HtmxVary);
		await AssertVaryAsync(htmx, HttpStatusCode.BadRequest, HtmxVary);
		Assert.Equal(0, app.Services.GetRequiredService<Issue288Probe>().InitializationCount);
	}

	[Theory]
	[InlineData("Accept-Language", new[] { "Accept-Language", "HX-Request", "HX-Request-Type" })]
	[InlineData("Accept-Language, hx-request", new[] { "Accept-Language", "hx-request", "HX-Request-Type" })]
	[InlineData("HX-Request-Type, HX-Request", new[] { "HX-Request-Type", "HX-Request" })]
	[InlineData("*", new[] { "*" })]
	public async Task Application_vary_values_are_kept_and_not_duplicated(string applicationVary, string[] expected)
	{
		await using var app = await StartAsync(useOutputCache: false);
		using var client = app.GetTestClient();

		using var normalRequest = new HttpRequestMessage(HttpMethod.Get, "/issue-288/dual");
		normalRequest.Headers.Add(AppVaryHeader, applicationVary);
		using var normal = await client.SendAsync(normalRequest);
		using var partialRequest = Partial(HttpMethod.Get, "/issue-288/dual");
		partialRequest.Headers.Add(AppVaryHeader, applicationVary);
		using var partial = await client.SendAsync(partialRequest);

		await AssertVaryAsync(normal, HttpStatusCode.OK, expected);
		await AssertVaryAsync(partial, HttpStatusCode.OK, expected);
	}

	[Fact]
	public async Task Output_cache_hit_replays_the_dual_page_vary()
	{
		await using var app = await StartAsync(useOutputCache: true);
		using var client = app.GetTestClient();

		using var miss = await client.GetAsync("/issue-288/cached");
		using var hit = await client.GetAsync("/issue-288/cached");

		await AssertVaryAsync(miss, HttpStatusCode.OK, HtmxVary, "data-issue-288-cached");
		await AssertVaryAsync(hit, HttpStatusCode.OK, HtmxVary, "data-issue-288-cached");
		Assert.Equal(1, app.Services.GetRequiredService<Issue288Probe>().InitializationCount);
	}

	[Theory]
	[MemberData(nameof(OutputCacheModes))]
	public async Task Endpoints_with_one_representation_get_no_added_vary(bool useOutputCache)
	{
		await using var app = await StartAsync(useOutputCache);
		using var client = app.GetTestClient();

		using var normalOnly = await client.GetAsync("/issue-288/normal-only");
		using var normalOnlyFull = await client.SendAsync(Full("/issue-288/normal-only"));
		using var htmxOnly = await client.SendAsync(Partial(HttpMethod.Get, "/issue-288/htmx-only"));
		using var nonComponent = await client.GetAsync("/issue-288/plain");
		using var nonComponentHtmx = await client.SendAsync(Partial(HttpMethod.Get, "/issue-288/plain"));

		await AssertVaryAsync(normalOnly, HttpStatusCode.OK, [], "data-issue-288-normal-only");
		await AssertVaryAsync(normalOnlyFull, HttpStatusCode.OK, [], "data-issue-288-normal-only");
		await AssertVaryAsync(htmxOnly, HttpStatusCode.OK, [], "data-issue-288-htmx-only");
		await AssertVaryAsync(nonComponent, HttpStatusCode.OK, [], "issue-288 plain");
		await AssertVaryAsync(nonComponentHtmx, HttpStatusCode.OK, [], "issue-288 plain");
	}

	private static HttpRequestMessage Partial(HttpMethod method, string path)
	{
		var request = new HttpRequestMessage(method, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		return request;
	}

	private static HttpRequestMessage Full(string path)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "full");
		return request;
	}

	private static async Task<HttpRequestMessage> ActionRequestAsync(HttpClient client, bool htmx)
	{
		using var page = await client.GetAsync("/issue-288/dual");
		var body = await page.Content.ReadAsStringAsync();
		var token = Regex.Match(body, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
		Assert.True(token.Success, body);
		var cookie = Assert.Single(
			page.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
		var request = htmx ? Partial(HttpMethod.Post, "/issue-288/dual") : new HttpRequestMessage(HttpMethod.Post, "/issue-288/dual");
		request.Headers.Add("Cookie", cookie.Split(';')[0]);
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
		});
		return request;
	}

	private static async Task AssertVaryAsync(
		HttpResponseMessage response,
		HttpStatusCode expectedStatus,
		string[] expectedVary,
		string? expectedBody = null)
	{
		var body = await response.Content.ReadAsStringAsync();
		Assert.True(
			response.StatusCode == expectedStatus,
			$"Expected {(int)expectedStatus}, received {(int)response.StatusCode}. Body: {body}");
		Assert.Equal(expectedVary, response.Headers.Vary.ToArray());
		if (expectedBody is not null)
		{
			Assert.Contains(expectedBody, body, StringComparison.Ordinal);
		}
	}

	private static async Task<WebApplication> StartAsync(bool useOutputCache)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue288VaryHeaderTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Production,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue288Probe>();
		builder.Services.AddRazorComponents().AddHtmxor();
		if (useOutputCache)
		{
			builder.Services.AddOutputCache();
		}

		var app = builder.Build();
		app.UseExceptionHandler("/issue-288/error");
		app.Use((context, next) =>
		{
			if (context.Request.Headers.TryGetValue(AppVaryHeader, out var applicationVary))
			{
				context.Response.Headers.Vary = applicationVary;
			}

			return next(context);
		});
		app.UseAntiforgery();
		if (useOutputCache)
		{
			app.UseOutputCache();
		}

		app.MapGet("/issue-288/error", static () => Results.Text("issue-288 error endpoint", statusCode: 500));
		app.MapGet("/issue-288/plain", static () => Results.Text("issue-288 plain"));
		app.MapRazorComponents<Issue78App>().AddHtmxorEndpoints();
		await app.StartAsync();
		return app;
	}
}

internal sealed class Issue288Probe
{
	private int initializationCount;

	public int InitializationCount => Volatile.Read(ref initializationCount);

	public void RecordInitialization() => Interlocked.Increment(ref initializationCount);
}
