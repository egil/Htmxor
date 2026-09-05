using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ReleaseDiscoveryTests
{
	[Fact]
	public async Task Latest_supported_stable_release_at_the_reviewed_commit_is_discovery_only()
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson(
			"/repos/dotnet/aspnetcore/releases?per_page=100",
			Fixture.Read("github/releases.json"));
		transport.AddJson(
			"/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11",
			Fixture.Read("github/ref-v10.0.11-direct.json"));
		var application = Fixture.Application(transport);

		var result = await application.RunAsync(new MonitorRequest(Fixture.Manifest(), 10));

		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.CurrentReport());
		Assert.Equal(MonitorStatus.Current, result.Status);
		Assert.Equal(new UpstreamRevision("v10.0.11", Fixture.ReviewedCommit), result.Upstream);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Null(result.Issue);
		Assert.Equal(
			[
				(HttpMethod.Get, "/repos/dotnet/aspnetcore/releases?per_page=100"),
				(HttpMethod.Get, "/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11"),
			],
			transport.Requests.Select(request => (request.Method, request.PathAndQuery)));
	}

	[Fact]
	public async Task Explicit_annotated_tag_resolves_to_its_commit_without_release_discovery()
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson(
			"/repos/dotnet/aspnetcore/git/ref/tags/v10.0.10",
			Fixture.Read("github/ref-v10.0.10-annotated.json"));
		transport.AddJson(
			"/repos/dotnet/aspnetcore/git/tags/dddddddddddddddddddddddddddddddddddddddd",
			Fixture.Read("github/tag-v10.0.10.json"));
		var application = Fixture.Application(transport);
		var request = new MonitorRequest(
			Fixture.Manifest(),
			10,
			RequestedTag: "v10.0.10",
			BaselineCommit: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

		var result = await application.RunAsync(request);

		Assert.Equal(MonitorStatus.Current, result.Status);
		Assert.Equal(
			new UpstreamRevision("v10.0.10", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
			result.Upstream);
		Assert.Equal(
			[
				(HttpMethod.Get, "/repos/dotnet/aspnetcore/git/ref/tags/v10.0.10"),
				(HttpMethod.Get, "/repos/dotnet/aspnetcore/git/tags/dddddddddddddddddddddddddddddddddddddddd"),
			],
			transport.Requests.Select(request => (request.Method, request.PathAndQuery)));
	}
}
