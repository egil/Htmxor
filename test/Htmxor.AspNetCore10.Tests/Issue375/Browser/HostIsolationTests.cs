using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Htmxor.AspNetCore10.Issue375.Browser;

public sealed class HostIsolationTests
{
	[Fact]
	public async Task Protection_host_keeps_CORS_metadata_local_to_its_endpoints()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue56App).Assembly.GetName().Name,
		});
		builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		builder.Services.AddRazorComponents();
		await using var plain = builder.Build();
		plain.MapRazorComponents<Issue56App>();
		await plain.StartAsync();
		var metadata = ComponentMethods(plain);

		await using var protection = Issue211Host.Create(new Issue211Probe(true));
		await protection.StartAsync();
		_ = ComponentMethods(protection);

		Assert.False(metadata.AcceptCorsPreflight);
	}

	private static IHttpMethodMetadata ComponentMethods(WebApplication app) =>
		((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
			.Single(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue83ProtectedPage))
			.Metadata.OfType<IHttpMethodMetadata>().Last();
}
