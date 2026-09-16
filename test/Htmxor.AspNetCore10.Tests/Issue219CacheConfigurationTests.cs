#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// Each case pairs Htmxor against a stock host, so the framework decides the outcome and Htmxor only has to
// match it. That keeps these honest even where the stock answer is not obvious in advance.
public sealed class Issue219CacheConfigurationTests
{
	[Fact]
	public async Task An_already_expired_entry_is_not_reused()
	{
		var expected = await ReadPairAsync<Issue219ExpiredPage>(htmxor: false);
		var actual = await ReadPairAsync<Issue219ExpiredPage>(htmxor: true);

		// The entry's expiry has already passed when it would be reused, so the second request re-renders.
		Assert.Contains("data-version=\"2\"", expected[1], StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task A_boundary_inside_a_streaming_subtree_is_not_cached()
	{
		var expected = await ReadPairAsync<Issue219StreamingPage>(htmxor: false);
		var actual = await ReadPairAsync<Issue219StreamingPage>(htmxor: true);

		// Caching is suppressed inside a streaming render context, which the candidate must inherit from the
		// logical parent exactly as stock does. The comparison is on the cached application data rather than
		// the whole body: the candidate's streaming boundary markers already differ from stock for a component
		// that did not itself opt into streaming, which is pre-existing and owned by #192.
		Assert.Equal(["1", "2"], expected.Select(DataVersion));
		Assert.Equal(expected.Select(DataVersion), actual.Select(DataVersion));
	}

	[Fact]
	public async Task A_cache_hit_does_not_mutate_the_response_headers()
	{
		var expected = await ReadHeadersPairAsync(htmxor: false);
		var actual = await ReadHeadersPairAsync(htmxor: true);

		// The name's own claim: within one host, the second (hit) response's headers equal the first (miss)
		// response's headers.
		Assert.Equal(expected[0], expected[1]);
		Assert.Equal(actual[0], actual[1]);

		// Parity: Htmxor produces the same header set stock does, for both the miss and the hit.
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task An_expired_boundary_stops_reusing_while_its_sibling_still_does()
	{
		var expected = await ReadPairAsync<Issue219MixedExpiryPage>(htmxor: false);
		var actual = await ReadPairAsync<Issue219MixedExpiryPage>(htmxor: true);

		// Contrastive rather than absolute: one page, two boundaries, identical content, differing only in
		// expiry. The kept one still serves version 1 while the expired one has moved to version 2, which
		// distinguishes expiry from a boundary that simply never cached.
		Assert.Equal("1", Slot(expected[1], "kept"));
		Assert.Equal("2", Slot(expected[1], "expired"));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task A_sibling_of_an_abandoning_boundary_still_caches()
	{
		var stock = await ReadPairAsync<Issue219AbandonSiblingPage>(htmxor: false);
		var candidate = await ReadPairAsync<Issue219AbandonSiblingPage>(htmxor: true);

		// Htmxor deliberately caches less than stock here. A named fragment is registered only when its
		// component is constructed, which serving stored output never does, so Htmxor stores nothing for the
		// boundary holding it while stock happily caches it.
		Assert.Equal("1", Slot(stock[1], "fragment"));
		Assert.Equal("2", Slot(candidate[1], "fragment"));

		// The point of the case: abandoning that boundary must not disable the ordinary one beside it.
		Assert.Equal("1", Slot(stock[1], "ordinary"));
		Assert.Equal("1", Slot(candidate[1], "ordinary"));
	}

	[Fact]
	public async Task A_disabled_boundary_still_refuses_content_stock_refuses()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219DisabledAuthPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219DisabledAuthPage>(htmxor: true);

		// A disabled boundary caches nothing, but stock still validates what it contains, so the guard must run
		// even when there is no render state to capture into.
		var expected = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(stock.GetTestClient(), "/issue-219/configured"));
		var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(candidate.GetTestClient(), "/issue-219/configured"));

		Assert.Contains("cannot be used inside a CacheView", expected.Message, StringComparison.Ordinal);
		Assert.Equal(expected.Message, actual.Message);
	}

	[Fact]
	public async Task An_authorize_view_under_an_ordinary_wrapper_on_a_streaming_page_renders_like_stock()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219StreamingWrappedAuthPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219StreamingWrappedAuthPage>(htmxor: true);

		// The wrapper carries no [StreamRendering] of its own, so the descendant guard must consult the inherited
		// streaming state rather than the wrapper's own type, exactly as it does for the CacheView boundary itself.
		// Measured before the fix: stock returned 200 here while the candidate threw. Streaming boundary markers
		// are compared elsewhere (A_boundary_inside_a_streaming_subtree_is_not_cached) and are pre-existing,
		// #192-owned noise for a component that never opted into streaming itself, so only the authorized content
		// is compared here.
		var expected = await ReadAsAuthenticatedAsync(stock, "alice");
		var actual = await ReadAsAuthenticatedAsync(candidate, "alice");

		Assert.Equal(HttpStatusCode.OK, expected.Status);
		Assert.Contains("authorized: alice", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected.Status, actual.Status);
		Assert.Contains("authorized: alice", actual.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task An_authorize_view_under_a_named_fragment_on_a_streaming_page_renders_like_stock()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219StreamingFragmentAuthPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219StreamingFragmentAuthPage>(htmxor: true);

		// The sibling case above puts a plain wrapper between the boundary and the AuthorizeView; this one puts a
		// named fragment there, which is a kind Htmxor discards the capture for. Discarding must still pause the
		// capture as stock pauses for anything it will not store -- returning without pausing left the capture
		// active over a subtree stock stops validating at that point, and the framework's refusal fired on every
		// request rather than none. Measured before the fix: stock 200, candidate threw.
		var expected = await ReadAsAuthenticatedAsync(stock, "alice");
		var actual = await ReadAsAuthenticatedAsync(candidate, "alice");

		Assert.Equal(HttpStatusCode.OK, expected.Status);
		Assert.Contains("authorized: alice", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected.Status, actual.Status);
		Assert.Contains("authorized: alice", actual.Body, StringComparison.Ordinal);
	}

	private static async Task<(HttpStatusCode Status, string Body)> ReadAsAuthenticatedAsync(WebApplication app, string user)
	{
		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/auth");
		request.Headers.Add("X-Issue-219-User", user);
		using var response = await client.SendAsync(request);
		return (response.StatusCode, await response.Content.ReadAsStringAsync());
	}

	private static string Slot(string body, string slot)
		=> System.Text.RegularExpressions.Regex.Match(body, $"data-slot=\"{slot}\" data-version=\"([^\"]+)\"").Groups[1].Value;

	private static async Task<string[]> ReadPairAsync<TRoot>(bool htmxor) where TRoot : IComponent
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<TRoot>(htmxor);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		var first = await ReadAsync(client, "/issue-219/configured");
		data.Version = 2;
		return [first, await ReadAsync(client, "/issue-219/configured")];
	}

