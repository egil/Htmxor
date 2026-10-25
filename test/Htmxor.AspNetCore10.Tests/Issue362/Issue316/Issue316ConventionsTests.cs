using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Issue316;

public sealed class Issue316ConventionsTests : IClassFixture<Issue316ConventionHosts>
{
	public const string MarkerHeaderName = "X-Issue-316-Marker";
	public const string StaticAssetsHeaderName = "X-Issue-316-Static-Assets";
	public const string AntiforgeryHeaderName = "X-Issue-316-Antiforgery";

	private const string NoAttrStockPath = "/issue-362-316/no-attr/stock/1";
	private const string NoAttrHtmxPath = "/issue-362-316/no-attr/htmx/1";
	private const string UnsafeActionPath = "/issue-362-316/unsafe-action/1";

	private readonly Issue316ConventionHosts hosts;

	public Issue316ConventionsTests(Issue316ConventionHosts hosts) => this.hosts = hosts;

	public static IEnumerable<object[]> ChainedConfigurations()
	{
		yield return new object[] { Issue316Configuration.Before };
		yield return new object[] { Issue316Configuration.After };
	}

	[Theory]
	[MemberData(nameof(ChainedConfigurations))]
	public async Task Chained_WithStaticAssets_reaches_the_HtmxRoute_endpoint_exactly_as_the_stock_twin(
		Issue316Configuration configuration)
	{
		var host = hosts.Get(configuration);

		using var stockResponse = await SendAsync(host.Client, NoAttrStockPath, HttpMethod.Get, direct: false);
		using var htmxResponse = await SendAsync(host.Client, NoAttrHtmxPath, HttpMethod.Get, direct: true);

		Assert.Multiple(
			() => Assert.True(
			stockResponse.Headers.Contains(StaticAssetsHeaderName),
			$"Stock twin ({configuration}) must carry the ResourceAssetCollection metadata " +
			"WithStaticAssets() attaches."),
			() => Assert.True(
			htmxResponse.Headers.Contains(StaticAssetsHeaderName),
			$"HtmxRoute twin ({configuration}) must carry the same ResourceAssetCollection metadata as its " +
			"stock twin; the framework's own WithStaticAssets() convention, chained onto MapRazorComponents, " +
			"must also reach generated HtmxRoute endpoints."));
	}

