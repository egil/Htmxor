#if NET11_0_OR_GREATER
using System.Net;
using Htmxor.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

// A CacheView living beneath an interactive render-mode boundary (not the reverse, which the sibling streaming
// case already covers) is covered here on its own, because it needs a genuinely interactive-server-capable host
// rather than the plain static host the other Issue219 fixtures share.
public sealed class Issue219CacheInteractiveTests
{
	[Fact]
	public async Task A_cache_view_beneath_an_interactive_render_mode_boundary_stores_nothing()
	{
		await using var app = await StartAsync();
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		var first = await ReadAsync(client);
		Assert.Contains("data-version=\"1\"", first, StringComparison.Ordinal);

		data.Version = 2;
		var second = await ReadAsync(client);

		// A stored entry would still read version 1 here. The boundary living beneath a render-mode component
		// must abandon its capture rather than key prerendered interactive content as weakly as an ordinary one.
		Assert.Contains("data-version=\"2\"", second, StringComparison.Ordinal);
	}

	private static async Task<string> ReadAsync(HttpClient client)
	{
		using var response = await client.GetAsync("/issue-219/interactive");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<WebApplication> StartAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue219InteractiveCachePage).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue219Data>();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		var components = builder.Services.AddRazorComponents().AddInteractiveServerComponents();
		components.AddHtmxor();

		var app = builder.Build();
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue219InteractiveCachePage>();
		endpoints.AddInteractiveServerRenderMode();
		endpoints.AddHtmxorEndpoints();

		await app.StartAsync();
		return app;
	}
}

[Route("/issue-219/interactive")]
public sealed class Issue219InteractiveCachePage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<Issue219InteractiveBoundary>(0);
		builder.AddComponentRenderMode(RenderMode.InteractiveServer);
		builder.CloseComponent();
	}
}

public sealed class Issue219InteractiveBoundary : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-interactive");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219CachedContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
