using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Htmxor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Issue209;

public sealed class Issue209GetTests
{
	[Fact]
	public async Task Normal_root_preserves_the_stock_shell()
	{
		await using var stock = await StartAsync(false);
		await using var htmxor = await StartAsync(true);
		using var stockClient = stock.GetTestClient();
		using var client = htmxor.GetTestClient();
		using var expected = await stockClient.GetAsync("/issue-362-209/");
		using var actual = await client.GetAsync("/issue-362-209/");
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, expected.StatusCode),
			() => Assert.Equal(expected.StatusCode, actual.StatusCode));
		var body = await actual.Content.ReadAsStringAsync();
		var expectedBody = await expected.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(expectedBody, body),
			() => Assert.Equal("<!DOCTYPE html><html lang=\"en\"><body><main>package GET</main></body></html>", body));
	}

	[Fact]
	public async Task Direct_root_returns_only_the_page()
	{
		await using var app = await StartAsync(true);
		using var client = app.GetTestClient();
		using var request = DirectRequest("/issue-362-209/");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Equal("<main>package GET</main>", body));
	}

	[Fact]
	public async Task Generated_route_binds_typed_values_in_the_current_request_scope()
	{
		await using var app = await StartAsync(true);
		using var client = app.GetTestClient();
		using var first = DirectRequest("/issue-362-209/reports/42?Count=7");
		first.Headers.Add("X-Probe", "first");
		using var firstResponse = await client.SendAsync(first);
		var firstBody = await firstResponse.Content.ReadAsStringAsync();
		using var second = DirectRequest("/issue-362-209/reports/19?Count=3");
		second.Headers.Add("X-Probe", "second");
		using var secondResponse = await client.SendAsync(second);
		var secondBody = await secondResponse.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode),
			() => Assert.Equal("<output>42|7|first</output>", firstBody),
			() => Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode),
			() => Assert.Equal("<output>19|3|second</output>", secondBody));
	}

	[Theory]
	[InlineData("/issue-362-209/reports/42", false, 404)]
	[InlineData("/issue-362-209/reports/invalid", true, 404)]
	[InlineData("/issue-362-209/missing", true, 404)]
	[InlineData("/issue-362-209/reports/42", true, 200)]
	public async Task Generated_GET_has_component_owned_reachability(string path, bool direct, int status)
	{
		await using var app = await StartAsync(true);
		using var client = app.GetTestClient();
		using var request = direct ? DirectRequest(path) : new HttpRequestMessage(HttpMethod.Get, path);
		using var response = await client.SendAsync(request);
		Assert.Equal(status, (int)response.StatusCode);
	}

	private static HttpRequestMessage DirectRequest(string path)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		return request;
	}

	[Theory]
	[InlineData(false, "Ada", "valid")]
	[InlineData(false, "", "invalid")]
	[InlineData(true, "Ada", "valid")]
	[InlineData(true, "", "invalid")]
	public async Task Existing_named_EditForm_binds_validates_and_submits(bool direct, string name, string result)
	{
		await using var app = await StartAsync(true);
		using var client = app.GetTestClient();
		using var get = await client.GetAsync("/issue-362-209/form");
		var form = await get.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, get.StatusCode);
		var token = Regex.Match(form, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
		Assert.True(token.Success, form);
		using var request = direct ? DirectRequest("/issue-362-209/form") : new HttpRequestMessage(HttpMethod.Post, "/issue-362-209/form");
		request.Method = HttpMethod.Post;
		request.Headers.Add("Cookie", Assert.Single(get.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Antiforgery.")).Split(';')[0]);
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "save",
			["Model.Name"] = name,
			["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
		});
		using var response = await client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.True(response.StatusCode == HttpStatusCode.OK, html),
			() => Assert.Contains($"data-result=\"{result}\">{name}</output>", html),
			() => Assert.Equal(result == "invalid", html.Contains(">The Name field is required.</div>")));
	}

	private static async Task<WebApplication> StartAsync(bool useHtmxor)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddScoped<Issue209RequestProbe>();
		builder.Services.AddAuthentication();
		builder.Services.AddAuthorization(options => options.AddPolicy("package-reader", policy => policy.RequireAuthenticatedUser()));
		var components = builder.Services.AddRazorComponents();
		if (useHtmxor)
			components.AddHtmxor();
		var app = builder.Build();
		app.Use(async (context, next) =>
		{
			context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "package-reader")], "test"));
			context.RequestServices.GetRequiredService<Issue209RequestProbe>().Value = context.Request.Headers["X-Probe"].ToString();
			await next(context);
		});
		app.UseAuthorization();
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue209App>();
		if (useHtmxor)
			endpoints.AddHtmxorEndpoints();
		await app.StartAsync();
		return app;
	}
}

public sealed class Issue209RequestProbe
{
	public string Value { get; set; } = "";
}
