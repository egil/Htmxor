using System.Net;
using static Htmxor.AspNetCore10.Issue189FormAssertions;

namespace Htmxor.AspNetCore10;

public sealed class Issue189TokenTimingTests
{
	[Theory]
	[InlineData(Issue189HostPair.FormPath, false)]
	[InlineData(Issue189HostPair.FormPath, true)]
	[InlineData(Issue187ParityConstants.RequestPath, false)]
	[InlineData(Issue187ParityConstants.RequestPath, true)]
	public async Task Non_streaming_GET_preserves_token_generation_and_storage_timing(string path, bool existingCookie)
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => CreateGet(path, tokens, existingCookie));

		var stock = pair.Observe(pair.Stock, "get");
		var candidate = pair.Observe(pair.Candidate, "get");
		CandidateReached(candidate);
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Equal(TokenOperations(stock), TokenOperations(candidate));
		Assert.Equal(stock.GeneratedTokens.Distinct(), responses.Stock.Tokens);
		Assert.Equal(candidate.GeneratedTokens.Distinct(), responses.Candidate.Tokens);
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(stock, candidate);
	}

	[Fact]
	public async Task Non_streaming_POST_preserves_token_generation_and_storage_timing()
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			Issue189HostPair.FormPath, "post", tokens, Form()));

		CandidateReached(pair.Observe(pair.Candidate, "post"));
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Equal(TokenOperations(pair.Observe(pair.Stock, "post")), TokenOperations(pair.Observe(pair.Candidate, "post")));
		Assert.Equal(pair.Observe(pair.Stock, "post").GeneratedTokens.Distinct(), responses.Stock.Tokens);
		Assert.Equal(pair.Observe(pair.Candidate, "post").GeneratedTokens.Distinct(), responses.Candidate.Tokens);
		EqualResponse(responses.Stock, responses.Candidate);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Completed_non_form_response_preserves_late_token_availability_for_configured_render_modes(bool configureServer)
	{
		await using var pair = await Issue189HostPair.CreateAsync(configureServerRenderMode: configureServer);

		var responses = await SendAsync(pair, CreateLateTokenRequest);

		var stock = pair.Observe(pair.Stock, "late-token");
		var candidate = pair.Observe(pair.Candidate, "late-token");
		CandidateReached(candidate);
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Contains("late-response-started:True", stock.Operations);
		Assert.Contains("late-response-started:True", candidate.Operations);
		Assert.Contains($"late-token:{configureServer}", stock.Operations);
		// Configured modes also emit persisted state; complete response/timing parity for that belongs to #191.
		Assert.Contains($"late-token:{configureServer}", candidate.Operations);
		Assert.Equal(HttpStatusCode.OK, responses.Candidate.Status);
		Assert.Contains("ordinary-output", responses.Stock.Body, StringComparison.Ordinal);
		Assert.Contains("ordinary-output", responses.Candidate.Body, StringComparison.Ordinal);
	}

	private static HttpRequestMessage CreateLateTokenRequest()
	{
		var request = Issue189HostPair.Request(HttpMethod.Get, Issue187ParityConstants.RequestPath, "late-token");
		request.Headers.Add(Issue187AuthenticationHandler.UserHeaderName, Issue187ParityConstants.AuthorizedUser);
		request.Headers.Add("X-Issue-189-Late-Token", "true");
		return request;
	}

	private static string[] TokenOperations(Issue189Observation observation)
		=> observation.Operations.Where(operation => operation.Contains(":started=", StringComparison.Ordinal)).ToArray();

	private static HttpRequestMessage CreateGet(string path, Issue189Tokens tokens, bool existingCookie)
	{
		var request = Issue189HostPair.Request(HttpMethod.Get, path, "get");
		request.Headers.Add(Issue187AuthenticationHandler.UserHeaderName, Issue187ParityConstants.AuthorizedUser);
		if (existingCookie)
		{
			request.Headers.Add("Cookie", tokens.Cookie);
		}

		return request;
	}
}
