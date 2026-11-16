using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

/// <summary>
/// When an application adds <c>VaryByHtmx()</c> to an output-cache policy, a URL's full and direct
/// representations are cached and served separately, and every variation the policy already declared
/// still applies.
/// </summary>
public sealed class Issue295VaryByHtmxTests
{
	private const string Path = "/issue-295/cached";

	public static TheoryData<bool> HtmxFirst => new() { false, true };

	[Theory]
	[MemberData(nameof(HtmxFirst))]
	public async Task Without_VaryByHtmx_the_output_cache_serves_one_entry_to_both_representations(bool htmxFirst)
	{
		await using var app = await StartAsync(static policy => policy);
		using var client = app.GetTestClient();

		var first = await GetAsync(client, Path, htmx: htmxFirst);
		var second = await GetAsync(client, Path, htmx: !htmxFirst);

		Assert.Equal(first, second);
		Assert.Equal(1, app.Services.GetRequiredService<Issue295Probe>().RenderCount);
	}

	[Theory]
	[MemberData(nameof(HtmxFirst))]
	public async Task VaryByHtmx_caches_and_serves_each_representation_from_its_own_entry(bool htmxFirst)
	{
		await using var app = await StartAsync(static policy => policy.VaryByHtmx());
		using var client = app.GetTestClient();

		var first = await GetAsync(client, Path, htmx: htmxFirst);
		var second = await GetAsync(client, Path, htmx: !htmxFirst);
		var firstAgain = await GetAsync(client, Path, htmx: htmxFirst);
		var secondAgain = await GetAsync(client, Path, htmx: !htmxFirst);

		var (full, direct) = htmxFirst ? (second, first) : (first, second);
		Assert.Contains("<html", full, StringComparison.Ordinal);
		Assert.DoesNotContain("<html", direct, StringComparison.Ordinal);
		Assert.Contains("data-issue-295-render", direct, StringComparison.Ordinal);
		Assert.Equal(first, firstAgain);
		Assert.Equal(second, secondAgain);
		Assert.Equal(2, app.Services.GetRequiredService<Issue295Probe>().RenderCount);
	}

	[Fact]
	public async Task VaryByHtmx_separates_htmx_full_and_partial_request_types()
	{
		await using var app = await StartAsync(static policy => policy.VaryByHtmx());
		using var client = app.GetTestClient();

		var partial = await GetAsync(client, Path, htmx: true, requestType: "partial");
		var full = await GetAsync(client, Path, htmx: true, requestType: "full");

		Assert.Equal(partial, await GetAsync(client, Path, htmx: true, requestType: "partial"));
		Assert.Equal(full, await GetAsync(client, Path, htmx: true, requestType: "full"));
		Assert.Equal(2, app.Services.GetRequiredService<Issue295Probe>().RenderCount);
	}

	public static TheoryData<string> PreservedHeaderPolicies => new() { "before", "after" };

	[Theory]
	[MemberData(nameof(PreservedHeaderPolicies))]
	public async Task VaryByHtmx_preserves_the_policy_header_variation(string order)
	{
		await using var app = await StartAsync(order == "before"
			? static policy => policy.SetVaryByHeader("Accept-Language").VaryByHtmx()
			: static policy => policy.VaryByHtmx().SetVaryByHeader("Accept-Language"));
		using var client = app.GetTestClient();

		var responses = await GetEachTwiceAsync(client, Path, ["en", "da"], static (request, language) =>
			request.Headers.Add("Accept-Language", language));

		Assert.All(responses, pair => Assert.Equal(pair.First, pair.Second));
		Assert.Equal(4, responses.Select(pair => pair.First).Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(4, app.Services.GetRequiredService<Issue295Probe>().RenderCount);
	}

	[Fact]
	public async Task VaryByHtmx_preserves_the_policy_query_variation()
	{
		await using var app = await StartAsync(static policy => policy.SetVaryByQuery("page").VaryByHtmx());
		using var client = app.GetTestClient();

		var responses = await GetEachTwiceAsync(client, Path, ["1", "2"], static (request, page) =>
			request.RequestUri = new Uri($"{Path}?page={page}", UriKind.Relative));

		Assert.All(responses, pair => Assert.Equal(pair.First, pair.Second));
		Assert.Equal(4, responses.Select(pair => pair.First).Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(4, app.Services.GetRequiredService<Issue295Probe>().RenderCount);
	}

	[Fact]
	public void VaryByHtmx_rejects_a_null_builder()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => ((OutputCachePolicyBuilder)null!).VaryByHtmx());
		Assert.Equal("builder", exception.ParamName);
	}

	private static async Task<List<(string First, string Second)>> GetEachTwiceAsync(
		HttpClient client,
		string path,
		string[] values,
		Action<HttpRequestMessage, string> vary)
	{
		var cells = values.SelectMany(value => new[] { (value, htmx: false), (value, htmx: true) }).ToArray();
		var first = new List<string>();
		foreach (var (value, htmx) in cells)
		{
			first.Add(await GetAsync(client, path, htmx, configure: request => vary(request, value)));
		}

		var pairs = new List<(string, string)>();
		for (var i = 0; i < cells.Length; i++)
		{
			var (value, htmx) = cells[i];
			pairs.Add((first[i], await GetAsync(client, path, htmx, configure: request => vary(request, value))));
		}

		return pairs;
	}

	private static async Task<string> GetAsync(
		HttpClient client,
		string path,
		bool htmx,
		string requestType = "partial",
		Action<HttpRequestMessage>? configure = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		if (htmx)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", requestType);
		}

		configure?.Invoke(request);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, $"Received {(int)response.StatusCode}. Body: {body}");
		return body;
	}

	private static async Task<WebApplication> StartAsync(Func<OutputCachePolicyBuilder, OutputCachePolicyBuilder> policy)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue295VaryByHtmxTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Production,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton<Issue295Probe>();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddOutputCache(options => options.AddBasePolicy(builder => policy(builder)));

		var app = builder.Build();
		app.UseAntiforgery();
		app.UseOutputCache();
		app.MapRazorComponents<Issue78App>().AddHtmxorEndpoints();
		await app.StartAsync();
		return app;
	}
}

internal sealed class Issue295Probe
{
	private int renderCount;

	public int RenderCount => Volatile.Read(ref renderCount);

	public int RecordRender() => Interlocked.Increment(ref renderCount);
}
