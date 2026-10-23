using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal sealed class Issue318Host(WebApplication app) : IAsyncDisposable
{
	public HttpClient Client { get; } = app.GetTestClient();
	public Issue318Probe Probe => app.Services.GetRequiredService<Issue318Probe>();

	public static async Task<Issue318Host> StartAsync(bool disableValidation = false, bool disableAfterMiddleware = false)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue318Host).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		// Select token antiforgery on .NET 11 so native CSRF rejection cannot mask a token bypass.
		builder.Configuration["DisableCsrfProtection"] = "true";
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue318Probe>();
		var app = builder.Build();
		app.UseRouting();
		app.UseAntiforgery();
		app.Use((context, next) => ObserveValidationAsync(context, next, disableAfterMiddleware));
		var endpoints = app.MapRazorComponents<Issue78App>().AddHtmxorEndpoints();
		if (disableValidation)
		{
			endpoints.Add(endpoint => endpoint.Metadata.Add(new RequireAntiforgeryTokenAttribute(false)));
		}

		await app.StartAsync();
		return new(app);
	}

	private static async Task ObserveValidationAsync(HttpContext context, RequestDelegate next, bool disableAfterMiddleware)
	{
		var probe = context.RequestServices.GetRequiredService<Issue318Probe>();
		probe.MiddlewareValid = context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid;
		if (disableAfterMiddleware) DisableEndpointValidation(context);

		probe.RequiresValidation = context.GetEndpoint()?.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation;
		await next(context);
	}

	private static void DisableEndpointValidation(HttpContext context)
	{
		if (context.GetEndpoint() is RouteEndpoint endpoint)
		{
			Assert.NotNull(endpoint.RequestDelegate);
			// Preserve the real middleware verdict while exposing the endpoint's opt-out branch.
			context.SetEndpoint(new RouteEndpoint(endpoint.RequestDelegate, endpoint.RoutePattern, endpoint.Order,
				new EndpointMetadataCollection(endpoint.Metadata.Append(new RequireAntiforgeryTokenAttribute(false))), endpoint.DisplayName));
		}
	}

	public async Task AddCredentialsAsync(HttpRequestMessage request)
	{
		using var get = Request("GET");
		using var response = await Client.SendAsync(get);
		var html = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		var token = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
		Assert.True(token.Success, html);
		request.Headers.Add("RequestVerificationToken", WebUtility.HtmlDecode(token.Groups[1].Value));
		request.Headers.Add("Cookie", Assert.Single(response.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';', 2)[0]);
		Probe.Reset();
	}

	public static HttpRequestMessage Request(string method, bool multipart = false, bool action = true)
	{
		var request = new HttpRequestMessage(new HttpMethod(method), action ? "/issue-318/actions" : "/issue-318/actionless");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		if (method == "GET") return request;
		request.Content = multipart
			? new MultipartFormDataContent { { new StringContent("5"), "CurrentCount" } }
			: new FormUrlEncodedContent([new KeyValuePair<string, string>("CurrentCount", "5")]);
		return request;
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await app.DisposeAsync();
	}
}

internal sealed class Issue318Probe
{
	public int FormAssignments { get; set; }
	public int Initializations { get; set; }
	public int Callbacks { get; set; }
	public int? BoundCount { get; set; }
	public bool? MiddlewareValid { get; set; }
	public bool? RequiresValidation { get; set; }

	public void Reset() => (FormAssignments, Initializations, Callbacks, BoundCount, MiddlewareValid, RequiresValidation) = (0, 0, 0, null, null, null);

	public void AssertNoApplicationEffects() => Assert.Equal((0, 0, 0, null), (FormAssignments, Initializations, Callbacks, BoundCount));
}