	[Theory]
	[MemberData(nameof(ChainedConfigurations))]
	public async Task Anonymous_unsafe_action_on_a_no_attribute_HtmxRoute_component_is_rejected_before_antiforgery_binding_and_callback(
		Issue316Configuration configuration)
	{
		var host = hosts.Get(configuration);
		host.Probe.Reset();

		using var response = await SendAsync(host.Client, UnsafeActionPath, HttpMethod.Delete, direct: true);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.Unauthorized,
			$"({configuration}) expected 401 from RequireAuthorization() before antiforgery, binding, or the " +
			$"callback ran; got {(int)response.StatusCode} {response.StatusCode}. Body: {body}"),
			() => Assert.Equal(0, host.Probe.InitializationCount),
			() => Assert.Equal(0, host.Probe.BindingCount),
			() => Assert.Equal(0, host.Probe.CallbackCount));
	}
	[Theory]
	[MemberData(nameof(ChainedConfigurations))]
	public async Task Authorized_request_renders_the_HtmxRoute_component_exactly_as_its_stock_twin(
		Issue316Configuration configuration)
	{
		var host = hosts.Get(configuration);

		using var stockResponse = await SendAsync(host.Client, NoAttrStockPath, HttpMethod.Get, direct: false, authenticated: true);
		using var htmxResponse = await SendAsync(host.Client, NoAttrHtmxPath, HttpMethod.Get, direct: true, authenticated: true);
		using var htmxPlainResponse = await SendAsync(host.Client, NoAttrHtmxPath, HttpMethod.Get, direct: false, authenticated: true);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			stockResponse.StatusCode == HttpStatusCode.OK && stockBody.Contains("data-issue-316-result", StringComparison.Ordinal),
			$"Stock twin ({configuration}) expected 200 with the rendered body, got {(int)stockResponse.StatusCode}. Body: {stockBody}"),
			() => Assert.True(
			htmxResponse.StatusCode == HttpStatusCode.OK && htmxBody.Contains("data-issue-316-result", StringComparison.Ordinal),
			$"HtmxRoute twin ({configuration}) expected 200 with the rendered body to match its authorized stock " +
			$"twin, got {(int)htmxResponse.StatusCode}. Body: {htmxBody}"),
			() => Assert.True(
			stockResponse.Headers.Contains(MarkerHeaderName) && htmxResponse.Headers.Contains(MarkerHeaderName),
			$"Both twins ({configuration}) must carry the WithMetadata marker on a rendered, authorized response."),
			() => Assert.True(
			stockResponse.Headers.Contains(StaticAssetsHeaderName) && htmxResponse.Headers.Contains(StaticAssetsHeaderName),
			$"Both twins ({configuration}) must carry the WithStaticAssets resource-collection metadata on a " +
			"rendered, authorized response."),
			() => Assert.True(
			htmxPlainResponse.StatusCode == HttpStatusCode.NotFound,
			$"An authorized HtmxRoute request without HX headers ({configuration}) expected 404 (never 200), " +
			$"got {(int)htmxPlainResponse.StatusCode}."));
	}
	[Theory]
	[MemberData(nameof(ChainedConfigurations))]
	public async Task Authorized_unsafe_action_with_a_valid_antiforgery_token_runs_the_callback_once(
		Issue316Configuration configuration)
	{
		var host = hosts.Get(configuration);
		host.Probe.Reset();
		var (token, cookie) = CreateAntiforgeryCredentials(host.App);

		using var request = new HttpRequestMessage(HttpMethod.Delete, UnsafeActionPath);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add(Issue316AuthenticationHandler.UserHeaderName, "issue-316-user");
		request.Headers.Add("Cookie", cookie);
		request.Headers.Add("RequestVerificationToken", token);
		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"({configuration}) expected 200 for an authorized DELETE with a valid token, got " +
			$"{(int)response.StatusCode} {response.StatusCode}. Body: {body}"),
			() => Assert.Equal(1, host.Probe.InitializationCount),
			() => Assert.Equal(1, host.Probe.BindingCount),
			() => Assert.Equal(1, host.Probe.CallbackCount));
	}
	[Theory]
	[MemberData(nameof(ChainedConfigurations))]
	public async Task Chained_conventions_leave_the_GET_only_HtmxRoute_endpoints_antiforgery_metadata_matching_its_stock_twin(
		Issue316Configuration configuration)
	{
		var host = hosts.Get(configuration);

		using var stockResponse = await SendAsync(host.Client, NoAttrStockPath, HttpMethod.Get, direct: false);
		using var htmxResponse = await SendAsync(host.Client, NoAttrHtmxPath, HttpMethod.Get, direct: true);
		var stockValue = Assert.Single(stockResponse.Headers.GetValues(AntiforgeryHeaderName));
		var htmxValue = Assert.Single(htmxResponse.Headers.GetValues(AntiforgeryHeaderName));

		Assert.True(
			stockValue == htmxValue,
			$"HtmxRoute twin ({configuration}) antiforgery metadata \"{htmxValue}\" must match its stock twin's " +
			$"\"{stockValue}\".");
	}

	[Fact]
	public async Task Route_group_RequireAuthorization_protects_the_HtmxRoute_endpoint()
	{
		// Control for the chained rows: the same requirement applied through a route group reaches the
		// HtmxRoute endpoint, so a chained-row failure points at the convention builder, not at
		// RequireAuthorization() itself.
		using var response = await SendAsync(hosts.Group.Client, NoAttrHtmxPath, HttpMethod.Get, direct: true);

		Assert.True(
			response.StatusCode == HttpStatusCode.Unauthorized,
			$"Expected 401 under MapGroup(\"\").RequireAuthorization(), got {(int)response.StatusCode}.");
	}

	private static async Task<HttpResponseMessage> SendAsync(
		HttpClient client,
		string path,
		HttpMethod method,
		bool direct,
		bool authenticated = false)
	{
		using var request = new HttpRequestMessage(method, path);
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		if (authenticated)
		{
			request.Headers.Add(Issue316AuthenticationHandler.UserHeaderName, "issue-316-user");
		}

		return await client.SendAsync(request);
	}
	private static (string Token, string Cookie) CreateAntiforgeryCredentials(WebApplication app)
	{
		var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "issue-316-user")],
			Issue316AuthenticationHandler.SchemeName);
		var context = new DefaultHttpContext
		{
			RequestServices = app.Services,
			User = new ClaimsPrincipal(identity),
		};
		var tokenSet = antiforgery.GetAndStoreTokens(context);
		var setCookie = context.Response.Headers["Set-Cookie"]
			.Single(value => value!.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))!;

		return (tokenSet.RequestToken!, setCookie.Split(';')[0]);
	}
}

public enum Issue316Configuration
{
	Before,
	After,
}
public sealed class Issue316ConventionHosts : IAsyncLifetime
{
	private Issue316ConventionHost before = default!;
	private Issue316ConventionHost after = default!;

	public Issue316ConventionHost Group { get; private set; } = default!;

