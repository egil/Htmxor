#if NET11_0_OR_GREATER
using System.Net;
using Htmxor;
using Htmxor.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

public sealed class Issue219CacheSafetyTests
{
	[Fact]
	public async Task Named_fragment_inside_a_cached_subtree_stops_the_boundary_storing_anything()
	{
		await using var app = await StartAsync();
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// Two htmx requests to the same URL at the same representation: both omit HX-Request-Type, so both are
		// RoutingMode.Standard and would share one cache entry. The page's SelectFragment call is inert at that
		// routing mode, which is deliberate — a fragment-selecting request writes straight from the selected
		// component's own live render tree and never revisits its CacheView ancestor, so it cannot exercise the
		// boundary's store-or-abandon decision at all.
		var first = await ReadAsync(client);
		Assert.Equal(HttpStatusCode.OK, first.Status);
		Assert.Contains("data-version=\"1\"", first.Body, StringComparison.Ordinal);

		data.Version = 2;
		var second = await ReadAsync(client);

		// A stored entry would still read version 1 here. The boundary must instead have abandoned its capture on
		// both requests, because the fragment it holds is registered only when its component is constructed,
		// which serving stored output never does. That is what this case proves: not that a hit occurred, but
		// that the boundary never stores an entry for the fragment to be replayed from.
		Assert.Equal(HttpStatusCode.OK, second.Status);
		Assert.Contains("data-inner", second.Body, StringComparison.Ordinal);
		Assert.Contains("data-version=\"2\"", second.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Unnamed_fragment_inside_a_cached_subtree_still_lets_the_boundary_cache()
	{
		await using var app = await StartAsync<Issue219UnnamedFragmentPage>(true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// Same two-request, same-representation shape as the named-fragment case above (both
		// `RoutingMode.Standard`), but this fragment declares no `Name`. An unnamed fragment is never registered
		// for selection, so it cannot be selected at all and is ordinary content — the guard must not treat it
		// like the named case, and the boundary holding it should cache and reuse normally.
		var first = await ReadAsync(client, "/issue-219/fragment-unnamed");
		Assert.Equal(HttpStatusCode.OK, first.Status);
		Assert.Contains("data-version=\"1\"", first.Body, StringComparison.Ordinal);

		data.Version = 2;
		var second = await ReadAsync(client, "/issue-219/fragment-unnamed");

		// Unlike the named case, a stored entry is exactly what should happen here: the second response must
		// still read version 1, proving the boundary's first-request capture was stored and replayed rather than
		// abandoned or freshly re-rendered.
		Assert.Equal(HttpStatusCode.OK, second.Status);
		Assert.Contains("data-inner", second.Body, StringComparison.Ordinal);
		Assert.Contains("data-version=\"1\"", second.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task An_htmx_response_header_set_during_render_reaches_every_request()
	{
		await using var app = await StartAsync<Issue219HeaderWritingPage>(true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// HtmxResponse.Retarget is advertised v1 behaviour and it is set while the component renders, which a
		// cache hit never does. The body would be correct and the client would swap it into the wrong element,
		// so the failure is silent at the protocol level rather than visible in the markup.
		var first = await RetargetAsync(client);
		Assert.Equal("#panel", first.Retarget);

		data.Version = 2;
		var second = await RetargetAsync(client);

		// Not a parity case: stock never caches an htmx response, so there is no stock behaviour to match here.
		// The boundary must keep nothing once its subtree has written an htmx response header, because a stored
		// entry can carry the body and not the header.
		Assert.Equal("#panel", second.Retarget);
		Assert.Contains("data-version=\"2\"", second.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task An_htmx_status_code_set_during_render_reaches_every_request()
	{
		await using var app = await StartAsync<Issue219HeaderWritingPage>(true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// A status code is not a header, so a rule written over response headers cannot see it. A lost 202 on a
		// polling client reads as "stop polling" turning into "carry on", and nothing in the markup shows it.
		Assert.Equal(HttpStatusCode.Accepted, (await InstructAsync(client, "status")).Status);

		data.Version = 2;
		Assert.Equal(HttpStatusCode.Accepted, (await InstructAsync(client, "status")).Status);
	}

	[Fact]
	public async Task An_htmx_empty_body_request_during_render_reaches_every_request()
	{
		await using var app = await StartAsync<Issue219HeaderWritingPage>(true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// EmptyBody sets no header at all: it is renderer state. Replaying a stored entry puts the markup back
		// that the component asked to suppress, which is the one outcome the call exists to prevent.
		Assert.Equal("", (await InstructAsync(client, "empty")).Body);

		data.Version = 2;
		Assert.Equal("", (await InstructAsync(client, "empty")).Body);
	}

	[Fact]
	public async Task An_htmx_redirect_issued_during_render_reaches_every_request()
	{
		await using var app = await StartAsync<Issue219HeaderWritingPage>(true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// The most damaging of the family: losing this leaves the client on the page it asked to leave, holding
		// stale markup, with no error anywhere.
		var first = await InstructAsync(client, "redirect");
		Assert.Equal("/issue-219/elsewhere", first.Redirect);
		Assert.Equal("", first.Body);

		data.Version = 2;
		var second = await InstructAsync(client, "redirect");
		Assert.Equal("/issue-219/elsewhere", second.Redirect);
		Assert.Equal("", second.Body);
	}

	private static async Task<(HttpStatusCode Status, string? Redirect, string Body)> InstructAsync(HttpClient client, string instruction)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, $"/issue-219/header-writing?instruction={instruction}");
		request.Headers.Add("HX-Request", "true");
		using var response = await client.SendAsync(request);
		var redirect = response.Headers.TryGetValues("HX-Redirect", out var values) ? string.Join(",", values) : null;
		return (response.StatusCode, redirect, await response.Content.ReadAsStringAsync());
	}

	private static async Task<(string? Retarget, string Body)> RetargetAsync(HttpClient client)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/header-writing");
		request.Headers.Add("HX-Request", "true");
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var retarget = response.Headers.TryGetValues("HX-Retarget", out var values) ? string.Join(",", values) : null;
		return (retarget, await response.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Content_beside_a_named_fragment_is_not_kept_without_it()
	{
		await using var app = await StartAsync<Issue219FragmentSiblingPage>(true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		var first = await ReadAsync(client, "/issue-219/fragment-sibling");
		Assert.Equal(HttpStatusCode.OK, first.Status);
		Assert.Contains("data-shell=\"1\"", first.Body, StringComparison.Ordinal);
		Assert.Contains("data-version=\"1\"", first.Body, StringComparison.Ordinal);

		data.Version = 2;
		var second = await ReadAsync(client, "/issue-219/fragment-sibling");

		// This boundary holds markup of its own beside the fragment, which the case above does not: there the
		// fragment is the whole cached body. Excluding only the fragment's subtree from the capture and storing
		// the rest leaves an entry with a hole nothing fills, because no live cached component is recorded for
		// it -- the second response then carries a stale shell and loses the fragment's content altogether.
		// Measured: that is exactly what happens when the boundary keeps its capture instead of abandoning it.
		// Both must be current, and both must still be present.
		Assert.Equal(HttpStatusCode.OK, second.Status);
		Assert.Contains("data-shell=\"2\"", second.Body, StringComparison.Ordinal);
		Assert.Contains("data-version=\"2\"", second.Body, StringComparison.Ordinal);
	}

	private static Task<(HttpStatusCode Status, string Body)> ReadAsync(HttpClient client)
		=> ReadAsync(client, "/issue-219/fragment");

	private static async Task<(HttpStatusCode Status, string Body)> ReadAsync(HttpClient client, string path)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		using var response = await client.SendAsync(request);
		return (response.StatusCode, await response.Content.ReadAsStringAsync());
	}

	private static Task<WebApplication> StartAsync() => StartAsync<Issue219FragmentPage>(true);

	internal static async Task<WebApplication> StartAsync<TRoot>(bool htmxor) where TRoot : IComponent
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue219FragmentPage).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue219Data>();
		builder.Services.AddAuthorization();
		builder.Services.AddCascadingAuthenticationState();
		var components = builder.Services.AddRazorComponents();
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
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return app;
	}
}

public sealed class Issue219CacheBehaviourTests
{
	[Fact]
	public async Task Rerender_component_inside_a_cached_subtree_is_not_frozen()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219TokenPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219TokenPage>(htmxor: true);

		var stockTokens = await ReadTokensAsync(stock);
		var candidateTokens = await ReadTokensAsync(candidate);

		// AntiforgeryToken is [CacheBehavior(Rerender)]: stock re-renders it on a hit rather than replaying it.
		// The data version beside it is the discriminator: differing tokens alone would also be explained by
		// nothing having been cached, so the surrounding boundary must be shown to have been reused.
		Assert.NotEqual(stockTokens[0].Token, stockTokens[1].Token);
		Assert.NotEqual(candidateTokens[0].Token, candidateTokens[1].Token);
		Assert.Equal("1", stockTokens[1].Version);
		Assert.Equal("1", candidateTokens[1].Version);
	}

	private static async Task<(string Token, string Version)[]> ReadTokensAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		var first = await ReadTokenAsync(client);
		data.Version = 2;
		return [first, await ReadTokenAsync(client)];
	}

	private static async Task<(string Token, string Version)> ReadTokenAsync(HttpClient client)
	{
		using var response = await client.GetAsync("/issue-219/token");
		var body = await response.Content.ReadAsStringAsync();
		var token = System.Text.RegularExpressions.Regex.Match(body, "value=\"([^\"]+)\"");
		Assert.True(token.Success, $"expected a token in: {body}");
		var version = System.Text.RegularExpressions.Regex.Match(body, "data-version=\"([^\"]+)\"");
		Assert.True(version.Success, $"expected a data version in: {body}");
		return (token.Groups[1].Value, version.Groups[1].Value);
	}
}

public sealed class Issue219CacheAuthorizationTests
{
	[Fact]
	public async Task Authorized_content_in_a_non_user_varying_cache_is_refused_like_stock()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219AuthPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219AuthPage>(htmxor: true);

		// AuthorizeViewCore is [CacheBehavior(Throw)] with [CacheCondition(CacheVaryBy.User)]. Stock refuses to
		// cache it; Htmxor must refuse identically rather than replay one principal's markup to another.
		var expected = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsAsync(stock, "alice"));
		var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsAsync(candidate, "alice"));

		Assert.Contains("cannot be used inside a CacheView", expected.Message, StringComparison.Ordinal);
		Assert.Equal(expected.Message, actual.Message);
	}

	[Fact]
	public async Task Authorized_content_varying_by_user_stays_isolated_between_principals()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219AuthByUserPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219AuthByUserPage>(htmxor: true);

		var expected = await ReadPairAsync(stock);
		var actual = await ReadPairAsync(candidate);

		// Each principal is read twice across an application data change, so a green result requires the entry
		// to have been stored and reused per principal rather than merely re-rendered correctly.
		Assert.Contains("alice", expected[0], StringComparison.Ordinal);
		Assert.Contains("bob", expected[1], StringComparison.Ordinal);
		Assert.Equal(["1", "1"], expected.Select(DataVersion));
		Assert.Equal(expected, actual);
	}

	private static async Task<string[]> ReadPairAsync(WebApplication app)
	{
		var data = app.Services.GetRequiredService<Issue219Data>();
		var warmAlice = await ReadAsAsync(app, "alice");
		var warmBob = await ReadAsAsync(app, "bob");
		Assert.Equal(HttpStatusCode.OK, warmAlice.Status);
		Assert.Equal(HttpStatusCode.OK, warmBob.Status);

		data.Version = 2;
		var alice = await ReadAsAsync(app, "alice");
		var bob = await ReadAsAsync(app, "bob");
		Assert.Equal(HttpStatusCode.OK, alice.Status);
		Assert.Equal(HttpStatusCode.OK, bob.Status);
		return [alice.Body, bob.Body];
	}

	private static string DataVersion(string body)
		=> System.Text.RegularExpressions.Regex.Match(body, "data-version=\"([^\"]+)\"").Groups[1].Value;

	private static async Task<(HttpStatusCode Status, string Body)> ReadAsAsync(WebApplication app, string user)
	{
		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/auth");
		request.Headers.Add("X-Issue-219-User", user);
		using var response = await client.SendAsync(request);
		return (response.StatusCode, await response.Content.ReadAsStringAsync());
	}
}

[Route("/issue-219/auth")]
public sealed class Issue219AuthPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder) => Issue219Auth.Build(builder, varyByUser: false);
}

