using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
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

/// <summary>
/// A <see cref="DisableHtmxDirectRoutingAttribute"/>-marked component, authored as .razor,
/// .razor.cs, and all-C#, keeps its stock page for normal, boosted, and full requests, and is
/// never selected for a direct partial request.
/// </summary>
public sealed class Issue175NormalOnlyReachabilityTests : IAsyncLifetime
{
	private const string AuthorizedUser = "issue-175-user";
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue175NormalOnlyReachabilityTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthentication(Issue175AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue175AuthenticationHandler>(
				Issue175AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(
			"issue-175-policy",
			policy => policy.RequireClaim(Issue175AuthenticationHandler.AccessClaim, "granted")));
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue175ApplicationProbe>();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.WithMetadata(Issue175MetadataSentinel.Instance)
			.AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
	}

	public static IEnumerable<object[]> MarkedPages()
	{
		yield return new object[] { "/issue-175/razor", typeof(Issue175RazorPage), "razor" };
		yield return new object[] { "/issue-175/codebehind", typeof(Issue175CodeBehindPage), "codebehind" };
		yield return new object[] { "/issue-175/csharp", typeof(Issue175CSharpPage), "csharp" };
	}

	[Theory]
	[MemberData(nameof(MarkedPages))]
	public async Task Normal_boosted_and_full_requests_all_get_the_stock_page_with_preserved_metadata(
		string path,
		Type componentType,
		string form)
	{
		AssertSingleMarkedEndpoint(path, componentType);

		await AssertStockResponseAsync(path, form, new HttpRequestMessage(HttpMethod.Get, path));

		using var boosted = new HttpRequestMessage(HttpMethod.Get, path);
		boosted.Headers.Add("HX-Request", "true");
		boosted.Headers.Add("HX-Boosted", "true");
		boosted.Headers.Add("HX-Request-Type", "full");
		await AssertStockResponseAsync(path, form, boosted);

		using var full = new HttpRequestMessage(HttpMethod.Get, path);
		full.Headers.Add("HX-Request", "true");
		full.Headers.Add("HX-Request-Type", "full");
		await AssertStockResponseAsync(path, form, full);

		// Positive control: the marked component does legitimately initialize once per stock
		// request above, so this discriminates a fixture that lost its OnInitialized override or
		// its probe injection.
		Assert.Equal(3, app.Services.GetRequiredService<Issue175ApplicationProbe>().InitializationCount);
	}

	[Theory]
	[MemberData(nameof(MarkedPages))]
	public async Task Direct_partial_GET_is_not_selected_and_gets_404_with_no_lifecycle_work(
		string path,
		Type componentType,
		string form)
	{
		_ = componentType;
		_ = form;

		var probe = app.Services.GetRequiredService<Issue175ApplicationProbe>();
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal(0, probe.InitializationCount);
	}

