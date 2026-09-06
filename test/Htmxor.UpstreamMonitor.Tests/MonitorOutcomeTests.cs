using System.Net;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class MonitorOutcomeTests
{
	[Theory]
	[InlineData("open")]
	[InlineData("closed")]
	public async Task Identity_on_second_issue_page_excludes_pull_requests_and_prevents_duplicate_creation(string state)
	{
		const string firstPage = "/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100";
		const string nextPage = firstPage + "&page=2";
		var transport = new FakeGitHubTransport();
		var first = Enumerable.Range(1, 100).Select(number => new
		{
			number,
			state = "open",
			body = ExpectedMonitorArtifacts.SingleFileIssue().Body,
			pull_request = new { url = $"https://api.github.test/repos/egil/Htmxor/pulls/{number}" },
			labels = new[] { new { name = "upstream-monitor" } },
		});
		transport.AddJson(firstPage, System.Text.Json.JsonSerializer.Serialize(first), nextPage);
		transport.AddJson(nextPage, Issues(new IssueFixture(142, state, ExpectedMonitorArtifacts.SingleFileIssue().Body)));
		transport.AddJson("/repos/egil/Htmxor/issues/142", "{\"number\":142,\"state\":\"open\"}");
		transport.AddJson("/repos/egil/Htmxor/issues/142", "{\"number\":142,\"state\":\"open\"}");

		var observation = await UpsertAsync(transport, DriftResult());

		Assert.Equal(new IssueWriteResult(state == "open" ? IssueWriteAction.Updated : IssueWriteAction.ReopenedAndUpdated, 142, null), observation.Result);
		Assert.Equal(new[] { firstPage, nextPage }, transport.Requests.Where(request => request.Method == HttpMethod.Get).Select(request => request.PathAndQuery));
		var writes = transport.Requests.Where(request => request.Method != HttpMethod.Get).ToArray();
		Assert.Single(writes);
		Assert.All(writes, request => Assert.Equal((HttpMethod.Patch, "/repos/egil/Htmxor/issues/142"), (request.Method, request.PathAndQuery)));
		AssertUpdatedIssue(writes[0], state);
	}

	[Fact]
	public async Task Provider_failure_reports_infrastructure_error_without_a_misleading_issue()
	{
		var transport = new FakeGitHubTransport();
		transport.AddStatus(
			"/repos/dotnet/aspnetcore/releases?per_page=100",
			HttpStatusCode.ServiceUnavailable);
		var application = Fixture.Application(transport);

		var result = await application.RunAsync(new MonitorRequest(Fixture.Manifest(), 10));

		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.InfrastructureReport());
		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Equal(ExpectedMonitorArtifacts.InfrastructureError, result.InfrastructureError);
		Assert.Null(result.Issue);
		var observed = Assert.Single(transport.Requests);
		Assert.Equal(HttpMethod.Get, observed.Method);
		Assert.Equal("/repos/dotnet/aspnetcore/releases?per_page=100", observed.PathAndQuery);
	}

	[Fact]
	public async Task Same_drift_produces_the_same_actionable_issue_upsert_input()
	{
		var first = await RunSingleFileDriftAsync();
		var second = await RunSingleFileDriftAsync();

		var expected = ExpectedMonitorArtifacts.SingleFileIssue();
		Assert.Equal(expected, first.Issue);
		Assert.Equal(expected, second.Issue);
	}

	[Fact]
	public async Task Created_issue_is_reused_on_the_second_upsert()
	{
		var transport = IssueTransport(UnrelatedIssues());
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");
		transport.AddJson(
			"/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100",
			IssuesWithMatch("open"));
		transport.AddJson("/repos/egil/Htmxor/issues/42", "{\"number\":42,\"state\":\"open\"}");

		var first = await UpsertAsync(transport, DriftResult());
		var second = await UpsertAsync(transport, DriftResult());

		Assert.Equal(
			new RepeatedIssueWriteObservation(
				new IssueWriteResult(IssueWriteAction.Created, 42, null),
				new IssueWriteResult(IssueWriteAction.Updated, 42, null),
				string.Join('\n',
					"GET /repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100 ",
					$"POST /repos/egil/Htmxor/issues {CreateBody()}",
					"GET /repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100 ",
					$"PATCH /repos/egil/Htmxor/issues/42 {UpdateBody()}")),
			new RepeatedIssueWriteObservation(first.Result, second.Result, second.Requests));
	}

	[Fact]
	public async Task Open_matching_issue_is_updated_without_duplicate_creation()
	{
		var transport = IssueTransport(IssuesWithMatch("open"));
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
	public async Task Closed_matching_issue_is_reopened_with_fresh_content_in_one_write()
	{
		var transport = IssueTransport(Issues(new IssueFixture(42, "closed",
			$"Identity: {ExpectedMonitorArtifacts.SingleFileIssue().Identity}\nOld drift report.")));
		transport.AddJson("/repos/egil/Htmxor/issues/42", "{\"number\":42,\"state\":\"open\"}");
		transport.AddJson("/repos/egil/Htmxor/issues/42", "{\"number\":42,\"state\":\"open\"}");

		var outcome = await UpsertAsync(transport, DriftResult());

		Assert.Equal(new IssueWriteResult(IssueWriteAction.ReopenedAndUpdated, 42, null), outcome.Result);
		var write = Assert.Single(transport.Requests.Where(request => request.Method != HttpMethod.Get));
		Assert.Equal((HttpMethod.Patch, "/repos/egil/Htmxor/issues/42"), (write.Method, write.PathAndQuery));
		AssertUpdatedIssue(write, "closed");
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
			Issue = status == MonitorStatus.Current ? null : ExpectedMonitorArtifacts.SingleFileIssue(),
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
		var manifest = Fixture.Manifest(Fixture.Watch(ExpectedMonitorArtifacts.Invoker));
		var request = new MonitorRequest(
			manifest,
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		return await application.RunAsync(request);
	}

	private static MonitorResult DriftResult() => new(
		MonitorStatus.Drift,
		new UpstreamRevision("v10.0.12", Fixture.TargetCommit),
		[new SourceChange(ExpectedMonitorArtifacts.Invoker, ChangeKind.Changed, ReviewClassification.ParityRequired)],
		[],
		"{}",
		"report",
		ExpectedMonitorArtifacts.SingleFileIssue(),
		null);

	private static FakeGitHubTransport IssueTransport(string searchResponse)
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson(
			"/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100",
			searchResponse);
		return transport;
	}

	private static string UnrelatedIssues() => Issues(
		new(7, "open", "unrelated open upstream monitor issue"),
		new(8, "closed", "unrelated closed upstream monitor issue"));

	private static string IssuesWithMatch(string state) => Issues(
		new(7, "open", "unrelated open upstream monitor issue"),
		new(8, "closed", "unrelated closed upstream monitor issue"),
		new(42, state, ExpectedMonitorArtifacts.SingleFileIssue().Body));

	private static string Issues(params IssueFixture[] issues) =>
		System.Text.Json.JsonSerializer.Serialize(issues.Select(issue => new
		{
			number = issue.Number,
			state = issue.State,
			body = issue.Body,
			labels = new[] { new { name = "upstream-monitor" } },
		}));

	private static async Task<IssueWriteObservation> UpsertAsync(
		FakeGitHubTransport transport,
		MonitorResult result)
	{
		using var client = new HttpClient(transport, disposeHandler: false) { BaseAddress = new Uri("https://api.github.test") };
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

	private static void AssertUpdatedIssue(ObservedRequest request, string previousState)
	{
		using var document = System.Text.Json.JsonDocument.Parse(request.Body!);
		var properties = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString());
		var expected = ExpectedMonitorArtifacts.SingleFileIssue();
		Assert.Equal(expected.Title, properties["title"]);
		Assert.Equal(expected.Body, properties["body"]);
		if (previousState == "closed")
		{
			Assert.Equal("open", properties["state"]);
		}
	}

	private static string CreateBody() => CanonicalJson(System.Text.Json.JsonSerializer.Serialize(new
	{
		title = ExpectedMonitorArtifacts.SingleFileIssue().Title,
		body = ExpectedMonitorArtifacts.SingleFileIssue().Body,
		labels = new[] { "upstream-monitor" },
	}));

	private static string UpdateBody() => CanonicalJson(System.Text.Json.JsonSerializer.Serialize(new
	{
		title = ExpectedMonitorArtifacts.SingleFileIssue().Title,
		body = ExpectedMonitorArtifacts.SingleFileIssue().Body,
	}));

	private sealed record IssueWriteObservation(
		IssueWriteResult Result,
		string Requests);

	private sealed record RepeatedIssueWriteObservation(
		IssueWriteResult First,
		IssueWriteResult Second,
		string Requests);

	private sealed record IssueFixture(long Number, string State, string Body);
}
