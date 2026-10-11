using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

/// <summary>
/// A generated <c>HtmxRoute</c> endpoint carries the same <c>ConfiguredRenderModesMetadata</c> as its
/// stock <c>@page</c> twin (#316). With no <c>AddInteractiveServerRenderMode()</c> chained onto
/// <c>MapRazorComponents</c>, an <c>InteractiveServer</c> HtmxRoute component is rejected before it
/// renders, exactly as the stock twin is; with it chained, both twins render the configured server
/// component instead. Both outcomes come from the framework adapter that <c>AddHtmxor()</c>
/// registers, which reads that metadata from the selected endpoint.
/// </summary>
public sealed class Issue316RenderModeParityTests
{
	[Fact]
	public async Task Unconfigured_interactive_server_HtmxRoute_component_is_rejected_like_its_stock_twin()
	{
		await using var app = await CreateHostAsync(configured: false);
		using var client = app.GetTestClient();

		using var stockResponse = await client.GetAsync("/issue-316-rendermode/stock");
		using var htmxResponse = await SendHtmxRequestAsync(client);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();

		Assert.True(
			stockResponse.StatusCode == HttpStatusCode.InternalServerError &&
				stockBody.Contains("AddInteractiveServerRenderMode", StringComparison.Ordinal),
			$"Stock twin expected 500 naming AddInteractiveServerRenderMode, got {(int)stockResponse.StatusCode}. Body: {stockBody}");
		Assert.True(
			htmxResponse.StatusCode == stockResponse.StatusCode &&
				htmxBody.Contains("AddInteractiveServerRenderMode", StringComparison.Ordinal),
			$"HtmxRoute twin expected to match its stock twin's rejection, got {(int)htmxResponse.StatusCode}. Body: {htmxBody}");
	}

	/// <summary>
	/// Positive control for the row above: once <c>AddInteractiveServerRenderMode()</c> is actually
	/// chained, both twins render the configured server component instead of being rejected. This
	/// guards against a fix that attaches empty or fixed <c>ConfiguredRenderModesMetadata</c> to the
	/// HtmxRoute endpoint rather than the one actually configured.
	/// </summary>
	[Fact]
	public async Task Configured_interactive_server_HtmxRoute_component_renders_like_its_stock_twin()
	{
		await using var app = await CreateHostAsync(configured: true);
		using var client = app.GetTestClient();

		using var stockResponse = await client.GetAsync("/issue-316-rendermode/stock");
		using var htmxResponse = await SendHtmxRequestAsync(client);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();

		Assert.True(
			stockResponse.StatusCode == HttpStatusCode.OK &&
				stockBody.Contains("\"type\":\"server\"", StringComparison.Ordinal),
			$"Stock twin expected 200 with the server prerender marker, got {(int)stockResponse.StatusCode}. Body: {stockBody}");
		Assert.True(
			htmxResponse.StatusCode == HttpStatusCode.OK &&
				htmxBody.Contains("\"type\":\"server\"", StringComparison.Ordinal),
			$"HtmxRoute twin expected 200 with the server prerender marker to match its configured stock twin, got {(int)htmxResponse.StatusCode}. Body: {htmxBody}");
	}

	private static async Task<HttpResponseMessage> SendHtmxRequestAsync(HttpClient client)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-316-rendermode/htmx/1");
		request.Headers.TryAddWithoutValidation("HX-Request", "true");
		request.Headers.TryAddWithoutValidation("HX-Request-Type", "partial");
		return await client.SendAsync(request);
	}

	private static async Task<WebApplication> CreateHostAsync(bool configured)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue316RenderModeParityTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddHtmxor();

		var app = builder.Build();
		app.UseAntiforgery();
		var componentBuilder = app.MapRazorComponents<Issue316RenderModeApp>();
		if (configured)
		{
			componentBuilder.AddInteractiveServerRenderMode();
		}

		componentBuilder.AddHtmxorEndpoints();
		await app.StartAsync();
		return app;
	}
}
