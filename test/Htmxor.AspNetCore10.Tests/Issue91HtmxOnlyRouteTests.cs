using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Htmxor.Endpoints;
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

public sealed class Issue91HtmxOnlyRouteTests : IAsyncLifetime
{
	private const string PolicyName = "issue-91-policy";
	private const string RoutePrefix = "/issue-91-group";
	private const string DeclaredRoute = "/reports/{ReportId:int}";
	private const string RequestPath = "/issue-91-group/reports/42?query=from-query";
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue91HtmxOnlyRouteTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Services.AddAuthentication(Issue91AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue91AuthenticationHandler>(
				Issue91AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(
			PolicyName,
			policy => policy.RequireClaim(Issue91AuthenticationHandler.AccessClaim, "granted")));
		builder.Services.AddRazorComponents().AddHtmx();
		builder.Services.AddScoped(_ => new Issue91RequestProbe("from-scoped-di"));

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		var routes = app.MapGroup(RoutePrefix)
			.WithMetadata(Issue91GroupMetadata.Instance);
		routes.MapRazorComponents<Issue78App>()
			.AddHtmxorComponentEndpoints(routes);

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task Htmx_only_get_uses_one_generated_component_endpoint()
	{
		using var directResponse = await SendAsync(HttpMethod.Get, direct: true, authenticated: true);
		var directBody = await directResponse.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, directResponse.StatusCode);
		Assert.Contains("data-issue-91-component", directBody, StringComparison.Ordinal);
		Assert.Contains(
			"data-request-values>42|from-query|issue-91-user|from-scoped-di|1|group-convention-preserved</p>",
			directBody,
			StringComparison.Ordinal);
		Assert.Equal(2, directBody.Split("data-issue-91-component", StringSplitOptions.None).Length);
		Assert.DoesNotContain("<html", directBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", directBody, StringComparison.Ordinal);

		var componentEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue91HtmxOnlyComponent));
		var componentEndpoint = Assert.Single(componentEndpoints);
		Assert.Equal(RoutePrefix + DeclaredRoute, componentEndpoint.RoutePattern.RawText);
		Assert.Equal(
			[HttpMethods.Get],
			componentEndpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
		Assert.Same(
			Issue91GroupMetadata.Instance,
			componentEndpoint.Metadata.GetRequiredMetadata<Issue91GroupMetadata>());
		Assert.Equal(
			typeof(HtmxorDirectComponentHost),
			componentEndpoint.Metadata.GetRequiredMetadata<RootComponentMetadata>().Type);
		var declaredRoute = componentEndpoint.Metadata.GetRequiredMetadata<HtmxRouteAttribute>();
		Assert.Equal(DeclaredRoute, declaredRoute.Template);
		Assert.Equal(HttpMethods.Get, Assert.Single(declaredRoute.Methods));
		Assert.Contains(
			componentEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
			metadata => string.Equals(metadata.Policy, PolicyName, StringComparison.Ordinal));

		using var normalResponse = await SendAsync(HttpMethod.Get, direct: false, authenticated: true);
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.NotFound, normalResponse.StatusCode);
		Assert.DoesNotContain("data-issue-91-component", normalBody, StringComparison.Ordinal);

		using var anonymousResponse = await SendAsync(HttpMethod.Get, direct: true, authenticated: false);
		var anonymousBody = await anonymousResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
		Assert.DoesNotContain("data-issue-91-component", anonymousBody, StringComparison.Ordinal);

		using var rejectedConstraintResponse = await SendAsync(
			HttpMethod.Get,
			direct: true,
			authenticated: true,
			"/issue-91-group/reports/not-an-int?query=from-query");
		Assert.Equal(HttpStatusCode.NotFound, rejectedConstraintResponse.StatusCode);

		foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
		{
			using var response = await SendAsync(method, direct: true, authenticated: true);
			var body = await response.Content.ReadAsStringAsync();
			Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
			Assert.DoesNotContain("data-issue-91-component", body, StringComparison.Ordinal);
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("unknown")]
	[InlineData("full")]
	public async Task Htmx_only_get_rejects_requests_without_one_partial_request_type(string? requestType)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, RequestPath);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add(Issue91AuthenticationHandler.UserHeaderName, "issue-91-user");
		if (requestType is not null)
		{
			request.Headers.TryAddWithoutValidation("HX-Request-Type", requestType);
		}

		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.DoesNotContain("data-issue-91-component", body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Htmx_only_get_rejects_contradictory_request_types()
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, RequestPath);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add(Issue91AuthenticationHandler.UserHeaderName, "issue-91-user");
		request.Headers.TryAddWithoutValidation("HX-Request-Type", ["partial", "full"]);

		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.DoesNotContain("data-issue-91-component", body, StringComparison.Ordinal);
	}

	private async Task<HttpResponseMessage> SendAsync(
		HttpMethod method,
		bool direct,
		bool authenticated,
		string path = RequestPath)
	{
		using var request = new HttpRequestMessage(method, path);
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		if (authenticated)
		{
			request.Headers.Add(Issue91AuthenticationHandler.UserHeaderName, "issue-91-user");
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

internal sealed record Issue91GroupMetadata(string Value)
{
	public static Issue91GroupMetadata Instance { get; } = new("group-convention-preserved");
}

internal sealed class Issue91RequestProbe(string value)
{
	private int initializationCount;

	public string Value { get; } = value;

	public int RecordInitialization() => ++initializationCount;
}

internal sealed class Issue91AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-91-test";
	public const string UserHeaderName = "X-Issue-91-User";
	public const string AccessClaim = "issue-91-access";

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
