using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

public sealed class Issue83AuthorizationTests : IAsyncLifetime
{
	private const string PolicyName = "issue-83-policy";
	private const string RequiredClaimType = "issue-83-access";
	private const string RequiredClaimValue = "granted";
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue83AuthorizationTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Services.AddAuthentication(Issue83AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue83AuthenticationHandler>(
				Issue83AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(
			PolicyName,
			policy => policy.RequireClaim(RequiredClaimType, RequiredClaimValue)));
		builder.Services.AddRazorComponents().AddHtmx();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.AddHtmxorComponentEndpoints(app);

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Theory]
	[InlineData(null, HttpStatusCode.Unauthorized)]
	[InlineData(Issue83AuthenticationHandler.ForbiddenUser, HttpStatusCode.Forbidden)]
	public async Task Protected_page_rejects_normal_and_direct_gets_equally(
		string? user,
		HttpStatusCode expectedStatusCode)
	{
		using var normalResponse = await SendAsync(user, direct: false);
		using var directResponse = await SendAsync(user, direct: true);

		Assert.Equal(expectedStatusCode, normalResponse.StatusCode);
		Assert.Equal(normalResponse.StatusCode, directResponse.StatusCode);
	}

	[Fact]
	public async Task Authorized_user_reaches_normal_and_direct_gets_with_the_same_principal()
	{
		using var normalResponse = await SendAsync(Issue83AuthenticationHandler.AuthorizedUser, direct: false);
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		using var directResponse = await SendAsync(Issue83AuthenticationHandler.AuthorizedUser, direct: true);
		var directBody = await directResponse.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, normalResponse.StatusCode);
		Assert.Equal(normalResponse.StatusCode, directResponse.StatusCode);
		Assert.Contains("data-stock-shell", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-issue-83-page", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-authenticated-user>authorized-user|granted</p>", normalBody, StringComparison.Ordinal);
		Assert.DoesNotContain("data-stock-shell", directBody, StringComparison.Ordinal);
		Assert.Contains("data-issue-83-page", directBody, StringComparison.Ordinal);
		Assert.Contains("data-authenticated-user>authorized-user|granted</p>", directBody, StringComparison.Ordinal);
	}

	[Fact]
	public void Protected_page_owns_one_component_route()
	{
		var pageEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue83ProtectedPage));

		Assert.Single(pageEndpoints);
	}

	private async Task<HttpResponseMessage> SendAsync(string? user, bool direct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-83");
		if (user is not null)
		{
			request.Headers.Add(Issue83AuthenticationHandler.UserHeaderName, user);
		}

		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		return await client.SendAsync(request);
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

internal sealed class Issue83AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-83-test";
	public const string UserHeaderName = "X-Issue-83-User";
	public const string AuthorizedUser = "authorized-user";
	public const string ForbiddenUser = "forbidden-user";

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
		if (string.Equals(user, AuthorizedUser, StringComparison.Ordinal))
		{
			claims.Add(new Claim("issue-83-access", "granted"));
		}

		var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
		var ticket = new AuthenticationTicket(principal, SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