	private static async Task<string[]> ReadHeadersPairAsync(bool htmxor)
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219ConfiguredPage>(htmxor);
		using var client = app.GetTestClient();
		return [await ReadHeadersAsync(client), await ReadHeadersAsync(client)];
	}

	private static async Task<string> ReadHeadersAsync(HttpClient client)
	{
		using var response = await client.GetAsync("/issue-219/configured");
		var headers = response.Headers.Concat(response.Content.Headers)
			.Where(header => !string.Equals(header.Key, "Date", StringComparison.OrdinalIgnoreCase))
			.Select(header => $"{header.Key}: {string.Join(",", header.Value)}")
			.OrderBy(header => header, StringComparer.Ordinal);
		return $"{(int)response.StatusCode} {string.Join("|", headers)}";
	}

	private static string DataVersion(string body)
		=> System.Text.RegularExpressions.Regex.Match(body, "data-version=\"([^\"]+)\"").Groups[1].Value;

	private static async Task<string> ReadAsync(HttpClient client, string path)
	{
		using var response = await client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}
}

[Route("/issue-219/configured")]
public sealed class Issue219ConfiguredPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
		=> Issue219Configured.Build(builder, expired: false);
}

public sealed class Issue219ExpiredPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
		=> Issue219Configured.Build(builder, expired: true);
}

public sealed class Issue219MixedExpiryPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-kept");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), Issue219Slot.Render("kept"));
		builder.CloseComponent();
		builder.OpenComponent<CacheView>(3);
		builder.AddAttribute(4, nameof(CacheView.CacheKey), "issue-219-expiring");
		builder.AddAttribute(5, nameof(CacheView.ExpiresOn), DateTimeOffset.UtcNow.AddSeconds(-30));
		builder.AddAttribute(6, nameof(CacheView.ChildContent), Issue219Slot.Render("expired"));
		builder.CloseComponent();
	}
}

