using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Htmxor.AspNetCore10;

public sealed class Issue359ResponseHost : IAsyncLifetime
{
	public WebApplication App { get; private set; } = default!;
	public HttpClient Client { get; private set; } = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue359ResponseHost).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthentication(Issue359AuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, Issue359AuthenticationHandler>(Issue359AuthenticationHandler.SchemeName, _ => { });
		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy("issue359-report", policy => policy.RequireClaim(Issue359AuthenticationHandler.AccessClaim, "report"));
			options.AddPolicy("issue359-group", policy => policy.RequireClaim(Issue359AuthenticationHandler.GroupAccessClaim));
		});
		builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
		builder.Services.AddRazorComponents().AddHtmxor();
		App = builder.Build();
		App.UseAuthentication();
		App.UseAuthorization();
		App.UseAntiforgery();
		App.MapGroup("/issue359-response").RequireAuthorization("issue359-group")
			.MapRazorComponents<Issue359ResponseApp>().AddHtmxorEndpoints();
		await App.StartAsync();
		Client = App.GetTestClient();
	}

	public async Task DisposeAsync()
	{
		Client.Dispose();
		await App.DisposeAsync();
	}
}

internal sealed class Issue359AuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue359-test";
	public const string UserHeaderName = "X-Issue-97-User";
	public const string AccessClaim = "issue-97-access";
	public const string GroupAccessClaim = "issue-145-group-access";
	public const string ReportUserName = "issue-97-report-user";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(UserHeaderName, out var userName))
			return Task.FromResult(AuthenticateResult.NoResult());
		Claim[] claims =
		[
			new(ClaimTypes.Name, userName.ToString()),
			new(AccessClaim, "report"),
			new(GroupAccessClaim, "allowed"),
		];
		var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
		return Task.FromResult(AuthenticateResult.Success(new(principal, SchemeName)));
	}
}
