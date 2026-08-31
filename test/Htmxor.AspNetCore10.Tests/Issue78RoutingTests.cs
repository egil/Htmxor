using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Htmxor.AspNetCore10;

public sealed class Issue78RoutingTests : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue78RoutingTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddScoped(_ => new Issue81RequestProbe("from-di"));

		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.WithMetadata(RouteSentinelMetadata.Instance)
			.AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task StockPage_normal_and_htmx_gets_use_one_component_route()
	{
		var pageEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue78Page));
		Assert.Single(pageEndpoints);

		using var normalResponse = await client.GetAsync("/issue-78");
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, normalResponse.StatusCode);
		Assert.Contains("<html", normalBody, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-issue-78-page", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-route-metadata=\"preserved\"", normalBody, StringComparison.Ordinal);

		using var htmxRequest = new HttpRequestMessage(HttpMethod.Get, "/issue-78");
		htmxRequest.Headers.Add("HX-Request", "true");
		htmxRequest.Headers.Add("HX-Request-Type", "partial");
		using var htmxResponse = await client.SendAsync(htmxRequest);
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, htmxResponse.StatusCode);
		Assert.Contains("data-issue-78-page", htmxBody, StringComparison.Ordinal);
		Assert.Contains("data-route-metadata=\"preserved\"", htmxBody, StringComparison.Ordinal);
		Assert.DoesNotContain("<html", htmxBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", htmxBody, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("/issue-81/bool/{BoolValue:bool}", "/issue-81/bool/true", "System.Boolean", "True")]
	[InlineData("/issue-81/datetime/{DateTimeValue:datetime}", "/issue-81/datetime/2026-08-27", "System.DateTime", "2026-08-27T00:00:00.0000000")]
	[InlineData("/issue-81/decimal/{DecimalValue:decimal}", "/issue-81/decimal/123.45", "System.Decimal", "123.45")]
	[InlineData("/issue-81/double/{DoubleValue:double}", "/issue-81/double/123.5", "System.Double", "123.5")]
	[InlineData("/issue-81/float/{FloatValue:float}", "/issue-81/float/123.5", "System.Single", "123.5")]
	[InlineData("/issue-81/guid/{GuidValue:guid}", "/issue-81/guid/01234567-89ab-cdef-0123-456789abcdef", "System.Guid", "01234567-89ab-cdef-0123-456789abcdef")]
	[InlineData("/issue-81/int/{IntValue:int}", "/issue-81/int/-42", "System.Int32", "-42")]
	[InlineData("/issue-81/long/{LongValue:long}", "/issue-81/long/9223372036854775806", "System.Int64", "9223372036854775806")]
	[InlineData("/issue-81/nonfile/{NonFileValue:nonfile}", "/issue-81/nonfile/notes", "System.String", "notes")]
	public async Task Stock_route_constraint_normal_and_htmx_gets_bind_the_same_value_once(
		string routeTemplate,
		string requestPath,
		string clrType,
		string expectedValue)
	{
		AssertSingleComponentRoute(routeTemplate);
		await AssertSuccessfulNormalAndHtmxResponses(
			requestPath,
			$"data-route-type=\"{clrType}\">{expectedValue}</span>");
	}

	[Theory]
	[InlineData("/issue-81/bool/{BoolValue:bool}", "/issue-81/bool/not-bool")]
	[InlineData("/issue-81/datetime/{DateTimeValue:datetime}", "/issue-81/datetime/not-a-date")]
	[InlineData("/issue-81/decimal/{DecimalValue:decimal}", "/issue-81/decimal/not-decimal")]
	[InlineData("/issue-81/double/{DoubleValue:double}", "/issue-81/double/not-double")]
	[InlineData("/issue-81/float/{FloatValue:float}", "/issue-81/float/not-float")]
	[InlineData("/issue-81/guid/{GuidValue:guid}", "/issue-81/guid/not-a-guid")]
	[InlineData("/issue-81/int/{IntValue:int}", "/issue-81/int/not-an-int")]
	[InlineData("/issue-81/long/{LongValue:long}", "/issue-81/long/not-a-long")]
	[InlineData("/issue-81/nonfile/{NonFileValue:nonfile}", "/issue-81/nonfile/document.txt")]
	public async Task Stock_route_constraint_normal_and_htmx_gets_reject_the_same_value(
		string routeTemplate,
		string requestPath)
	{
		AssertSingleComponentRoute(routeTemplate);

		using var normalResponse = await client.GetAsync(requestPath);
		using var htmxRequest = new HttpRequestMessage(HttpMethod.Get, requestPath);
		htmxRequest.Headers.Add("HX-Request", "true");
		htmxRequest.Headers.Add("HX-Request-Type", "partial");
		using var htmxResponse = await client.SendAsync(htmxRequest);

		Assert.Equal(HttpStatusCode.NotFound, normalResponse.StatusCode);
		Assert.Equal(normalResponse.StatusCode, htmxResponse.StatusCode);
	}

	[Theory]
	[InlineData("/issue-81/optional", "absent", "absent")]
	[InlineData("/issue-81/optional/-42", "System.Int32", "-42")]
	public async Task Optional_typed_route_value_normal_and_htmx_gets_match(
		string requestPath,
		string clrType,
		string expectedValue)
	{
		AssertSingleComponentRoute("/issue-81/optional/{OptionalIntValue:int?}");
		await AssertSuccessfulNormalAndHtmxResponses(
			requestPath,
			$"data-optional-route-type=\"{clrType}\">{expectedValue}</span>");
	}

	private void AssertSingleComponentRoute(string routeTemplate)
	{
		var pageEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue81Page));
		Assert.Single(pageEndpoints.Where(endpoint =>
			string.Equals(endpoint.RoutePattern.RawText, routeTemplate, StringComparison.OrdinalIgnoreCase)));
	}

	private async Task AssertSuccessfulNormalAndHtmxResponses(string requestPath, string expectedRouteValue)
	{
		using var normalResponse = await client.GetAsync($"{requestPath}?query=from-query");
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, normalResponse.StatusCode);
		Assert.Contains("<html", normalBody, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", normalBody, StringComparison.Ordinal);
		Assert.Contains(expectedRouteValue, normalBody, StringComparison.Ordinal);
		Assert.Contains("data-request-values>from-query|from-di|1</span>", normalBody, StringComparison.Ordinal);

		using var htmxRequest = new HttpRequestMessage(HttpMethod.Get, $"{requestPath}?query=from-query");
		htmxRequest.Headers.Add("HX-Request", "true");
		htmxRequest.Headers.Add("HX-Request-Type", "partial");
		using var htmxResponse = await client.SendAsync(htmxRequest);
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, htmxResponse.StatusCode);
		Assert.Contains(expectedRouteValue, htmxBody, StringComparison.Ordinal);
		Assert.Contains("data-request-values>from-query|from-di|1</span>", htmxBody, StringComparison.Ordinal);
		Assert.DoesNotContain("<html", htmxBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", htmxBody, StringComparison.Ordinal);
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

internal sealed record RouteSentinelMetadata(string Value)
{
	public static RouteSentinelMetadata Instance { get; } = new("preserved");
}

internal sealed class Issue81RequestProbe(string value)
{
	private int initializationCount;

	public string Value { get; } = value;

	public int RecordInitialization() => ++initializationCount;
}
