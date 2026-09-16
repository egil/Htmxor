#if NET11_0_OR_GREATER
using System.Net;
using Htmxor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Htmxor.AspNetCore10;

// HtmxAsyncLoad is IConditionalRender: it decides its own markup from the request's Source/Target element
// identity, which neither Htmxor's representation key nor the framework's cache key carries. A CacheView
// wrapping it must abandon its capture, or one request's element-identity decision gets replayed for another.
public sealed class Issue219CacheAsyncLoadTests
{
	[Fact]
	public async Task A_cached_async_load_boundary_never_replays_another_requests_element_identity_decision()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219AsyncLoadPage>(htmxor: true);
		using var client = app.GetTestClient();

		// Same URL, same representation (IsHtmxRequest=true, RoutingMode.Direct) for both requests, differing
		// only in the untrusted Source/Target element identity headers a stored entry cannot carry.
		var matching = await ReadAsync(client, elementIdentity: "div#lazy");
		Assert.Contains("data-async-child", matching, StringComparison.Ordinal);

		var mismatched = await ReadAsync(client, elementIdentity: "div#other");
		Assert.DoesNotContain("data-async-child", mismatched, StringComparison.Ordinal);
	}

	private static async Task<string> ReadAsync(HttpClient client, string elementIdentity)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/async-load");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("HX-Source", elementIdentity);
		request.Headers.Add("HX-Target", elementIdentity);
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}
}

[Route("/issue-219/async-load")]
public sealed class Issue219AsyncLoadPage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-async-load");
		builder.AddAttribute(5, nameof(CacheView.VaryBy), "issue-219-async-load");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			cached.OpenComponent<HtmxAsyncLoad>(0);
			cached.AddAttribute(1, nameof(HtmxAsyncLoad.Id), "lazy");
			cached.AddAttribute(2, nameof(HtmxAsyncLoad.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddAttribute(1, "data-async-child", "true");
				inner.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
