using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Htmxor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

public sealed class Issue187ParityTests
{
	[Fact]
	public async Task Ordinary_request_has_byte_exact_paired_response_parity()
	{
		await using var stock = await Issue187ParityHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue187ParityHost.CreateAsync(useHtmxor: true);

		using var stockResponse = await stock.Client.SendAsync(CreateAuthorizedRequest());
		using var htmxorResponse = await htmxor.Client.SendAsync(CreateAuthorizedRequest());
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var htmxorSnapshot = await Issue187ResponseSnapshot.CreateAsync(htmxorResponse);

		Assert.Equal(HttpStatusCode.OK, stockSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.StatusCode, htmxorSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Headers, htmxorSnapshot.Headers);
		Assert.Equal(stockSnapshot.Body, htmxorSnapshot.Body);
		Assert.Contains("data-item-id=\"42\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-query=\"from-query\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-di=\"from-di\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-user=\"issue-187-user\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-auth-state-user=\"issue-187-user\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-authorization-policy=\"issue-187-policy\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-endpoint-marker=\"preserved\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-session=\"session-value\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-lifecycle=\"initialized|parameters-set\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("ordinary-output", stockSnapshot.Body, StringComparison.Ordinal);
		AssertSessionCookie(stockSnapshot.Headers);
	}

	[Fact]
	public async Task Paired_endpoints_preserve_component_route_and_authorization_metadata()
	{
		await using var stock = await Issue187ParityHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue187ParityHost.CreateAsync(useHtmxor: true);

		var stockEndpoint = stock.GetIssueEndpoint();
		var htmxorEndpoint = htmxor.GetIssueEndpoint();

		Assert.Equal(stockEndpoint.RoutePattern.RawText, htmxorEndpoint.RoutePattern.RawText);
		Assert.Equal(
			stockEndpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type,
			htmxorEndpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type);
		Assert.Equal(
			stockEndpoint.Metadata.GetMetadata<RootComponentMetadata>()?.Type,
			htmxorEndpoint.Metadata.GetMetadata<RootComponentMetadata>()?.Type);
		Assert.Equal(
			GetAuthorizationPolicies(stockEndpoint),
			GetAuthorizationPolicies(htmxorEndpoint));
		Assert.Equal(
			stockEndpoint.Metadata.GetMetadata<Issue187EndpointMetadata>(),
			htmxorEndpoint.Metadata.GetMetadata<Issue187EndpointMetadata>());
	}

	[Fact]
	public async Task Unauthorized_request_has_paired_rejection()
	{
		await using var stock = await Issue187ParityHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue187ParityHost.CreateAsync(useHtmxor: true);

		using var stockResponse = await stock.Client.GetAsync(Issue187ParityConstants.RequestPath);
		using var htmxorResponse = await htmxor.Client.GetAsync(Issue187ParityConstants.RequestPath);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxorBody = await htmxorResponse.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, stockResponse.StatusCode);
		Assert.Equal(stockResponse.StatusCode, htmxorResponse.StatusCode);
		Assert.Equal(stockBody, htmxorBody);
		Assert.DoesNotContain("data-issue-187", stockBody, StringComparison.Ordinal);
	}

	private static HttpRequestMessage CreateAuthorizedRequest()
	{
		var request = new HttpRequestMessage(HttpMethod.Get, Issue187ParityConstants.RequestPath);
		request.Headers.Add(Issue187AuthenticationHandler.UserHeaderName, Issue187ParityConstants.AuthorizedUser);
		return request;
	}

	private static string[] GetAuthorizationPolicies(RouteEndpoint endpoint)
		=> endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
			.Select(metadata => metadata.Policy ?? $"roles:{metadata.Roles}|schemes:{metadata.AuthenticationSchemes}")
			.ToArray();

	private static void AssertSessionCookie(IReadOnlyDictionary<string, string> headers)
	{
		Assert.True(headers.TryGetValue("Set-Cookie", out var setCookie));
		Assert.Contains("issue187-session=<session-id>", setCookie, StringComparison.Ordinal);
	}
}

internal static class Issue187ParityConstants
{
	public const string PolicyName = "issue-187-policy";
	public const string AuthorizedUser = "issue-187-user";
	public const string RequestPath = "/issue-187/42?query=from-query";
	public const string SessionKey = "issue-187-session-key";
	public const string SessionValue = "session-value";
}

internal sealed record Issue187EndpointMetadata(string Value)
{
	public static Issue187EndpointMetadata Instance { get; } = new("preserved");
}

internal sealed class Issue187RequestProbe
{
	public string Value { get; } = "from-di";

	public List<string> Lifecycle { get; } = [];

	public void Record(string lifecycleEvent) => Lifecycle.Add(lifecycleEvent);
}

internal sealed class Issue187ParityHost(WebApplication app, HttpClient client) : IAsyncDisposable
{
	public WebApplication App { get; } = app;

	public HttpClient Client { get; } = client;

	public static async Task<Issue187ParityHost> CreateAsync(bool useHtmxor)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue187App).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddDistributedMemoryCache();
		builder.Services.AddSession(options => options.Cookie.Name = "issue187-session");
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddAuthentication(Issue187AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue187AuthenticationHandler>(
				Issue187AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(
			Issue187ParityConstants.PolicyName,
			policy => policy.RequireClaim(
				Issue187AuthenticationHandler.AccessClaim,
				Issue187AuthenticationHandler.AccessValue)));
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddScoped<Issue187RequestProbe>();
		var razorComponents = builder.Services.AddRazorComponents();
		if (useHtmxor)
		{
			razorComponents.AddHtmxor();
		}

		var app = builder.Build();
		app.UseSession();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue187App>()
			.WithMetadata(Issue187EndpointMetadata.Instance);
		if (useHtmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new Issue187ParityHost(app, app.GetTestClient());
	}

	public RouteEndpoint GetIssueEndpoint()
		=> ((IEndpointRouteBuilder)App).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Single(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue187Page));

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await App.DisposeAsync();
	}
}

internal sealed class Issue187AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-187-test";
	public const string UserHeaderName = "X-Issue-187-User";
	public const string AccessClaim = "issue-187-access";
	public const string AccessValue = "granted";

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
			new Claim(AccessClaim, AccessValue),
		};
		var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
		var ticket = new AuthenticationTicket(principal, SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}

internal sealed record Issue187ResponseSnapshot(
	HttpStatusCode StatusCode,
	IReadOnlyDictionary<string, string> Headers,
	string Body)
{
	public static async Task<Issue187ResponseSnapshot> CreateAsync(HttpResponseMessage response)
	{
		var headers = response.Headers
			.Concat(response.Content.Headers)
			.GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				group => group.Key,
				group => NormalizeHeader(group.Key, group.SelectMany(header => header.Value)),
				StringComparer.OrdinalIgnoreCase);
		var body = await response.Content.ReadAsStringAsync();
		return new Issue187ResponseSnapshot(response.StatusCode, headers, body);
	}

	private static string NormalizeHeader(string name, IEnumerable<string> values)
	{
		if (name.Equals("Date", StringComparison.OrdinalIgnoreCase))
		{
			return "<dynamic-date>";
		}

		if (!name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
		{
			return string.Join("\n", values);
		}

		return string.Join("\n", values.Select(NormalizeCookie));
	}

	private static string NormalizeCookie(string value)
	{
		const string cookiePrefix = "issue187-session=";
		if (!value.StartsWith(cookiePrefix, StringComparison.Ordinal))
		{
			return value;
		}

		var separator = value.IndexOf(';');
		return cookiePrefix + "<session-id>" + (separator < 0 ? string.Empty : value[separator..]);
	}
}
