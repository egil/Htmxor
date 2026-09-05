namespace Htmxor.UpstreamMonitor;

internal sealed class UpstreamMonitorApplication(HttpClient httpClient)
{
	public Task<MonitorResult> RunAsync(
		MonitorRequest request,
		CancellationToken cancellationToken = default)
	{
		_ = httpClient;
		_ = request;
		_ = cancellationToken;

		return Task.FromResult(new MonitorResult(
			MonitorStatus.InfrastructureError,
			null,
			[],
			[],
			string.Empty,
			string.Empty,
			null,
			"Upstream monitoring behavior is not implemented."));
	}
}
