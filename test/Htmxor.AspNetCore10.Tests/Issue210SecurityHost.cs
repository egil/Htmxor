using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal sealed class Issue210SecurityHost(WebApplication app, HttpClient client) : IAsyncDisposable
{
	public WebApplication App { get; } = app;
	public HttpClient Client { get; } = client;
	public Issue210Probe Probe => App.Services.GetRequiredService<Issue210Probe>();
	public Issue210StreamGate StreamGate => App.Services.GetRequiredService<Issue210StreamGate>();

	public static async Task<Issue210SecurityHost> StartAsync(
		bool tokens = true,
		bool disableNative = false,
		string? cors = null,
		bool groupAuthorization = false,
		bool htmxor = true,
		string groupPrefix = "/group")
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue210SecurityHost).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.Configuration["DisableCsrfProtection"] = disableNative.ToString();
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		ConfigureServices(builder.Services, htmxor);
		var app = builder.Build();
		app.UseRouting();
		app.UseCors();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseRateLimiter();
		if (tokens)
		{
			app.UseAntiforgery();
		}

		var group = app.MapGroup(groupPrefix).RequireHost("localhost").RequireRateLimiting("bounded");
		if (cors is not null)
		{
			group.RequireCors(cors);
		}

		if (groupAuthorization)
		{
			group.RequireAuthorization("custom");
		}

		var components = group.MapRazorComponents<Issue78App>();
		if (htmxor)
		{
			components.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		var client = app.GetTestClient();
		client.DefaultRequestHeaders.Add(Issue83AuthenticationHandler.UserHeaderName, Issue83AuthenticationHandler.AuthorizedUser);
		return new(app, client);
	}

	private static void ConfigureServices(IServiceCollection services, bool htmxor)
	{
		services.AddDataProtection().UseEphemeralDataProtectionProvider();
		services.AddAuthentication(Issue83AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue83AuthenticationHandler>(Issue83AuthenticationHandler.SchemeName, _ => { });
		services.AddAuthorization(options =>
		{
			options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
			options.AddPolicy("custom", policy => policy.RequireAuthenticatedUser().AddRequirements(new Issue210Requirement()));
		});
		services.AddSingleton<IAuthorizationHandler, Issue210RequirementHandler>();
		services.AddCors(options =>
		{
			options.AddPolicy("trusted", policy => policy.WithOrigins("https://trusted.example").AllowAnyMethod().AllowAnyHeader());
			options.AddPolicy("wildcard", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
		});
		services.AddRateLimiter(options => options.AddFixedWindowLimiter("bounded", limiter =>
		{
			limiter.PermitLimit = 100;
			limiter.Window = TimeSpan.FromMinutes(1);
		}));
		var components = services.AddRazorComponents();
		if (htmxor)
		{
			components.AddHtmxor();
		}

		services.AddSingleton<Issue210Probe>();
		services.AddSingleton<Issue210StreamGate>();
	}

	public static HttpRequestMessage Request(string method, string page = "actions", bool form = false, bool direct = true)
	{
		var path = page == "actionless" ? "actionless/1" : page;
		var request = new HttpRequestMessage(new HttpMethod(method), $"/group/issue-210/{path}");
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		if (form)
		{
			var fields = new Dictionary<string, string>
			{
				["Value"] = "submitted",
			};
			if (page.EndsWith("form", StringComparison.Ordinal))
			{
				fields["_handler"] = "save";
			}

			request.Content = new FormUrlEncodedContent(fields);
		}

		return request;
	}

	public async Task AddCredentialsAsync(HttpRequestMessage request)
	{
		using var get = Request("GET", "form", direct: false);
		using var response = await Client.SendAsync(get);
		var html = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		var match = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
		Assert.True(match.Success, html);
		request.Headers.Add("RequestVerificationToken", WebUtility.HtmlDecode(match.Groups[1].Value));
		request.Headers.Add("Cookie", Assert.Single(response.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';', 2)[0]);
		Probe.Reset();
	}

	public async ValueTask DisposeAsync()
	{
		StreamGate.Completion.TrySetResult();
		Client.Dispose();
		await App.DisposeAsync();
	}
}

internal sealed class Issue210Probe
{
	public int Bindings { get; private set; }
	public int Initializations { get; private set; }
	public int Callbacks { get; private set; }
	public void Binding() => Bindings++;
	public void Initialize() => Initializations++;
	public void Callback() => Callbacks++;
	public void Reset() => (Bindings, Initializations, Callbacks) = (0, 0, 0);
	public void AssertUntouched() => Assert.Equal((0, 0, 0), (Bindings, Initializations, Callbacks));
}

internal sealed class Issue210StreamGate
{
	public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class Issue210Requirement : IAuthorizationRequirement;

internal sealed class Issue210RequirementHandler : AuthorizationHandler<Issue210Requirement>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Issue210Requirement requirement)
	{
		if (context.User.HasClaim("issue-83-access", "granted"))
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}
