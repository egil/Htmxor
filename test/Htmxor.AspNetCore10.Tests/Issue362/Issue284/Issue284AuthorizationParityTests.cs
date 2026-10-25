using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Issue284;

public sealed class Issue284AuthorizationParityTests : IClassFixture<Issue284ParityHosts>
{
	private readonly Issue284ParityHosts hosts;

	private static readonly Dictionary<string, int[]> Expected = new()
	{
		["none"] = [200, 200, 200, 200, 200, 200],
		["bare"] = [401, 200, 200, 200, 200, 401],
		["policy"] = [401, 403, 403, 200, 200, 401],
		["roles"] = [401, 403, 200, 403, 200, 401],
		["schemes"] = [401, 401, 401, 401, 401, 200],
		["two"] = [401, 403, 403, 403, 200, 401],
		["inherited"] = [401, 403, 403, 200, 200, 401],
		["anonymous"] = [200, 200, 200, 200, 200, 200],
	};

	private static readonly string?[] Users =
	[
		null,
		Issue284AuthenticationHandler.LimitedUser,
		Issue284AuthenticationHandler.RoleOnlyUser,
		Issue284AuthenticationHandler.PolicyOnlyUser,
		Issue284AuthenticationHandler.FullUser,
		Issue284AuthenticationHandler.AltSchemeUser,
	];

	private static readonly Dictionary<string, (string Htmx, string Stock)> Paths = new()
	{
		["none"] = ("/issue-362-284/none/htmx/1", "/issue-362-284/none/stock/1"),
		["bare"] = ("/issue-362-284/bare/htmx/1", "/issue-362-284/bare/stock/1"),
		["policy"] = ("/issue-362-284/policy/htmx/1", "/issue-362-284/policy/stock/1"),
		["roles"] = ("/issue-362-284/roles/htmx/1", "/issue-362-284/roles/stock/1"),
		["schemes"] = ("/issue-362-284/schemes/htmx/1", "/issue-362-284/schemes/stock/1"),
		["two"] = ("/issue-362-284/two/htmx/1", "/issue-362-284/two/stock/1"),
		["inherited"] = ("/issue-362-284/inherited/htmx/1", "/issue-362-284/inherited/stock/1"),
		["anonymous"] = ("/issue-362-284/anonymous/htmx/1", "/issue-362-284/anonymous/stock/1"),
	};

	public Issue284AuthorizationParityTests(Issue284ParityHosts hosts) => this.hosts = hosts;

	public static IEnumerable<object[]> Cells()
	{
		foreach (var useFallbackPolicy in new[] { false, true })
		{
			foreach (var (shape, statuses) in Expected)
			{
				for (var index = 0; index < Users.Length; index++)
				{
					var status = ExpectedStatus(useFallbackPolicy, shape, index, statuses);
					yield return new object[] { useFallbackPolicy, shape, Users[index]!, status };
				}
			}
		}
	}

	private static int ExpectedStatus(bool fallback, string shape, int index, int[] statuses)
		=> fallback && shape == "none" ? Expected["policy"][index] : statuses[index];

	[Theory]
	[MemberData(nameof(Cells))]
	public async Task Htmx_route_matches_its_stock_twin_and_htmx_headers_never_change_the_outcome(
		bool useFallbackPolicy,
		string shape,
		string? user,
		int expected)
	{
		var host = useFallbackPolicy ? hosts.Fallback : hosts.NoFallback;
		var (htmxPath, stockPath) = Paths[shape];

		using var stockPlain = await SendAsync(host, stockPath, user, direct: false);
		using var stockDirect = await SendAsync(host, stockPath, user, direct: true);
		using var htmxDirect = await SendAsync(host, htmxPath, user, direct: true);

		Assert.Multiple(
			() => Assert.True(
			expected == (int)stockPlain.StatusCode,
			$"Stock '{shape}' (fallback={useFallbackPolicy}, user={user ?? "anonymous"}) expected {expected}, got {(int)stockPlain.StatusCode}."),
			() => Assert.True(
			expected == (int)stockDirect.StatusCode,
			$"Stock '{shape}' with HTMX headers (fallback={useFallbackPolicy}, user={user ?? "anonymous"}) expected {expected}, got {(int)stockDirect.StatusCode}; HTMX headers must never change the authorized outcome."),
			() => Assert.True(
			expected == (int)htmxDirect.StatusCode,
			$"HtmxRoute twin of '{shape}' (fallback={useFallbackPolicy}, user={user ?? "anonymous"}) expected {expected}, got {(int)htmxDirect.StatusCode}; it must match its stock twin."));
	}
	private static async Task<HttpResponseMessage> SendAsync(
		Issue284TestHost host,
		string path,
		string? user,
		bool direct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		if (string.Equals(user, Issue284AuthenticationHandler.AltSchemeUser, StringComparison.Ordinal))
		{
			request.Headers.Add(Issue284AuthenticationHandler.AltUserHeaderName, user);
		}
		else if (user is not null)
		{
			request.Headers.Add(Issue284AuthenticationHandler.UserHeaderName, user);
		}

		return await host.Client.SendAsync(request);
	}
}
public sealed class Issue284ParityHosts : IAsyncLifetime
{
	public Issue284TestHost NoFallback { get; private set; } = default!;

