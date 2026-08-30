using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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

public sealed class Issue89LifecycleCompositionTests : IAsyncLifetime
{
	private const string AuthorizedUser = "issue-89-user";
	private const string PolicyName = "issue-89-policy";
	private WebApplication app = default!;
	private HttpClient client = default!;
	private Issue89ApplicationProbe applicationProbe = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue89LifecycleCompositionTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthentication(Issue89AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue89AuthenticationHandler>(
				Issue89AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(
			PolicyName,
			policy => policy.RequireClaim(Issue89AuthenticationHandler.AccessClaim, "granted")));
		builder.Services.AddRazorComponents().AddHtmx();
		builder.Services.AddSingleton<Issue89ApplicationProbe>();
		builder.Services.AddScoped<Issue89RequestProbe>();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		Issue89GeneratedAction.Register(app.MapRazorComponents<Issue78App>(), app);

		await app.StartAsync();
		client = app.GetTestClient();
		applicationProbe = app.Services.GetRequiredService<Issue89ApplicationProbe>();
	}

	[Fact]
	public async Task Generated_hook_preserves_the_application_parameter_lifecycle_for_normal_and_armed_requests()
	{
		using var pageRequest = CreateAuthorizedRequest(HttpMethod.Get);
		using var pageResponse = await client.SendAsync(pageRequest);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();

		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);
		Assert.Contains("<html", pageBody, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", pageBody, StringComparison.Ordinal);
		AssertLifecycleState(pageBody, expectedCallbackCount: 0, expectedSequence: NormalLifecycleSequence);
		Assert.Equal(1, applicationProbe.OverrideStartCount);
		Assert.Equal(1, applicationProbe.OverrideCompletionCount);
		Assert.Equal(1, applicationProbe.InitializationCount);
		Assert.Equal(1, applicationProbe.ParameterCount);
		Assert.Equal(0, applicationProbe.CallbackCount);

		var token = ExtractAttribute(pageBody, "name=\"__RequestVerificationToken\"", "value");
		var cookie = ExtractAntiforgeryCookie(pageResponse);
		var pageRequestId = ExtractAttribute(pageBody, "data-issue-89-result", "data-request-id");
		applicationProbe.Reset();

		using var deleteRequest = CreateDeleteRequest(cookie, token);
		using var deleteResponse = await client.SendAsync(deleteRequest);
		var deleteBody = await deleteResponse.Content.ReadAsStringAsync();

		Assert.True(deleteResponse.StatusCode == HttpStatusCode.OK, deleteBody);
		Assert.DoesNotContain("<html", deleteBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-item-id=\"42\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-query-source=\"from-query\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains($"data-user=\"{AuthorizedUser}\"", deleteBody, StringComparison.Ordinal);
		Assert.Contains("data-request-value=\"from-request-scope\"", deleteBody, StringComparison.Ordinal);
		AssertLifecycleState(deleteBody, expectedCallbackCount: 1, expectedSequence: ActionLifecycleSequence);
		Assert.Contains(
			$"delete|42|from-query|{AuthorizedUser}|from-request-scope|parameters-complete",
			deleteBody,
			StringComparison.Ordinal);
		Assert.NotEqual(pageRequestId, ExtractAttribute(deleteBody, "data-issue-89-result", "data-request-id"));
		Assert.Equal(1, applicationProbe.OverrideStartCount);
		Assert.Equal(1, applicationProbe.OverrideCompletionCount);
		Assert.Equal(1, applicationProbe.InitializationCount);
		Assert.Equal(1, applicationProbe.ParameterCount);
		Assert.Equal(1, applicationProbe.CallbackCount);
	}

	private const string NormalLifecycleSequence =
		"override-start|initialized|parameters-set|override-complete";

	private const string ActionLifecycleSequence =
		"override-start|initialized|parameters-set|override-complete|callback";