public sealed class Issue219AuthByUserPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder) => Issue219Auth.Build(builder, varyByUser: true);
}

internal static class Issue219Auth
{
	public static void Build(RenderTreeBuilder builder, bool varyByUser)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), varyByUser ? "issue-219-auth-user" : "issue-219-auth");
		if (varyByUser)
		{
			builder.AddAttribute(2, nameof(CacheView.VaryByUser), true);
		}

		builder.AddAttribute(3, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Microsoft.AspNetCore.Components.Authorization.AuthorizeView>(0);
			cached.AddAttribute(1, "Authorized", (RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>)(state => inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddAttribute(1, "data-user", state.User.Identity?.Name);
				inner.AddContent(2, $"authorized: {state.User.Identity?.Name}");
				inner.CloseElement();

				// Carries the application data version, so a green result needs real reuse per principal.
				inner.OpenComponent<Issue219CachedContent>(3);
				inner.CloseComponent();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219CacheKeyTests
{
	[Fact]
	public async Task Sibling_boundaries_without_explicit_keys_do_not_share_one_entry()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219SiblingPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219SiblingPage>(htmxor: true);

		// Two CacheView components under one parent with no CacheKey are disambiguated only by their tree
		// position, so this is what proves the mirrored key computation, within a single host and cache.
		var expected = await ReadAsync(stock);
		var actual = await ReadAsync(candidate);

		Assert.Contains("data-slot=\"first\"", expected, StringComparison.Ordinal);
		Assert.Contains("data-slot=\"second\"", expected, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	private static async Task<string> ReadAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		using var first = await client.GetAsync("/issue-219/siblings");
		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		using var second = await client.GetAsync("/issue-219/siblings");
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);
		return await second.Content.ReadAsStringAsync();
	}
}

[Route("/issue-219/siblings")]
public sealed class Issue219SiblingPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.ChildContent), Slot("first"));
		builder.CloseComponent();
		builder.OpenComponent<CacheView>(2);
		builder.AddAttribute(3, nameof(CacheView.ChildContent), Slot("second"));
		builder.CloseComponent();
	}

	private static RenderFragment Slot(string slot) => cached =>
	{
		cached.OpenElement(0, "p");
		cached.AddAttribute(1, "data-slot", slot);
		cached.AddContent(2, slot);
		cached.CloseElement();
	};
}

