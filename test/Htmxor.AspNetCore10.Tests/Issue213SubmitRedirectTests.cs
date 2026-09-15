using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

// The candidate previously let a NavigationException from a completed submit escape the invoker. These run on
// every target framework, because that repair is not .NET 11 only unlike the TempData adapter that exposed it.
public sealed class Issue213SubmitRedirectTests
{
	private const string FormPath = "/issue-213/submit-redirect";
	private const string Destination = "/issue-213/after-submit";
	private const string External = "https://example.com/elsewhere";

	[Fact]
	public async Task Completed_submit_navigation_has_stock_redirect_parity()
	{
		await using var stock = await Issue213RedirectHost.StartAsync(htmxor: false);
		await using var candidate = await Issue213RedirectHost.StartAsync(htmxor: true);

		var stockSnapshot = await stock.SubmitAsync(htmx: false);
		var candidateSnapshot = await candidate.SubmitAsync(htmx: false);

		Assert.Equal(HttpStatusCode.Found, stockSnapshot.Status);
		Assert.EndsWith(Destination, stockSnapshot.Location, StringComparison.Ordinal);
		Assert.Equal(stockSnapshot.Status, candidateSnapshot.Status);
		Assert.Equal(stockSnapshot.Location, candidateSnapshot.Location);
		Assert.Null(candidateSnapshot.HxRedirect);
		// Stock writes the body it had already rendered before the submit, so the redirect carries it too.
		Assert.Equal(NormalizeBody(stockSnapshot.Body), NormalizeBody(candidateSnapshot.Body));
		Assert.Contains("data-issue-213-output", candidateSnapshot.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Completed_submit_navigation_asks_an_htmx_client_to_navigate()
	{
		await using var candidate = await Issue213RedirectHost.StartAsync(htmxor: true);

		var snapshot = await candidate.SubmitAsync(htmx: true);

		// An htmx request cannot follow a 302 for a partial response, so the destination arrives as a header it
		// can act on, keeping the request's own path and query rather than a base-relative fragment.
		Assert.Equal(HttpStatusCode.OK, snapshot.Status);
		Assert.Equal(Destination, snapshot.HxRedirect);
		Assert.Null(snapshot.Location);
	}

	[Fact]
	public async Task Awaited_submit_navigation_has_stock_redirect_parity()
	{
		await using var stock = await Issue213RedirectHost.StartAsync(htmxor: false);
		await using var candidate = await Issue213RedirectHost.StartAsync(htmxor: true);

		// The navigation surfaces on the dispatch task, not from the dispatch call, which stock also covers.
		var stockSnapshot = await stock.SubmitAsync(htmx: false, awaited: true);
		var candidateSnapshot = await candidate.SubmitAsync(htmx: false, awaited: true);

		Assert.Equal(HttpStatusCode.Found, stockSnapshot.Status);
		Assert.Equal(stockSnapshot.Status, candidateSnapshot.Status);
		Assert.Equal(stockSnapshot.Location, candidateSnapshot.Location);
	}

	[Fact]
	public async Task External_submit_navigation_under_enhanced_navigation_has_stock_opaque_redirect_parity()
	{
		await using var stock = await Issue213RedirectHost.StartAsync(htmxor: false);
		await using var candidate = await Issue213RedirectHost.StartAsync(htmxor: true);

		var stockSnapshot = await stock.SubmitAsync(htmx: false, destination: External, enhancedNavigation: true);
		var candidateSnapshot = await candidate.SubmitAsync(htmx: false, destination: External, enhancedNavigation: true);

		// Stock prefers an opaque redirection for an external destination so post-redirect-get keeps working.
		Assert.NotNull(stockSnapshot.EnhancedNavigationLocation);
		Assert.Null(stockSnapshot.Location);
		Assert.Equal(stockSnapshot.Status, candidateSnapshot.Status);
		Assert.NotNull(candidateSnapshot.EnhancedNavigationLocation);
		Assert.Null(candidateSnapshot.Location);
	}

	[Fact]
	public async Task Submit_navigation_keeps_the_fragment_an_htmx_client_needs()
	{
		await using var candidate = await Issue213RedirectHost.StartAsync(htmxor: true);

		var snapshot = await candidate.SubmitAsync(htmx: true, destination: Destination + "#section");

		// A relative reference carrying a fragment is not a well-formed URI, so the absolute form is kept
		// rather than dropping the anchor the client needs.
		Assert.NotNull(snapshot.HxRedirect);
		Assert.EndsWith(Destination + "#section", snapshot.HxRedirect, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Submit_navigation_to_a_non_http_destination_has_stock_redirect_parity()
	{
		await using var stock = await Issue213RedirectHost.StartAsync(htmxor: false);
		await using var candidate = await Issue213RedirectHost.StartAsync(htmxor: true);

		// HX-Redirect only accepts http(s), so a scheme htmx cannot act on must keep the stock representation
		// rather than failing the request.
		var stockSnapshot = await stock.SubmitAsync(htmx: true, destination: "mailto:someone@example.com");
		var candidateSnapshot = await candidate.SubmitAsync(htmx: true, destination: "mailto:someone@example.com");

		Assert.Equal(stockSnapshot.Status, candidateSnapshot.Status);
		Assert.Equal(stockSnapshot.Location, candidateSnapshot.Location);
	}

	// The antiforgery request token is fresh per request, so it is not a parity difference.
	private static string NormalizeBody(string body)
		=> Regex.Replace(body, "value=\"CfDJ8[^\"]+\"", "value=\"token\"", RegexOptions.CultureInvariant);

	internal sealed class Issue213RedirectHost(WebApplication app) : IAsyncDisposable
	{
		private readonly HttpClient client = app.GetTestClient();

		public static async Task<Issue213RedirectHost> StartAsync(bool htmxor)
		{
			var builder = WebApplication.CreateBuilder(new WebApplicationOptions
			{
				ApplicationName = typeof(Issue213SubmitRedirectPage).Assembly.GetName().Name,
			});
			builder.WebHost.UseTestServer();
			builder.Logging.ClearProviders();
			builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
			var components = builder.Services.AddRazorComponents();
			if (htmxor)
			{
				components.AddHtmxor();
			}

			var app = builder.Build();
			app.UseAntiforgery();
			var endpoints = app.MapRazorComponents<Issue78App>();
			if (htmxor)
			{
				endpoints.AddHtmxorEndpoints();
			}

			await app.StartAsync();
			return new(app);
		}

		public async Task<Issue213RedirectSnapshot> SubmitAsync(
			bool htmx, string? destination = null, bool awaited = false, bool enhancedNavigation = false)
		{
			var query = destination is null ? "" : $"?Destination={Uri.EscapeDataString(destination)}";
			using var page = await client.GetAsync(FormPath + query);
			var body = await page.Content.ReadAsStringAsync();
			var token = Regex.Match(body, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
			Assert.True(token.Success, "The stock form must issue a real antiforgery request token.");

			using var request = BuildRequest(query, page, WebUtility.HtmlDecode(token.Groups[1].Value), awaited);
			AddRequestHeaders(request, htmx, enhancedNavigation);
			using var response = await client.SendAsync(request);
			return new(
				response.StatusCode,
				response.Headers.Location?.ToString(),
				Header(response, "HX-Redirect"),
				Header(response, "blazor-enhanced-nav-redirect-location"),
				await response.Content.ReadAsStringAsync());
		}

		private static HttpRequestMessage BuildRequest(string query, HttpResponseMessage page, string token, bool awaited)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, FormPath + query);
			if (page.Headers.TryGetValues("Set-Cookie", out var cookies))
			{
				request.Headers.Add("Cookie", string.Join("; ", cookies.Select(cookie => cookie.Split(';')[0])));
			}

			request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["_handler"] = awaited ? "issue-213-go-awaited" : "issue-213-go",
				["__RequestVerificationToken"] = token,
			});
			return request;
		}

		private static void AddRequestHeaders(HttpRequestMessage request, bool htmx, bool enhancedNavigation)
		{
			request.Headers.Add("Sec-Fetch-Site", "same-origin");
			if (enhancedNavigation)
			{
				request.Headers.Add("Accept", "text/html; blazor-enhanced-nav=on");
			}

			if (htmx)
			{
				request.Headers.Add("HX-Request", "true");
				request.Headers.Add("HX-Request-Type", "partial");
			}
		}

		private static string? Header(HttpResponseMessage response, string name)
			=> response.Headers.TryGetValues(name, out var values) ? values.Single() : null;

		public async ValueTask DisposeAsync()
		{
			client.Dispose();
			await app.DisposeAsync();
		}
	}

	internal sealed record Issue213RedirectSnapshot(
		HttpStatusCode Status, string? Location, string? HxRedirect, string? EnhancedNavigationLocation, string Body);
}
