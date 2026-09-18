using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class UnresolvedWatchPathTests
{
	// The exact wrong path #219 shipped for two releases before it was noticed: see issue #232.
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";
	private const string UnmatchedPrefix = "src/Components/Endpoints/src/CacheView/Retired";

	[Fact]
	public async Task File_watch_at_a_path_that_never_existed_upstream_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(WrongFilePath);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, WrongFilePath);
	}

	[Fact]
	public async Task Prefix_watch_matching_no_upstream_files_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(UnmatchedPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, UnmatchedPrefix);
	}

	[Fact]
	public async Task Unresolved_watch_path_is_not_reported_as_current_when_upstream_has_not_moved()
	{
		// RunAsync returns Current before examining any watch whenever upstream has not moved
		// past the reviewed commit (UpstreamMonitorApplication.cs:14-17) — the common steady
		// state, and the state most runs are in. A fix placed only inside CompareWatchedAsync
		// would leave this path unresolved: criterion 1 is stated against the reviewed commit,
		// not against the existence of drift.
		var watch = Fixture.Watch(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11", Fixture.Read("github/ref-v10.0.11-direct.json"));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, RequestedTag: "v10.0.11");

		var result = await Fixture.Application(transport).RunAsync(request);

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, WrongFilePath);
	}

	[Fact]
	public async Task Mixed_run_reports_the_unresolved_watch_without_losing_a_different_watchs_drift_from_the_reports()
	{
		// #232's committed fix (e8c35e4) resolves every watch absent from the changed-file list
		// regardless of what else the same run found, because silence is a property of each watch,
		// not of the run: gating resolution on "the run has nothing else to report" left every
		// other entry of the committed 52-watch manifest unchecked for as long as any one of them
		// kept drifting — the #219 failure reintroduced one level up. DriftingPath is present in
		// the compare and drifts ordinarily; WrongFilePath is absent and never resolves. The run
		// must report UnresolvedWatch (not Drift, and not merely "not Current"), and DriftingPath's
		// finding must still appear in both reports rather than being discarded.
		var driftingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var unresolvedWatch = Fixture.Watch(WrongFilePath);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" } } }));
		var request = new MonitorRequest(
			Fixture.Manifest(driftingWatch, unresolvedWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		Assert.Contains(ExpectedMonitorArtifacts.Invoker, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(ExpectedMonitorArtifacts.Invoker, result.MarkdownReport, StringComparison.Ordinal);
		Assert.Contains(WrongFilePath, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(WrongFilePath, result.MarkdownReport, StringComparison.Ordinal);

		// The reports alone do not prove the drift finding reaches a human: MonitorReports.
		// IssuesFor derives the drift issue from `sources`/`apis` and the unresolved issue from
		// `unresolved` independently of the run's single Status, so both must be written in the
		// same MonitorResult rather than the unresolved status silently displacing the drift issue
		// the way it displaced the drift SourceChange before #232's fix.
		Assert.Equal(2, result.Issues.Count);
		var driftIssue = Assert.Single(result.Issues, issue => issue.Body.Contains(ExpectedMonitorArtifacts.Invoker, StringComparison.Ordinal));
		var unresolvedIssue = Assert.Single(result.Issues, issue => issue.Body.Contains(WrongFilePath, StringComparison.Ordinal));
		Assert.NotEqual(driftIssue.Identity, unresolvedIssue.Identity);
		Assert.DoesNotContain(WrongFilePath, driftIssue.Body, StringComparison.Ordinal);
		Assert.DoesNotContain(ExpectedMonitorArtifacts.Invoker, unresolvedIssue.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Review_issue_for_an_unresolved_watch_path_differs_from_the_issue_for_the_same_path_actually_removed()
	{
		// Assert.Single(result.Issues) plus a path-containment check (below) holds equally for an
		// ordinary drift issue about the very same path: MonitorReports.Issue(...) takes no
		// MonitorStatus, and Classify() can legitimately assign the unresolved watch the same
		// classification an ordinarily-removed file gets. Comparing the same watch's issue for
		// "actually removed" against "never resolved" is the comparative shape criterion 3 asks
		// for, without pinning what either rendering must say. Only Body is asserted — the
		// criterion's literal requirement. Identity is deliberately not pinned here: a single
		// shared per-major-version tracking issue with clearly separated body sections also
		// satisfies "distinguishable... in the review issue" as worded, and today Identity is a
		// pure function of SupportedMajorVersion, not of content. Reusing the ordinary-drift
		// Identity for the unresolved state is a real residual risk (GitHubIssueUpserter dedupes
		// by Identity, so a shared issue must not let one state's findings silently erase the
		// other's on a later run) that belongs to the Implementor's design and complete-change
		// review, not to this test — pinning it here would decide the design.
		var watch = Fixture.Watch(WrongFilePath);

		var removed = await Fixture.Application(ActuallyRemovedTransport(WrongFilePath))
			.RunAsync(ProviderInventoryTests.Request(watch));
		var unresolved = await Fixture.Application(UnrelatedChangeTransport())
			.RunAsync(ProviderInventoryTests.Request(watch));

		Assert.Equal(MonitorStatus.Drift, removed.Status);
		var removedIssue = Assert.Single(removed.Issues);
		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(unresolved, WrongFilePath);
		var unresolvedIssue = Assert.Single(unresolved.Issues);
		Assert.NotEqual(removedIssue.Body, unresolvedIssue.Body);
	}

	// Acceptance criterion 3 requires the unresolved state to be distinguishable from ordinary
	// drift in the JSON report, the Markdown report, and the review issue — not merely "not
	// Current". Excluding Drift directly rules out the counter-implementation that funnels an
	// unresolved watch through an ordinary SourceChange and the existing Drift pipeline, which
	// would produce output shaped exactly like drift in a file that does exist. Requiring a
	// populated Issues list rules out the other counter-implementation that classifies an
	// unresolved path as InfrastructureError (a natural shape if resolution failure is thrown and
	// caught by RunAsync's existing catch): before this issue's fix, MonitorReports.Create
	// populated its one issue only for Drift, and today IssuesFor still never populates one for
	// InfrastructureError (MonitorOutcomeTests.Current_or_infrastructure_outcome_never_writes_an_
	// issue pins that an InfrastructureError result never writes one), so that shape would
	// suppress the review issue on every unresolved-path run while still passing a bare "not
	// Current" check. This does not by itself force the review issue's rendered content to differ
	// from ordinary drift; see Review_issue_for_an_unresolved_watch_path_differs_from_the_issue_
	// for_the_same_path_actually_removed for that comparative check.
	private static void AssertUnresolvedIsDistinguishableFromOrdinaryDrift(MonitorResult result, string path)
	{
		Assert.NotEqual(MonitorStatus.Current, result.Status);
		Assert.NotEqual(MonitorStatus.Drift, result.Status);
		Assert.Contains(path, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(path, result.MarkdownReport, StringComparison.Ordinal);
		var issue = Assert.Single(result.Issues);
		Assert.Contains(path, issue.Body, StringComparison.Ordinal);
	}

	// The wrong or unmatched watch path can never appear in a real GitHub compare response, so an
	// unrelated changed file is the faithful shape of what the provider actually returns.
	private static FakeGitHubTransport UnrelatedChangeTransport()
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		return transport;
	}

	// The counterpart to UnrelatedChangeTransport: the same path, but it genuinely resolves and
	// was removed, so this is ordinary drift rather than an unresolvable watch.
	private static FakeGitHubTransport ActuallyRemovedTransport(string path)
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = path, status = "removed" } } }));
		return transport;
	}
}
