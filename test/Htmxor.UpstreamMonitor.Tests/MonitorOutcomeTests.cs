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
		Assert.Empty(result.Issues);
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
		Assert.Equal(expected, Assert.Single(first.Issues));
		Assert.Equal(expected, Assert.Single(second.Issues));
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

	[Fact]
	public async Task Mixed_run_writes_both_issues_and_returns_the_last_one_attempted()
	{
		// GitHubIssueUpserter.UpsertAsync's per-issue loop (GitHubIssueUpserter.cs:24-36) is the
		// exact seam both prior complete-change review rounds' P1 findings pointed at, one layer
		// up each time: first the run-level resolution gate, then MonitorReports.IssuesFor's
		// status-derived single issue. A regression that wrote only Issues[0] (or `.Last()`, or
		// otherwise stopped continuing after one write) would pass every other test in this
		// suite, because nothing before this test drives the loop with more than one issue.
		// Computes a real mixed MonitorResult (one drifting watch, one unresolved) through the
		// real UpstreamMonitorApplication first, so both the computation and the write are
		// exercised together against a genuinely mixed result rather than a hand-built double fed
		// only to the write half. IssuesFor adds the drift issue before the unresolved one, so
		// the unresolved issue is the last one the loop attempts: the returned IssueWriteResult
		// should be its outcome, not the drift issue's, pinning the "returns the last write
		// attempted" contract GitHubIssueUpserter.cs's own comment documents.
		var mixed = await MixedDriftAndUnresolvedResultAsync();
		Assert.Equal(2, mixed.Issues.Count);
		Assert.NotEqual(mixed.Issues[0].Identity, mixed.Issues[1].Identity);

		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":43,\"state\":\"open\"}");

		var outcome = await UpsertAsync(transport, mixed);

		var writes = transport.Requests.Where(request => request.Method != HttpMethod.Get).ToArray();
		Assert.Equal(2, writes.Length);
		Assert.Equal((HttpMethod.Post, "/repos/egil/Htmxor/issues"), (writes[0].Method, writes[0].PathAndQuery));
		Assert.Contains(mixed.Issues[0].Identity, writes[0].Body, StringComparison.Ordinal);
		Assert.Equal((HttpMethod.Post, "/repos/egil/Htmxor/issues"), (writes[1].Method, writes[1].PathAndQuery));
		Assert.Contains(mixed.Issues[1].Identity, writes[1].Body, StringComparison.Ordinal);
		Assert.Equal(new IssueWriteResult(IssueWriteAction.Created, 43, null), outcome.Result);
	}

	[Fact]
	public async Task First_write_failure_stops_before_a_later_issue_is_attempted()
	{
		// The loop's first-failure-stops behaviour is real, but its mechanism is exception
		// propagation, not a per-iteration error check: CreateAsync/UpdateAsync never construct an
		// Error-populated IssueWriteResult themselves (GitHubApi.WriteAsync -> ReadAsync throws
		// MonitorFailure on any non-success response instead), so a failed write's exception is
		// uncaught inside the foreach and aborts the whole loop, landing only in UpsertAsync's
		// outer catch (GitHubIssueUpserter.cs's own comment now names this directly). Unverified
		// anywhere else in this suite: every other test writes zero, one, or (the sibling test
		// above) two issues that both succeed. Fails the first (drift) issue's create call by
		// leaving it unstubbed, and asserts the second (unresolved) issue's write — which would
		// otherwise also be a plain, unstubbed create — is never attempted at all, not merely that
		// it also happens to fail. Confirmed by controlled inversion (not a hypothetical): wrapping
		// this loop's body in its own try/catch that swallows a write failure and continues to the
		// next issue reddens this test at `Assert.NotNull(outcome.Result.Error)` (the loop
		// completes normally with `written` still at its unassigned `(None, null, null)` default,
		// so UpsertAsync returns it directly instead of reaching the outer catch) — a second,
		// independent symptom of the same regression is `writes.Length` becoming 2, since the
		// swallowed first failure lets the second issue's write also occur.
		var mixed = await MixedDriftAndUnresolvedResultAsync();
		Assert.Equal(2, mixed.Issues.Count);

		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		// No create stub: the first issue's POST 404s, and GitHubApi.WriteAsync throws.

		var outcome = await UpsertAsync(transport, mixed);

		Assert.Equal(IssueWriteAction.None, outcome.Result.Action);
		Assert.NotNull(outcome.Result.Error);
		var writes = transport.Requests.Where(request => request.Method != HttpMethod.Get).ToArray();
		var write = Assert.Single(writes);
		Assert.Contains(mixed.Issues[0].Identity, write.Body, StringComparison.Ordinal);
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
			Issues = status == MonitorStatus.Current ? [] : [ExpectedMonitorArtifacts.SingleFileIssue()],
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

	// The exact wrong path #219 shipped for two releases before it was noticed: see issue #232.
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";

	// Mirrors UnresolvedWatchPathTests.Mixed_run_reports_the_unresolved_watch_without_losing_a_
	// different_watchs_drift_from_the_reports's fixture exactly, so the two issues driven through
	// GitHubIssueUpserter here are the same real, production-computed shapes that test already
	// proved are distinct at the computation boundary — not a hand-built double that could mask a
	// defect either MonitorReports.IssuesFor or the write loop introduced.
	private static async Task<MonitorResult> MixedDriftAndUnresolvedResultAsync()
	{
		var driftingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var unresolvedWatch = Fixture.Watch(WrongFilePath);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			System.Text.Json.JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" } } }));
		var request = new MonitorRequest(Fixture.Manifest(driftingWatch, unresolvedWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		return await Fixture.Application(transport).RunAsync(request);
	}

	private static MonitorResult DriftResult() => new(
		MonitorStatus.Drift,
		new UpstreamRevision("v10.0.12", Fixture.TargetCommit),
		[new SourceChange(ExpectedMonitorArtifacts.Invoker, ChangeKind.Changed, ReviewClassification.ParityRequired)],
		[],
		"{}",
		"report",
		[ExpectedMonitorArtifacts.SingleFileIssue()],
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
