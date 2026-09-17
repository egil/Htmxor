#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// Most cases pair Htmxor against a stock host, so the framework decides the outcome and Htmxor only has to
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
	public async Task A_cache_hit_produces_the_same_headers_stock_does()
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
	public async Task A_boundary_beneath_an_htmx_layout_caches_like_stock()
	{
		var stock = await ReadPairAsync<Issue219LayoutHostPage>(htmxor: false);
		var candidate = await ReadPairAsync<Issue219LayoutHostPage>(htmxor: true);

		// HtmxLayoutComponentBase is the layout the documentation teaches, and it implements IConditionalRender
		// with a constant ShouldOutput. Classifying that interface as request-varying therefore stopped every
		// boundary beneath the documented layout from caching, on ordinary requests, which is the parity defect
		// the scope reversal repaired. Nothing measured the repair in this direction until this case: re-adding
		// the interface to the ancestor walk in HasUncacheableAncestor leaves it as the only thing that reddens.
		// Re-adding it to IsRequestVarying itself also reddens the holding half, which
		// A_boundary_holding_a_conditional_component_caches_like_stock owns.
		Assert.Equal("1", Slot(stock[1], "layout"));
		Assert.Equal(Slot(stock[1], "layout"), Slot(candidate[1], "layout"));
	}

	[Fact]
	public async Task An_ordinary_request_caches_a_named_fragments_boundary_like_stock()
	{
		var stock = await ReadPairAsync<Issue219FragmentBoundaryPage>(htmxor: false);
		var candidate = await ReadPairAsync<Issue219FragmentBoundaryPage>(htmxor: true);

		// Nothing selects a fragment on an ordinary request, so a boundary holding one is ordinary content and
		// must cache exactly as stock does. While a named fragment was classified as request-varying by type,
		// this boundary abandoned its capture on ordinary requests too. The layout case above covers the other
		// half of that classification; this one covers the fragment half, and the two are not interchangeable.
		Assert.Equal("1", Slot(stock[1], "fragment"));
		Assert.Equal(Slot(stock[1], "fragment"), Slot(candidate[1], "fragment"));
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
		// named fragment there. That was a kind Htmxor discarded the capture for when this case was written, and
		// is not one now -- the scope reversal removed it, so both compositions take the same streaming-pause
		// path today and the distinction the two cases were built on no longer exists. Retained because the
		// behaviour it pins is still the contract: pausing as stock pauses for anything it will not store.
		// Returning without pausing left the capture active over a subtree stock stops validating, and the
		// framework's refusal fired on every request rather than none. Measured before that fix: stock 200,
		// candidate threw.
		var expected = await ReadAsAuthenticatedAsync(stock, "alice");
		var actual = await ReadAsAuthenticatedAsync(candidate, "alice");

		Assert.Equal(HttpStatusCode.OK, expected.Status);
		Assert.Contains("authorized: alice", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected.Status, actual.Status);
		Assert.Contains("authorized: alice", actual.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task A_nested_boundary_beneath_an_async_load_raises_the_frameworks_refusal()
	{
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219NestedAsyncBoundaryPage>(htmxor: true);

		// The tripwire for the capture pause, recomposed on a kind that is live. Its previous form composed an
		// HtmxFragment, which stopped being request-varying one commit after the tripwire was written, so it
		// passed for an unrelated reason and measured nothing while a pause was reintroduced beneath it.
		// HtmxAsyncLoad is the live kind. The framework refuses a CacheView nested inside a capturing one, so
		// if the kind's subtree is paused rather than left to validate, the refusal is skipped and this renders.
		//
		// No stock arm exists: HtmxAsyncLoad needs Htmxor's own scoped context and a stock host cannot
		// construct it. The paired equivalent without it is the ordinary-wrapper case above.
		var thrown = await Assert.ThrowsAnyAsync<Exception>(async () =>
		{
			using var client = candidate.GetTestClient();
			using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/nested-async-boundary");
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
			request.Headers.Add("HX-Source", "div#lazy");
			request.Headers.Add("HX-Target", "div#lazy");
			using var response = await client.SendAsync(request);
			_ = await response.Content.ReadAsStringAsync();
		});

		// The matching source and target are what make HtmxAsyncLoad render ChildContent rather than Loading,
		// so the inner boundary is constructed at all.
		var refusal = Assert.IsType<InvalidOperationException>(thrown.GetBaseException());
		Assert.Contains("cannot be nested inside another CacheView", refusal.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task A_sibling_of_a_boundary_that_stores_nothing_still_caches()
	{
		await using var app = await Issue219CacheInteractiveTests.StartAsync<Issue219SiblingOfAbandoningPage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		var first = await ReadAsync(client, "/issue-219/configured");
		data.Version = 2;
		var candidate = new[] { first, await ReadAsync(client, "/issue-219/configured") };

		// Two boundaries on one page, one holding an interactive render-mode boundary and therefore storing
		// nothing. Abandoning that one must not disable the other. The claim that any sibling boundary stays
		// cacheable ships in two documents and lost its case when the htmx fixtures went: every remaining
		// abandoning fixture holds exactly one CacheView.
		Assert.Equal("2", Slot(candidate[1], "abandoning"));
		Assert.Equal("1", Slot(candidate[1], "sibling"));
	}

	// Stock raises these while the response body is written, so the exception can arrive wrapped rather than
	// thrown from SendAsync. GetBaseException unwraps to the framework's own InvalidOperationException.
	private static async Task<InvalidOperationException> ReadRootCauseAsync(
		WebApplication app, string path, string? user = null, bool htmx = false)
	{
		var thrown = await Assert.ThrowsAnyAsync<Exception>(async () =>
		{
			using var client = app.GetTestClient();
			_ = user is null
				? await ReadAsync(client, path, htmx)
				: await ReadAuthenticatedAsync(client, path, user, htmx);
		});

		return Assert.IsType<InvalidOperationException>(thrown.GetBaseException());
	}

	private static async Task<string> ReadAuthenticatedAsync(HttpClient client, string path, string user, bool htmx = false)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("X-Issue-219-User", user);
		if (htmx)
		{
			request.Headers.Add("HX-Request", "true");
		}

		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
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

	private static async Task<string[]> ReadPairAsync<TRoot>(bool htmxor, bool htmx = false) where TRoot : IComponent
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<TRoot>(htmxor);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		var first = await ReadAsync(client, "/issue-219/configured", htmx);
		data.Version = 2;
		return [first, await ReadAsync(client, "/issue-219/configured", htmx)];
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

	private static async Task<string> ReadAsync(HttpClient client, string path, bool htmx = false)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (htmx)
		{
			request.Headers.Add("HX-Request", "true");
		}

		using var response = await client.SendAsync(request);
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

public sealed class Issue219FragmentBoundaryPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-fragment-boundary");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Htmxor.Components.HtmxFragment>(0);
			cached.AddAttribute(1, nameof(Htmxor.Components.HtmxFragment.Name), "named");
			cached.AddAttribute(2, nameof(Htmxor.Components.HtmxFragment.ChildContent), Issue219Slot.Render("fragment"));
			cached.CloseComponent();
		}));
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
// A boundary beneath a component deriving from the layout base the documentation teaches.
public sealed class Issue219LayoutHostPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<Issue219Layout>(0);
		builder.AddAttribute(1, nameof(Issue219Layout.Body), (RenderFragment)(body =>
		{
			body.OpenComponent<CacheView>(0);
			body.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-layout");
			body.AddAttribute(2, nameof(CacheView.ChildContent), Issue219Slot.Render("layout"));
			body.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219Layout : Htmxor.Components.HtmxLayoutComponentBase
{
}
// A CacheView nested inside another, with a live request-varying kind between them.
[Route("/issue-219/nested-async-boundary")]
public sealed class Issue219NestedAsyncBoundaryPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-nested-async-outer");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Htmxor.Components.HtmxAsyncLoad>(0);
			cached.AddAttribute(1, nameof(Htmxor.Components.HtmxAsyncLoad.Id), "lazy");
			cached.AddAttribute(2, nameof(Htmxor.Components.HtmxAsyncLoad.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenComponent<CacheView>(0);
				inner.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-nested-async-inner");
				inner.AddAttribute(2, nameof(CacheView.ChildContent), Issue219Slot.Render("inner"));
				inner.CloseComponent();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// One boundary that stores nothing beside one that caches.
public sealed class Issue219SiblingOfAbandoningPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-abandoning-sibling");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219SiblingInteractive>(0);
			cached.AddComponentRenderMode(Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer);
			cached.CloseComponent();
			cached.AddContent(1, Issue219Slot.Render("abandoning"));
		}));
		builder.CloseComponent();
		builder.OpenComponent<CacheView>(3);
		builder.AddAttribute(4, nameof(CacheView.CacheKey), "issue-219-abandoning-sibling-other");
		builder.AddAttribute(5, nameof(CacheView.ChildContent), Issue219Slot.Render("sibling"));
		builder.CloseComponent();
	}
}

public sealed class Issue219SiblingInteractive : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		builder.AddAttribute(1, "data-interactive", "true");
		builder.CloseElement();
	}
}
#endif
