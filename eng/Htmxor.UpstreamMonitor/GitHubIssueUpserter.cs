namespace Htmxor.UpstreamMonitor;

internal sealed class GitHubIssueUpserter(HttpClient httpClient)
{
	public Task<IssueWriteResult> UpsertAsync(
		MonitorResult result,
		CancellationToken cancellationToken = default)
	{
		_ = httpClient;
		_ = result;
		_ = cancellationToken;
		return Task.FromResult(new IssueWriteResult(IssueWriteAction.None, null, "Issue upsert is not implemented."));
	}
}
