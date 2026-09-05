using System.Net;
using System.Text;
using static Htmxor.AspNetCore10.Issue189FormAssertions;

namespace Htmxor.AspNetCore10;

public sealed class Issue189FormInputTests
{
	[Fact]
	public async Task Configured_antiforgery_exemption_allows_tokenless_named_submission()
	{
		await using var pair = await Issue189HostPair.CreateAsync();

		var responses = await SendAsync(pair, TokenlessPost);

		var stock = pair.Observe(pair.Stock, "exempt");
		var candidate = pair.Observe(pair.Candidate, "exempt");
		CandidateReached(candidate);
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Contains("data-name=\"Ada\"", responses.Stock.Body, StringComparison.Ordinal);
		Assert.Contains("data-result=\"valid\"", responses.Stock.Body, StringComparison.Ordinal);
		AssertExemptValidation(stock);
		AssertExemptValidation(candidate);
		RequestInstanceHandled(stock, "valid");
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(stock, candidate);
		RequestInstanceHandled(candidate, "valid");
	}

	[Fact]
	public async Task Multipart_named_submission_maps_file_content_and_metadata_to_request_callback()
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => MultipartPost(tokens));

		var stock = pair.Observe(pair.Stock, "upload");
		var candidate = pair.Observe(pair.Candidate, "upload");
		CandidateReached(candidate);
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Contains("data-caption=\"manifest\"", responses.Stock.Body, StringComparison.Ordinal);
		Assert.Contains("Model.Attachment|notes.txt|text/plain; charset=utf-8|12|hello upload", responses.Stock.Body, StringComparison.Ordinal);
		RequestInstanceHandled(stock, "uploaded");
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(stock, candidate);
		RequestInstanceHandled(candidate, "uploaded");
	}

	private static void AssertExemptValidation(Issue189Observation observation)
	{
		Assert.Contains("middleware-feature:", observation.Operations);
		Assert.DoesNotContain(observation.Operations, operation => operation.StartsWith("middleware-validation:", StringComparison.Ordinal));
		Assert.DoesNotContain(observation.Operations, operation => operation.StartsWith("invoker-validation:", StringComparison.Ordinal));
	}

	private static HttpRequestMessage TokenlessPost()
	{
		var request = Issue189HostPair.Request(HttpMethod.Post, "/issue-189/exempt", "exempt");
		request.Content = new FormUrlEncodedContent(Form());
		return request;
	}

	private static HttpRequestMessage MultipartPost(Issue189Tokens tokens)
	{
		var request = Issue189HostPair.Request(HttpMethod.Post, "/issue-189/upload", "upload");
		request.Headers.Add("Cookie", tokens.Cookie);
		request.Content = new MultipartFormDataContent
		{
			{ new StringContent("upload"), "_handler" },
			{ new StringContent(tokens.Token), "__RequestVerificationToken" },
			{ new StringContent("manifest"), "Model.Caption" },
			{ new StringContent("hello upload", Encoding.UTF8, "text/plain"), "Model.Attachment", "notes.txt" },
		};
		return request;
	}
}
