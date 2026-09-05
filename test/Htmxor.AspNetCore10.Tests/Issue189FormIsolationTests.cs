using Microsoft.Extensions.DependencyInjection;
using static Htmxor.AspNetCore10.Issue189FormAssertions;

namespace Htmxor.AspNetCore10;

public sealed class Issue189FormIsolationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Sequential_and_overlapping_requests_isolate_forms_scopes_validation_and_instances(bool overlap)
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var stock = await SendTwoAsync(pair.Stock, tokens, overlap);
		var candidate = await SendTwoAsync(pair.Candidate, tokens, overlap);

		CandidateReached(pair.Observe(pair.Candidate, "first"));
		CandidateReached(pair.Observe(pair.Candidate, "second"));
		Assert.Contains("data-result=\"invalid\"", stock[0].Body, StringComparison.Ordinal);
		Assert.Contains("The Name field is required.", stock[0].Body, StringComparison.Ordinal);
		Assert.Contains("data-result=\"valid\"", stock[1].Body, StringComparison.Ordinal);
		Assert.DoesNotContain("The Name field is required.", stock[1].Body, StringComparison.Ordinal);
		AssertIsolation(pair, pair.Stock);
		AssertGate(pair.Stock, overlap);
		AssertGate(pair.Candidate, overlap);
		EqualResponse(stock[0], candidate[0]);
		EqualResponse(stock[1], candidate[1]);
		AssertIsolation(pair, pair.Candidate);
		EqualComponents(pair.Observe(pair.Stock, "first"), pair.Observe(pair.Candidate, "first"));
		EqualComponents(pair.Observe(pair.Stock, "second"), pair.Observe(pair.Candidate, "second"));
	}

	private static async Task<Issue189Response[]> SendTwoAsync(Issue187ParityHost host, Issue189Tokens tokens, bool overlap)
	{
		var first = SendAsync(host, tokens, "first", Form("", "not-an-age", "[left]save"), overlap);
		if (!overlap)
		{
			await first;
		}

		var second = SendAsync(host, tokens, "second", Form("Lin", "42", "[right]save"), overlap);
		return await Task.WhenAll(first, second);
	}

	private static async Task<Issue189Response> SendAsync(
		Issue187ParityHost host, Issue189Tokens tokens, string requestId, KeyValuePair<string, string>[] fields, bool overlap)
	{
		using var request = Issue189HostPair.Post("/issue-189/scopes/right", requestId, tokens, fields);
		if (overlap)
		{
			request.Headers.Add("X-Issue-189-Overlap", "true");
		}

		using var response = await host.Client.SendAsync(request);
		return await Issue189Response.ReadAsync(response);
	}

	private static void AssertGate(Issue187ParityHost host, bool overlap)
		=> Assert.Equal(overlap ? 2 : 0, host.App.Services.GetRequiredService<Issue189OverlapGate>().RequestCount);

	private static void AssertIsolation(Issue189HostPair pair, Issue187ParityHost host)
	{
		var first = pair.Observe(host, "first");
		var second = pair.Observe(host, "second");
		var firstCallback = Assert.Single(first.Components, item => item.Phase == "invalid");
		var secondCallback = Assert.Single(second.Components, item => item.Phase == "valid");
		Assert.Equal("left", firstCallback.Label);
		Assert.Equal("right", secondCallback.Label);
		Assert.Equal("Lin", secondCallback.Value);
		Assert.Empty(first.Components.Select(item => item.Instance).Intersect(second.Components.Select(item => item.Instance)));
		RequestInstanceHandled(first, "invalid");
		RequestInstanceHandled(second, "valid");
	}
}
