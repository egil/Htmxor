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

		var expected = ExpectedIssue();
		Assert.Equal(expected, first.Issue);
		Assert.Equal(expected, second.Issue);
	}

	[Fact]
	public async Task Absent_matching_issue_is_created_once()
	{
		var transport = IssueTransport("[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var outcome = await UpsertAsync(transport, DriftResult());

		Assert.Equal(
			new IssueWriteObservation(
				new IssueWriteResult(IssueWriteAction.Created, 42, null),
				string.Join('\n',
					"GET /repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100 ",
					$"POST /repos/egil/Htmxor/issues {CreateBody()}")),
			outcome);
	}

	[Fact]
	public async Task Open_matching_issue_is_updated_without_duplicate_creation()
	{
		var transport = IssueTransport(OpenIssue(state: "open"));
		transport.AddJson("/repos/egil/Htmxor/issues/42", "{\"number\":42,\"state\":\"open\"}");

		var outcome = await UpsertAsync(transport, DriftResult());

		Assert.Equal(
			new IssueWriteObservation(
				new IssueWriteResult(IssueWriteAction.Updated, 42, null),
				string.Join('\n',
					"GET /repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100 ",
					$"PATCH /repos/egil/Htmxor/issues/42 {UpdateBody()}")),
			outcome);
	}

	[Fact]
	public async Task Closed_matching_issue_is_reopened_then_updated()
	{
		var transport = IssueTransport(OpenIssue(state: "closed"));
		transport.AddJson("/repos/egil/Htmxor/issues/42", "{\"number\":42,\"state\":\"open\"}");
		transport.AddJson("/repos/egil/Htmxor/issues/42", "{\"number\":42,\"state\":\"open\"}");

		var outcome = await UpsertAsync(transport, DriftResult());

		Assert.Equal(
			new IssueWriteObservation(
				new IssueWriteResult(IssueWriteAction.ReopenedAndUpdated, 42, null),
				string.Join('\n',
					"GET /repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100 ",
					"PATCH /repos/egil/Htmxor/issues/42 {\"state\":\"open\"}",
					$"PATCH /repos/egil/Htmxor/issues/42 {UpdateBody()}")),
			outcome);
	}

	[Theory]
	[InlineData("current")]
	[InlineData("infrastructure-error")]
	public async Task Current_or_infrastructure_outcome_never_writes_an_issue(string scenario)
	{
		var status = scenario == "current" ? MonitorStatus.Current : MonitorStatus.InfrastructureError;
		var transport = new FakeGitHubTransport();
		var result = DriftResult() with
		{
			Status = status,
			Issue = status == MonitorStatus.Current ? null : ExpectedIssue(),
			InfrastructureError = status == MonitorStatus.InfrastructureError ? "503 Service Unavailable" : null,
		};

		var outcome = await UpsertAsync(transport, result);

		Assert.Equal(
			new IssueWriteObservation(new IssueWriteResult(IssueWriteAction.None, null, null), string.Empty),
			outcome);
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

	private static IssueUpsertInput ExpectedIssue() => new(
		"aspnetcore-10-upstream-drift",
		"repo:egil/Htmxor is:issue label:upstream-monitor \"aspnetcore-10-upstream-drift\" in:body",
		"ASP.NET Core v10.0.12 requires Htmxor upstream review",
		$$"""
		## ASP.NET Core upstream drift

		- Previous: [v10.0.11 ({{Fixture.BaselineCommit}})](https://github.com/dotnet/aspnetcore/tree/{{Fixture.BaselineCommit}})
		- Current: [v10.0.12 ({{Fixture.TargetCommit}})](https://github.com/dotnet/aspnetcore/tree/{{Fixture.TargetCommit}})
		- Compare: https://github.com/dotnet/aspnetcore/compare/{{Fixture.BaselineCommit}}...{{Fixture.TargetCommit}}
		- Parity tests: pending review

		### Classified changes

		- Parity required | changed | {{Invoker}}

		### Review checklist

		- [ ] Review source changes
		- [ ] Review public/protected API changes
		- [ ] Run or update parity tests
		- [ ] Update the reviewed manifest baseline
		""");

	private static MonitorResult DriftResult() => new(
		MonitorStatus.Drift,
		new UpstreamRevision("v10.0.12", Fixture.TargetCommit),
		[new SourceChange(Invoker, ChangeKind.Changed, ReviewClassification.ParityRequired)],
		[],
		"{}",
		"report",
		ExpectedIssue(),
		null);

	private static FakeGitHubTransport IssueTransport(string searchResponse)
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson(
			"/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100",
			searchResponse);
		return transport;
	}

	private static string OpenIssue(string state) =>
		$"[{{\"number\":42,\"state\":\"{state}\",\"body\":\"identity: aspnetcore-10-upstream-drift\"}}]";

	private static async Task<IssueWriteObservation> UpsertAsync(
		FakeGitHubTransport transport,
		MonitorResult result)
	{
		using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.github.test") };
		var upserter = new GitHubIssueUpserter(client);

		var write = await upserter.UpsertAsync(result);

		return new IssueWriteObservation(
			write,
			string.Join('\n', transport.Requests.Select(request =>
				$"{request.Method} {request.PathAndQuery} {CanonicalJson(request.Body)}")));
	}

	private static string CanonicalJson(string? body) =>
		body is null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(
			System.Text.Json.JsonDocument.Parse(body).RootElement);

	private static string CreateBody() => CanonicalJson(System.Text.Json.JsonSerializer.Serialize(new
	{
		title = ExpectedIssue().Title,
		body = ExpectedIssue().Body,
		labels = new[] { "upstream-monitor" },
	}));

	private static string UpdateBody() => CanonicalJson(System.Text.Json.JsonSerializer.Serialize(new
	{
		title = ExpectedIssue().Title,
		body = ExpectedIssue().Body,
	}));

	private sealed record IssueWriteObservation(
		IssueWriteResult Result,
		string Requests);
}
