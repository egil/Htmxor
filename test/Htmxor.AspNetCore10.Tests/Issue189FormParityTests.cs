using System.Net;
using static Htmxor.AspNetCore10.Issue189FormAssertions;

namespace Htmxor.AspNetCore10;

public sealed class Issue189FormParityTests
{
	[Theory]
	[InlineData("Ada", "37", "valid", "data-name=\"Ada\"")]
	[InlineData("", "37", "invalid", "The Name field is required.")]
	[InlineData("Ada", "not-an-age", "invalid", "not-an-age")]
	public async Task Named_submission_preserves_mapping_validation_and_request_instance(
		string name, string age, string result, string expectedOutput)
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			Issue189HostPair.FormPath, "submit", tokens, Form(name, age)));

		CandidateReached(pair.Observe(pair.Candidate, "submit"));
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Contains(expectedOutput, responses.Stock.Body, StringComparison.Ordinal);
		Assert.Contains($"data-result=\"{result}\"", responses.Stock.Body, StringComparison.Ordinal);
		RequestInstanceHandled(pair.Observe(pair.Stock, "submit"), result);
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(pair.Observe(pair.Stock, "submit"), pair.Observe(pair.Candidate, "submit"));
		RequestInstanceHandled(pair.Observe(pair.Candidate, "submit"), result);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Valid_antiforgery_precedes_mapping_with_middleware_or_invoker_validation(bool invokerValidation)
	{
		await using var pair = await Issue189HostPair.CreateAsync(invokerValidation: invokerValidation);
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			Issue189HostPair.FormPath, "order", tokens, Form()));

		var stock = pair.Observe(pair.Stock, "order");
		var candidate = pair.Observe(pair.Candidate, "order");
		CandidateReached(candidate);
		AssertValidationBeforeMapping(stock, invokerValidation);
		AssertValidationBeforeMapping(candidate, invokerValidation);
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(stock, candidate);
	}

	[Theory]
	[InlineData("left", "right")]
	[InlineData("right", "left")]
	public async Task Named_scope_dispatches_only_its_request_component(string submitted, string untouched)
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			"/issue-189/scopes/right", "scoped", tokens, Form(handler: $"[{submitted}]save")));

		CandidateReached(pair.Observe(pair.Candidate, "scoped"));
		var callback = Assert.Single(pair.Observe(pair.Stock, "scoped").Components, item => item.Phase == "valid");
		Assert.Equal(submitted, callback.Label);
		Assert.Contains(pair.Observe(pair.Stock, "scoped").Components, item =>
			item.Label == untouched && item.Phase == "initialized" && item.Value == string.Empty);
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(pair.Observe(pair.Stock, "scoped"), pair.Observe(pair.Candidate, "scoped"));
	}

	[Theory]
	[InlineData("/issue-189/ambiguous", "save")]
	[InlineData("/issue-189/scopes/left", "[left]save")]
	public async Task Ambiguous_forms_preserve_the_stock_exception_and_do_not_dispatch(string path, string handler)
	{
		await using var pair = await Issue189HostPair.CreateAsync();
		var tokens = await pair.GetTokensAsync();

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(path, "ambiguous", tokens, Form(handler: handler)));

		CandidateReached(pair.Observe(pair.Candidate, "ambiguous"));
		Assert.Equal(HttpStatusCode.InternalServerError, responses.Stock.Status);
		Assert.Contains("There is more than one named submit event", responses.Stock.Body, StringComparison.Ordinal);
		Assert.DoesNotContain(pair.Observe(pair.Stock, "ambiguous").Components, item => item.Phase == "valid");
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(pair.Observe(pair.Stock, "ambiguous"), pair.Observe(pair.Candidate, "ambiguous"));
	}

	[Fact]
	public async Task Application_collection_limit_reaches_stock_mapping_and_validation()
	{
		await using var pair = await Issue189HostPair.CreateAsync(collectionLimit: 2);
		var tokens = await pair.GetTokensAsync();
		var fields = Form().Concat([
			new KeyValuePair<string, string>("Model.Tags[0]", "one"),
			new("Model.Tags[1]", "two"), new("Model.Tags[2]", "three")]);

		var responses = await SendAsync(pair, () => Issue189HostPair.Post(
			Issue189HostPair.FormPath, "limit", tokens, fields));

		CandidateReached(pair.Observe(pair.Candidate, "limit"));
		Assert.Equal(HttpStatusCode.OK, responses.Stock.Status);
		Assert.Contains("data-result=\"invalid\"", responses.Stock.Body, StringComparison.Ordinal);
		Assert.Contains("maximum", responses.Stock.Body, StringComparison.OrdinalIgnoreCase);
		EqualResponse(responses.Stock, responses.Candidate);
		EqualComponents(pair.Observe(pair.Stock, "limit"), pair.Observe(pair.Candidate, "limit"));
	}

	private static void AssertValidationBeforeMapping(Issue189Observation observation, bool invokerValidation)
	{
		var operations = observation.Operations.ToList();
		var validation = invokerValidation ? "invoker-validation:True" : "middleware-validation:True";
		Assert.Contains("middleware-feature:True", operations);
		Assert.Contains(validation, operations);
		Assert.Equal(invokerValidation, operations.Contains("invoker-validation:True"));
		Assert.Contains("map", operations);
		Assert.True(operations.IndexOf(validation) < operations.IndexOf("map"));
	}
}
