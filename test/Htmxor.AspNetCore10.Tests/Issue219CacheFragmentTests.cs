#if NET11_0_OR_GREATER
using System.Net;
using Htmxor;
using Htmxor.Components;
using Htmxor.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// The inverse of the nesting Issue219CacheRepresentationTests covers: a CacheView *beneath* a named fragment
// rather than around one. The response representation the key carries says whether a request is an htmx one,
// not which fragment it selected, so two selections reach the same tree position with the same representation.
public sealed class Issue219CacheFragmentTests
{
	[Fact]
	public async Task Selecting_a_second_fragment_does_not_replay_the_first_fragments_markup()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219SelectableFragmentPage>(htmxor: true);
		using var client = app.GetTestClient();

		var first = await SelectAsync(client, "a");
		Assert.Contains("data-fragment=\"a\"", first, StringComparison.Ordinal);

		// Before the ancestor discard this returned the first fragment's markup: one request was served
		// another request's content, because the boundary beneath the fragment had stored it.
		var second = await SelectAsync(client, "b");
		Assert.Contains("data-fragment=\"b\"", second, StringComparison.Ordinal);
		Assert.DoesNotContain("data-fragment=\"a\"", second, StringComparison.Ordinal);
	}

	[Fact]
	public async Task A_conditional_component_that_opted_out_still_raises_the_frameworks_refusal()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalRefusalPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalRefusalPage>(htmxor: true);

		// Htmxor discards a capture holding an IConditionalRender, which on its own would let an application
		// type that also carries CacheBehavior.Throw render where stock refuses it. Stock's guard is therefore
		// consulted before the discard, so the refusal survives.
		var expected = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(stock));
		var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(candidate));

		Assert.Contains("cannot be used inside a CacheView", expected.Message, StringComparison.Ordinal);
		Assert.Equal(expected.Message, actual.Message);
	}

	[Fact]
	public async Task A_boundary_is_not_served_what_it_stored_before_its_fragment_ancestor_was_named()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219NameableFragmentPage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// An HtmxFragment's Name is a parameter, so one position can be unnamed on one request and named on the
		// next at the same representation. Unnamed, the boundary beneath it is ordinary content and caches.
		var unnamed = await ReadAsync(client, "/issue-219/nameable");
		Assert.Contains("data-version=\"1\"", unnamed, StringComparison.Ordinal);

		data.Version = 2;

		// Named, the same position must not be handed the body stored while it was ordinary content. Abandoning
		// the capture cannot achieve that on its own, because abandonment governs the store and not the serve.
		var named = await ReadAsync(client, "/issue-219/nameable?name=details");
		Assert.Contains("data-version=\"2\"", named, StringComparison.Ordinal);
	}

	[Fact]
	public async Task A_boundary_beneath_a_conditional_component_stores_nothing()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalAncestorPage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		Assert.Contains("data-version=\"1\"", await ReadAsync(client, "/issue-219/conditional-ancestor"), StringComparison.Ordinal);
		data.Version = 2;

		// The ancestor decides from the request whether to produce markup at all, which no cache key carries,
		// so a boundary underneath it renders afresh every time rather than replaying an earlier decision.
		Assert.Contains("data-version=\"2\"", await ReadAsync(client, "/issue-219/conditional-ancestor"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task A_boundary_whose_request_varying_child_is_conditional_is_not_shared_across_targets()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalChildPage>(htmxor: true);
		using var client = app.GetTestClient();

		// The guards run while a component renders, and a cache hit is resolved before ChildContent is invoked,
		// so a subtree that produced no request-varying component on the storing request has none to guard on
		// the serving one. Two requests differing only in HX-Target: the first stores plain content, the second
		// should produce the fragment. Before the representation carried the header, the second was served the
		// first's body -- and when it selected, it failed with "No fragment named 'a' was rendered."
		var plain = await TargetAsync(client, "#other");
		Assert.Contains("data-plain", plain, StringComparison.Ordinal);

		var fragment = await TargetAsync(client, "#a");
		Assert.Contains("data-fragment-a", fragment, StringComparison.Ordinal);
		Assert.DoesNotContain("data-plain", fragment, StringComparison.Ordinal);
	}

	[Fact]
	public async Task An_undeclared_boundary_does_not_cache_an_htmx_request_at_all()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219UndeclaredHtmxPage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		Assert.Contains("data-version=\"1\"", await HtmxAsync(client), StringComparison.Ordinal);
		data.Version = 2;

		// Nothing declared what this boundary varies by, and Htmxor cannot know what its subtree read. Guessing
		// broadly would put attacker-chosen header values in the key; guessing narrowly serves one request
		// another's markup. It therefore stores nothing and renders afresh, and because the representation puts
		// htmx requests in their own key space, there is no entry from an ordinary request for it to be served.
		Assert.Contains("data-version=\"2\"", await HtmxAsync(client), StringComparison.Ordinal);
	}

	private static async Task<string> HtmxAsync(HttpClient client)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/undeclared");
		request.Headers.Add("HX-Request", "true");
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<string> TargetAsync(HttpClient client, string target)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/conditional-child");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("HX-Target", target);
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<string> ReadAsync(HttpClient client, string path)
	{
		using var response = await client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<string> ReadAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		using var response = await client.GetAsync("/issue-219/conditional-refusal");
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<string> SelectAsync(HttpClient client, string fragment)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, $"/issue-219/selectable?f={fragment}");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}
}

