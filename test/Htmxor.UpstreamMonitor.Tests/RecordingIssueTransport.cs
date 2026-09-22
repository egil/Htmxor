using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Htmxor.UpstreamMonitor.Tests;

// A small sibling to FakeGitHubTransport, kept separate rather than folded into it because it
// answers a different kind of question: FakeGitHubTransport lets a test script an exact response
// for an exact request, which is the right shape when the test's own hand-built expectation is
// the thing under test. Here the thing under test is whether a second GitHubIssueUpserter.
// UpsertAsync call, run against whatever the first call actually persisted, still finds both
// findings — so the fake must hold real mutable state across calls instead of a caller
// pre-computing what it believes that state will be. It only models the three issue routes
// GitHubIssueUpserter.cs uses (list, create, update); every other path 404s, which is sufficient
// because tests using this fake drive GitHubIssueUpserter directly rather than the full
// UpstreamMonitorApplication. See issue #238.
internal sealed class RecordingIssueTransport : HttpMessageHandler
{
	private const string ListPath = "/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100";
	private const string IssuesPath = "/repos/egil/Htmxor/issues";

	private readonly List<StoredIssue> issues = [];
	private readonly List<ObservedRequest> requests = [];
	private long nextNumber = 1;

	public IReadOnlyList<ObservedRequest> Requests => requests;

	// Snapshot of every issue as it stands right now, in creation order, so a test can inspect
	// exactly what a sequence of upserts left behind rather than re-deriving it.
	public IReadOnlyList<StoredIssue> Issues => issues;

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var pathAndQuery = request.RequestUri!.PathAndQuery;
		var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
		requests.Add(new ObservedRequest(request.Method, pathAndQuery, body, request.Headers.Authorization?.ToString()));
		return Route(request.Method, pathAndQuery, body);
	}

	private HttpResponseMessage Route(HttpMethod method, string pathAndQuery, string? body) => true switch
	{
		_ when method == HttpMethod.Get && pathAndQuery == ListPath => JsonResponse(SerializeList()),
		_ when method == HttpMethod.Post && pathAndQuery == IssuesPath => Create(body!),
		_ when method == HttpMethod.Patch && pathAndQuery.StartsWith(IssuesPath + "/", StringComparison.Ordinal) =>
			Update(long.Parse(pathAndQuery[(IssuesPath.Length + 1)..], CultureInfo.InvariantCulture), body!),
		_ => NotFound(method, pathAndQuery),
	};

	private static HttpResponseMessage NotFound(HttpMethod method, string pathAndQuery) => new(HttpStatusCode.NotFound)
	{
		Content = new StringContent($"RecordingIssueTransport has no route for {method} {pathAndQuery}.", Encoding.UTF8),
	};

	private HttpResponseMessage Create(string body)
	{
		using var document = JsonDocument.Parse(body);
		var root = document.RootElement;
		var issue = new StoredIssue(nextNumber++, "open", root.GetProperty("title").GetString()!, root.GetProperty("body").GetString()!);
		issues.Add(issue);
		return JsonResponse(JsonSerializer.Serialize(new { number = issue.Number, state = issue.State }));
	}

	private HttpResponseMessage Update(long number, string body)
	{
		using var document = JsonDocument.Parse(body);
		var root = document.RootElement;
		var index = issues.FindIndex(issue => issue.Number == number);
		var current = issues[index];
		var state = root.TryGetProperty("state", out var stateProperty) ? stateProperty.GetString()! : current.State;
		var updated = current with
		{
			State = state,
			Title = root.GetProperty("title").GetString()!,
			Body = root.GetProperty("body").GetString()!,
		};
		issues[index] = updated;
		return JsonResponse(JsonSerializer.Serialize(new { number = updated.Number, state = updated.State }));
	}

	private string SerializeList() => JsonSerializer.Serialize(issues.Select(issue => new
	{
		number = issue.Number,
		state = issue.State,
		body = issue.Body,
		labels = new[] { new { name = "upstream-monitor" } },
	}));

	private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
	{
		Content = new StringContent(json, Encoding.UTF8, "application/json"),
	};
}

internal sealed record StoredIssue(long Number, string State, string Title, string Body);
