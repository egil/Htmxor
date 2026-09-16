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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

// Both nestings of a CacheView and an interactive render-mode boundary are covered here, because both need a
// genuinely interactive-server-capable host rather than the plain static host the other Issue219 fixtures
// share. The boundary holding one is the only composition that reaches the discard a capture performs over
// content it must not store, now that the request-varying kinds are down to this one.
public sealed class Issue219CacheInteractiveTests
{
	[Fact]
	public async Task Content_beneath_an_interactive_boundary_is_validated_like_stock()
	{
		await using var stock = await StartAsync<Issue219InteractiveAuthInsidePage>(htmxor: false);
		await using var candidate = await StartAsync<Issue219InteractiveAuthInsidePage>(htmxor: true);

		// Stock pauses its capture at a render-mode boundary -- SSRRenderModeBoundary carries
		// CacheBehavior.Rerender, so IsCacheableComponent answers false for it -- and therefore never validates
		// the prerendered subtree below. Htmxor's boundary carries no such attribute, so leaving the kind to
		// fall through to the ordinary cacheable test validates content stock never looks at, and an
		// AuthorizeView beneath an interactive boundary is refused where stock renders the page.
		var expected = await ReadAuthorizedAsync(stock, "/issue-219/interactive-auth-inside");
		var actual = await ReadAuthorizedAsync(candidate, "/issue-219/interactive-auth-inside");

		Assert.Contains("authorized: alice", expected, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task A_boundary_beneath_an_interactive_boundary_inside_a_boundary_renders_like_stock()
	{
		await using var stock = await StartAsync<Issue219InteractiveNestedBoundaryPage>(htmxor: false);
		await using var candidate = await StartAsync<Issue219InteractiveNestedBoundaryPage>(htmxor: true);

		// The same property observed through the nesting refusal rather than the descendant one: stock's pause
		// means the inner boundary is not reached by a capturing writer, so it renders; validating instead
		// raises "cannot be nested inside another CacheView" where stock renders the page.
		var expected = await ReadAuthorizedAsync(stock, "/issue-219/interactive-nested-boundary");
		var actual = await ReadAuthorizedAsync(candidate, "/issue-219/interactive-nested-boundary");

		Assert.Contains("data-version=\"1\"", expected, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	private static async Task<string> ReadAuthorizedAsync(WebApplication app, string path)
	{
		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("X-Issue-219-User", "alice");
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadAsStringAsync();
		return System.Text.RegularExpressions.Regex.Replace(body, "<!--.*?-->", "");
	}

	[Fact]
	public async Task A_boundary_holding_an_interactive_render_mode_boundary_stores_nothing()
	{
		await using var app = await StartAsync<Issue219InteractiveInsidePage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		var first = await ReadAsync(client, "/issue-219/interactive-inside");
		Assert.Contains("data-version=\"1\"", first, StringComparison.Ordinal);

		data.Version = 2;

		// The reverse of the nesting the case above covers, and the only composition that reaches the discard
		// and the pause a capture performs over a render-mode boundary it *holds*. A stored entry would freeze
		// prerendered interactive markup and a component id with it, so the boundary keeps nothing and the
		// content beside it renders afresh too.
		Assert.Contains("data-version=\"2\"", await ReadAsync(client, "/issue-219/interactive-inside"), StringComparison.Ordinal);
	}

	private static async Task<string> ReadAsync(HttpClient client, string path)
	{
		using var response = await client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

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

	[Fact]
	public async Task A_position_whose_render_mode_ancestor_changes_never_replays_the_other_states_body()
	{
		var stock = await MeasureAsync(htmxor: false);
		var candidate = await MeasureAsync(htmxor: true);

		// Stock's own behavior, asserted to pin it rather than to guard Htmxor: no Htmxor code runs in this
		// host, so these two lines cannot detect any change under src/ -- measured, across every inversion run
		// against this suite. They fail only if ASP.NET Core changes how it keys a CacheView, which is worth
		// failing loudly because the contrast below is only meaningful in light of it. Nothing here couples the
		// two hosts; both pairs are absolute. Stock replays in both orderings.
		Assert.Equal(("1", "1"), stock.StaticFirst);
		Assert.Equal(("1", "1"), stock.BoundaryFirst);

		// Htmxor keys the two states apart, so neither ordering is served the other's body.
		//
		// Only the first of these detects that alone: remove the discriminator and this position is handed the
		// body stored before it had a render-mode ancestor. The second states the other half of the property
		// but cannot report it on its own -- with the discriminator present it stores under one key and reads
		// under another, so it misses whether or not the capture was abandoned, and in the ordering below the
		// line above fails first in the only case that would redden it. Whether the boundary stores anything
		// while beneath the ancestor is owned by
		// A_cache_view_beneath_an_interactive_render_mode_boundary_stores_nothing, which the discard reddens.
		Assert.Equal(("1", "2"), candidate.StaticFirst);
		Assert.Equal(("1", "2"), candidate.BoundaryFirst);
	}

	private static async Task<((string, string) StaticFirst, (string, string) BoundaryFirst)> MeasureAsync(bool htmxor)
	{
		return (await OrderAsync(htmxor, "static-first", boundaryFirst: false),
			await OrderAsync(htmxor, "boundary-first", boundaryFirst: true));
	}

	// A fresh host per ordering: one cache store must not carry an entry into the other measurement.
	private static async Task<(string, string)> OrderAsync(bool htmxor, string key, bool boundaryFirst)
	{
		await using var app = await StartAsync<Issue219ConditionalModePage>(htmxor);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		var first = await ReadVersionAsync(client, key, boundaryFirst);
		data.Version = 2;
		var second = await ReadVersionAsync(client, key, !boundaryFirst);
		return (first, second);
	}

	private static async Task<string> ReadVersionAsync(HttpClient client, string key, bool interactive)
	{
		var suffix = interactive ? "&interactive=1" : "";
		using var response = await client.GetAsync($"/issue-219/conditional-mode?key={key}{suffix}");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		const string marker = "data-version=\"";
		var start = html.IndexOf(marker, StringComparison.Ordinal);
		Assert.True(start >= 0, "the cached content did not reach the response");
		start += marker.Length;
		var end = html.IndexOf('"', start);
		Assert.True(end > start, "the cached content carried no version");
		return html[start..end];
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

	internal static async Task<WebApplication> StartAsync<TRoot>(bool htmxor) where TRoot : IComponent
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

// One page whose render mode is decided per request, so the same CacheView position is sometimes beneath a
// render-mode boundary and sometimes not. The key is taken from the query so each ordering starts clean.
[Route("/issue-219/conditional-mode")]
public sealed class Issue219ConditionalModePage : ComponentBase
{
	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<Issue219ConditionalModeBoundary>(0);
		builder.AddAttribute(1, nameof(Issue219ConditionalModeBoundary.CacheKey), HttpContext.Request.Query["key"].ToString());
		if (HttpContext.Request.Query.ContainsKey("interactive"))
		{
			builder.AddComponentRenderMode(RenderMode.InteractiveServer);
		}

		builder.CloseComponent();
	}
}

public sealed class Issue219ConditionalModeBoundary : ComponentBase
{
	[Parameter] public string CacheKey { get; set; } = "";

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), CacheKey);
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
// A CacheView *holding* an interactive render-mode boundary, rather than standing beneath one.
[Route("/issue-219/interactive-inside")]
public sealed class Issue219InteractiveInsidePage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-interactive-inside");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219CachedContent>(0);
			cached.CloseComponent();
			cached.OpenComponent<Issue219InteractiveInsideContent>(1);
			cached.AddComponentRenderMode(RenderMode.InteractiveServer);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219InteractiveInsideContent : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		builder.AddAttribute(1, "data-interactive", "true");
		builder.CloseElement();
	}
}
// An AuthorizeView beneath an interactive render-mode boundary that a CacheView holds.
[Route("/issue-219/interactive-auth-inside")]
public sealed class Issue219InteractiveAuthInsidePage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-interactive-auth-inside");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219InteractiveAuthInner>(0);
			cached.AddComponentRenderMode(RenderMode.InteractiveServer);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219InteractiveAuthInner : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<Microsoft.AspNetCore.Components.Authorization.AuthorizeView>(0);
		builder.AddAttribute(1, "Authorized", (RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>)(state => a =>
		{
			a.OpenElement(0, "p");
			a.AddContent(1, $"authorized: {state.User.Identity?.Name}");
			a.CloseElement();
		}));
		builder.CloseComponent();
	}
}

// A CacheView beneath an interactive render-mode boundary that another CacheView holds.
[Route("/issue-219/interactive-nested-boundary")]
public sealed class Issue219InteractiveNestedBoundaryPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-interactive-nested-outer");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219InteractiveNestedInner>(0);
			cached.AddComponentRenderMode(RenderMode.InteractiveServer);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219InteractiveNestedInner : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-interactive-nested-inner");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(deep =>
		{
			deep.OpenComponent<Issue219CachedContent>(0);
			deep.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
