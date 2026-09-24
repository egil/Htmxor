using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

public sealed class Issue246ComponentEndpointMetadataTests
{
	[Fact]
	public async Task Building_a_cors_host_does_not_change_another_hosts_component_endpoint_metadata()
	{
		await using var plain = await BuildPlainHostAsync();
		var plainMetadata = GetLastHttpMethodMetadata(plain);

		// The suite's only CORS-bearing host, built exactly as the real tests build it.
		await using var cors = await Issue210SecurityHost.StartAsync(cors: "trusted");

		Assert.False(
			plainMetadata.AcceptCorsPreflight,
			"Expected the plain host's component endpoint to still not accept CORS preflight after building a CORS host.");
	}

	private static async Task<WebApplication> BuildPlainHostAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue246ComponentEndpointMetadataTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddRazorComponents();

		var app = builder.Build();
		app.MapRazorComponents<Issue78App>();

		await app.StartAsync();
		return app;
	}

	private static HttpMethodMetadata GetLastHttpMethodMetadata(WebApplication app)
	{
		// Issue83ProtectedPage compiles for both target frameworks this project builds; Issue210SecurityHost
		// also maps it, so its endpoint exists on every host this test builds.
		var endpoint = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Single(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue83ProtectedPage));
		return Assert.IsType<HttpMethodMetadata>(endpoint.Metadata.OfType<IHttpMethodMetadata>().Last());
	}
}
