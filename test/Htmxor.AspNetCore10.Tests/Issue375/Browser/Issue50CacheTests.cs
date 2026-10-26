using System.Net;
using Xunit;

namespace Htmxor.AspNetCore10.Issue375.Browser;

public sealed class Issue50CacheTests
{
	private const string Issue50Path = "/issue-50/cache";
	private static readonly CacheRepresentation[] Issue50Representations =
	[
		new("normal", "normal", RequestType: null),
		new("whole-partial", "whole", "partial"),
		new("alpha-partial", "alpha", "partial"),
		new("beta-partial", "beta", "partial"),
		new("alpha-full", "alpha", "full"),
	];
	[Fact]
	public async Task Output_cache_keeps_normal_direct_whole_and_named_fragment_representations_distinct()
	{
		foreach (var alphaFirst in new[] { "alpha-full", "alpha-partial" })
		{
			var probe = new Issue50CacheProbe();
			await using var app = NamedSelectionHost.Create(cacheProbe: probe);
			await app.StartAsync();
			using var client = new HttpClient { BaseAddress = new Uri(Assert.Single(app.Urls)) };

			foreach (var representation in Issue50Representations
				.OrderBy(representation => representation.Label == alphaFirst ? 0 : 1))
			{
				await AssertCachedRepresentationAsync(client, representation);
			}

			Assert.Equal(Issue50Representations.Length, probe.RenderCount);
		}
	}


	private static async Task AssertCachedRepresentationAsync(HttpClient client, CacheRepresentation representation)
	{
		using var first = await SendCacheRequestAsync(client, representation);
		using var second = await SendCacheRequestAsync(client, representation);
		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);
		Assert.False(first.Headers.Contains("Set-Cookie"));
		Assert.False(second.Headers.Contains("Set-Cookie"));
		var firstBody = await first.Content.ReadAsStringAsync();
		var secondBody = await second.Content.ReadAsStringAsync();
		Assert.Equal(firstBody, secondBody);
		Assert.Equal(
			representation.RequestType is not "partial",
			firstBody.Contains("data-stock-shell", StringComparison.Ordinal));
		Assert.Contains($"data-representation=\"{representation.Selection}\"", firstBody, StringComparison.Ordinal);
		if (representation is { RequestType: "partial", Selection: "alpha" or "beta" })
		{
			Assert.Contains(
				$"data-issue-50-fragment=\"{representation.Selection}\"",
				firstBody,
				StringComparison.Ordinal);
		}
		else
		{
			Assert.Contains("data-issue-50-cache", firstBody, StringComparison.Ordinal);
		}
	}

	private static async Task<HttpResponseMessage> SendCacheRequestAsync(
		HttpClient client,
		CacheRepresentation representation)
	{
		using var request = new HttpRequestMessage(
			HttpMethod.Get,
			$"{Issue50Path}?representation={representation.Selection}");
		if (representation.RequestType is not null)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", representation.RequestType);
		}

		return await client.SendAsync(request);
	}

	private sealed record CacheRepresentation(string Label, string Selection, string? RequestType);

}
