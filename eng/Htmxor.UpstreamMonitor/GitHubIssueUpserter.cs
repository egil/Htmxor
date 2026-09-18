using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed class GitHubIssueUpserter(HttpClient httpClient)
{
	public async Task<IssueWriteResult> UpsertAsync(MonitorResult result, CancellationToken cancellationToken = default)
	{
		// Both finding states write; Current and InfrastructureError never do, even if a caller hands
		// over populated issues. Matching on the reporting states rather than on the issues alone
		// keeps that guarantee where it was before an unresolved watch became a second writable state.
		if (result.Issues.Count == 0 || result.Status is not (MonitorStatus.Drift or MonitorStatus.UnresolvedWatch))
		{
			return new(IssueWriteAction.None, null, null);
		}
		try
		{
			var api = new GitHubApi(httpClient);
			// One listing serves every issue this run reports; each is matched by its own identity, so
			// a drift finding and an unresolved-path finding reach separate issues and neither
			// suppresses the other. The first failure stops the loop rather than being masked by a
			// later success.
			var issues = await api.GetPagesAsync("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", cancellationToken);
			// IssueWriteResult describes one write, so for a run reporting several findings the
			// returned value is the last one attempted. A failed write throws out of CreateAsync or
			// UpdateAsync into the catch below, which is what stops a later issue being attempted and
			// what turns the run into an error; neither returns a populated Error for the loop to
			// inspect. Only Error is read by Program, so a run that failed to write is never reported
			// clean.
			var written = new IssueWriteResult(IssueWriteAction.None, null, null);
			foreach (var input in result.Issues)
			{
				var existing = issues.FirstOrDefault(issue => Matches(issue, input.Identity));
				written = existing.ValueKind == JsonValueKind.Undefined
					? await CreateAsync(api, input, cancellationToken)
					: await UpdateAsync(api, existing, input, cancellationToken);
			}
			return written;
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
