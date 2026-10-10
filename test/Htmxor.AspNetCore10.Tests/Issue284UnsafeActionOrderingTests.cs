using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

/// <summary>
/// An unsafe action on an <c>[Authorize(Roles = ...)]</c> <c>HtmxRoute</c> component keeps
/// authorization ahead of antiforgery, binding, and the callback. A request that fails
/// <see cref="Issue284UnsafeActionPage"/>'s authorization check is rejected with 401/403 without
/// running component initialization, parameter binding, or the callback, even when it carries no
/// antiforgery token at all. An authorized request with no antiforgery token is rejected with 400
/// by antiforgery, also without running any application code, which only holds meaning if
/// authorization has already let the request reach antiforgery.
/// </summary>
public sealed class Issue284UnsafeActionOrderingTests : IAsyncLifetime
{
	private const string AdminUser = "issue-284-admin-user";
	private const string NonAdminUser = "issue-284-nonadmin-user";
	private WebApplication app = default!;
	private HttpClient client = default!;
	private Issue284UnsafeActionProbe probe = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue284UnsafeActionOrderingTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthentication(Issue284ActionAuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue284ActionAuthenticationHandler>(
				Issue284ActionAuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue284UnsafeActionProbe>();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
		probe = app.Services.GetRequiredService<Issue284UnsafeActionProbe>();
	}

	[Theory]
	[InlineData(null, HttpStatusCode.Unauthorized)]
	[InlineData(NonAdminUser, HttpStatusCode.Forbidden)]
	public async Task Unauthorized_PUT_without_an_antiforgery_token_is_rejected_before_binding_or_callback(
		string? user,
		HttpStatusCode expected)
	{
		using var request = CreateUnsafeActionRequest(user, cookie: null, token: null);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(
			response.StatusCode == expected,
			$"Expected {expected}, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		AssertNoApplicationCodeRan();
	}

	[Fact]
	public async Task Authorized_PUT_without_an_antiforgery_token_is_rejected_before_binding_or_callback()
	{
		// Authorization alone does not explain a rejection here: this request is authorized
		// (AdminUser passes the Roles check), so this row's 400 Bad Request is meaningful only
		// because antiforgery itself enforces the token, and still runs before binding and the
		// callback.
		using var request = CreateUnsafeActionRequest(AdminUser, cookie: null, token: null);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(
			response.StatusCode == HttpStatusCode.BadRequest,
			$"Expected 400 Bad Request for an authorized PUT with no antiforgery token, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		AssertNoApplicationCodeRan();
	}

	[Fact]
	public async Task Authorized_PUT_with_a_valid_token_runs_the_callback()
	{
		// Positive control: proves the route and action genuinely work when both checks pass, so
		// the negative cases above are not merely failing for an unrelated reason.
		var (token, cookie) = await GetAntiforgeryCredentialsAsync();
		probe.Reset();

		using var request = CreateUnsafeActionRequest(AdminUser, cookie, token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Equal(1, probe.InitializationCount);
		Assert.Equal(1, probe.BindingCount);
		Assert.Equal(1, probe.CallbackCount);
	}

	private void AssertNoApplicationCodeRan()
	{
		Assert.Equal(0, probe.InitializationCount);
		Assert.Equal(0, probe.BindingCount);
		Assert.Equal(0, probe.CallbackCount);
	}

	private async Task<(string Token, string Cookie)> GetAntiforgeryCredentialsAsync()
	{
		using var pageRequest = new HttpRequestMessage(HttpMethod.Get, "/issue-284/unsafe-action/1");
		pageRequest.Headers.Add("HX-Request", "true");
		pageRequest.Headers.Add("HX-Request-Type", "partial");
		pageRequest.Headers.Add(Issue284ActionAuthenticationHandler.UserHeaderName, AdminUser);
		using var pageResponse = await client.SendAsync(pageRequest);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();
		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);

		var tokenMatch = Regex.Match(
			pageBody,
			"name=\"__RequestVerificationToken\" value=\"([^\"]+)\"",
			RegexOptions.CultureInvariant);
		Assert.True(tokenMatch.Success, pageBody);
		var cookie = Assert.Single(
			pageResponse.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));

		return (
			WebUtility.HtmlDecode(tokenMatch.Groups[1].Value),
			cookie.Split(';', 2)[0]);
	}

	private static HttpRequestMessage CreateUnsafeActionRequest(string? user, string? cookie, string? token)
	{
		var request = new HttpRequestMessage(HttpMethod.Put, "/issue-284/unsafe-action/1");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		if (user is not null)
		{
			request.Headers.Add(Issue284ActionAuthenticationHandler.UserHeaderName, user);
		}

		if (cookie is not null)
		{
			request.Headers.Add("Cookie", cookie);
		}

		if (token is not null)
		{
			request.Headers.Add("RequestVerificationToken", token);
		}

		return request;
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

internal sealed class Issue284UnsafeActionProbe
{
	private int initializationCount;
	private int bindingCount;
	private int callbackCount;

	public int InitializationCount => Volatile.Read(ref initializationCount);

	public int BindingCount => Volatile.Read(ref bindingCount);

	public int CallbackCount => Volatile.Read(ref callbackCount);

	public void RecordInitialization() => Interlocked.Increment(ref initializationCount);

	public void RecordBinding() => Interlocked.Increment(ref bindingCount);

	public void RecordCallback() => Interlocked.Increment(ref callbackCount);

	public void Reset()
	{
		Interlocked.Exchange(ref initializationCount, 0);
		Interlocked.Exchange(ref bindingCount, 0);
		Interlocked.Exchange(ref callbackCount, 0);
	}
}

internal sealed class Issue284ActionAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-284-action-test";
	public const string UserHeaderName = "X-Issue-284-Action-User";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(UserHeaderName, out var userHeader))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var user = userHeader.ToString();
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user),
			new(ClaimTypes.Name, user),
		};
		if (string.Equals(user, "issue-284-admin-user", StringComparison.Ordinal))
		{
			claims.Add(new Claim(ClaimTypes.Role, "issue-284-admin"));
		}

		var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
		var ticket = new AuthenticationTicket(principal, SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
