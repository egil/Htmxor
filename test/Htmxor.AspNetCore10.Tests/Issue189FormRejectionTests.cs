using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using static Htmxor.AspNetCore10.Issue189FormAssertions;

namespace Htmxor.AspNetCore10;

public sealed class Issue189FormRejectionTests
{
	public static TheoryData<string, bool, string, bool> AntiforgeryCases => new()
	{
		{ Environments.Development, false, "missing", false },
		{ Environments.Development, false, "invalid", false },
		{ Environments.Production, false, "missing", false },
		{ Environments.Production, false, "invalid", false },
		{ Environments.Production, true, "missing", false },
		{ Environments.Production, true, "invalid", false },
		{ Environments.Development, false, "missing", true },
		{ Environments.Development, false, "invalid", true },
		{ Environments.Production, false, "missing", true },
		{ Environments.Production, false, "invalid", true },
		{ Environments.Production, true, "missing", true },
		{ Environments.Production, true, "invalid", true },
	};

	[Theory]
	[MemberData(nameof(AntiforgeryCases))]
	public async Task Missing_or_invalid_token_rejects_before_binding_with_stock_details(
		string environment, bool detailedErrors, string tokenState, bool invokerValidation)
	{
		await using var pair = await Issue189HostPair.CreateAsync(environment, detailedErrors, invokerValidation);
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => RejectedPost(tokens, tokenState));

		var stock = pair.Observe(pair.Stock, "antiforgery");
		var candidate = pair.Observe(pair.Candidate, "antiforgery");
		CandidateReached(candidate);
		Assert.Equal(HttpStatusCode.BadRequest, responses.Stock.Status);
		Assert.Equal(environment == Environments.Development || detailedErrors, responses.Stock.Body.Length > 0);
		Assert.Contains("middleware-feature:False", stock.Operations);
		Assert.Contains("middleware-feature:False", candidate.Operations);
		RejectedBeforeBinding(stock);
		EqualResponse(responses.Stock, responses.Candidate);
		RejectedBeforeBinding(candidate);
		Assert.Equal(stock.Operations.Contains("invoker-validation:False"), candidate.Operations.Contains("invoker-validation:False"));
	}

	[Theory]
	[InlineData("Development", false, "application/json")]
	[InlineData("Development", false, "text/plain")]
	[InlineData("Production", false, "application/json")]
	[InlineData("Production", true, "application/json")]
	public async Task Unsupported_content_type_preserves_rejection_and_detail_policy(
		string environment, bool detailedErrors, string contentType)
	{
		await using var pair = await Issue189HostPair.CreateAsync(environment, detailedErrors);
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => UnsupportedPost(tokens, contentType));

		CandidateReached(pair.Observe(pair.Candidate, "content-type"));
		Assert.Equal(HttpStatusCode.BadRequest, responses.Stock.Status);
		Assert.Equal(environment == Environments.Development || detailedErrors, responses.Stock.Body.Length > 0);
		RejectedBeforeBinding(pair.Observe(pair.Stock, "content-type"));
		EqualResponse(responses.Stock, responses.Candidate);
		RejectedBeforeBinding(pair.Observe(pair.Candidate, "content-type"));
	}

	[Theory]
	[InlineData("Development", false)]
	[InlineData("Production", false)]
	[InlineData("Production", true)]
	public async Task Duplicate_handler_values_reject_before_components(string environment, bool detailedErrors)
	{
		await using var pair = await Issue189HostPair.CreateAsync(environment, detailedErrors);
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			Issue189HostPair.FormPath, "duplicate", tokens, Form().Append(new("_handler", "save"))));

		CandidateReached(pair.Observe(pair.Candidate, "duplicate"));
		Assert.Equal(HttpStatusCode.BadRequest, responses.Stock.Status);
		Assert.Empty(responses.Stock.Body);
		RejectedBeforeBinding(pair.Observe(pair.Stock, "duplicate"));
		EqualResponse(responses.Stock, responses.Candidate);
		RejectedBeforeBinding(pair.Observe(pair.Candidate, "duplicate"));
	}

	public static IEnumerable<object?[]> HandlerCases =>
		from environment in new[] { Environments.Development, Environments.Production }
		from handler in new string?[] { null, "", "unknown", "[left", "[left]save" }
		select new object?[] { environment, handler };

	[Theory]
	[MemberData(nameof(HandlerCases))]
	public async Task Missing_empty_unknown_or_malformed_handler_preserves_stock_submit_errors(string environment, string? handler)
	{
		await using var pair = await Issue189HostPair.CreateAsync(environment, detailedErrors: true);
		var tokens = await pair.GetTokensAsync();
		var fields = Form().Where(field => field.Key != "_handler").ToList();
		AddHandler(fields, handler);

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			Issue189HostPair.FormPath, "handler", tokens, fields));

		CandidateReached(pair.Observe(pair.Candidate, "handler"));
		Assert.Equal(HttpStatusCode.BadRequest, responses.Stock.Status);
		Assert.Equal(environment == Environments.Development, responses.Stock.Body.Length > 0);
		Assert.Equal("text/plain", responses.Stock.Headers["Content-Type"]);
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(pair.Observe(pair.Stock, "handler"), pair.Observe(pair.Candidate, "handler"));
	}

	private static void AddHandler(List<KeyValuePair<string, string>> fields, string? handler)
	{
		if (handler is not null)
		{
			fields.Add(new("_handler", handler));
		}
	}

	private static HttpRequestMessage RejectedPost(Issue189Tokens tokens, string tokenState)
	{
		var request = Issue189HostPair.Request(HttpMethod.Post, Issue189HostPair.FormPath, "antiforgery");
		request.Headers.Add("Cookie", tokens.Cookie);
		var fields = Form().ToList();
		if (tokenState == "invalid")
		{
			fields.Add(new("__RequestVerificationToken", "invalid"));
		}

		request.Content = new FormUrlEncodedContent(fields);
		return request;
	}

	private static HttpRequestMessage UnsupportedPost(Issue189Tokens tokens, string contentType)
	{
		var request = Issue189HostPair.Request(HttpMethod.Post, Issue189HostPair.FormPath, "content-type");
		request.Headers.Add("Cookie", tokens.Cookie);
		request.Headers.Add("RequestVerificationToken", tokens.Token);
		request.Content = new StringContent("{}", Encoding.UTF8, contentType);
		return request;
	}
}
