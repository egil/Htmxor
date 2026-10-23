using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

public sealed class Issue292FilterRejectionLoggingTests : IAsyncLifetime
{
	private readonly Issue292LogCapture logs = new();
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue292FilterRejectionLoggingTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
		builder.Logging.AddProvider(logs);
		builder.Services.AddRazorComponents().AddHtmxor();
		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>().AddHtmxorEndpoints();
		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Theory]
	[InlineData("current-url", "CurrentUrl", "/source", null, null, "absent")]
	[InlineData("current-url", "CurrentUrl", "/source", "https://localhost/different", null, "different")]
	[InlineData("target", "Target", "div#result", null, null, "absent")]
	[InlineData("target", "Target", "div#result", null, "div#different", "different")]
	[InlineData("targets", "Targets", "div#result, section#secondary", null, null, "absent")]
	[InlineData("targets", "Targets", "div#result, section#secondary", null, "div#different", "different")]
	public async Task Rejected_htmx_only_request_keeps_404_and_logs_one_debug_reason(
		string route, string filter, string declaredValue, string? currentUrl, string? target, string state)
	{
		using var response = await SendAsync(route, currentUrl, target);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		AssertRejection(Assert.Single(logs.Entries), ComponentFor(route), filter, declaredValue, state);
	}

	[Fact]
	public async Task Rejected_dual_route_logs_once_and_keeps_stock_fallback()
	{
		using var response = await SendAsync("dual", target: "div#different");
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("issue-292-dual-stock", body, StringComparison.Ordinal);
		AssertRejection(Assert.Single(logs.Entries), typeof(Issue292DualRoute), "Target", "div#result", "different");
	}

	[Theory]
	[InlineData(null, null, "CurrentUrl", "/source", "absent")]
	[InlineData("https://localhost/different", "div#different", "CurrentUrl", "/source", "different")]
	[InlineData("https://localhost/source", "div#different", "Target", "div#result", "different")]
	public async Task Multiple_failed_filters_log_only_the_first_rejecting_filter(
		string? currentUrl, string? target, string filter, string declaredValue, string state)
	{
		using var response = await SendAsync("multiple", currentUrl, target);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		AssertRejection(Assert.Single(logs.Entries), typeof(Issue292MultipleFilters), filter, declaredValue, state);
	}

	[Fact]
	public async Task Two_rejected_candidates_each_log_their_own_declared_filter()
	{
		using var response = await SendAsync("candidates", target: "aside#different");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal(2, logs.Entries.Length);
		Assert.Collection(logs.Entries.OrderBy(entry => entry.Fields["declaredValue"]?.ToString(), StringComparer.Ordinal),
			entry => AssertRejection(entry, typeof(Issue292Candidates), "Target", "div#first", "different"),
			entry => AssertRejection(entry, typeof(Issue292OtherCandidate), "Target", "section#second", "different"));
	}

	[Theory]
	[InlineData("current-url", "https://localhost/source", null)]
	[InlineData("target", null, "DIV#result")]
	[InlineData("targets", null, "SECTION#secondary")]
	[InlineData("multiple", "https://localhost/source", "div#result")]
	[InlineData("dual", null, "div#result")]
	public async Task Accepted_direct_requests_emit_no_rejection_event(string route, string? currentUrl, string? target)
	{
		using var response = await SendAsync(route, currentUrl, target);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains($"<p>issue-292-{route}</p>", body, StringComparison.Ordinal);
		Assert.DoesNotContain("data-stock-shell", body, StringComparison.Ordinal);
		Assert.Empty(logs.Entries);
	}

	[Theory]
	[InlineData("target", HttpStatusCode.NotFound)]
	[InlineData("dual", HttpStatusCode.OK)]
	public async Task Ordinary_requests_emit_no_filter_rejection_event(string route, HttpStatusCode status)
	{
		using var response = await client.GetAsync("/issue-292/" + route);

		Assert.Equal(status, response.StatusCode);
		Assert.Empty(logs.Entries);
	}

	[Fact]
	public async Task Registered_matcher_does_not_log_for_an_already_invalid_candidate()
	{
		var endpoint = Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints),
			candidate => candidate.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue292Target));
		var candidates = new CandidateSet([endpoint], [new RouteValueDictionary()], [0]);
		candidates.SetValidity(0, false);
		using var scope = app.Services.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Request.Headers["HX-Request"] = "true";
		context.Request.Headers["HX-Request-Type"] = "partial";
		var policy = Assert.Single(app.Services.GetServices<MatcherPolicy>(),
			candidate => candidate.GetType().FullName == Issue292LogCapture.Category);

		await Assert.IsAssignableFrom<IEndpointSelectorPolicy>(policy).ApplyAsync(context, candidates);

		Assert.False(candidates.IsValidCandidate(0));
		Assert.Empty(logs.Entries);
	}

	[Theory]
	[InlineData("current-url", "HX-Current-URL", "https://localhost/", false, "absent")]
	[InlineData("current-url", "HX-Current-URL", "https://localhost/", true, "different")]
	[InlineData("target", "HX-Target", "div#", false, "absent")]
	[InlineData("target", "HX-Target", "div#", true, "different")]
	[InlineData("targets", "HX-Target", "div#", false, "absent")]
	[InlineData("targets", "HX-Target", "div#", true, "different")]
	public async Task Untrusted_header_payload_never_enters_rejection_message_or_structured_fields(
		string route, string header, string prefix, bool longValue, string state)
	{
		const string secret = "private-request-secret";
		var payload = longValue ? secret + new string('x', 8192) : secret + "\r\nforged-log-entry";
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-292/" + route);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		Assert.True(request.Headers.TryAddWithoutValidation(header, prefix + payload));

		using var response = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		var entry = Assert.Single(logs.Entries);
		Assert.Equal(state, entry.Fields["requestValueState"]);
		Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
		Assert.All(entry.Fields, field => Assert.DoesNotContain(secret, field.Value?.ToString() ?? "", StringComparison.Ordinal));
		Assert.Null(entry.Exception);
	}

	private static void AssertRejection(Issue292LogCapture.Entry entry, Type component, string filter, string declaredValue, string state)
	{
		Assert.Equal(LogLevel.Debug, entry.Level);
		Assert.Equal(1, entry.EventId.Id);
		Assert.Equal("RepresentationFilterRejected", entry.EventId.Name);
		Assert.Equal(component.FullName, entry.Fields["componentType"]?.ToString());
		Assert.Equal(filter, entry.Fields["filter"]);
		Assert.Equal(declaredValue, entry.Fields["declaredValue"]);
		Assert.Equal(state, entry.Fields["requestValueState"]);
	}

	private static Type ComponentFor(string route) => route switch
	{
		"current-url" => typeof(Issue292CurrentUrl),
		"target" => typeof(Issue292Target),
		"targets" => typeof(Issue292Targets),
		_ => throw new ArgumentOutOfRangeException(nameof(route)),
	};

	private async Task<HttpResponseMessage> SendAsync(string route, string? currentUrl = null, string? target = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-292/" + route);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		if (currentUrl is not null)
		{
			request.Headers.TryAddWithoutValidation("HX-Current-URL", currentUrl);
		}
		if (target is not null)
		{
			request.Headers.TryAddWithoutValidation("HX-Target", target);
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
