using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

public sealed class Issue87DeleteActionTests : IAsyncLifetime
{
	private const string AuthorizedUser = "issue-87-user";
	private const string PolicyName = "issue-87-policy";
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue87DeleteActionTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthentication(Issue87AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue87AuthenticationHandler>(
				Issue87AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(
			PolicyName,
			policy => policy.RequireClaim(Issue87AuthenticationHandler.AccessClaim, "granted")));
		builder.Services.AddRazorComponents().AddHtmx();
		builder.Services.AddScoped(_ => new Issue87RequestProbe("from-request-scope"));

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.AddHtmxorComponentEndpoints(app);

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task Matching_delete_invokes_the_authorized_request_component_once()
	{
		using var pageRequest = CreateAuthorizedRequest(HttpMethod.Get, "/issue-87/42?source=from-query");
		using var pageResponse = await client.SendAsync(pageRequest);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();
		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);
		var token = ExtractAttribute(pageBody, "name=\"__RequestVerificationToken\"", "value");
		var cookie = ExtractAntiforgeryCookie(pageResponse);
		var pageRequestId = ExtractAttribute(pageBody, "data-issue-87-result", "data-request-id");

		using var deleteRequest = CreateAuthorizedRequest(HttpMethod.Delete, "/issue-87/42?source=from-query");
		deleteRequest.Headers.Add("Cookie", cookie);
		deleteRequest.Headers.Add("RequestVerificationToken", token);
		deleteRequest.Headers.Add("HX-Request", "true");
		using var deleteResponse = await client.SendAsync(deleteRequest);
		var deleteBody = await deleteResponse.Content.ReadAsStringAsync();

		Assert.True(
			deleteResponse.StatusCode == HttpStatusCode.OK,
			$"Expected 200 OK, received {(int)deleteResponse.StatusCode} {deleteResponse.StatusCode}. Body: {deleteBody}");
		Assert.Contains("data-item-id=\"42\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-query-source=\"from-query\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains($"data-user=\"{AuthorizedUser}\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-initialization-count=\"1\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-callback-count=\"1\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-deleted=\"true\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains($">\n    42|from-query|{AuthorizedUser}|from-request-scope\n</p>", deleteBody, StringComparison.Ordinal);
		Assert.NotEqual(pageRequestId, ExtractAttribute(deleteBody, "data-issue-87-result", "data-request-id"));
		Assert.DoesNotContain("<html", deleteBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", deleteBody, StringComparison.Ordinal);
	}

	private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path)
	{
		var request = new HttpRequestMessage(method, path);
		request.Headers.Add(Issue87AuthenticationHandler.UserHeaderName, AuthorizedUser);
		return request;
	}

	private static string ExtractAttribute(string html, string elementMarker, string attributeName)
	{
		var markerIndex = html.IndexOf(elementMarker, StringComparison.Ordinal);
		Assert.True(markerIndex >= 0, $"Expected an element containing '{elementMarker}'.");
		var elementStart = html.LastIndexOf('<', markerIndex);
		var elementEnd = html.IndexOf('>', markerIndex);
		Assert.True(elementStart >= 0 && elementEnd > elementStart, $"Expected complete markup around '{elementMarker}'.");
		var element = html[elementStart..(elementEnd + 1)];
		var match = Regex.Match(
			element,
			$"(?:^|\\s){Regex.Escape(attributeName)}=\"(?<value>[^\"]*)\"",
			RegexOptions.CultureInvariant);
		Assert.True(match.Success, $"Expected attribute '{attributeName}' in '{element}'.");
		return WebUtility.HtmlDecode(match.Groups["value"].Value);
	}

	private static string ExtractAntiforgeryCookie(HttpResponseMessage response)
	{
		var cookie = Assert.Single(
			response.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
		return cookie.Split(';', 2)[0];
	}

	public async Task DisposeAsync()
	{
		client?.Dispose();
		if (app is not null)
		{
			await app.DisposeAsync();
		}
	}
}

internal sealed class Issue87AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-87-test";
	public const string UserHeaderName = "X-Issue-87-User";
	public const string AccessClaim = "issue-87-access";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(UserHeaderName, out var userHeader))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var user = userHeader.ToString();
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, user),
			new Claim(ClaimTypes.Name, user),
			new Claim(AccessClaim, "granted"),
		};
		var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
		var ticket = new AuthenticationTicket(principal, SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}

internal sealed class Issue87RequestProbe(string value)
{
	public string Id { get; } = Guid.NewGuid().ToString("D");

	public string Value { get; } = value;

	public int InitializationCount { get; private set; }

	public int CallbackCount { get; private set; }

	public void RecordInitialization() => InitializationCount++;

	public void RecordCallback() => CallbackCount++;
}
