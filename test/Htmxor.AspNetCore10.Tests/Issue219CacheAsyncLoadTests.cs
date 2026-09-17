#if NET11_0_OR_GREATER
using System.Net;
using Htmxor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue219CacheAsyncLoadTests
{
	[Fact]
	public async Task An_async_load_placeholder_carries_the_path_of_the_request_that_asked_for_it()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219AsyncLoadRoutePage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		// Ordinary requests, one page, two routes. HtmxAsyncLoad writes hx-get="{request path}" into its
		// placeholder while rendering, and no path reaches a cache key unless the application declared
		// VaryByRoute. Replaying the placeholder therefore points the load trigger at the other route: the
		// response is a well-formed 200 and htmx swaps the first route's content into the second page.
		var first = await ReadAsync(client, "/issue-219/async-route/a");
		Assert.Contains("hx-get=\"/issue-219/async-route/a\"", first, StringComparison.Ordinal);

		data.Version = 2;
		var second = await ReadAsync(client, "/issue-219/async-route/b");

		Assert.Contains("hx-get=\"/issue-219/async-route/b\"", second, StringComparison.Ordinal);
		Assert.DoesNotContain("hx-get=\"/issue-219/async-route/a\"", second, StringComparison.Ordinal);

		// Deliberately no assertion about the content beside it. Whether the surrounding boundary still caches
		// depends on which mechanism excludes HtmxAsyncLoad, and the narrow one is unavailable: marking the
		// component CacheBehavior.Rerender makes the framework refuse it outright, because its required
		// ChildContent is a RenderFragment a live cached component cannot capture -- measured, every request
		// 500s. Classifying the component as request-varying works and stores nothing for the boundary, which
		// is less caching and the safe direction. This case pins the outcome an application depends on either
		// way, and stays true if a narrower mechanism arrives later.
	}

	[Fact]
	public async Task A_boundary_inside_an_async_loads_loading_content_stores_nothing()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219AsyncLoadingHostPage>(htmxor: true);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		Assert.Contains("data-version=\"1\"", await ReadAsync(client, "/issue-219/async-loading"), StringComparison.Ordinal);
		data.Version = 2;

		// The boundary is authored in the page and passed into HtmxAsyncLoad as Loading content, which renders
		// on every ordinary request, so this is the shape in which the physical and authoring ancestor chains
		// were expected to disagree. They do not: swapping HasUncacheableAncestor to walk
		// LogicalParentComponentState leaves this case green, so whatever stops the boundary caching here is
		// not the choice of chain. The case is kept for the behaviour it does pin -- a boundary beneath an
		// async load stores nothing -- and not as evidence about the walk.
		Assert.Contains("data-version=\"2\"", await ReadAsync(client, "/issue-219/async-loading"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task A_boundary_that_only_sometimes_holds_an_async_load_reuses_like_stock()
	{
		// Raised on the pull request: a boundary that holds HtmxAsyncLoad only on some requests can serve an
		// entry stored by an earlier request that did not hold one, because the holding-direction discard runs
		// while the inner component renders and a cache hit renders nothing.
		//
		// The mechanism is real and it is stock's. Both hosts store at version 1, and at version 2 both serve
		// that entry, so the async load the application would now render never appears on either. Paired
		// because the only question is whether Htmxor diverges; the answer is what AC1 protects, not a defect
		// Htmxor can fix -- excluding a kind that never renders would require deciding it before the hit.
		var expected = await ReadPairAsync(htmxor: false);
		var actual = await ReadPairAsync(htmxor: true);

		// The stock arm must show the hit, or this passes when neither host cached.
		Assert.Contains("data-version=\"1\"", expected[1], StringComparison.Ordinal);
		Assert.DoesNotContain("hx-get", expected[1], StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	private static async Task<string[]> ReadPairAsync(bool htmxor)
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219ConditionalAsyncLoadPage>(htmxor);
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();

		data.Version = 1;
		var first = await ReadAsync(client, "/issue-219/conditional-async-load");
		data.Version = 2;
		return [first, await ReadAsync(client, "/issue-219/conditional-async-load")];
	}

	private static async Task<string> ReadAsync(HttpClient client, string path)
	{
		using var response = await client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}
}

// One page at two routes, holding an HtmxAsyncLoad beside ordinary cached content.
[Route("/issue-219/async-route/{slug}")]
public sealed class Issue219AsyncLoadRoutePage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-async-route");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219CachedContent>(0);
			cached.CloseComponent();
			cached.OpenComponent<HtmxAsyncLoad>(1);
			cached.AddAttribute(2, nameof(HtmxAsyncLoad.Id), "lazy");
			cached.AddAttribute(3, nameof(HtmxAsyncLoad.ChildContent), (RenderFragment)(loaded =>
			{
				loaded.OpenElement(0, "p");
				loaded.AddAttribute(1, "data-async-child", "true");
				loaded.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
// A CacheView authored in the page and passed into HtmxAsyncLoad as its Loading content. This was built
// expecting the physical and authoring ancestor chains to disagree about what stands above it; they do not --
// for a component passed as a RenderFragment parameter both parents are the HtmxAsyncLoad, measured.
[Route("/issue-219/async-loading")]
public sealed class Issue219AsyncLoadingHostPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<HtmxAsyncLoad>(0);
		builder.AddAttribute(1, nameof(HtmxAsyncLoad.Id), "lazy");
		builder.AddAttribute(2, nameof(HtmxAsyncLoad.ChildContent), (RenderFragment)(loaded =>
		{
			loaded.OpenElement(0, "p");
			loaded.AddAttribute(1, "data-async-child", "true");
			loaded.CloseElement();
		}));
		builder.AddAttribute(3, nameof(HtmxAsyncLoad.Loading), (RenderFragment)(loading =>
		{
			loading.OpenComponent<CacheView>(0);
			loading.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-async-loading");
			loading.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
			{
				cached.OpenComponent<Issue219CachedContent>(0);
				cached.CloseComponent();
			}));
			loading.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

// A boundary whose child holds an HtmxAsyncLoad only from version 2 on, so the first request stores a tree
// with no excluded kind in it and the second would have held one.
[Route("/issue-219/conditional-async-load")]
public sealed class Issue219ConditionalAsyncLoadPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-conditional-async-load");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<Issue219ConditionalAsyncLoadContent>(0);
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219ConditionalAsyncLoadContent : ComponentBase
{
	[Inject] internal Issue219Data Data { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		builder.AddAttribute(1, "data-version", Data.Version.ToString(System.Globalization.CultureInfo.InvariantCulture));
		builder.AddContent(2, "body");
		builder.CloseElement();

		if (Data.Version >= 2)
		{
			builder.OpenComponent<HtmxAsyncLoad>(3);
			builder.AddAttribute(4, nameof(HtmxAsyncLoad.Id), "late");
			builder.AddAttribute(5, nameof(HtmxAsyncLoad.ChildContent), (RenderFragment)(inner => inner.AddContent(0, "late")));
			builder.CloseComponent();
		}
	}
}
#endif