	private static void AssertLifecycleState(
		string body,
		int expectedCallbackCount,
		string expectedSequence)
	{
		Assert.Contains("data-override-start-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains("data-override-completion-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains("data-initialization-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains("data-parameter-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains($"data-callback-count=\"{expectedCallbackCount}\"", body, StringComparison.Ordinal);
		Assert.Contains("data-application-state=\"parameters-complete\"", body, StringComparison.Ordinal);
		Assert.Contains($"data-lifecycle-sequence=\"{expectedSequence}\"", body, StringComparison.Ordinal);
	}

	private static HttpRequestMessage CreateDeleteRequest(string cookie, string token)
	{
		var request = CreateAuthorizedRequest(HttpMethod.Delete);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("Cookie", cookie);
		request.Headers.Add("RequestVerificationToken", token);
		return request;
	}

	private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method)
	{
		var request = new HttpRequestMessage(method, "/issue-89/42?source=from-query");
		request.Headers.Add(Issue89AuthenticationHandler.UserHeaderName, AuthorizedUser);
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

internal sealed class Issue89AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-89-test";
	public const string UserHeaderName = "X-Issue-89-User";
	public const string AccessClaim = "issue-89-access";

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

internal sealed class Issue89ApplicationProbe
{
	private int overrideStartCount;
	private int overrideCompletionCount;
	private int initializationCount;
	private int parameterCount;
	private int callbackCount;

	public int OverrideStartCount => Volatile.Read(ref overrideStartCount);

	public int OverrideCompletionCount => Volatile.Read(ref overrideCompletionCount);

	public int InitializationCount => Volatile.Read(ref initializationCount);

	public int ParameterCount => Volatile.Read(ref parameterCount);

	public int CallbackCount => Volatile.Read(ref callbackCount);

	public void RecordOverrideStart() => Interlocked.Increment(ref overrideStartCount);

	public void RecordOverrideCompletion() => Interlocked.Increment(ref overrideCompletionCount);

	public void RecordInitialization() => Interlocked.Increment(ref initializationCount);

	public void RecordParametersSet() => Interlocked.Increment(ref parameterCount);

	public void RecordCallback() => Interlocked.Increment(ref callbackCount);

	public void Reset()
	{
		Interlocked.Exchange(ref overrideStartCount, 0);
		Interlocked.Exchange(ref overrideCompletionCount, 0);
		Interlocked.Exchange(ref initializationCount, 0);
		Interlocked.Exchange(ref parameterCount, 0);
		Interlocked.Exchange(ref callbackCount, 0);
	}
}

internal sealed class Issue89RequestProbe(Issue89ApplicationProbe applicationProbe)
{
	private readonly List<string> lifecycleSequence = [];

	public string Id { get; } = Guid.NewGuid().ToString("D");

	public string Value { get; } = "from-request-scope";

	public int OverrideStartCount { get; private set; }

	public int OverrideCompletionCount { get; private set; }

	public int InitializationCount { get; private set; }

	public int ParameterCount { get; private set; }

	public int CallbackCount { get; private set; }

	public IReadOnlyList<string> LifecycleSequence => lifecycleSequence;

	public void RecordOverrideStart()
	{
		OverrideStartCount++;
		lifecycleSequence.Add("override-start");
		applicationProbe.RecordOverrideStart();
	}

	public void RecordOverrideCompletion()
	{
		OverrideCompletionCount++;
		lifecycleSequence.Add("override-complete");
		applicationProbe.RecordOverrideCompletion();
	}

	public void RecordInitialization()
	{
		InitializationCount++;
		lifecycleSequence.Add("initialized");
		applicationProbe.RecordInitialization();
	}

	public void RecordParametersSet()
	{
		ParameterCount++;
		lifecycleSequence.Add("parameters-set");
		applicationProbe.RecordParametersSet();
	}

	public void RecordCallback()
	{
		CallbackCount++;
		lifecycleSequence.Add("callback");
		applicationProbe.RecordCallback();
	}
}
