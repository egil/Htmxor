using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

/// <summary>
/// RequireAuthorization() chained onto MapRazorComponents&lt;App&gt;() -- before and after
/// AddHtmxorEndpoints() -- protects a generated HtmxRoute endpoint exactly as it protects its stock
/// @page twin, pinned on this project's supported framework matrix (#316): an anonymous request gets
/// 401 with HX headers and 404 without them, never 200; an authenticated request renders the
/// HtmxRoute component exactly as it renders the stock twin, and still 404s without HX headers; a
/// non-authorization WithMetadata marker reaches the HtmxRoute endpoint's metadata exactly as it
/// reaches the stock twin's. On net11.0 only, the framework's own WithBrowserOptions() convention
/// reaches the HtmxRoute endpoint exactly as it reaches the stock twin's.
/// </summary>
public sealed class Issue316ConventionParityTests : IAsyncLifetime
{
	private const string MarkerHeaderName = "X-Issue-316-Conv-Marker";
	private const string StockPath = "/issue-316-conv/no-attr/stock/1";
	private const string HtmxPath = "/issue-316-conv/no-attr/htmx/1";

	private WebApplication beforeApp = default!;
	private HttpClient beforeClient = default!;
	private WebApplication afterApp = default!;
	private HttpClient afterClient = default!;

	public async Task InitializeAsync()
	{
		(beforeApp, beforeClient) = await CreateHostAsync(before: true);
		(afterApp, afterClient) = await CreateHostAsync(before: false);
	}

	public async Task DisposeAsync()
	{
		beforeClient.Dispose();
		await beforeApp.DisposeAsync();
		afterClient.Dispose();
		await afterApp.DisposeAsync();
	}

	public static IEnumerable<object[]> Configurations()
	{
		yield return new object[] { "before" };
		yield return new object[] { "after" };
	}

	[Theory]
	[MemberData(nameof(Configurations))]
	public async Task HtmxRoute_component_with_no_attributes_matches_its_stock_twin_under_chained_RequireAuthorization(
		string configuration)
	{
		var client = Client(configuration);
		using var stockPlain = await SendAsync(client, StockPath, direct: false);
		using var stockDirect = await SendAsync(client, StockPath, direct: true);
		using var htmxDirect = await SendAsync(client, HtmxPath, direct: true);
		using var htmxPlain = await SendAsync(client, HtmxPath, direct: false);

		Assert.True(
			stockPlain.StatusCode == HttpStatusCode.Unauthorized,
			$"Stock twin ({configuration}) expected 401, got {(int)stockPlain.StatusCode}.");
		Assert.True(
			stockDirect.StatusCode == HttpStatusCode.Unauthorized,
			$"Stock twin with HTMX headers ({configuration}) expected 401, got {(int)stockDirect.StatusCode}.");
		Assert.True(
			htmxDirect.StatusCode == HttpStatusCode.Unauthorized,
			$"HtmxRoute twin ({configuration}) expected 401 to match its stock twin, got {(int)htmxDirect.StatusCode}.");
		Assert.True(
			htmxPlain.StatusCode == HttpStatusCode.NotFound,
			$"HtmxRoute twin ({configuration}) without HX headers expected 404 (never 200), got {(int)htmxPlain.StatusCode}.");
	}

	[Theory]
	[MemberData(nameof(Configurations))]
	public async Task Authorized_request_renders_the_HtmxRoute_component_exactly_as_its_stock_twin(
		string configuration)
	{
		var client = Client(configuration);
		using var stockResponse = await SendAsync(client, StockPath, direct: false, authenticated: true);
		using var htmxResponse = await SendAsync(client, HtmxPath, direct: true, authenticated: true);
		using var htmxPlainResponse = await SendAsync(client, HtmxPath, direct: false, authenticated: true);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();

		Assert.True(
			stockResponse.StatusCode == HttpStatusCode.OK &&
				stockBody.Contains("data-issue-316-conv-result", StringComparison.Ordinal),
			$"Stock twin ({configuration}) expected 200 with the rendered body, got {(int)stockResponse.StatusCode}. Body: {stockBody}");
		Assert.True(
			htmxResponse.StatusCode == HttpStatusCode.OK &&
				htmxBody.Contains("data-issue-316-conv-result", StringComparison.Ordinal),
			$"HtmxRoute twin ({configuration}) expected 200 with the rendered body, got {(int)htmxResponse.StatusCode}. Body: {htmxBody}");
		Assert.True(
			htmxPlainResponse.StatusCode == HttpStatusCode.NotFound,
			$"An authorized HtmxRoute request without HX headers ({configuration}) expected 404, got {(int)htmxPlainResponse.StatusCode}.");
	}

