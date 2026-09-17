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

// Compositions where an Htmxor component sits inside or above a cached boundary. Named fragments were once
// the subject here and are no longer excluded at all: selection is honoured only for htmx requests, which
// cache nothing, so on an ordinary request a fragment is ordinary content.
public sealed class Issue219CacheFragmentTests
{
	[Fact]
	public async Task A_conditional_component_that_opted_out_still_raises_the_frameworks_refusal()
	{
		await using var stock = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalRefusalPage>(htmxor: false);
		await using var candidate = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalRefusalPage>(htmxor: true);

		// A parity claim, and a weaker one than it used to be. While Htmxor discarded a capture holding an
		// IConditionalRender, this pinned the ordering: stock's refusal had to be consulted before the discard,
		// or an application type that was both would render where stock refuses it. That kind is gone, so
		// nothing pre-empts the refusal and reversing the ordering now reddens nothing -- measured. What it
		// still asserts is that Htmxor passes a CacheBehavior.Throw refusal through to the client unchanged.
		var expected = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(stock));
		var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(candidate));

		Assert.Contains("cannot be used inside a CacheView", expected.Message, StringComparison.Ordinal);
		Assert.Equal(expected.Message, actual.Message);
	}

	[Fact]
	public async Task A_keyed_boundary_is_not_served_another_keys_entry()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219KeyedBoundaryPage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// Ordinary requests, where caching still happens. One CacheView per request at one tree position, so the
		// @key value is the only thing telling two items apart: same frame sequence, same parent component type,
		// no CacheKey. Htmxor formats that value into the tree position key itself, and nothing else covers it.
		var itemA = await ReadAsync(client, "/issue-219/keyed?id=a");
		Assert.Contains("data-item=\"a\" data-version=\"1\"", itemA, StringComparison.Ordinal);

		data.Version = 2;

		// Item b must render itself rather than replay item a. Losing the key discriminator is silent here,
		// which is what makes it worth a case: the response is a well-formed 200 carrying the wrong item.
		var itemB = await ReadAsync(client, "/issue-219/keyed?id=b");
		Assert.Contains("data-item=\"b\" data-version=\"2\"", itemB, StringComparison.Ordinal);
		Assert.DoesNotContain("data-item=\"a\"", itemB, StringComparison.Ordinal);

		// And the entries are real rather than merely absent: item a still reads its stored body.
		Assert.Contains("data-item=\"a\" data-version=\"1\"", await ReadAsync(client, "/issue-219/keyed?id=a"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task An_htmx_request_is_not_cached_and_is_not_served_an_ordinary_entry()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219HtmxCachePage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// The contract this increment ships: an htmx request stores nothing, anywhere. Htmxor cannot know what
		// a subtree read from the request, guessing wrongly serves one request another's markup, and every
		// narrower rule tried against that produced a further instance of the same defect. Caching htmx
		// responses is deferred whole rather than shipped partly working.
		Assert.Contains("data-version=\"1\"", await HtmxAsync(client, "/issue-219/htmx-cache"), StringComparison.Ordinal);
		data.Version = 2;
		Assert.Contains("data-version=\"2\"", await HtmxAsync(client, "/issue-219/htmx-cache"), StringComparison.Ordinal);

		// And the other half, which storing nothing does not give on its own: an ordinary request does store,
		// so the representation must keep the two apart or the htmx request is served what it left behind.
		data.Version = 3;
		Assert.Contains("data-version=\"3\"", await ReadAsync(client, "/issue-219/htmx-cache"), StringComparison.Ordinal);
		data.Version = 4;
		Assert.Contains("data-version=\"3\"", await ReadAsync(client, "/issue-219/htmx-cache"), StringComparison.Ordinal);
		Assert.Contains("data-version=\"4\"", await HtmxAsync(client, "/issue-219/htmx-cache"), StringComparison.Ordinal);
	}

	private static async Task<string> HtmxAsync(HttpClient client, string path)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	[Fact]
	public async Task A_boundary_beneath_a_named_fragment_caches_like_stock()
	{
		// goal.md advertises ordinary caching parity "including beneath a named fragment", and the guide says a
		// boundary holding *or beneath* one caches normally. Every other fragment fixture puts the fragment
		// inside the boundary; the one that did this direction was orphaned and deleted, leaving the advertised
		// half of the claim unmeasured. Paired, because the claim is parity: the stock arm establishes what the
		// framework does with the same tree when HtmxFragment is just a component, which on an ordinary request
		// is all it is -- selection is honoured only for htmx requests, which cache nothing.
		var expected = await ReadPairAsync<Issue219FragmentAboveBoundaryPage>(htmxor: false);
		var actual = await ReadPairAsync<Issue219FragmentAboveBoundaryPage>(htmxor: true);

		// The stock arm must prove reuse, not merely agree: without this the case passes when neither host
		// caches, which is the regression it exists to catch.
		Assert.Contains("data-version=\"1\"", expected[0], StringComparison.Ordinal);
		Assert.Contains("data-version=\"1\"", expected[1], StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	private static async Task<string[]> ReadPairAsync<TRoot>(bool htmxor) where TRoot : IComponent
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<TRoot>(htmxor);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		var first = await ReadBodyAsync(client, "/issue-219/fragment-above-boundary");
		data.Version = 2;
		return [first, await ReadBodyAsync(client, "/issue-219/fragment-above-boundary")];
	}

	private static async Task<string> ReadBodyAsync(HttpClient client, string path)
	{
		using var response = await client.GetAsync(path);
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

// An ordinary boundary on a page reachable by both kinds of request.
[Route("/issue-219/htmx-cache")]
public sealed class Issue219HtmxCachePage : ComponentBase
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

// One boundary per request at one tree position, told apart only by @key. This is the shape in which losing
// the key discriminator is silent: same frame sequence, same parent type, so the key value is the entire
// discriminator and the second item would be served the first's markup rather than raising anything.
[Route("/issue-219/keyed")]
public sealed class Issue219KeyedBoundaryPage : ComponentBase
{
	[Inject] internal Issue219Data Data { get; set; } = default!;

	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var item = HttpContext.Request.Query["id"].ToString();
		builder.OpenComponent<CacheView>(0);
		builder.SetKey(item);
		builder.AddAttribute(1, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenElement(0, "p");
			cached.AddAttribute(1, "data-item", item);
			cached.AddAttribute(2, "data-version", Data.Version);
			cached.CloseElement();
		}));
		builder.CloseComponent();
	}
}


// A named HtmxFragment standing *above* a cached boundary: the direction goal.md advertises and no other
// fixture composes. On an ordinary request nothing selects the fragment, so the boundary beneath it is
// ordinary cached content.
[Route("/issue-219/fragment-above-boundary")]
public sealed class Issue219FragmentAboveBoundaryPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<HtmxFragment>(0);
		builder.AddAttribute(1, nameof(HtmxFragment.Name), "above");
		builder.AddAttribute(2, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
		{
			inner.OpenComponent<CacheView>(0);
			inner.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-fragment-above-boundary");
			inner.AddAttribute(2, nameof(CacheView.ChildContent), Issue219Slot.Render("beneath-fragment"));
			inner.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
