using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Htmxor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Htmxor.Explicit.Anywhere;

public sealed class Issue180ExplicitActionTests(ITestOutputHelper output) : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;
	private readonly Issue180Probe probe = new();

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Issue180App).Assembly.GetName().Name });
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddSingleton(probe);
		builder.Services.AddAuthentication("issue180").AddScheme<AuthenticationSchemeOptions, Issue180AuthenticationHandler>("issue180", _ => { });
		builder.Services.AddAuthorization();
		builder.Services.AddRazorComponents().AddHtmxor();
		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue180App>().AddHtmxorEndpoints();
		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Theory]
	[InlineData("razor", "POST", "POST")]
	[InlineData("razor", "PUT", "PUT")]
	[InlineData("razor", "PATCH", "PATCH")]
	[InlineData("razor", "DELETE", "DELETE")]
	[InlineData("razor", "QUERY", "QUERY")]
	[InlineData("behind", "POST", "POST")]
	[InlineData("behind", "PUT", "PUT")]
	[InlineData("behind", "PATCH", "PATCH")]
	[InlineData("behind", "DELETE", "DELETE")]
	[InlineData("behind", "QUERY", "QUERY")]
	[InlineData("csharp", "POST", "POST")]
	[InlineData("csharp", "PUT", "PUT")]
	[InlineData("csharp", "PATCH", "PATCH")]
	[InlineData("csharp", "DELETE", "DELETE")]
	[InlineData("csharp", "QUERY", "QUERY")]
	public async Task Explicit_action_uses_one_fresh_route_owner_instance(string shape, string method, string expected)
	{
		var credentials = await CredentialsAsync(shape);
		probe.Reset();
		using var request = Request(shape, method, credentials.Cookie, credentials.Token);
		request.Headers.Add("HXOR-Event-Handler-Id", "forged-other-route-and-method");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		output.WriteLine($"{shape} {method}: HTTP {(int)response.StatusCode}, callbacks={probe.Callbacks}, lifecycle={probe.Initializations}; {body}");

		Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}; callbacks={probe.Callbacks}; {body}");
		Assert.Contains(expected + ":42", body, StringComparison.Ordinal);
		Assert.Contains("data-callbacks=\"1\"", body, StringComparison.Ordinal);
		Assert.Equal(1, probe.Callbacks);
		Assert.Equal(1, probe.Initializations);
		Assert.Equal(1, probe.Bindings);
		Assert.Contains("data-owner=\"" + probe.Owner + "\"", body, StringComparison.Ordinal);
		Assert.NotEqual(credentials.Owner, probe.Owner);
	}

	[Theory]
	[InlineData("razor", false)]
	[InlineData("razor", true)]
	[InlineData("behind", false)]
	[InlineData("behind", true)]
	[InlineData("csharp", false)]
	[InlineData("csharp", true)]
	public async Task Missing_or_invalid_token_rejects_before_binding_lifecycle_or_callback(string shape, bool invalid)
	{
		var credentials = await CredentialsAsync(shape);
		probe.Reset();
		using var request = Request(shape, "PUT", credentials.Cookie, invalid ? "invalid" : null);
		using var response = await client.SendAsync(request);

		output.WriteLine($"Invalid token {shape}: HTTP {(int)response.StatusCode}, callbacks={probe.Callbacks}, lifecycle={probe.Initializations}");
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal(0, probe.Initializations);
		Assert.Equal(0, probe.Bindings);
		Assert.Equal(0, probe.Callbacks);
	}

	[Fact]
	public async Task Htmx_headers_do_not_authorize_an_explicit_action()
	{
		using var request = Request("csharp", "PUT", null, null, authenticated: false);
		using var response = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal(0, probe.Initializations);
		Assert.Equal(0, probe.Bindings);
		Assert.Equal(0, probe.Callbacks);
	}

	[Theory]
	[InlineData("PROPFIND", "/issue-180/razor/42", 405)]
	[InlineData("PUT", "/issue-180/undeclared/42", 404)]
	public async Task Client_headers_cannot_create_an_action(string method, string path, int status)
	{
		using var request = Request("razor", method, null, null);
		request.RequestUri = new Uri(path, UriKind.Relative);
		request.Headers.Add("HXOR-Event-Handler-Id", "Put");
		using var response = await client.SendAsync(request);

		Assert.Equal(status, (int)response.StatusCode);
		Assert.Equal(0, probe.Callbacks);
	}

	private async Task<(string Cookie, string Token, string Owner)> CredentialsAsync(string shape)
	{
		using var request = Request(shape, "GET", null, null);
		using var response = await client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
		Assert.NotEmpty(token);
		var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0];
		return (cookie, WebUtility.HtmlDecode(token), Regex.Match(html, "data-owner=\"([^\"]+)\"").Groups[1].Value);
	}

	private static HttpRequestMessage Request(string shape, string method, string? cookie, string? token, bool authenticated = true)
	{
		var request = new HttpRequestMessage(new HttpMethod(method), "/issue-180/" + shape + "/42") { Content = ActionBody(method) };
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		if (authenticated)
			request.Headers.Add("X-Issue180-User", "Ada");
		if (cookie is not null)
			request.Headers.Add("Cookie", cookie);
		if (token is not null)
			request.Headers.Add("RequestVerificationToken", token);
		return request;
	}

	private static HttpContent? ActionBody(string method) => method == "GET" ? null : new FormUrlEncodedContent([]);

	public async Task DisposeAsync()
	{
		client?.Dispose();
		if (app is not null)
			await app.DisposeAsync();
	}
}

public sealed class Issue180Probe
{
	public int Bindings { get; private set; }
	public int Initializations { get; private set; }
	public int Callbacks { get; private set; }
	public string? Owner { get; private set; }
	public void Bind() => Bindings++;
	public void Initialize() => Initializations++;
	public void Invoke(string owner) { Callbacks++; Owner = owner; }
	public void Reset() { Bindings = 0; Initializations = 0; Callbacks = 0; Owner = null; }
}

internal sealed class Issue180AuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.ContainsKey("X-Issue180-User"))
			return Task.FromResult(AuthenticateResult.NoResult());
		var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Ada")], "issue180"));
		return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "issue180")));
	}
}