	[Theory]
	[MemberData(nameof(Configurations))]
	public async Task Chained_WithMetadata_marker_reaches_the_HtmxRoute_endpoint_exactly_as_the_stock_twin(
		string configuration)
	{
		var client = Client(configuration);
		using var stockResponse = await SendAsync(client, StockPath, direct: false);
		using var htmxResponse = await SendAsync(client, HtmxPath, direct: true);

		Assert.True(
			stockResponse.Headers.Contains(MarkerHeaderName),
			$"Stock twin ({configuration}) must carry the marker chained onto MapRazorComponents.");
		Assert.True(
			htmxResponse.Headers.Contains(MarkerHeaderName),
			$"HtmxRoute twin ({configuration}) must carry the same WithMetadata marker as its stock twin.");
	}

#if NET11_0_OR_GREATER
	[Theory]
	[MemberData(nameof(Configurations))]
	public async Task Chained_WithBrowserOptions_reaches_the_HtmxRoute_endpoint_exactly_as_the_stock_twin(
		string configuration)
	{
		var client = Client(configuration);
		using var stockResponse = await SendAsync(client, StockPath, direct: false);
		using var htmxResponse = await SendAsync(client, HtmxPath, direct: true);

		Assert.True(
			stockResponse.Headers.Contains(BrowserOptionsHeaderName),
			$"Stock twin ({configuration}) must carry the BrowserOptions metadata WithBrowserOptions() attaches.");
		Assert.True(
			htmxResponse.Headers.Contains(BrowserOptionsHeaderName),
			$"HtmxRoute twin ({configuration}) must carry the same BrowserOptions metadata as its stock twin; " +
			"the framework's own WithBrowserOptions() convention, chained onto MapRazorComponents, must also " +
			"reach generated HtmxRoute endpoints on net11.0.");
	}

	private const string BrowserOptionsHeaderName = "X-Issue-316-Conv-BrowserOptions";
#endif

	private HttpClient Client(string configuration) => configuration == "before" ? beforeClient : afterClient;

	private static async Task<HttpResponseMessage> SendAsync(
		HttpClient client,
		string path,
		bool direct,
		bool authenticated = false)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		if (authenticated)
		{
			request.Headers.Add(Issue316ConventionAuthenticationHandler.UserHeaderName, "issue-316-conv-user");
		}

		return await client.SendAsync(request);
	}

	private static async Task<(WebApplication App, HttpClient Client)> CreateHostAsync(bool before)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue316ConventionParityTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddAuthentication(Issue316ConventionAuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue316ConventionAuthenticationHandler>(
				Issue316ConventionAuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization();
		builder.Services.AddRazorComponents().AddHtmxor();

		var app = builder.Build();
		app.UseRouting();
		app.Use(RecordObservedConventionsAsync);
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		MapConventions(app, before);

		await app.StartAsync();
		return (app, app.GetTestClient());
	}

	private static async Task RecordObservedConventionsAsync(HttpContext context, Func<Task> next)
	{
		var endpoint = context.GetEndpoint();
		SetHeaderIfMetadataPresent<Issue316ConventionMarker>(context, endpoint, MarkerHeaderName);
#if NET11_0_OR_GREATER
		SetHeaderIfMetadataPresent<Microsoft.AspNetCore.Components.BrowserOptions>(context, endpoint, BrowserOptionsHeaderName);
#endif

		await next();
	}

	private static void SetHeaderIfMetadataPresent<T>(HttpContext context, Endpoint? endpoint, string headerName)
		where T : class
	{
		if (endpoint?.Metadata.GetMetadata<T>() is not null)
		{
			context.Response.Headers[headerName] = "present";
		}
	}

	private static void MapConventions(WebApplication app, bool before)
	{
		if (before)
		{
			var componentBuilder = app.MapRazorComponents<Issue78App>()
				.RequireAuthorization()
				.WithMetadata(new Issue316ConventionMarker());
			ApplyNet11Conventions(componentBuilder);
			componentBuilder.AddHtmxorEndpoints();
			return;
		}

		var afterBuilder = app.MapRazorComponents<Issue78App>()
			.AddHtmxorEndpoints()
			.RequireAuthorization()
			.WithMetadata(new Issue316ConventionMarker());
		ApplyNet11Conventions(afterBuilder);
	}

	private static void ApplyNet11Conventions(RazorComponentsEndpointConventionBuilder componentBuilder)
	{
#if NET11_0_OR_GREATER
		componentBuilder.WithBrowserOptions(_ => { });
#endif
	}
}

internal sealed class Issue316ConventionMarker
{
}

/// <summary>
/// Authenticates a request that carries <see cref="UserHeaderName"/> and leaves every other request
/// anonymous, so the chained RequireAuthorization() is observed both rejecting (401) and admitting
/// (200) through real middleware.
/// </summary>
internal sealed class Issue316ConventionAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-316-conv-test";

	public const string UserHeaderName = "X-Issue-316-Conv-User";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.ContainsKey(UserHeaderName))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "issue-316-conv-user")], SchemeName);
		var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
