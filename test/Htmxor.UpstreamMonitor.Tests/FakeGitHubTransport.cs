using System.Net;
using System.Text;

namespace Htmxor.UpstreamMonitor.Tests;

internal sealed record ObservedRequest(HttpMethod Method, string PathAndQuery, string? Body);

internal sealed class FakeGitHubTransport : HttpMessageHandler
{
	private readonly Dictionary<string, Func<HttpResponseMessage>> responses = new(StringComparer.Ordinal);
	private readonly List<ObservedRequest> requests = [];

	public IReadOnlyList<ObservedRequest> Requests => requests;

	public void AddJson(string pathAndQuery, string json) =>
		Add(pathAndQuery, () => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json"),
		});

	public void AddStatus(string pathAndQuery, HttpStatusCode status) =>
		Add(pathAndQuery, () => new HttpResponseMessage(status));

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var pathAndQuery = request.RequestUri!.PathAndQuery;
		var body = await ReadBodyAsync(request, cancellationToken);
		requests.Add(new ObservedRequest(request.Method, pathAndQuery, body));
		return FindResponse(pathAndQuery);
	}

	private HttpResponseMessage FindResponse(string pathAndQuery)
	{
		if (!responses.TryGetValue(pathAndQuery, out var response))
		{
			return new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				Content = new StringContent($"No fake response for {pathAndQuery}.", Encoding.UTF8),
			};
		}

		return response();
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

	private void Add(string pathAndQuery, Func<HttpResponseMessage> response) =>
		responses.Add(pathAndQuery, response);
}
