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