	public async Task InitializeAsync()
	{
		before = await Issue316ConventionHost.CreateAsync(Issue316Configuration.Before);
		after = await Issue316ConventionHost.CreateAsync(Issue316Configuration.After);
		Group = await Issue316ConventionHost.CreateGroupGuardAsync();
	}

	public Issue316ConventionHost Get(Issue316Configuration configuration) => configuration switch
	{
		Issue316Configuration.Before => before,
		Issue316Configuration.After => after,
		_ => throw new ArgumentOutOfRangeException(nameof(configuration), configuration, message: null),
	};

	public async Task DisposeAsync()
	{
		await before.DisposeAsync();
		await after.DisposeAsync();
		await Group.DisposeAsync();
	}
}

public sealed class Issue316ConventionHost : IAsyncDisposable
{
	public WebApplication App { get; }

	public HttpClient Client { get; }

	public Issue316UnsafeActionProbe Probe { get; }

	private Issue316ConventionHost(WebApplication app, HttpClient client, Issue316UnsafeActionProbe probe)
	{
		App = app;
		Client = client;
		Probe = probe;
	}
	public static Task<Issue316ConventionHost> CreateAsync(Issue316Configuration configuration) =>
		CreateAsync(configuration, useGroup: false);
	public static Task<Issue316ConventionHost> CreateGroupGuardAsync() =>
		CreateAsync(configuration: null, useGroup: true);

	private static async Task<Issue316ConventionHost> CreateAsync(Issue316Configuration? configuration, bool useGroup)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue316App).Assembly.GetName().Name,
		});
		builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Logging.ClearProviders();
		builder.Services.AddAuthentication(Issue316AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue316AuthenticationHandler>(
				Issue316AuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue316UnsafeActionProbe>();

		var app = builder.Build();
		app.MapStaticAssets();
		app.UseRouting();
		app.Use(RecordObservedConventionsAsync);
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();

		if (useGroup)
		{
			app.MapGroup(string.Empty)
				.RequireAuthorization()
				.MapRazorComponents<Issue316App>()
				.AddHtmxorEndpoints();
		}
		else if (configuration == Issue316Configuration.Before)
		{
			app.MapRazorComponents<Issue316App>()
				.RequireAuthorization()
				.WithMetadata(new Issue316ConventionMarker())
				.WithStaticAssets()
				.AddHtmxorEndpoints();
		}
		else
		{
			app.MapRazorComponents<Issue316App>()
				.AddHtmxorEndpoints()
				.RequireAuthorization()
				.WithMetadata(new Issue316ConventionMarker())
				.WithStaticAssets();
		}

		await app.StartAsync();

		var addressesFeature = app.Services
			.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
			.Features
			.Get<IServerAddressesFeature>()!;
		var boundAddress = addressesFeature.Addresses.Single();
		var client = new HttpClient { BaseAddress = new Uri(boundAddress) };
		var probe = app.Services.GetRequiredService<Issue316UnsafeActionProbe>();

		return new Issue316ConventionHost(app, client, probe);
	}

	private static async Task RecordObservedConventionsAsync(HttpContext context, Func<Task> next)
	{
		SetHeaderIfMetadataPresent<Issue316ConventionMarker>(context, Issue316ConventionsTests.MarkerHeaderName);
		SetHeaderIfMetadataPresent<Microsoft.AspNetCore.Components.ResourceAssetCollection>(context, Issue316ConventionsTests.StaticAssetsHeaderName);
		context.Response.Headers[Issue316ConventionsTests.AntiforgeryHeaderName] =
			AntiforgeryValue(context.GetEndpoint()?.Metadata.GetMetadata<IAntiforgeryMetadata>());
		await next();
	}

	private static void SetHeaderIfMetadataPresent<T>(HttpContext context, string header) where T : class
	{
		if (context.GetEndpoint()?.Metadata.GetMetadata<T>() is not null)
		{
			context.Response.Headers[header] = "present";
		}
	}

	private static string AntiforgeryValue(IAntiforgeryMetadata? metadata)
		=> metadata is null ? "absent" : metadata.RequiresValidation ? "required" : "not-required";

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await App.DisposeAsync();
	}
}
public sealed class Issue316ConventionMarker
{
}
internal sealed class Issue316AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-316-test";

	public const string UserHeaderName = "X-Issue-316-User";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.ContainsKey(UserHeaderName))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "issue-316-user")], SchemeName);
		var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}

public sealed class Issue316UnsafeActionProbe
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
		Volatile.Write(ref initializationCount, 0);
		Volatile.Write(ref bindingCount, 0);
		Volatile.Write(ref callbackCount, 0);
	}
}
