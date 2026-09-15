#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal sealed class Issue213TempDataHost(WebApplication app) : IAsyncDisposable
{
	private readonly HttpClient client = app.GetTestClient();

	public static async Task<Issue213TempDataHost> StartAsync(bool htmxor, IDataProtectionProvider protection)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue213TempDataPage).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection();
		builder.Services.AddSingleton(protection);
		var components = builder.Services.AddRazorComponents(options =>
		{
			options.TempDataProviderType = TempDataProviderType.Cookie;
			options.TempDataCookie.Name = "issue213-tempdata";
		});
		if (htmxor)
		{
			components.AddHtmxor();
		}

		var app = builder.Build();
		var endpoints = app.MapRazorComponents<Issue78App>();
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new(app);
	}

	public async Task<Issue213Response> SendAsync(CookieContainer cookies, bool fragment,
		string path, string method = "GET", bool rejected = false)
	{
		using var request = new HttpRequestMessage(new HttpMethod(method), path);
		var cookieHeader = cookies.GetCookieHeader(client.BaseAddress!);
		if (cookieHeader.Length > 0)
		{
			request.Headers.Add("Cookie", cookieHeader);
		}
		request.Headers.Add("Sec-Fetch-Site", rejected ? "cross-site" : "same-origin");
		if (fragment)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		if (method == "POST")
		{
			request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["_handler"] = "save" });
		}

		using var response = await client.SendAsync(request);
		return await ReadResponseAsync(cookies, response);
	}

	private async Task<Issue213Response> ReadResponseAsync(CookieContainer cookies, HttpResponseMessage response)
	{
		var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];
		foreach (var cookie in setCookies)
		{
			cookies.SetCookies(client.BaseAddress!, cookie);
		}

		var redirect = response.Headers.TryGetValues("HX-Redirect", out var redirects) ? redirects.Single() : null;
		return new(response.StatusCode, await response.Content.ReadAsStringAsync(), setCookies,
			response.Headers.Location?.ToString(), redirect);
	}

	public async ValueTask DisposeAsync()
	{
		client.Dispose();
		await app.DisposeAsync();
	}
}

internal sealed record Issue213Response(HttpStatusCode Status, string Body, string[] Cookies,
	string? Location, string? HxRedirect);
#endif
