using System.Net;
using System.Security.Claims;
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
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Routing;

public sealed class Issue145RootRegistrationTests : IAsyncLifetime
{
	private const string ReportPolicyName = "issue-97-report-policy";
	private const string SummaryPolicyName = "issue-97-summary-policy";
	private const string AuditPolicyName = "issue-141-audit-policy";
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue145RootRegistrationTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthentication(PackageConsumerAuthenticationHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, PackageConsumerAuthenticationHandler>(
				PackageConsumerAuthenticationHandler.SchemeName,
				_ => { });
		builder.Services.AddAuthorization(options =>
		{
			AddPolicy(options, ReportPolicyName, PackageConsumerAuthenticationHandler.ReportAccess);
			AddPolicy(options, SummaryPolicyName, PackageConsumerAuthenticationHandler.SummaryAccess);
			AddPolicy(options, AuditPolicyName, PackageConsumerAuthenticationHandler.AuditAccess);
		});
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue100ApplicationProbe>();
		builder.Services.AddScoped<Issue100RequestProbe>();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue97App>()
			.AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task Root_registration_preserves_stock_and_generated_component_endpoints()
	{
		using var normalResponse = await SendAsync(
			"/issue-362/reports/42",
			direct: false,
			PackageConsumerAuthenticationHandler.ReportUserName,
			"reports.internal.example");
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, normalResponse.StatusCode),
			() => Assert.Contains("data-stock-shell", normalBody, StringComparison.Ordinal),
			() => Assert.Contains("data-issue-100-report-page", normalBody, StringComparison.Ordinal));

		using var directResponse = await SendAsync(
			"/issue-362/htmx-reports/42",
			direct: true,
			PackageConsumerAuthenticationHandler.ReportUserName,
			"reports.internal.example");
		var directBody = await directResponse.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, directResponse.StatusCode),
			() => Assert.Contains("data-issue-97-report-component", directBody, StringComparison.Ordinal),
			() => Assert.DoesNotContain("data-stock-shell", directBody, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(typeof(Issue100ReportPage), "/issue-362/reports/{ReportId:int}")]
	[InlineData(typeof(Issue97ReportComponent), "/issue-362/htmx-reports/{ReportId:int}")]
	[InlineData(typeof(Issue97SummaryComponent), "/issue-362/summaries/{SummaryId:int}")]
	[InlineData(typeof(Issue141AuditComponent), "/issue-362/audits/{AuditId:int}")]
	public void Root_registration_preserves_each_component_endpoints_identity_and_route(Type componentType, string route)
	{

		var componentEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(static dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(static endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>() is not null)
			.ToArray();
		var endpoint = Assert.Single(componentEndpoints, candidate =>
			candidate.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type == componentType);
		Assert.Multiple(
			() => Assert.Equal(route, endpoint.RoutePattern.RawText),
			() => Assert.Null(endpoint.Metadata.GetMetadata<Issue97GroupMetadata>()));
	}

	private static void AddPolicy(AuthorizationOptions options, string name, string access)
		=> options.AddPolicy(
			name,
			policy => policy.RequireClaim(PackageConsumerAuthenticationHandler.AccessClaim, access));

	private async Task<HttpResponseMessage> SendAsync(
		string path,
		bool direct,
		string userName,
		string host)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Host = host;
		request.Headers.Add(PackageConsumerAuthenticationHandler.UserHeaderName, userName);
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
			request.Headers.Add("HX-Current-URL", "https://example.test/orders");
			request.Headers.Add("HX-Target", "section#patch-result");
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