// An application component may be both an IConditionalRender and one the framework refuses to cache. Nothing
// stops the two from meeting on the same type, and the refusal is the framework's to raise, not Htmxor's to
// swallow by discarding the capture first.
[Route("/issue-219/conditional-refusal")]
public sealed class Issue219ConditionalRefusalPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-conditional-refusal");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219ConditionalRefusedContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

[CacheBehavior(CacheBehavior.Throw)]
[CacheCondition(CacheVaryBy.User)]
public sealed class Issue219ConditionalRefusedContent : ComponentBase, IConditionalRender
{
	public bool ShouldOutput([System.Diagnostics.CodeAnalysis.NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren) => true;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		builder.AddContent(1, "refused");
		builder.CloseElement();
	}
}

// One position whose HtmxFragment ancestor is unnamed on one request and named on the next.
[Route("/issue-219/nameable")]
public sealed class Issue219NameableFragmentPage : ComponentBase
{
	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var name = HttpContext.Request.Query["name"].ToString();
		builder.OpenComponent<HtmxFragment>(0);
		builder.AddAttribute(1, nameof(HtmxFragment.Name), string.IsNullOrEmpty(name) ? null : name);
		builder.AddAttribute(2, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
		{
			inner.OpenComponent<CacheView>(0);
			inner.AddAttribute(5, nameof(CacheView.VaryBy), "issue-219-nameable");
			inner.AddAttribute(1, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
			{
				cached.OpenComponent<Issue219CachedContent>(0);
				cached.CloseComponent();
			}));
			inner.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// An IConditionalRender standing above a boundary, rather than inside one. ConditionalComponentBase is the
// public base an application derives from, so this is the shape a consumer reaches, not an internal one.
[Route("/issue-219/conditional-ancestor")]
public sealed class Issue219ConditionalAncestorPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<Issue219ConditionalAncestor>(0);
		builder.CloseComponent();
	}
}

public sealed class Issue219ConditionalAncestor : ComponentBase, IConditionalRender
{
	public bool ShouldOutput([System.Diagnostics.CodeAnalysis.NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren) => true;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(5, nameof(CacheView.VaryBy), "issue-219-conditional-ancestor");
		builder.AddAttribute(1, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219CachedContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// The request-varying component exists only on some requests, so neither guard can see it on the others. This
// is ordinary page control flow, not a component anyone could annotate with [CacheBehavior].
[Route("/issue-219/conditional-child")]
public sealed class Issue219ConditionalChildPage : ComponentBase
{
	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var wantsFragment = HttpContext.GetHtmxContext().Request.Target == "#a";
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-conditional-child");
		// Declares the header its content actually varies by. Declaring HX-Request alone would enable caching
		// without separating the two targets, and the boundary would be served the other target's body -- the
		// same incomplete-declaration exposure stock has for any undeclared input, tracked as #235.
		builder.AddAttribute(5, nameof(CacheView.VaryByHeader), "HX-Target");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			if (!wantsFragment)
			{
				cached.OpenElement(0, "p");
				cached.AddAttribute(1, "data-plain", "true");
				cached.CloseElement();
				return;
			}

			cached.OpenComponent<HtmxFragment>(2);
			cached.AddAttribute(3, nameof(HtmxFragment.Name), "a");
			cached.AddAttribute(4, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddAttribute(1, "data-fragment-a", "true");
				inner.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// Declares no variation, so an htmx request must not be cached at all.
[Route("/issue-219/undeclared")]
public sealed class Issue219UndeclaredHtmxPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-undeclared");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219CachedContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

[Route("/issue-219/selectable")]
public sealed class Issue219SelectableFragmentPage : ComponentBase
{
	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	private string Selected => HttpContext.Request.Query["f"].ToString() is { Length: > 0 } name ? name : "a";

	protected override void OnInitialized()
	{
		var context = HttpContext.GetHtmxContext();
		if (context.Request.IsHtmxRequest)
		{
			context.Response.SelectFragment(Selected);
		}
	}

	// Only the requested fragment is built, so no two CacheView components coexist in one render pass and the
	// framework's own duplicate-key guard never fires. Across requests they still share one tree position: the
	// same CacheView frame sequence under the same HtmxFragment parent type.
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var name = Selected;
		builder.OpenComponent<HtmxFragment>(0);
		builder.AddAttribute(1, nameof(HtmxFragment.Name), name);
		builder.AddAttribute(2, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
		{
			inner.OpenComponent<CacheView>(0);
			inner.AddAttribute(5, nameof(CacheView.VaryBy), "issue-219-selectable");
			inner.AddAttribute(1, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
			{
				cached.OpenElement(0, "p");
				cached.AddAttribute(1, "data-fragment", name);
				cached.CloseElement();
			}));
			inner.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