public sealed class Issue219AbandonSiblingPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-abandoning");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Htmxor.Components.HtmxFragment>(0);
			cached.AddAttribute(1, nameof(Htmxor.Components.HtmxFragment.Name), "abandon");
			cached.AddAttribute(2, nameof(Htmxor.Components.HtmxFragment.ChildContent), Issue219Slot.Render("fragment"));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
		builder.OpenComponent<CacheView>(3);
		builder.AddAttribute(4, nameof(CacheView.CacheKey), "issue-219-ordinary");
		builder.AddAttribute(5, nameof(CacheView.ChildContent), Issue219Slot.Render("ordinary"));
		builder.CloseComponent();
	}
}

// Reached as the root component through the shared route, like the other fixtures in this file.
public sealed class Issue219DisabledAuthPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-disabled");
		builder.AddAttribute(2, nameof(CacheView.Enabled), false);
		builder.AddAttribute(3, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Microsoft.AspNetCore.Components.Authorization.AuthorizeView>(0);
			cached.AddAttribute(1, "Authorized", (RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>)(_ => inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddContent(1, "authorized");
				inner.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// Reached as the root component through the shared route, like the other fixtures in this file. Streaming and a
// plain (non-streaming) wrapper both sit between the page and the AuthorizeView on purpose: the guard must read
// the inherited streaming state through the wrapper rather than the wrapper's own (absent) attribute.
[StreamRendering]
public sealed class Issue219StreamingWrappedAuthPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-streaming-wrapped-auth");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219PlainWrapper>(0);
			cached.AddAttribute(1, nameof(Issue219PlainWrapper.ChildContent), (RenderFragment)(wrapped =>
			{
				wrapped.OpenComponent<Microsoft.AspNetCore.Components.Authorization.AuthorizeView>(0);
				wrapped.AddAttribute(1, "Authorized", (RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>)(state => inner =>
				{
					inner.OpenElement(0, "p");
					inner.AddContent(1, $"authorized: {state.User.Identity?.Name}");
					inner.CloseElement();
				}));
				wrapped.CloseComponent();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// The same shape as Issue219StreamingWrappedAuthPage with a named fragment where the plain wrapper sits, so the
// discard path is the one under test rather than the ordinary pause path.
[StreamRendering]
public sealed class Issue219StreamingFragmentAuthPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-streaming-fragment-auth");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Htmxor.Components.HtmxFragment>(0);
			cached.AddAttribute(1, nameof(Htmxor.Components.HtmxFragment.Name), "probe");
			cached.AddAttribute(2, nameof(Htmxor.Components.HtmxFragment.ChildContent), (RenderFragment)(wrapped =>
			{
				wrapped.OpenComponent<Microsoft.AspNetCore.Components.Authorization.AuthorizeView>(0);
				wrapped.AddAttribute(1, "Authorized", (RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>)(state => inner =>
				{
					inner.OpenElement(0, "p");
					inner.AddContent(1, $"authorized: {state.User.Identity?.Name}");
					inner.CloseElement();
				}));
				wrapped.CloseComponent();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// Carries no render-mode or streaming attribute of its own, so any streaming state it exposes to its content must
// come from its parent.
public sealed class Issue219PlainWrapper : ComponentBase
{
	[Parameter] public RenderFragment? ChildContent { get; set; }

	protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}

internal static class Issue219Slot
{
	public static RenderFragment Render(string slot) => cached =>
	{
		cached.OpenComponent<Issue219SlotContent>(0);
		cached.AddAttribute(1, nameof(Issue219SlotContent.Slot), slot);
		cached.CloseComponent();
	};
}

public sealed class Issue219SlotContent : ComponentBase
{
	[Parameter] public string Slot { get; set; } = "";

	[Inject] internal Issue219Data Data { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		builder.AddAttribute(1, "data-slot", Slot);
		builder.AddAttribute(2, "data-version", Data.Version);
		builder.CloseElement();
	}
}

[StreamRendering]
public sealed class Issue219StreamingPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
		=> Issue219Configured.Build(builder, expired: false);
}

internal static class Issue219Configured
{
	public static void Build(RenderTreeBuilder builder, bool expired)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), expired ? "issue-219-expired" : "issue-219-configured");
		if (expired)
		{
			builder.AddAttribute(2, nameof(CacheView.ExpiresOn), DateTimeOffset.UtcNow.AddSeconds(-30));
		}

		builder.AddAttribute(3, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219CachedContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
