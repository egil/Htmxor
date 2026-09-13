#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal sealed class Issue212SessionHost(WebApplication app) : IAsyncDisposable
{
	public HttpClient Client { get; } = app.GetTestClient();
	public Issue212Gate Gate => app.Services.GetRequiredService<Issue212Gate>();

	public static async Task<Issue212SessionHost> StartAsync(bool htmxor, bool rootSession = false)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue212SessionPage).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddDistributedMemoryCache();
		builder.Services.AddSession(options => options.Cookie.Name = "issue212-session");
		builder.Services.AddSingleton<Issue212Gate>();
		var components = builder.Services.AddRazorComponents();
		if (htmxor)
		{
			components.AddHtmxor();
		}

		var app = builder.Build();
		app.UseSession();
		var endpoints = rootSession
			? app.MapRazorComponents<Issue212SessionApp>()
			: app.MapRazorComponents<Issue78App>();
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new(app);
	}

	public async Task<string> CreateSessionAsync()
	{
		using var response = await Client.GetAsync("/issue-212/session");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return Assert.Single(response.Headers.GetValues("Set-Cookie"),
			cookie => cookie.StartsWith("issue212-session=", StringComparison.Ordinal)).Split(';', 2)[0];
	}

	public async Task<(HttpStatusCode Status, string Body)> SendAsync(string cookie, bool fragment,
		string query = "", string method = "GET", bool rejected = false)
	{
		var path = method == "PUT" ? "/issue-212/action" : "/issue-212/session";
		using var request = new HttpRequestMessage(new HttpMethod(method), path + query);
		request.Headers.Add("Cookie", cookie);
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

		using var response = await Client.SendAsync(request);
		return (response.StatusCode, await response.Content.ReadAsStringAsync());
	}

	public async ValueTask DisposeAsync()
	{
		Gate.Release.TrySetResult();
		Client.Dispose();
		await app.DisposeAsync();
	}
}

internal sealed class Issue212Gate
{
	private int arrivals;
	public TaskCompletionSource BothArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public async Task CompleteAsync(bool hold)
	{
		await Task.Yield();
		if (hold)
		{
			if (Interlocked.Increment(ref arrivals) == 2)
			{
				BothArrived.TrySetResult();
			}

			await Release.Task.WaitAsync(TimeSpan.FromSeconds(20));
		}
	}
}
#endif
