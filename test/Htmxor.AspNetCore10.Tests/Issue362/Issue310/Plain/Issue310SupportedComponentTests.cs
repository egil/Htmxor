using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Issue310.Plain;

public sealed class Issue310SupportedComponentTests : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddRazorComponents().AddHtmxor();
		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue310App>().AddHtmxorEndpoints();
		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task Supported_component_tag_binding_dispatches_the_request_owner_callback()
	{
		var (token, cookie) = CreateAntiforgeryCredentials();

		using var request = new HttpRequestMessage(HttpMethod.Put, "/issue-362-310/plain");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("Cookie", cookie);
		request.Headers.Add("RequestVerificationToken", token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected the supported component-tag @onput action to answer 200 OK, received " +
			$"{(int)response.StatusCode} {response.StatusCode}. Body: {body}"),
			() => Assert.Contains("data-owner-callback=\"1\"", body, StringComparison.Ordinal));
	}

	private (string Token, string Cookie) CreateAntiforgeryCredentials()
	{
		var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
		var context = new DefaultHttpContext { RequestServices = app.Services };
		var tokenSet = antiforgery.GetAndStoreTokens(context);
		var setCookie = context.Response.Headers["Set-Cookie"]
			.Single(value => value!.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))!;

		return (tokenSet.RequestToken!, setCookie.Split(';')[0]);
	}

	public async Task DisposeAsync()
	{
		client.Dispose();
		await app.DisposeAsync();
	}
}