	public Issue284TestHost Fallback { get; private set; } = default!;

	public async Task InitializeAsync()
	{
		NoFallback = await Issue284TestHost.CreateAsync(useFallbackPolicy: false);
		Fallback = await Issue284TestHost.CreateAsync(useFallbackPolicy: true);
	}

	public async Task DisposeAsync()
	{
		await NoFallback.DisposeAsync();
		await Fallback.DisposeAsync();
	}
}

public sealed class Issue284TestHost : IAsyncDisposable
{
	public WebApplication App { get; }

	public HttpClient Client { get; }

	private Issue284TestHost(WebApplication app, HttpClient client)
	{
		App = app;
		Client = client;
	}

	public static async Task<Issue284TestHost> CreateAsync(bool useFallbackPolicy)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue284TestHost).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddAuthentication(Issue284AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue284AuthenticationHandler>(
				Issue284AuthenticationHandler.SchemeName,
				_ => { })
			.AddScheme<AuthenticationSchemeOptions, Issue284AltAuthenticationHandler>(
				Issue284AuthenticationHandler.AltSchemeName,
				_ => { });
		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy(
				Issue284Policy.Name,
				policy => policy.RequireClaim(Issue284Policy.ClaimType, Issue284Policy.ClaimValue));
			if (useFallbackPolicy)
			{
				options.FallbackPolicy = new AuthorizationPolicyBuilder()
					.RequireClaim(Issue284Policy.ClaimType, Issue284Policy.ClaimValue)
					.Build();
			}
		});
		builder.Services.AddRazorComponents().AddHtmxor();

		var app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue284App>().AddHtmxorEndpoints();

		await app.StartAsync();
		return new Issue284TestHost(app, app.GetTestClient());
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await App.DisposeAsync();
	}
}

internal sealed class Issue284AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-284-test";
	public const string AltSchemeName = "issue-284-alt-test";

	public const string UserHeaderName = "X-Issue-284-User";
	public const string AltUserHeaderName = "X-Issue-284-Alt-User";
	public const string LimitedUser = "limited-user";
	public const string RoleOnlyUser = "role-only-user";
	public const string PolicyOnlyUser = "policy-only-user";
	public const string FullUser = "full-user";
	public const string AltSchemeUser = "alt-scheme-user";

	public static bool HasRole(string user)
		=> string.Equals(user, RoleOnlyUser, StringComparison.Ordinal) ||
			string.Equals(user, FullUser, StringComparison.Ordinal);

	public static bool HasClaim(string user)
		=> string.Equals(user, PolicyOnlyUser, StringComparison.Ordinal) ||
			string.Equals(user, FullUser, StringComparison.Ordinal);
	public static bool AuthenticatesViaDefaultScheme(string? user)
		=> user is not null && !string.Equals(user, AltSchemeUser, StringComparison.Ordinal);

	public static bool AuthenticatesViaAltScheme(string? user)
		=> string.Equals(user, AltSchemeUser, StringComparison.Ordinal);

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(UserHeaderName, out var userHeader))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var ticket = new AuthenticationTicket(CreatePrincipal(userHeader.ToString()), SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
	public static ClaimsPrincipal CreatePrincipal(string user)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user),
			new(ClaimTypes.Name, user),
		};
		if (HasClaim(user))
		{
			claims.Add(new Claim(Issue284Policy.ClaimType, Issue284Policy.ClaimValue));
		}

		if (HasRole(user))
		{
			claims.Add(new Claim(ClaimTypes.Role, Issue284Policy.Role));
		}

		return new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
	}
}
internal sealed class Issue284AltAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(Issue284AuthenticationHandler.AltUserHeaderName, out var userHeader))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var user = userHeader.ToString();
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, user),
			new Claim(ClaimTypes.Name, user),
		};
		var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Issue284AuthenticationHandler.AltSchemeName));
		var ticket = new AuthenticationTicket(principal, Issue284AuthenticationHandler.AltSchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
