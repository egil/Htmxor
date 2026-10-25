using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Htmxor.AspNetCore10.Issue362.Issue179;

public sealed class Issue179StockFormTests(ITestOutputHelper output) : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;
	private readonly Issue179Probe probe = new();

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddSingleton(probe);
		builder.Services.AddRazorComponents().AddHtmxor();
		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue179App>().AddHtmxorEndpoints();
		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Theory]
	[InlineData("literal")]
	[InlineData("expression")]
	[InlineData("plain")]
	public async Task Named_stock_form_infers_POST_and_invokes_the_request_component_once(string shape)
	{
		var path = "/issue-362-179/" + shape;
		var credentials = await ReadFormAsync(path, direct: true);
		probe.Reset();

		using var request = Submit(path, credentials, "Ada", direct: true);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		output.WriteLine($"HTTP {(int)response.StatusCode}; binding={probe.Binding}; lifecycle={probe.Lifecycle}; callback={probe.Callback}; HTML={body}");

		Assert.Multiple(
			() => Assert.True(response.StatusCode == HttpStatusCode.OK,
			$"Named {shape} form POST should infer reachability: received {(int)response.StatusCode}. HTML: {body}"),
			() => Assert.Contains("data-callback=\"1\"", body, StringComparison.Ordinal),
			() => Assert.Contains("Ada", body, StringComparison.Ordinal),
			() => Assert.Equal(1, probe.Callback),
			() => Assert.True(probe.Binding > 0),
			() => Assert.True(probe.Lifecycle > 0),
			() => Assert.Contains($"data-owner=\"{probe.CallbackOwner}\"", body, StringComparison.Ordinal));
	}

	[Fact]
	public async Task Inferred_EditForm_keeps_stock_validation_and_does_not_call_valid_submit_for_invalid_model()
	{
		var credentials = await ReadFormAsync("/issue-362-179/literal", direct: true);
		probe.Reset();
		using var request = Submit("/issue-362-179/literal", credentials, "", direct: true);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		output.WriteLine($"HTTP {(int)response.StatusCode}; binding={probe.Binding}; lifecycle={probe.Lifecycle}; callback={probe.Callback}; HTML={body}");

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains("The Name field is required.", body, StringComparison.Ordinal),
			() => Assert.Contains("data-callback=\"0\"", body, StringComparison.Ordinal),
			() => Assert.Equal(0, probe.Callback),
			() => Assert.True(probe.Binding > 0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Missing_or_invalid_antiforgery_precedes_binding_lifecycle_and_callback(bool invalid)
	{
		var credentials = await ReadFormAsync("/issue-362-179/explicit", direct: true);
		probe.Reset();
		var rejected = credentials with { Token = invalid ? "invalid-token" : null };
		using var request = Submit("/issue-362-179/explicit", rejected, "Ada", direct: true);
		using var response = await client.SendAsync(request);

		output.WriteLine($"Rejected token HTTP {(int)response.StatusCode}; binding={probe.Binding}; lifecycle={probe.Lifecycle}; callback={probe.Callback}");
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
			() => Assert.Equal(0, probe.Binding),
			() => Assert.Equal(0, probe.Lifecycle),
			() => Assert.Equal(0, probe.Callback));
	}

	[Fact]
	public async Task Normal_only_form_keeps_stock_POST_without_direct_reachability()
	{
		var credentials = await ReadFormAsync("/issue-362-179/normal", direct: false);
		probe.Reset();
		using var request = Submit("/issue-362-179/normal", credentials, "Ada", direct: false);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		output.WriteLine($"HTTP {(int)response.StatusCode}; binding={probe.Binding}; lifecycle={probe.Lifecycle}; callback={probe.Callback}; HTML={body}");

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains("data-stock-shell", body, StringComparison.Ordinal),
			() => Assert.Contains("data-callback=\"1\"", body, StringComparison.Ordinal),
			() => Assert.Equal(1, probe.Callback));

		using var directGet = new HttpRequestMessage(HttpMethod.Get, "/issue-362-179/normal");
		AddDirectHeaders(directGet);
		using var getResponse = await client.SendAsync(directGet);
		probe.Reset();
		using var directPost = Submit("/issue-362-179/normal", credentials, "Ada", direct: true);
		using var postResponse = await client.SendAsync(directPost);
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode),
			() => Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode),
			() => Assert.Equal(0, probe.Binding),
			() => Assert.Equal(0, probe.Lifecycle),
			() => Assert.Equal(0, probe.Callback));
	}

	[Theory]
	[InlineData("parent")]
	[InlineData("client")]
	public async Task Child_form_and_client_hx_post_do_not_infer_parent_POST(string shape)
	{
		var path = "/issue-362-179/" + shape;
		using var get = new HttpRequestMessage(HttpMethod.Get, path);
		AddDirectHeaders(get);
		using var getResponse = await client.SendAsync(get);
		Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
		using var post = new HttpRequestMessage(HttpMethod.Post, path);
		AddDirectHeaders(post);
		using var response = await client.SendAsync(post);
		Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
	}

	private async Task<FormCredentials> ReadFormAsync(string path, bool direct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (direct)
			AddDirectHeaders(request);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		output.WriteLine($"Form GET {path}: HTTP {(int)response.StatusCode}; HTML contains token input and named handler checked below.");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var token = Regex.Match(body, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
		Assert.True(token.Success, "GET did not render a stock antiforgery input: " + body);
		var handler = Regex.Match(body, "name=\"_handler\"[^>]*value=\"([^\"]+)\"");
		Assert.True(handler.Success, "GET did not render its named form handler: " + body);
		var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0];
		output.WriteLine($"Form credentials obtained from public GET: tokenLength={token.Groups[1].Value.Length}; cookieLength={cookie.Length}; handler={WebUtility.HtmlDecode(handler.Groups[1].Value)}");
		return new(WebUtility.HtmlDecode(token.Groups[1].Value), cookie, WebUtility.HtmlDecode(handler.Groups[1].Value));
	}

	private static HttpRequestMessage Submit(string path, FormCredentials credentials, string name, bool direct)
	{
		var request = new HttpRequestMessage(HttpMethod.Post, path);
		if (direct)
			AddDirectHeaders(request);
		request.Headers.Add("Cookie", credentials.Cookie);
		var fields = new List<KeyValuePair<string, string>> { new("_handler", credentials.Handler), new("Model.Name", name) };
		if (credentials.Token is not null)
			fields.Add(new("__RequestVerificationToken", credentials.Token));
		request.Content = new FormUrlEncodedContent(fields);
		return request;
	}

	private static void AddDirectHeaders(HttpRequestMessage request)
	{
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
	}

	private sealed record FormCredentials(string? Token, string Cookie, string Handler);

	public async Task DisposeAsync()
	{
		client.Dispose();
		await app.DisposeAsync();
	}
}
