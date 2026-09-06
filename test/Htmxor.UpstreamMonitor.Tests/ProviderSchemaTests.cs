using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ProviderSchemaTests
{
	private const string Releases = "/repos/dotnet/aspnetcore/releases?per_page=100";
	private const string NextReleases = Releases + "&page=2";
	private const string Compare = "/repos/dotnet/aspnetcore/compare/" + Fixture.BaselineCommit + "..." + Fixture.TargetCommit;

	[Theory]
	[InlineData("{}", false)]
	[InlineData("{}", true)]
	[InlineData("null", false)]
	[InlineData("\"unavailable\"", true)]
	public async Task Non_array_release_page_reports_actionable_infrastructure_failure(string page, bool afterValidPage)
	{
		const string error = "GitHub pagination response must be an array.";
		var transport = new FakeGitHubTransport();
		if (afterValidPage)
		{
			transport.AddJson(Releases, Fixture.Read("github/releases.json"), NextReleases);
		}
		transport.AddJson(afterValidPage ? NextReleases : Releases, page);

		var result = await Fixture.Application(transport).RunAsync(new MonitorRequest(Fixture.Manifest(), 10));

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Null(result.Upstream);
		Assert.Null(result.Issue);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Equal(error, result.InfrastructureError);
		ReportAssertions.Equal(result, new ReportExpectation("infrastructure-error",
			new("v10.0.11", Fixture.ReviewedCommit), null, [], [], error));
		Assert.Equal(afterValidPage ? [Releases, NextReleases] : [Releases],
			transport.Requests.Select(request => request.PathAndQuery));
		Assert.All(transport.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
	}

	[Theory]
	[InlineData("{}")]
	[InlineData("{\"files\":null}")]
	[InlineData("{\"files\":{}}")]
	[InlineData("{\"files\":\"unavailable\"}")]
	[InlineData("[]")]
	public async Task Missing_or_non_array_compare_files_report_actionable_infrastructure_failure(string comparison)
	{
		const string error = "GitHub compare response must contain a files array.";
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(Compare, comparison);

		var result = await Fixture.Application(transport).RunAsync(
			ProviderInventoryTests.Request(Fixture.Watch(ExpectedMonitorArtifacts.Invoker)));

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Null(result.Issue);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Equal(error, result.InfrastructureError);
		ReportAssertions.Equal(result, ProviderInventoryTests.FailureReport(error));
		Assert.All(transport.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
	}

	[Fact]
	public async Task Empty_compare_files_array_is_current_without_an_issue()
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(Compare, """{"files":[]}""");

		var result = await Fixture.Application(transport).RunAsync(
			ProviderInventoryTests.Request(Fixture.Watch(ExpectedMonitorArtifacts.Invoker)));

		Assert.Equal(MonitorStatus.Current, result.Status);
		Assert.Null(result.Issue);
		Assert.Null(result.InfrastructureError);
		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.NewerCurrentReport());
		Assert.All(transport.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
	}
}