	[Theory]
	[MemberData(nameof(MarkedPages))]
	public async Task Direct_partial_POST_is_not_selected_and_gets_404_with_no_lifecycle_work(
		string path,
		Type componentType,
		string form)
	{
		_ = componentType;
		_ = form;

		var probe = app.Services.GetRequiredService<Issue175ApplicationProbe>();
		using var request = new HttpRequestMessage(HttpMethod.Post, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal(0, probe.InitializationCount);
	}

	[Fact]
	public async Task Anonymous_direct_partial_requests_on_an_authorized_marked_page_get_404_with_no_lifecycle_work()
	{
		const string path = "/issue-175/authorized";
		var probe = app.Services.GetRequiredService<Issue175ApplicationProbe>();

		using var getRequest = new HttpRequestMessage(HttpMethod.Get, path);
		getRequest.Headers.Add("HX-Request", "true");
		getRequest.Headers.Add("HX-Request-Type", "partial");
		using var getResponse = await client.SendAsync(getRequest);

		using var postRequest = new HttpRequestMessage(HttpMethod.Post, path);
		postRequest.Headers.Add("HX-Request", "true");
		postRequest.Headers.Add("HX-Request-Type", "partial");
		using var postResponse = await client.SendAsync(postRequest);

		Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
		Assert.Equal(0, probe.InitializationCount);
	}

	[Fact]
	public async Task Anonymous_normal_GET_on_an_authorized_marked_page_is_challenged()
	{
		using var response = await client.GetAsync("/issue-175/authorized");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Authenticated_normal_boosted_and_full_requests_on_an_authorized_marked_page_get_the_stock_page()
	{
		const string path = "/issue-175/authorized";

		await AssertAuthorizedStockResponseAsync(new HttpRequestMessage(HttpMethod.Get, path));

		using var boosted = new HttpRequestMessage(HttpMethod.Get, path);
		boosted.Headers.Add("HX-Request", "true");
		boosted.Headers.Add("HX-Boosted", "true");
		boosted.Headers.Add("HX-Request-Type", "full");
		await AssertAuthorizedStockResponseAsync(boosted);

		using var full = new HttpRequestMessage(HttpMethod.Get, path);
		full.Headers.Add("HX-Request", "true");
		full.Headers.Add("HX-Request-Type", "full");
		await AssertAuthorizedStockResponseAsync(full);
	}

	[Fact]
	public async Task A_stock_named_form_on_a_marked_component_keeps_its_stock_POST()
	{
		const string path = "/issue-175/stockform";
		using var pageResponse = await client.GetAsync(path);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();
		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);
		var token = ExtractAttribute(pageBody, "name=\"__RequestVerificationToken\"", "value");
		var cookie = ExtractAntiforgeryCookie(pageResponse);

		var fields = new Dictionary<string, string>
		{
			["_handler"] = "issue-175-stock-form",
			["Input.Value"] = "accepted-value",
			["__RequestVerificationToken"] = token,
		};
		using var request = new HttpRequestMessage(HttpMethod.Post, path)
		{
			Content = new FormUrlEncodedContent(fields),
		};
		request.Headers.Add("Cookie", cookie);

		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", body, StringComparison.Ordinal);
		Assert.Contains("data-submitted-value=\"accepted-value\"", body, StringComparison.Ordinal);
	}

	private void AssertSingleMarkedEndpoint(string path, Type componentType)
	{
		var endpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == componentType);
		var endpoint = Assert.Single(endpoints);

		Assert.Equal(path, endpoint.RoutePattern.RawText);
		Assert.Same(Issue175MetadataSentinel.Instance, endpoint.Metadata.GetMetadata<Issue175MetadataSentinel>());

		// Stock Blazor's own endpoint-metadata convention attaches a plain class attribute exactly
		// as it attaches [Authorize] or [Host(...)] elsewhere in this suite, independent of anything
		// Htmxor wires for it.
		Assert.NotNull(endpoint.Metadata.GetMetadata<DisableHtmxDirectRoutingAttribute>());
	}

	private async Task AssertStockResponseAsync(string path, string form, HttpRequestMessage request)
	{
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK for {path}, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", body, StringComparison.Ordinal);
		Assert.Contains($"data-issue-175-page=\"{form}\"", body, StringComparison.Ordinal);
		Assert.Contains("data-route-metadata=\"preserved\"", body, StringComparison.Ordinal);

		// HtmxLayout only applies to the direct HTMX render path; the stock response must never
		// show its wrapper.
		Assert.DoesNotContain("data-issue-175-htmx-layout", body, StringComparison.Ordinal);
	}

	private async Task AssertAuthorizedStockResponseAsync(HttpRequestMessage request)
	{
		request.Headers.Add(Issue175AuthenticationHandler.UserHeaderName, AuthorizedUser);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", body, StringComparison.Ordinal);
		Assert.Contains("data-issue-175-page=\"authorized\"", body, StringComparison.Ordinal);
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

internal sealed class Issue175ApplicationProbe
{
	private int initializationCount;

	public int InitializationCount => Volatile.Read(ref initializationCount);

	public void RecordInitialization() => Interlocked.Increment(ref initializationCount);
}

internal sealed record Issue175MetadataSentinel(string Value)
{
	public static Issue175MetadataSentinel Instance { get; } = new("preserved");
}

internal sealed class Issue175AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-175-test";
	public const string UserHeaderName = "X-Issue-175-User";
	public const string AccessClaim = "issue-175-access";

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
