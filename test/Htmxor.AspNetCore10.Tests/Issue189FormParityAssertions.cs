using System.Net;
using System.Text.RegularExpressions;

namespace Htmxor.AspNetCore10;

internal sealed record Issue189Response(HttpStatusCode Status, IReadOnlyDictionary<string, string> Headers, string Body, string[] Tokens)
{
	public static async Task<Issue189Response> ReadAsync(HttpResponseMessage response)
	{
		var snapshot = await Issue187ResponseSnapshot.CreateAsync(response);
		var headers = snapshot.Headers.ToDictionary(pair => pair.Key, pair =>
			pair.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ? NormalizeCookie(pair.Value) : pair.Value);
		var tokens = Regex.Matches(snapshot.Body, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant)
			.Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).Distinct().ToArray();
		return new(snapshot.StatusCode, headers, NormalizeBody(snapshot.Body), tokens);
	}

	private static string NormalizeCookie(string value)
		=> Regex.Replace(value, "(^|\\n)(issue189-antiforgery=)[^;\\n]+", "$1$2<token>", RegexOptions.CultureInvariant);

	private static string NormalizeBody(string body)
	{
		// Independent hosts generate different protected tokens and component identities; retain all surrounding markup.
		body = Regex.Replace(body, "(name=\"__RequestVerificationToken\" value=\")[^\"]+(\")", "$1<token>$2", RegexOptions.CultureInvariant);
		return Regex.Replace(body, "data-instance=\"[0-9a-f-]{36}\"", "data-instance=\"<instance>\"", RegexOptions.CultureInvariant);
	}
}

internal static class Issue189FormAssertions
{
	public static void EqualResponse(Issue189Response stock, Issue189Response candidate)
	{
		Assert.Equal(stock.Status, candidate.Status);
		Assert.Equal(stock.Body, candidate.Body);
		Assert.Equal(stock.Headers, candidate.Headers);
	}

	public static void CandidateReached(Issue189Observation observation)
		=> Assert.Contains("invoker:HtmxorEndpointCandidateInvoker", observation.Operations);

	public static void EqualComponents(Issue189Observation stock, Issue189Observation candidate)
		=> Assert.Equal(
			stock.Components.Select(item => (item.Label, item.Phase, item.Value)),
			candidate.Components.Select(item => (item.Label, item.Phase, item.Value)));

	public static void RequestInstanceHandled(Issue189Observation observation, string result)
	{
		var callback = Assert.Single(observation.Components, item => item.Phase == result);
		var initialization = Assert.Single(observation.Components, item =>
			item.Label == callback.Label && item.Phase == "initialized");
		Assert.Equal(initialization.Instance, callback.Instance);
		Assert.Contains(observation.Components, item =>
			item.Instance == callback.Instance && item.Phase == "parameters-set");
	}

	public static void RejectedBeforeBinding(Issue189Observation observation)
	{
		Assert.DoesNotContain("map", observation.Operations);
		Assert.Empty(observation.Components);
	}

	public static KeyValuePair<string, string>[] Form(string name = "Ada", string age = "37", string handler = "save")
		=> [new("_handler", handler), new("Model.Name", name), new("Model.Age", age)];

	public static async Task<(Issue189Response Stock, Issue189Response Candidate)> SendAsync(
		Issue189HostPair pair, Func<HttpRequestMessage> create)
	{
		using var stockRequest = create();
		using var candidateRequest = create();
		using var stock = await pair.Stock.Client.SendAsync(stockRequest);
		using var candidate = await pair.Candidate.Client.SendAsync(candidateRequest);
		return (await Issue189Response.ReadAsync(stock), await Issue189Response.ReadAsync(candidate));
	}
}
