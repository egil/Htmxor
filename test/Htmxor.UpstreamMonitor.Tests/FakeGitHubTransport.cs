using System.Net;
using System.Text;

namespace Htmxor.UpstreamMonitor.Tests;

internal sealed record ObservedRequest(
	HttpMethod Method,
	string PathAndQuery,
	string? Body,
	string? Authorization);

internal sealed class FakeGitHubTransport : HttpMessageHandler
{
	private readonly Dictionary<string, Queue<Func<HttpResponseMessage>>> responses = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Func<HttpResponseMessage>> repeatingResponses = new(StringComparer.Ordinal);
	private readonly List<ObservedRequest> requests = [];

	public IReadOnlyList<ObservedRequest> Requests => requests;

	public void AddJson(string pathAndQuery, string json, string? nextPage = null) =>
		Add(pathAndQuery, () => JsonResponse(json, nextPage));

	// Answers every GET of this URL with the same response, the way GitHub's real contents API does
	// for a path whose content has not changed between calls. Use this for a `contents` single-path
	// or directory-listing URL a correct implementation may read more than once (for example,
	// resolving a compare-matched watch at the target after API-surface comparison already read the
	// same URL); the one-shot queue would otherwise answer a second read with a synthetic 404. Keep
	// the one-shot queue where the exact request count is itself part of the contract, such as the
	// sequential issue-upsert POSTs.
	public void AddRepeatingJson(string pathAndQuery, string json) =>
		repeatingResponses[pathAndQuery] = () => JsonResponse(json, nextPage: null);

	// Clears both registration stores for the URL, so a replaced repeating response cannot leave the
	// original answer in place for every read after the first.
	public void ReplaceJson(string pathAndQuery, string json)
	{
		responses.Remove(pathAndQuery);
		if (repeatingResponses.Remove(pathAndQuery))
		{
			AddRepeatingJson(pathAndQuery, json);
			return;
		}
		AddJson(pathAndQuery, json);
	}

	public void ReplaceWithFailure(string pathAndQuery)
	{
		responses.Remove(pathAndQuery);
		repeatingResponses.Remove(pathAndQuery);
		AddStatus(pathAndQuery, HttpStatusCode.ServiceUnavailable);
	}

	private static HttpResponseMessage JsonResponse(string json, string? nextPage)
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json"),
		};
		if (nextPage is not null)
		{
			response.Headers.Add("Link", $"<https://api.github.test{nextPage}>; rel=\"next\"");
		}

		return response;
	}

	public void AddStatus(string pathAndQuery, HttpStatusCode status) =>
		Add(pathAndQuery, () => new HttpResponseMessage(status));

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var pathAndQuery = request.RequestUri!.PathAndQuery;
		var body = await ReadBodyAsync(request, cancellationToken);
		requests.Add(new ObservedRequest(
			request.Method,
			pathAndQuery,
			body,
			request.Headers.Authorization?.ToString()));
		return FindResponse(pathAndQuery);
	}

	private HttpResponseMessage FindResponse(string pathAndQuery)
	{
		if (responses.TryGetValue(pathAndQuery, out var responsesForPath) && responsesForPath.Count > 0)
		{
			return responsesForPath.Dequeue()();
		}
		if (repeatingResponses.TryGetValue(pathAndQuery, out var repeating))
		{
			return repeating();
		}

		return new HttpResponseMessage(HttpStatusCode.NotFound)
		{
			Content = new StringContent($"No fake response for {pathAndQuery}.", Encoding.UTF8),
		};
	}

	private static Task<string?> ReadBodyAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken) =>
		request.Content is null
			? Task.FromResult<string?>(null)
			: ReadContentAsync(request.Content, cancellationToken);

	private static async Task<string?> ReadContentAsync(
		HttpContent content,
		CancellationToken cancellationToken) =>
		await content.ReadAsStringAsync(cancellationToken);

	private void Add(string pathAndQuery, Func<HttpResponseMessage> response)
	{
		if (!responses.TryGetValue(pathAndQuery, out var responsesForPath))
		{
			responsesForPath = new Queue<Func<HttpResponseMessage>>();
			responses.Add(pathAndQuery, responsesForPath);
		}

		responsesForPath.Enqueue(response);
	}
}
