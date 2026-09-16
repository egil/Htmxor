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
