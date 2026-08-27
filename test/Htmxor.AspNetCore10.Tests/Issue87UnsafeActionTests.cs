using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Htmxor.Builder;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

public sealed class Issue87UnsafeActionTests : IAsyncLifetime
{
	private const string AuthorizedUser = "issue-87-user";
	private const string PolicyName = "issue-87-policy";
	private WebApplication app = default!;
	private HttpClient client = default!;
	private Issue87ApplicationProbe applicationProbe = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue87UnsafeActionTests).Assembly.GetName().Name,
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
		builder.Services.AddSingleton<Issue87ApplicationProbe>();
		builder.Services.AddScoped<Issue87RequestProbe>();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		Issue87GeneratedActions.Register(app.MapRazorComponents<Issue78App>(), app);

		await app.StartAsync();
		client = app.GetTestClient();
		applicationProbe = app.Services.GetRequiredService<Issue87ApplicationProbe>();
	}

	[Fact]
	public async Task Delete_uses_the_authorized_request_component_and_renders_callback_state()
	{
		var endpoint = Assert.Single(GetIssue87Endpoints());
		var methods = Assert.IsAssignableFrom<IHttpMethodMetadata>(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>());
		Assert.Equal(
			[HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete],
			methods.HttpMethods);
		Assert.Collection(
			endpoint.Metadata.GetOrderedMetadata<HtmxorComponentActionDescriptor>(),
			descriptor => AssertDescriptor(descriptor, Issue87GeneratedActions.PutDescriptor),
			descriptor => AssertDescriptor(descriptor, Issue87GeneratedActions.PatchDescriptor),
			descriptor => AssertDescriptor(descriptor, Issue87GeneratedActions.DeleteDescriptor));
		Assert.Contains(
			endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
			metadata => metadata.Policy == PolicyName);
		Assert.True(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);

		using var pageRequest = CreateAuthorizedRequest(HttpMethod.Get, "/issue-87/42?source=from-query");
		using var pageResponse = await client.SendAsync(pageRequest);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();
		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);
		var token = ExtractAttribute(pageBody, "name=\"__RequestVerificationToken\"", "value");
		var cookie = ExtractAntiforgeryCookie(pageResponse);
		var pageRequestId = ExtractAttribute(pageBody, "data-issue-87-result", "data-request-id");
		applicationProbe.Reset();

		using var deleteRequest = CreateUnsafeActionRequest(HttpMethods.Delete, cookie, token);
		using var deleteResponse = await client.SendAsync(deleteRequest);
		var deleteBody = await deleteResponse.Content.ReadAsStringAsync();

		Assert.True(
			deleteResponse.StatusCode == HttpStatusCode.OK,
			$"Expected 200 OK, received {(int)deleteResponse.StatusCode} {deleteResponse.StatusCode}. Body: {deleteBody}");
		Assert.Contains("data-item-id=\"42\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-query-source=\"from-query\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains($"data-user=\"{AuthorizedUser}\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-binding-count=\"1\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-initialization-count=\"1\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-callback-count=\"1\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-callback-name=\"delete\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-action-completed=\"true\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains($"delete|42|from-query|{AuthorizedUser}|from-request-scope", deleteBody, StringComparison.Ordinal);
		Assert.NotEqual(pageRequestId, ExtractAttribute(deleteBody, "data-issue-87-result", "data-request-id"));
		Assert.DoesNotContain("<html", deleteBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", deleteBody, StringComparison.Ordinal);
		Assert.Equal(1, applicationProbe.BindingCount);
		Assert.Equal(1, applicationProbe.InitializationCount);
		Assert.Equal(1, applicationProbe.CallbackCount);
	}

	[Theory]
	[InlineData("PUT", "put")]
	[InlineData("PATCH", "patch")]
	[InlineData("DELETE", "delete")]
	public async Task Each_declared_method_invokes_its_distinct_callback(string method, string expectedCallback)
	{
		var (token, cookie) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateUnsafeActionRequest(method, cookie, token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains($"data-callback-name=\"{expectedCallback}\"", body, StringComparison.Ordinal);
		Assert.Contains("data-callback-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Equal(1, applicationProbe.CallbackCount);
	}

	[Theory]
	[InlineData("PUT", "PATCH")]
	[InlineData("PATCH", "DELETE")]
	[InlineData("DELETE", "PUT")]
	public async Task Client_identity_cannot_select_another_method_callback(string method, string forgedMethod)
	{
		var (token, cookie) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateUnsafeActionRequest(
			method,
			cookie,
			token,
			Issue87GeneratedActions.GetDescriptor(forgedMethod).HandlerIdentity);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains($"data-callback-name=\"{method.ToLowerInvariant()}\"", body, StringComparison.Ordinal);
		Assert.DoesNotContain($"data-callback-name=\"{forgedMethod.ToLowerInvariant()}\"", body, StringComparison.Ordinal);
		Assert.Equal(1, applicationProbe.CallbackCount);
	}

	[Theory]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	[InlineData("DELETE")]
	public async Task Invalid_antiforgery_token_rejects_before_binding_or_application_code(string method)
	{
		var (_, cookie) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateUnsafeActionRequest(method, cookie, "invalid-token");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.DoesNotContain("data-issue-87-result", body, StringComparison.Ordinal);
		AssertNoApplicationCodeRan();
	}

	[Fact]
	public async Task Client_identity_cannot_widen_the_server_method_allow_list()
	{
		using var request = CreateUnsafeActionRequest(
			"PROPFIND",
			cookie: null,
			token: null,
			Issue87GeneratedActions.DeleteHandlerIdentity);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
		Assert.DoesNotContain("data-issue-87-result", body, StringComparison.Ordinal);
		AssertNoApplicationCodeRan();
	}

	private IEnumerable<RouteEndpoint> GetIssue87Endpoints()
		=> ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue87Page));

	private async Task<(string Token, string Cookie)> GetAntiforgeryCredentialsAsync()
	{
		using var pageRequest = CreateAuthorizedRequest(HttpMethod.Get, "/issue-87/42?source=from-query");
		using var pageResponse = await client.SendAsync(pageRequest);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();
		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);
		return (
			ExtractAttribute(pageBody, "name=\"__RequestVerificationToken\"", "value"),
			ExtractAntiforgeryCookie(pageResponse));
	}

	private void AssertNoApplicationCodeRan()
	{
		Assert.Equal(0, applicationProbe.BindingCount);
		Assert.Equal(0, applicationProbe.InitializationCount);
		Assert.Equal(0, applicationProbe.CallbackCount);
	}

	private static void AssertDescriptor(
		HtmxorComponentActionDescriptor actual,
		HtmxorComponentActionDescriptor expected)
	{
		Assert.Same(expected, actual);
		Assert.Equal(typeof(Issue87Page), actual.ComponentType);
		Assert.Equal("/issue-87/{ItemId:int}", actual.NormalizedRoute);
		Assert.Equal(expected.HttpMethod, actual.HttpMethod);
		Assert.Equal(expected.HandlerIdentity, actual.HandlerIdentity);
	}

	private static HttpRequestMessage CreateUnsafeActionRequest(
		string method,
		string? cookie,
		string? token,
		string? clientHandlerIdentity = null)
	{
		var request = CreateAuthorizedRequest(new HttpMethod(method), "/issue-87/42?source=from-query");
		request.Headers.Add("HX-Request", "true");
		if (cookie is not null)
		{
			request.Headers.Add("Cookie", cookie);
		}

		if (token is not null)
		{
			request.Headers.Add("RequestVerificationToken", token);
		}

		if (clientHandlerIdentity is not null)
		{
			request.Headers.Add("HXOR-Event-Handler-Id", clientHandlerIdentity);
		}

		return request;
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

internal sealed class Issue87ApplicationProbe
{
	private int bindingCount;
	private int initializationCount;
	private int callbackCount;

	public int BindingCount => Volatile.Read(ref bindingCount);

	public int InitializationCount => Volatile.Read(ref initializationCount);

	public int CallbackCount => Volatile.Read(ref callbackCount);

	public void RecordBinding() => Interlocked.Increment(ref bindingCount);

	public void RecordInitialization() => Interlocked.Increment(ref initializationCount);

	public void RecordCallback() => Interlocked.Increment(ref callbackCount);

	public void Reset()
	{
		Interlocked.Exchange(ref bindingCount, 0);
		Interlocked.Exchange(ref initializationCount, 0);
		Interlocked.Exchange(ref callbackCount, 0);
	}
}

internal sealed class Issue87RequestProbe(Issue87ApplicationProbe applicationProbe)
{
	public string Id { get; } = Guid.NewGuid().ToString("D");

	public string Value { get; } = "from-request-scope";

	public int BindingCount { get; private set; }

	public int InitializationCount { get; private set; }

	public int CallbackCount { get; private set; }

	public void RecordBinding()
	{
		BindingCount++;
		applicationProbe.RecordBinding();
	}

	public void RecordInitialization()
	{
		InitializationCount++;
		applicationProbe.RecordInitialization();
	}

	public void RecordCallback()
	{
		CallbackCount++;
		applicationProbe.RecordCallback();
	}
}
