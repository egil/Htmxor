using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Htmxor;
using Htmxor.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Routing;

public sealed partial class Issue362RoutingActionsTests : IAsyncLifetime
{
	private const string ReportPolicyName = "issue-97-report-policy";
	private const string SummaryPolicyName = "issue-97-summary-policy";
	private const string AuditPolicyName = "issue-141-audit-policy";
	private const string GroupPolicyName = "issue-145-group-policy";
	private const string RoutePrefix = "/issue-362-group";
	private const string ReportDeclaredRoute = "/issue-362/htmx-reports/{ReportId:int}";
	private const string SummaryDeclaredRoute = "/issue-362/summaries/{SummaryId:int}";
	private const string AuditDeclaredRoute = "/issue-362/audits/{AuditId:int}";
	private const string PageDeclaredRoute = "/issue-362/reports/{ReportId:int}";
	private const string ReportRequestPath = "/issue-362-group/issue-362/htmx-reports/42";
	private const string SummaryRequestPath = "/issue-362-group/issue-362/summaries/84";
	private const string AuditRequestPath = "/issue-362-group/issue-362/audits/126";
	private const string PageRequestPath = RoutePrefix + "/issue-362/reports/42";
	private const string RejectedReportPath = "/issue-362-group/issue-362/htmx-reports/not-an-int";
	private const string RejectedSummaryPath = "/issue-362-group/issue-362/summaries/not-an-int";
	private const string RejectedAuditPath = "/issue-362-group/issue-362/audits/not-an-int";
	private const string ReportHost = "reports.internal.example";
	private const string SummaryHost = "summaries.internal.example";
	private const string AuditHost = "audits.internal.example";
	private const string PutHandlerIdentity =
		"Htmxor.AspNetCore10.Issue362.Routing.Issue100ReportPage.PUT.PutReport";
	private const string PatchHandlerIdentity =
		"Htmxor.AspNetCore10.Issue362.Routing.Issue97ReportComponent.PATCH.PatchReport";
	private WebApplication app = default!;
	private HttpClient client = default!;
	private Issue100ApplicationProbe applicationProbe = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue362RoutingActionsTests).Assembly.GetName().Name,
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
			options.AddPolicy(
				ReportPolicyName,
				policy => policy.RequireClaim(
					PackageConsumerAuthenticationHandler.AccessClaim,
					PackageConsumerAuthenticationHandler.ReportAccess));
			options.AddPolicy(
				SummaryPolicyName,
				policy => policy.RequireClaim(
					PackageConsumerAuthenticationHandler.AccessClaim,
					PackageConsumerAuthenticationHandler.SummaryAccess));
			options.AddPolicy(
				AuditPolicyName,
				policy => policy.RequireClaim(
					PackageConsumerAuthenticationHandler.AccessClaim,
					PackageConsumerAuthenticationHandler.AuditAccess));
			options.AddPolicy(
				GroupPolicyName,
				policy => policy.RequireClaim(PackageConsumerAuthenticationHandler.GroupAccessClaim));
		});
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue100ApplicationProbe>();
		builder.Services.AddScoped<Issue100RequestProbe>();

		app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseAntiforgery();
		var routes = app.MapGroup(RoutePrefix)
			.RequireAuthorization(GroupPolicyName)
			.WithMetadata(Issue97GroupMetadata.Instance);
		routes.MapRazorComponents<Issue97App>()
			.AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
		applicationProbe = app.Services.GetRequiredService<Issue100ApplicationProbe>();
	}

	[Fact]
	public void Report_route_preserves_its_endpoint_metadata()
	{
		var endpoint = AssertGeneratedEndpoint<Issue97ReportComponent>(
			GetGeneratedComponentEndpoints(),
			ReportDeclaredRoute,
			ReportPolicyName,
			[HttpMethods.Get, HttpMethods.Patch],
			[HttpMethods.Get, HttpMethods.Patch],
			"/orders",
			"section#patch-result",
			["section#patch-result", "div#fallback"]);
		Assert.Multiple(
			() => AssertHost(endpoint, ReportHost),
			() => Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation));
	}

	[Fact]
	public void Summary_route_preserves_its_endpoint_metadata()
	{
		var endpoints = GetGeneratedComponentEndpoints();
		var endpoint = AssertGeneratedEndpoint<Issue97SummaryComponent>(
			endpoints,
			SummaryDeclaredRoute,
			SummaryPolicyName,
			[HttpMethods.Get],
			[HttpMethods.Get],
			"/orders",
			"section",
			[]);
		AssertHost(endpoint, SummaryHost);
		// A GET-only HtmxRoute endpoint carries the same antiforgery metadata its stock twin does
		// (#316): antiforgery middleware does not validate GET regardless of this metadata's presence.
		Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation);
	}

	[Fact]
	public void Audit_route_preserves_its_endpoint_metadata()
	{
		var endpoints = GetGeneratedComponentEndpoints();

		var endpoint = AssertGeneratedEndpoint<Issue141AuditComponent>(
			endpoints,
			AuditDeclaredRoute,
			AuditPolicyName,
			[HttpMethods.Get, HttpMethods.Post],
			[HttpMethods.Get, HttpMethods.Post],
			"/orders",
			null,
			["section", "div#fallback"]);
		Assert.Multiple(
			() => AssertHost(endpoint, AuditHost),
			() => Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation));
	}

	[Fact]
	public async Task Report_route_preserves_its_component_policy_and_metadata()
	{
		await AssertDirectResponseAsync(
			ReportRequestPath,
			PackageConsumerAuthenticationHandler.ReportUserName,
			ReportHost,
			"data-issue-97-report-component",
			"data-report-request-values>42|issue-97-report-user|group-convention-preserved</p>",
			"data-issue-97-summary-component",
			"data-issue-141-audit-component");

	}

	[Fact]
	public async Task Summary_route_preserves_its_component_policy_and_metadata()
	{
		await AssertDirectResponseAsync(
			SummaryRequestPath,
			PackageConsumerAuthenticationHandler.SummaryUserName,
			SummaryHost,
			"data-issue-97-summary-component",
			"data-summary-request-values=\"\">84|issue-97-summary-user|group-convention-preserved|1|1|true</p>",
			"data-issue-97-report-component",
			"data-issue-141-audit-component");

	}

	[Fact]
	public async Task Third_route_preserves_deterministic_identity_and_its_component_contract()
	{
		await AssertDirectResponseAsync(
			AuditRequestPath,
			PackageConsumerAuthenticationHandler.AuditUserName,
			AuditHost,
			"data-issue-141-audit-component",
			"data-audit-request-values>126|issue-141-audit-user|group-convention-preserved</p>",
			"data-issue-97-report-component",
			"data-issue-97-summary-component");

	}

	private sealed record RouteCase(string Path, string User, string Host, string OtherUser, string OtherHost,
		string InvalidPath, string Marker, string Target);

	private static readonly RouteCase[] RouteCases =
	[
		new(ReportRequestPath, PackageConsumerAuthenticationHandler.ReportUserName, ReportHost,
			PackageConsumerAuthenticationHandler.SummaryUserName, SummaryHost, RejectedReportPath,
			"data-issue-97-report-component", "section#patch-result"),
		new(SummaryRequestPath, PackageConsumerAuthenticationHandler.SummaryUserName, SummaryHost,
			PackageConsumerAuthenticationHandler.ReportUserName, ReportHost, RejectedSummaryPath,
			"data-issue-97-summary-component", "section"),
		new(AuditRequestPath, PackageConsumerAuthenticationHandler.AuditUserName, AuditHost,
			PackageConsumerAuthenticationHandler.ReportUserName, ReportHost, RejectedAuditPath,
			"data-issue-141-audit-component", "div#fallback"),
	];

	public static IEnumerable<object?[]> RouteRejections()
	{
		foreach (var route in RouteCases)
		{
			yield return [route.Path, false, route.User, route.Host, HttpStatusCode.NotFound];
			yield return [route.Path, true, route.OtherUser, route.Host, HttpStatusCode.Forbidden];
			yield return [route.Path, true, null, route.Host, HttpStatusCode.Unauthorized];
			yield return [route.Path, true, route.User, route.OtherHost, HttpStatusCode.NotFound];
			yield return [route.InvalidPath, true, route.User, route.Host, HttpStatusCode.NotFound];
		}
	}

	[Theory]
	[MemberData(nameof(RouteRejections))]
	public async Task Component_route_preserves_representation_policy_host_and_constraint_rejection(
		string path, bool direct, string? user, string host, HttpStatusCode expected)
		=> await AssertUnavailableAsync(path, direct, user, host, expected);

	public static IEnumerable<object[]> MatchingRepresentations()
		=> RouteCases.Select(route => new object[] { route.Path, route.User, route.Host, route.Marker, route.Target });

	[Theory]
	[MemberData(nameof(MatchingRepresentations))]
	public async Task Matching_representation_selects_its_component(string path, string user, string host, string marker, string target)
	{
		using var response = await SendAsync(path, true, user, host, target: target);
		var body = await response.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains(marker, body, StringComparison.Ordinal));
	}

	public static IEnumerable<object[]> MismatchedRepresentations()
	{
		foreach (var route in RouteCases)
		{
			yield return [route.Path, route.User, route.Host, route.Marker, "https://example.test/other", route.Target];
			yield return [route.Path, route.User, route.Host, route.Marker, "https://example.test/orders", "div#other"];
			yield return [route.Path, route.User, route.Host, route.Marker, "ftp://example.test/orders", route.Target];
			yield return [route.Path, route.User, route.Host, route.Marker, "https://example.test/orders", " "];
		}
	}

	[Theory]
	[MemberData(nameof(MismatchedRepresentations))]
	public async Task Mismatched_representation_cannot_select_its_component(
		string path, string user, string host, string marker, string currentUrl, string target)
	{
		using var response = await SendAsync(path, true, user, host, currentUrl, target);
		var body = await response.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode),
			() => Assert.DoesNotContain(marker, body, StringComparison.Ordinal));
	}

	public static TheoryData<string, string, string, HttpStatusCode> GroupOwnedPaths => new()
	{
		{ PageRequestPath, PackageConsumerAuthenticationHandler.ReportWithoutGroupUserName, ReportHost, HttpStatusCode.Forbidden },
		{ ReportRequestPath, PackageConsumerAuthenticationHandler.ReportWithoutGroupUserName, ReportHost, HttpStatusCode.Forbidden },
		{ "/issue-362/reports/42", PackageConsumerAuthenticationHandler.ReportUserName, ReportHost, HttpStatusCode.NotFound },
		{ "/issue-362/htmx-reports/42", PackageConsumerAuthenticationHandler.ReportUserName, ReportHost, HttpStatusCode.NotFound },
		{ "/issue-362/summaries/84", PackageConsumerAuthenticationHandler.SummaryUserName, SummaryHost, HttpStatusCode.NotFound },
		{ "/issue-362/audits/126", PackageConsumerAuthenticationHandler.AuditUserName, AuditHost, HttpStatusCode.NotFound },
	};

	[Theory]
	[MemberData(nameof(GroupOwnedPaths))]
	public async Task Group_owns_stock_and_generated_paths_without_unprefixed_duplicates(
		string path, string user, string host, HttpStatusCode expected)
		=> await AssertUnavailableAsync(path, true, user, host, expected);

	[Fact]
	public void Stock_page_keeps_the_groups_metadata() => AssertStockPageEndpoint();

	[Fact]
	public void Generated_component_routes_keep_one_deterministic_identity_each()
	{
		var endpoints = GetGeneratedComponentEndpoints();
		Assert.Multiple(
			() => Assert.Equal(3, endpoints.Length),
			() => Assert.Equal(
				[typeof(Issue141AuditComponent), typeof(Issue97ReportComponent), typeof(Issue97SummaryComponent)],
				endpoints.Select(endpoint => endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type)));
	}

	[Fact]
	public async Task Manual_render_tree_attributes_cannot_widen_the_explicit_get_allow_list()
	{
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Delete,
			SummaryRequestPath,
			SummaryHost,
			PackageConsumerAuthenticationHandler.AllAccessUserName,
			cookie: null,
			token: null);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-97-summary-component", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}

	[Fact]
	public async Task Put_action_uses_the_authorized_request_component_and_renders_callback_state()
	{
		var (token, cookie, getRequestId) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Put,
			PageRequestPath + "?source=from-query",
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token,
			"Htmxor.AspNetCore10.Issue362.Routing.Forged.PUT.OtherCallback");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. " +
			$"Probe: binding={applicationProbe.BindingCount}, " +
			$"initialization={applicationProbe.InitializationCount}, " +
			$"callback={applicationProbe.CallbackCount}. Body: {body}"),
			() => Assert.Contains("data-report-id=\"42\"", body, StringComparison.Ordinal),
			() => Assert.Contains("data-query-source=\"from-query\"", body, StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-user=\"{PackageConsumerAuthenticationHandler.ReportUserName}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains("data-binding-count=\"1\"", body, StringComparison.Ordinal),
			() => Assert.Contains("data-initialization-count=\"1\"", body, StringComparison.Ordinal),
			() => Assert.Contains("data-callback-count=\"1\"", body, StringComparison.Ordinal),
			() => Assert.Contains("data-lifecycle-ready=\"true\"", body, StringComparison.Ordinal),
			() => Assert.Contains(
			$"put|42|from-query|{PackageConsumerAuthenticationHandler.ReportUserName}|from-request-scope|true",
			body,
			StringComparison.Ordinal),
			() => Assert.NotEqual(getRequestId, ExtractAttribute(body, "data-issue-100-put-result", "data-request-id")),
			() => Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase),
			() => Assert.DoesNotContain("data-stock-shell", body, StringComparison.Ordinal),
			() => Assert.Equal(1, applicationProbe.BindingCount),
			() => Assert.Equal(1, applicationProbe.InitializationCount),
			() => Assert.Equal(1, applicationProbe.CallbackCount),
			() => AssertStockPageEndpoint());
	}

	[Fact]
	public async Task Delete_action_uses_the_stock_page_request_component()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Delete,
			PageRequestPath + "?source=from-query",
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. " +
			$"Probe: binding={applicationProbe.BindingCount}, " +
			$"initialization={applicationProbe.InitializationCount}, " +
			$"callback={applicationProbe.CallbackCount}. Body: {body}"),
			() => Assert.Contains(
			$"delete|42|from-query|{PackageConsumerAuthenticationHandler.ReportUserName}|from-request-scope|true",
			body,
			StringComparison.Ordinal),
			() => Assert.Equal(1, applicationProbe.BindingCount),
			() => Assert.Equal(1, applicationProbe.InitializationCount),
			() => Assert.Equal(1, applicationProbe.CallbackCount));
	}

	[Fact]
	public async Task Patch_action_uses_the_htmx_only_request_component_and_code_behind_handler()
	{
		var (token, cookie, getRequestId) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Patch,
			ReportRequestPath + "?source=from-query",
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token,
			"Htmxor.AspNetCore10.Issue362.Routing.Forged.PATCH.OtherCallback");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. " +
			$"Probe: binding={applicationProbe.BindingCount}, " +
			$"initialization={applicationProbe.InitializationCount}, " +
			$"callback={applicationProbe.CallbackCount}. Body: {body}"),
			() => Assert.Contains(
			$"patch|42|from-query|{PackageConsumerAuthenticationHandler.ReportUserName}|from-request-scope|true",
			body,
			StringComparison.Ordinal),
			() => Assert.NotEqual(getRequestId, ExtractAttribute(body, "data-issue-103-patch-result", "data-request-id")),
			() => Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase),
			() => Assert.Equal(1, applicationProbe.BindingCount),
			() => Assert.Equal(1, applicationProbe.InitializationCount),
			() => Assert.Equal(1, applicationProbe.CallbackCount));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("invalid-token")]
	public async Task Missing_or_invalid_antiforgery_rejects_before_binding_or_callback(string? token)
	{
		var (_, cookie, _) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Put,
			PageRequestPath + "?source=rejected",
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-100-put-result", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}

	[Fact]
	public async Task Unauthorized_put_rejects_before_binding_or_callback()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Put,
			PageRequestPath + "?source=rejected",
			ReportHost,
			PackageConsumerAuthenticationHandler.SummaryUserName,
			cookie,
			token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-100-put-result", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}

	[Fact]
	public async Task Different_method_cannot_invoke_the_put_callback()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Patch,
			PageRequestPath + "?source=rejected",
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token,
			PutHandlerIdentity);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-100-put-result", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}

	[Fact]
	public async Task Stock_put_cannot_invoke_the_htmx_only_patch_callback()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Put,
			ReportRequestPath,
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token,
			PatchHandlerIdentity);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-103-patch-result", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}

	[Fact]
	public async Task Client_only_hx_put_on_htmx_only_report_cannot_invoke_stock_report_callback()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync();
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Put,
			ReportRequestPath,
			ReportHost,
			PackageConsumerAuthenticationHandler.ReportUserName,
			cookie,
			token,
			PutHandlerIdentity);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-100-put-result", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}

	private async Task<(string Token, string Cookie, string RequestId)> GetAntiforgeryCredentialsAsync(
		string userName = PackageConsumerAuthenticationHandler.ReportUserName)
	{
		using var response = await SendAsync(
			PageRequestPath + "?source=from-query",
			direct: false,
			userName,
			ReportHost);
		var body = await response.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.True(response.StatusCode == HttpStatusCode.OK, body),
			() => Assert.Contains("data-stock-shell", body, StringComparison.Ordinal),
			() => Assert.Contains("data-issue-100-report-page", body, StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-page-request-values>42|from-query|{userName}</p>",
			body,
			StringComparison.Ordinal));

		return (
			ExtractAttribute(body, "name=\"__RequestVerificationToken\"", "value"),
			ExtractAntiforgeryCookie(response),
			ExtractAttribute(body, "data-issue-100-put-result", "data-request-id"));
	}

	private static HttpRequestMessage CreateActionRequest(
		HttpMethod method,
		string path,
		string host,
		string? userName,
		string? cookie,
		string? token,
		string? clientHandlerIdentity = null)
	{
		var request = new HttpRequestMessage(method, path);
		request.Headers.Host = host;
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add(HtmxRequestHeaderNames.CurrentUrl, "https://example.test/orders");
		request.Headers.Add(
			HtmxRequestHeaderNames.Target,
			DefaultTarget(path));
		if (userName is not null)
		{
			request.Headers.Add(PackageConsumerAuthenticationHandler.UserHeaderName, userName);
		}

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

	private static string DefaultTarget(string path)
		=> path.Contains("htmx-reports", StringComparison.Ordinal) ? "section#patch-result" : "section";

	private void AssertNoApplicationCodeRan()
	{
		Assert.Multiple(
			() => Assert.Equal(0, applicationProbe.BindingCount),
			() => Assert.Equal(0, applicationProbe.InitializationCount),
			() => Assert.Equal(0, applicationProbe.CallbackCount));
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

	private async Task AssertDirectResponseAsync(
		string path,
		string userName,
		string host,
		string expectedComponent,
		string expectedValues,
		params string[] otherComponents)
	{
		using var response = await SendAsync(path, direct: true, userName, host);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}"),
			() => Assert.Contains(expectedComponent, body, StringComparison.Ordinal),
			() => Assert.Contains(expectedValues, body, StringComparison.Ordinal),
			() => Assert.All(otherComponents, otherComponent =>
			Assert.DoesNotContain(otherComponent, body, StringComparison.Ordinal)),
			() => Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase),
			() => Assert.DoesNotContain("data-stock-shell", body, StringComparison.Ordinal));
	}

	private async Task AssertUnavailableAsync(
		string path,
		bool direct,
		string? userName,
		string host,
		HttpStatusCode expectedStatus)
	{
		using var response = await SendAsync(path, direct, userName, host);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(expectedStatus, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-97-report-component", body, StringComparison.Ordinal),
			() => Assert.DoesNotContain("data-issue-97-summary-component", body, StringComparison.Ordinal),
			() => Assert.DoesNotContain("data-issue-141-audit-component", body, StringComparison.Ordinal),
			() => Assert.DoesNotContain("data-issue-100-report-page", body, StringComparison.Ordinal));
	}

	private RouteEndpoint[] GetGeneratedComponentEndpoints() =>
		((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint =>
			{
				var componentType = endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type;
				return componentType == typeof(Issue141AuditComponent) ||
					componentType == typeof(Issue97ReportComponent) ||
					componentType == typeof(Issue97SummaryComponent);
			})
			.ToArray();

	private void AssertStockPageEndpoint()
	{
		var endpoint = Assert.Single(
			((IEndpointRouteBuilder)app).DataSources
				.SelectMany(dataSource => dataSource.Endpoints)
				.OfType<RouteEndpoint>(),
			endpoint =>
				endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type ==
					typeof(Issue100ReportPage) &&
				string.Equals(
					endpoint.RoutePattern.RawText,
					RoutePrefix + PageDeclaredRoute,
					StringComparison.Ordinal));
		Assert.Multiple(
			() => Assert.Equal(RoutePrefix + PageDeclaredRoute, endpoint.RoutePattern.RawText),
			() => Assert.Equal(
#if NET11_0_OR_GREATER
			[HttpMethods.Get, HttpMethods.Head, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete],
#else
			[HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete],
#endif
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods),
			() => Assert.Same(
			Issue97GroupMetadata.Instance,
			endpoint.Metadata.GetRequiredMetadata<Issue97GroupMetadata>()),
			() => Assert.Empty(endpoint.Metadata.GetOrderedMetadata<HtmxRouteAttribute>()));
		var authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
		Assert.Multiple(
			() => Assert.Contains(authorization, data => data.Policy == ReportPolicyName),
			() => Assert.Contains(authorization, data => data.Policy == GroupPolicyName),
			() => Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation));
	}

	private static RouteEndpoint AssertGeneratedEndpoint<TComponent>(
		IEnumerable<RouteEndpoint> endpoints,
		string declaredRoute,
		string policyName,
		IReadOnlyList<string> expectedMethods,
		IReadOnlyList<string>? declaredMethods = null,
		string? expectedCurrentUrl = null,
		string? expectedTarget = null,
		IReadOnlyList<string>? expectedTargets = null)
	{
		var endpoint = Assert.Single(endpoints, endpoint =>
			endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type == typeof(TComponent));
		Assert.Multiple(
			() => Assert.Equal(RoutePrefix + declaredRoute, endpoint.RoutePattern.RawText),
			() => Assert.Equal(
			expectedMethods,
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods),
			() => Assert.Same(
			Issue97GroupMetadata.Instance,
			endpoint.Metadata.GetRequiredMetadata<Issue97GroupMetadata>()));
		var route = Assert.Single(endpoint.Metadata.GetOrderedMetadata<HtmxRouteAttribute>());
		Assert.Multiple(
			() => Assert.Equal(declaredRoute, route.Template),
			() => Assert.Equal(declaredMethods ?? [HttpMethods.Get], route.Methods),
			() => Assert.Equal(expectedCurrentUrl, route.CurrentUrl),
			() => Assert.Equal(expectedTarget, route.Target),
			() => Assert.Equal(expectedTargets ?? [], route.Targets));
		var authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
		Assert.Multiple(
			() => Assert.Contains(authorization, data => data.Policy == policyName),
			() => Assert.Contains(authorization, data => data.Policy == GroupPolicyName));

		return endpoint;
	}

	private static void AssertHost(RouteEndpoint endpoint, string host)
	{
		var hostMetadata = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IHostMetadata>());
		Assert.Equal([host], hostMetadata.Hosts);
	}

	private async Task<HttpResponseMessage> SendAsync(
		string path,
		bool direct,
		string? userName,
		string host,
		string currentUrl = "https://example.test/orders",
		string? target = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Host = host;
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
			request.Headers.Add(HtmxRequestHeaderNames.CurrentUrl, currentUrl);
			request.Headers.Add(
				HtmxRequestHeaderNames.Target,
				target ?? DefaultTarget(path));
		}

		if (userName is not null)
		{
			request.Headers.Add(PackageConsumerAuthenticationHandler.UserHeaderName, userName);
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

internal sealed record Issue97GroupMetadata(string Value)
{
	public static Issue97GroupMetadata Instance { get; } = new("group-convention-preserved");
}

internal sealed class PackageConsumerAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "issue-97-test";
	public const string UserHeaderName = "X-Issue-97-User";
	public const string AccessClaim = "issue-97-access";
	public const string GroupAccessClaim = "issue-145-group-access";
	public const string ReportAccess = "report";
	public const string SummaryAccess = "summary";
	public const string AuditAccess = "audit";
	public const string ReportUserName = "issue-97-report-user";
	public const string ReportWithoutGroupUserName = "issue-145-report-without-group-user";
	public const string SummaryUserName = "issue-97-summary-user";
	public const string AuditUserName = "issue-141-audit-user";
	public const string AllAccessUserName = "issue-103-all-access-user";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue(UserHeaderName, out var userName))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var user = userName.ToString();
		var identity = new ClaimsIdentity(CreateClaims(user), SchemeName);
		var principal = new ClaimsPrincipal(identity);

		return Task.FromResult(AuthenticateResult.Success(new(principal, SchemeName)));
	}

	private static Claim[] CreateClaims(string userName) =>
		userName switch
		{
			ReportUserName =>
			[
				new Claim(ClaimTypes.Name, userName),
				new Claim(AccessClaim, ReportAccess),
				new Claim(GroupAccessClaim, "allowed"),
			],
			ReportWithoutGroupUserName =>
			[
				new Claim(ClaimTypes.Name, userName),
				new Claim(AccessClaim, ReportAccess),
			],
			SummaryUserName =>
			[
				new Claim(ClaimTypes.Name, userName),
				new Claim(AccessClaim, SummaryAccess),
				new Claim(GroupAccessClaim, "allowed"),
			],
			AuditUserName =>
			[
				new Claim(ClaimTypes.Name, userName),
				new Claim(AccessClaim, AuditAccess),
				new Claim(GroupAccessClaim, "allowed"),
			],
			AllAccessUserName =>
			[
				new Claim(ClaimTypes.Name, userName),
				new Claim(AccessClaim, ReportAccess),
				new Claim(AccessClaim, SummaryAccess),
				new Claim(AccessClaim, AuditAccess),
				new Claim(GroupAccessClaim, "allowed"),
			],
			_ => [new Claim(ClaimTypes.Name, userName)],
		};
}

internal sealed class Issue100ApplicationProbe
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

internal sealed class Issue100RequestProbe(Issue100ApplicationProbe applicationProbe)
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