[Route("/issue-219/token")]
public sealed class Issue219TokenPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-token");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Microsoft.AspNetCore.Components.Forms.AntiforgeryToken>(0);
			cached.CloseComponent();
			cached.OpenComponent<Issue219CachedContent>(1);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

[Route("/issue-219/fragment")]
public sealed class Issue219FragmentPage : ComponentBase
{
	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	[Inject] internal Issue219Data Data { get; set; } = default!;

	protected override void OnInitialized()
		=> HttpContext.GetHtmxContext().Response.SelectFragment("inner");

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-fragment");
		builder.AddAttribute(4, nameof(CacheView.VaryByHeader), "HX-Target");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<HtmxFragment>(0);
			cached.AddAttribute(1, nameof(HtmxFragment.Name), "inner");
			cached.AddAttribute(2, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddAttribute(1, "data-inner", "true");
				inner.AddAttribute(2, "data-version", Data.Version);
				inner.AddContent(3, "inner");
				inner.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

[Route("/issue-219/fragment-unnamed")]
public sealed class Issue219UnnamedFragmentPage : ComponentBase
{
	[Inject] internal Issue219Data Data { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-fragment-unnamed");
		builder.AddAttribute(4, nameof(CacheView.VaryByHeader), "HX-Target");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			// No Name is set: this HtmxFragment is unnamed and therefore never registered for selection.
			cached.OpenComponent<HtmxFragment>(0);
			cached.AddAttribute(1, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddAttribute(1, "data-inner", "true");
				inner.AddAttribute(2, "data-version", Data.Version);
				inner.AddContent(3, "inner");
				inner.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
// Markup of the boundary's own beside a named fragment, rather than a fragment that is the whole cached body.
[Route("/issue-219/fragment-sibling")]
public sealed class Issue219FragmentSiblingPage : ComponentBase
{
	[Inject] internal Issue219Data Data { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-fragment-sibling");
		builder.AddAttribute(4, nameof(CacheView.VaryByHeader), "HX-Target");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenElement(0, "p");
			cached.AddAttribute(1, "data-shell", Data.Version);
			cached.CloseElement();
			cached.OpenComponent<HtmxFragment>(2);
			cached.AddAttribute(3, nameof(HtmxFragment.Name), "sibling");
			cached.AddAttribute(4, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenComponent<Issue219CachedContent>(0);
				inner.CloseComponent();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
// A component inside a declared boundary that writes an htmx response header while it renders. Declared, so
// the boundary is otherwise eligible to cache: the header is the only reason it must not.
[Route("/issue-219/header-writing")]
public sealed class Issue219HeaderWritingPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-header-writing");
		builder.AddAttribute(4, nameof(CacheView.VaryByHeader), "HX-Target");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219HeaderWritingContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219HeaderWritingContent : ComponentBase
{
	[Inject] internal Issue219Data Data { get; set; } = default!;

	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	protected override void OnInitialized() => Apply(HttpContext);

	private static void Apply(HttpContext context)
	{
		var response = context.GetHtmxContext().Response;
		var instruction = context.Request.Query["instruction"].ToString();
		if (instruction is "status")
		{
			response.StatusCode(System.Net.HttpStatusCode.Accepted);
			return;
		}

		if (instruction is "empty")
		{
			response.EmptyBody();
			return;
		}

		if (instruction is "redirect")
		{
			response.Redirect("/issue-219/elsewhere");
			return;
		}

		response.Retarget("#panel");
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		builder.AddAttribute(1, "data-version", Data.Version);
		builder.CloseElement();
	}
}
#endif
