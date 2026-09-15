#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

public sealed class Issue219CacheViewTests
{
	[Fact]
	public async Task Ordinary_cache_hit_preserves_the_stock_data_version()
	{
		await using var stock = await CreateHostAsync(false);
		await using var candidate = await CreateHostAsync(true);

		var expected = await ReadMissAndHitAsync(stock);
		var actual = await ReadMissAndHitAsync(candidate);

		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task Configured_query_variation_keeps_distinct_responses_isolated()
	{
		await using var stock = await CreateHostAsync(false);
		await using var candidate = await CreateHostAsync(true);

		var expected = await ReadVariantsAsync(stock);
		var actual = await ReadVariantsAsync(candidate);

		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task Cached_subtree_runs_its_component_once_and_is_reused_afterwards()
	{
		await using var candidate = await CreateHostAsync(true);
		using var client = candidate.GetTestClient();
		var data = candidate.Services.GetRequiredService<Issue219Data>();

		await client.GetAsync("/issue-219/cache");
		var afterMiss = data.Renders;
		await client.GetAsync("/issue-219/cache");
		await client.GetAsync("/issue-219/cache");

		// A hit must reuse stored output rather than invoking the cached component again.
		Assert.Equal(1, afterMiss);
		Assert.Equal(1, data.Renders);
	}

	[Fact]
	public async Task A_variant_not_yet_cached_observes_the_current_data()
	{
		await using var stock = await CreateHostAsync(false);
		await using var candidate = await CreateHostAsync(true);

		// Negative control: the reuse assertions must not pass merely because the page never re-renders.
		var expected = await ReadFreshVariantAsync(stock);
		var actual = await ReadFreshVariantAsync(candidate);

		Assert.Contains("data-version=\"2\"", actual, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	private static async Task<string> ReadFreshVariantAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		await ReadAsync(client, "/issue-219/cache?tenant=alpha");
		data.Version = 2;
		return await ReadAsync(client, "/issue-219/cache?tenant=gamma");
	}

	private static async Task<string[]> ReadVariantsAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		var alpha = await ReadAsync(client, "/issue-219/cache?tenant=alpha");
		var beta = await ReadAsync(client, "/issue-219/cache?tenant=beta");
		data.Version = 2;
		return [alpha, beta, await ReadAsync(client, "/issue-219/cache?tenant=alpha"), await ReadAsync(client, "/issue-219/cache?tenant=beta")];
	}

	private static async Task<string> ReadAsync(HttpClient client, string path)
	{
		using var response = await client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		return await response.Content.ReadAsStringAsync();
	}

	private static async Task<string[]> ReadMissAndHitAsync(WebApplication app)
	{
		using var client = app.GetTestClient();
		var data = app.Services.GetRequiredService<Issue219Data>();
		using var miss = await client.GetAsync("/issue-219/cache");
		Assert.Equal(HttpStatusCode.OK, miss.StatusCode);
		var first = await miss.Content.ReadAsStringAsync();
		Assert.Contains("data-version=\"1\"", first, StringComparison.Ordinal);

		data.Version = 2;
		using var hit = await client.GetAsync("/issue-219/cache");
		Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
		var second = await hit.Content.ReadAsStringAsync();
		Assert.Equal(first, second);
		return [first, second];
	}

	private static async Task<WebApplication> CreateHostAsync(bool htmxor)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue219CachePage).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue219Data>();
		var components = builder.Services.AddRazorComponents();
		if (htmxor)
		{
			components.AddHtmxor();
		}

		var app = builder.Build();
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue219CachePage>();
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return app;
	}
}

internal sealed class Issue219Data
{
	public int Version { get; set; } = 1;

	public int Renders { get; set; }
}

[Route("/issue-219/cache")]
public sealed class Issue219CachePage : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219");
		builder.AddAttribute(2, nameof(CacheView.VaryByQuery), "tenant");
		builder.AddAttribute(3, nameof(CacheView.ChildContent), (RenderFragment)(content =>
		{
			content.OpenComponent<Issue219CachedContent>(0);
			content.CloseComponent();
		}));
		builder.CloseComponent();
	}
}

public sealed class Issue219CachedContent : ComponentBase
{
	[Inject] internal Issue219Data Data { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "p");
		Data.Renders++;
		builder.AddAttribute(1, "data-version", Data.Version);
		builder.AddContent(2, "cached application data");
		builder.CloseElement();
	}
}
#endif
