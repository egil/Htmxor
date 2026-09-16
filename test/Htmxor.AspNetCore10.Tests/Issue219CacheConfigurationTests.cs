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
