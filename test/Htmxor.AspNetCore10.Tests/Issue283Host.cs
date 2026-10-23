using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal static class Issue283Host
{
	public static async Task<WebApplication> StartAsync(bool htmxor)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Issue283App).Assembly.GetName().Name });
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue283FailureProbe>();
		var components = builder.Services.AddRazorComponents();
		if (htmxor) components.AddHtmxor();
		var app = builder.Build();
		app.Use(async (context, next) =>
		{
			try { await next(context); }
			catch (InvalidOperationException)
			{
				app.Services.GetRequiredService<Issue283FailureProbe>().ResponseStarted = context.Response.HasStarted;
				throw;
			}
		});
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue283App>();
		if (htmxor) endpoints.AddHtmxorEndpoints();
		await app.StartAsync();
		return app;
	}

	public static HttpRequestMessage Request(string path, string? representation)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (representation is null) return request;
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", representation == "boosted" ? "full" : representation);
		if (representation == "boosted") request.Headers.Add("HX-Boosted", "true");
		return request;
	}

	public static async Task<byte[]> ReadBytesAsync(WebApplication app, string path, string? representation)
	{
		using var client = app.GetTestClient();
		using var request = Request(path, representation);
		using var response = await client.SendAsync(request);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsByteArrayAsync();
	}

	public static async Task<string> ReadAsync(WebApplication app, string path, string? representation) =>
		System.Text.Encoding.UTF8.GetString(await ReadBytesAsync(app, path, representation));
}

internal sealed class Issue283FailureProbe
{
	public bool? ResponseStarted { get; set; }
}
