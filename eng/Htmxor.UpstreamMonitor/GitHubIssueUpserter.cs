using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed class GitHubIssueUpserter(HttpClient httpClient)
{
	public async Task<IssueWriteResult> UpsertAsync(MonitorResult result, CancellationToken cancellationToken = default)
	{
		// Both finding states write; Current and InfrastructureError never do, even if a caller hands
		// over a populated Issue. Matching on the reporting states rather than on Issue alone keeps
		// that guarantee where it was before an unresolved watch became a second writable state.
		if (result.Issue is null || result.Status is not (MonitorStatus.Drift or MonitorStatus.UnresolvedWatch))
		{
			return new(IssueWriteAction.None, null, null);
		}
		try
		{
			var api = new GitHubApi(httpClient);
			var issues = await api.GetPagesAsync("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", cancellationToken);
			var existing = issues.FirstOrDefault(issue => Matches(issue, result.Issue.Identity));
			return existing.ValueKind == JsonValueKind.Undefined
				? await CreateAsync(api, result.Issue, cancellationToken)
				: await UpdateAsync(api, existing, result.Issue, cancellationToken);
		}
		catch (Exception exception)
		{
			return new(IssueWriteAction.None, null, MonitorErrors.SafeMessage(exception));
		}
	}

	private static bool Matches(JsonElement issue, string identity) =>
		!issue.TryGetProperty("pull_request", out _) && issue.TryGetProperty("body", out var body) &&
		(body.GetString() ?? string.Empty).Split('\n').Any(line => line.TrimEnd('\r') == $"Identity: {identity}");

	private static async Task<IssueWriteResult> CreateAsync(GitHubApi api, IssueUpsertInput issue, CancellationToken cancellationToken)
	{
		var response = await api.WriteAsync(HttpMethod.Post, "/repos/egil/Htmxor/issues",
			new { title = issue.Title, body = issue.Body, labels = new[] { "upstream-monitor" } }, cancellationToken);
		return new(IssueWriteAction.Created, response.GetProperty("number").GetInt64(), null);
	}

	private static async Task<IssueWriteResult> UpdateAsync(GitHubApi api, JsonElement existing, IssueUpsertInput issue, CancellationToken cancellationToken)
	{
		var number = existing.GetProperty("number").GetInt64();
		var closed = existing.GetProperty("state").GetString() == "closed";
		var path = $"/repos/egil/Htmxor/issues/{number}";
		object payload = closed
			? new { state = "open", title = issue.Title, body = issue.Body }
			: new { title = issue.Title, body = issue.Body };
		await api.WriteAsync(HttpMethod.Patch, path, payload, cancellationToken);
		return new(closed ? IssueWriteAction.ReopenedAndUpdated : IssueWriteAction.Updated, number, null);
	}
}
