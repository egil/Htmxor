using System.Net;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class MonitorOutcomeTests
{
	private const string Invoker = "src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs";

	[Fact]
	public async Task Provider_failure_reports_infrastructure_error_without_a_misleading_issue()
	{
		var transport = new FakeGitHubTransport();
		transport.AddStatus(
			"/repos/dotnet/aspnetcore/releases?per_page=100",
			HttpStatusCode.ServiceUnavailable);
		var application = Fixture.Application(transport);

		var result = await application.RunAsync(new MonitorRequest(Fixture.Manifest(), 10));

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Contains("503", result.InfrastructureError, StringComparison.Ordinal);
		Assert.Null(result.Issue);
		Assert.Contains("infrastructure-error", result.JsonReport, StringComparison.Ordinal);
		Assert.Contains("infrastructure error", result.MarkdownReport, StringComparison.OrdinalIgnoreCase);
		var observed = Assert.Single(transport.Requests);
		Assert.Equal(HttpMethod.Get, observed.Method);
		Assert.Equal("/repos/dotnet/aspnetcore/releases?per_page=100", observed.PathAndQuery);
	}

	[Fact]
	public async Task Same_drift_produces_the_same_actionable_issue_upsert_input()
	{
		var first = await RunSingleFileDriftAsync();
		var second = await RunSingleFileDriftAsync();

		var issue = Assert.IsType<IssueUpsertInput>(first.Issue);
		Assert.Equal(issue, second.Issue);
		Assert.Contains("aspnetcore-10", issue.Identity, StringComparison.Ordinal);
		Assert.Contains("repo:egil/Htmxor", issue.SearchQuery, StringComparison.Ordinal);
		Assert.Contains("is:issue", issue.SearchQuery, StringComparison.Ordinal);
		Assert.Contains("v10.0.12", issue.Title, StringComparison.Ordinal);
		Assert.Contains(Fixture.TargetCommit, issue.Body, StringComparison.Ordinal);
		Assert.Contains(Invoker, issue.Body, StringComparison.Ordinal);
		Assert.Contains("[ ] Review source changes", issue.Body, StringComparison.Ordinal);
		Assert.Contains("[ ] Review public/protected API changes", issue.Body, StringComparison.Ordinal);
		Assert.Contains("[ ] Update the reviewed manifest baseline", issue.Body, StringComparison.Ordinal);
	}

	private static async Task<MonitorResult> RunSingleFileDriftAsync()
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-watched-files.json");
		var application = Fixture.Application(transport);
		var manifest = Fixture.Manifest(Fixture.Watch(Invoker));
		var request = new MonitorRequest(
			manifest,
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		return await application.RunAsync(request);
	}
}
