using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed class GitHubIssueUpserter(HttpClient httpClient)
{
	public async Task<IssueWriteResult> UpsertAsync(MonitorResult result, CancellationToken cancellationToken = default)
	{
		if (result.Status != MonitorStatus.Drift || result.Issue is null)
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
		if (closed)
		{
			await api.WriteAsync(HttpMethod.Patch, path, new { state = "open" }, cancellationToken);
		}
		await api.WriteAsync(HttpMethod.Patch, path, new { title = issue.Title, body = issue.Body }, cancellationToken);
		return new(closed ? IssueWriteAction.ReopenedAndUpdated : IssueWriteAction.Updated, number, null);
	}
}
