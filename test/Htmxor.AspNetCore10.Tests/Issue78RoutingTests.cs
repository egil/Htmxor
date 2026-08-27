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
		builder.Services.AddRazorComponents().AddHtmx();
		builder.Services.AddScoped(_ => new Issue81RequestProbe("from-di"));

		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.WithMetadata(RouteSentinelMetadata.Instance)
			.AddHtmxorComponentEndpoints(app);

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
		using var htmxResponse = await client.SendAsync(htmxRequest);
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, htmxResponse.StatusCode);
		Assert.Contains("data-issue-78-page", htmxBody, StringComparison.Ordinal);
		Assert.Contains("data-route-metadata=\"preserved\"", htmxBody, StringComparison.Ordinal);
		Assert.DoesNotContain("<html", htmxBody, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", htmxBody, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Bound_stock_page_normal_and_htmx_gets_receive_the_same_request_values_once()
	{
		var pageEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue81Page));
		Assert.Single(pageEndpoints);

		using var normalResponse = await client.GetAsync("/issue-81/42?query=from-query");
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, normalResponse.StatusCode);
		Assert.Contains("<html", normalBody, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-issue-81-values>42|from-query|from-di|1</p>", normalBody, StringComparison.Ordinal);

		using var htmxRequest = new HttpRequestMessage(HttpMethod.Get, "/issue-81/42?query=from-query");
		htmxRequest.Headers.Add("HX-Request", "true");
		using var htmxResponse = await client.SendAsync(htmxRequest);
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, htmxResponse.StatusCode);
		Assert.Contains("data-issue-81-values>42|from-query|from-di|1</p>", htmxBody, StringComparison.Ordinal);
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
