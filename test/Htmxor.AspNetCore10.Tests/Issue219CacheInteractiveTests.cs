#if NET11_0_OR_GREATER
using System.Net;
using System.Security.Claims;
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
		await using var app = await StartAsync<Issue219InteractiveCachePage>(htmxor: true);
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

	[Fact]
	public async Task A_cache_view_beneath_an_interactive_render_mode_boundary_still_refuses_content_stock_refuses()
	{
		await using var stock = await StartAsync<Issue219InteractiveAuthPage>(htmxor: false);
		await using var candidate = await StartAsync<Issue219InteractiveAuthPage>(htmxor: true);

		// Beginning and discarding the capture, instead of returning before it starts, is what makes stock's
		// descendant refusal guard run over what the boundary holds. Nothing here is ever stored either way.
		// Under interactive rendering the exception surfaces while the TestServer response body is copied, so it
		// arrives wrapped rather than thrown directly from SendAsync; GetBaseException unwraps to the guard's own
		// InvalidOperationException for both hosts identically.
		var expected = await ReadRootCauseAsync(stock);
		var actual = await ReadRootCauseAsync(candidate);

		Assert.Contains("cannot be used inside a CacheView", expected.Message, StringComparison.Ordinal);
		Assert.Equal(expected.Message, actual.Message);
	}

	private static async Task<string> ReadAsync(HttpClient client)
	{
		using var response = await client.GetAsync("/issue-219/interactive");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<InvalidOperationException> ReadRootCauseAsync(WebApplication app)
	{
		var thrown = await Assert.ThrowsAnyAsync<Exception>(() => ReadAsAuthenticatedAsync(app));
		return Assert.IsType<InvalidOperationException>(thrown.GetBaseException());
	}

	private static async Task<string> ReadAsAuthenticatedAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/interactive-auth");
		request.Headers.Add("X-Issue-219-User", "alice");
		using var response = await client.SendAsync(request);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<WebApplication> StartAsync<TRoot>(bool htmxor) where TRoot : IComponent
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(TRoot).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue219Data>();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddAuthorization();
		builder.Services.AddCascadingAuthenticationState();
		var components = builder.Services.AddRazorComponents().AddInteractiveServerComponents();
		if (htmxor)
		{
			components.AddHtmxor();
		}

		var app = builder.Build();
		app.Use(async (context, next) =>
		{
			if (context.Request.Headers.TryGetValue("X-Issue-219-User", out var user))
			{
				context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, user.ToString())], "issue-219"));
			}

			await next(context);
		});
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<TRoot>();
		endpoints.AddInteractiveServerRenderMode();
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

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

[Route("/issue-219/interactive-auth")]
public sealed class Issue219InteractiveAuthPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<Issue219InteractiveAuthBoundary>(0);
		builder.AddComponentRenderMode(RenderMode.InteractiveServer);
		builder.CloseComponent();
	}
}

// Reuses the non-varying CacheView-around-AuthorizeView shape from Issue219CacheSafetyTests, now beneath an
// interactive render-mode boundary instead of a plain static root.
public sealed class Issue219InteractiveAuthBoundary : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder) => Issue219Auth.Build(builder, varyByUser: false);
}
#endif
