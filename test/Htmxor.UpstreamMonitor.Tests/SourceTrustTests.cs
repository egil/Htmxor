using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class SourceTrustTests
{
	[Fact]
	public async Task Upstream_source_is_inspected_as_data_without_execution()
	{
		using var sentinel = new ExecutionSentinel();
		var transport = SourceChangeTests.DriftTransport("github/compare-source-execution-sentinel.json");
		AddSourceResponses(transport, sentinel.MarkerPath);
		var application = Fixture.Application(transport);
		var request = new MonitorRequest(
			Fixture.Manifest(Fixture.Watch(
				ExpectedMonitorArtifacts.InvokerInterface,
				apiSurface: ApiSurface.Interface,
				relationship: WatchRelationship.Implements)),
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.False(sentinel.WasExecuted, "Downloaded source must never be compiled, loaded, or executed.");
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Equal(
			[new SourceChange(ExpectedMonitorArtifacts.InvokerInterface, ChangeKind.Changed, ReviewClassification.CompatibilityRisk)],
			result.SourceChanges);
		Assert.Equal(ExpectedMonitorArtifacts.InterfaceApiChanges(), result.ApiChanges);
		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.InterfaceApiReport());
	}

	private static void AddSourceResponses(FakeGitHubTransport transport, string markerPath)
	{
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		var target = Fixture.Read("source/target/IRazorComponentEndpointInvoker.ExecutionSentinel.cs")
			.Replace("__SENTINEL_PATH__", CSharpString(markerPath), StringComparison.Ordinal);
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText(target));
	}

	private static string CSharpString(string value) =>
		value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

	private sealed class ExecutionSentinel : IDisposable
	{
		public ExecutionSentinel()
		{
			DirectoryPath = Path.Combine(Path.GetTempPath(), $"htmxor-source-sentinel-{Guid.NewGuid():N}");
			Directory.CreateDirectory(DirectoryPath);
		}

		private string DirectoryPath { get; }

		public string MarkerPath => Path.Combine(DirectoryPath, "executed.marker");

		public bool WasExecuted => File.Exists(MarkerPath);

		public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
	}
}
