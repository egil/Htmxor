using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class UnresolvedWatchPathTests
{
	// The exact wrong path #219 shipped for two releases before it was noticed: see issue #232.
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";
	private const string UnmatchedPrefix = "src/Components/Endpoints/src/CacheView/Retired";

	// WrongFilePath's own containing directory: a real path upstream, but the wrong kind of thing
	// for a `match: file` watch to name.
	private const string DirectoryInsteadOfFile = "src/Components/Endpoints/src/CacheView";

	[Fact]
	public async Task File_watch_pointing_at_a_directory_is_not_reported_as_current()
	{
		// PR #239 review (5333c33): the contents API answers 200 for a directory exactly as
		// readily as for a file, so a status-only check (the shape ExistsAsync used before this
		// fix) reported a `match: file` watch aimed at a directory as resolved — the same silence
		// #232 exists to remove, surfacing only later once SourceAsync failed looking for a file
		// body that was never there. The stub below is the actual shape GitHub's contents API
		// returns for a directory: a JSON array of entries, not the single file object
		// GitHubContent produces.
		var watch = Fixture.Watch(DirectoryInsteadOfFile);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryInsteadOfFile}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryInsteadOfFile}/{Path.GetFileName(WrongFilePath)}") }));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, DirectoryInsteadOfFile);
	}

	[Fact]
	public async Task File_watch_pointing_at_a_symlink_is_not_reported_as_current()
	{
		// The other half of the same guard: GitHub answers an object, not an array, for a symlink or
		// a submodule, so the ValueKind check alone still resolves the watch and only IsFile rejects
		// it. SourceAsync would then fail on a base64 body that was never there — the same late
		// surprise #232 exists to remove, for a third kind of wrong path.
		var watch = Fixture.Watch(WrongFilePath);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{WrongFilePath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(PrefixInventoryFixture.Entry(WrongFilePath, "symlink")));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, WrongFilePath);
	}

	[Fact]
	public async Task Prefix_watch_whose_parent_path_is_a_file_is_not_reported_as_current()
	{
		// The mirror of the case above, for the prefix branch PR #239 review also flagged:
		// pointing a prefix watch's parent directory at a path that is actually a file. Before
		// this fix the prefix branch accepted any 200 for the parent and leaned on
		// PrefixSourcePathsAsync's own array check to reject it, which throws MonitorFailure and
		// would have reported this manifest mistake as an infrastructure error rather than as the
		// unresolved watch it is. WrongFilePath is a real file, so treating it as a directory by
		// nesting a prefix watch one level under it is the faithful shape of the mistake.
		var prefix = $"{WrongFilePath}/Nested";
		var watch = Fixture.Watch(prefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{WrongFilePath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(PrefixInventoryFixture.Entry(WrongFilePath)));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, prefix);
	}

	[Fact]
	public async Task File_watch_at_a_path_that_never_existed_upstream_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(WrongFilePath);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, WrongFilePath);
	}

	[Theory]
	[InlineData(System.Net.HttpStatusCode.ServiceUnavailable, "503")]
	[InlineData(System.Net.HttpStatusCode.Forbidden, "403")]
	public async Task Outage_while_resolving_a_watch_is_an_infrastructure_error_not_an_unresolved_path(
		System.Net.HttpStatusCode status, string expectedCode)
	{
		// TryGetAsync answers null only for 404 ("the path is not there at this ref"); every other
		// non-success status must still throw, or a rate limit or outage during resolution is
		// reported as a manifest defect — an unresolved-watch review issue and exit 3 for a path
		// that may be perfectly correct, asserting a fact about upstream this run never observed.
		var watch = Fixture.Watch(WrongFilePath);
		var transport = UnrelatedChangeTransport();
		transport.AddStatus(
			$"/repos/dotnet/aspnetcore/contents/{WrongFilePath}?ref={Fixture.BaselineCommit}",
			status);

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Contains(expectedCode, result.InfrastructureError!, StringComparison.Ordinal);
		Assert.Empty(result.Issues);
		Assert.DoesNotContain(WrongFilePath, result.JsonReport, StringComparison.Ordinal);
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
	public async Task Prefix_watch_whose_parent_directory_lists_only_unrelated_entries_is_not_reported_as_current()
	{
		// The case above exits at ResolvesAsync's own "listing is not an array" guard
		// (UpstreamRepository.Inventory.cs:24-27) before PrefixSourcePathsAsync's own emptiness
		// check at line 28 is ever reached, because its parent directory is unstubbed and 404s. This
		// fixture instead drives a parent directory that resolves as a genuine, non-empty entry
		// array in which nothing matches the prefix, so the array guard is satisfied and
		// `(await PrefixSourcePathsAsync(...)).Count > 0` is the only thing left standing between
		// this watch and a false "resolved" result.
		var watch = Fixture.Watch(UnmatchedPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		var listing = JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry(WrongFilePath) });
		// ResolvesAsync's own guard and PrefixSourcePathsAsync each fetch this same directory
		// independently (a pre-existing duplicate request, not introduced by this fixture), so the
		// stub must answer the same path twice.
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{DirectoryInsteadOfFile}?ref={Fixture.BaselineCommit}", listing);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{DirectoryInsteadOfFile}?ref={Fixture.BaselineCommit}", listing);

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, UnmatchedPrefix);
	}

	[Fact]
	public async Task Unresolved_watch_path_is_not_reported_as_current_when_upstream_has_not_moved()
	{
		// Before this fix, UpstreamMonitorApplication.RunAsync's steady-state branch (upstream
		// has not moved past the reviewed commit) returned Current directly without resolving any
		// watch — the common steady state, and the state most runs are in. This test guards
		// against that regression: a fix placed only inside CompareWatchedAsync would leave this
		// path unresolved, because criterion 1 is stated against the reviewed commit, not against
		// the existence of drift.
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

		// The four Contains checks above cannot tell a dedicated unresolved channel from the
		// ordinary drift channel: both simply put the path in the report text, so a
		// counter-implementation that rendered WrongFilePath as an ordinary source-change row
		// (drift's own channel) would satisfy every one of them. Criterion 3 requires the two
		// states to be distinguishable, so this run — the only fixture with a real drift row and
		// an unresolved path together — also pins each path to its own JSON key and Markdown
		// section, and the other path's absence from it.
		using var json = JsonDocument.Parse(result.JsonReport);
		var unresolvedWatchPaths = json.RootElement.GetProperty("unresolvedWatchPaths")
			.EnumerateArray().Select(element => element.GetString()).ToArray();
		var sourceChangePaths = json.RootElement.GetProperty("sourceChanges")
			.EnumerateArray().Select(element => element.GetProperty("path").GetString()).ToArray();
		Assert.Contains(WrongFilePath, unresolvedWatchPaths);
		Assert.DoesNotContain(WrongFilePath, sourceChangePaths);
		Assert.Contains(ExpectedMonitorArtifacts.Invoker, sourceChangePaths);

		var sourceChangesSection = MarkdownSection(result.MarkdownReport, "## Source changes", "## API changes");
		var unresolvedSection = MarkdownSection(result.MarkdownReport, "## Unresolved watch paths", nextHeading: null);
		Assert.Contains(ExpectedMonitorArtifacts.Invoker, sourceChangesSection, StringComparison.Ordinal);
		Assert.DoesNotContain(WrongFilePath, sourceChangesSection, StringComparison.Ordinal);
		Assert.Contains(WrongFilePath, unresolvedSection, StringComparison.Ordinal);

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

	// Isolates one heading's own body from MonitorReports.Markdown's single concatenated string, so
	// a path found "in the Markdown report" can be pinned to the specific section that names it
	// rather than merely being present somewhere in the whole document.
	private static string MarkdownSection(string markdown, string heading, string? nextHeading)
	{
		var start = markdown.IndexOf(heading, StringComparison.Ordinal);
		Assert.True(start >= 0, $"Markdown report does not contain '{heading}'.");
		var end = nextHeading is null ? markdown.Length : markdown.IndexOf(nextHeading, start, StringComparison.Ordinal);
		Assert.True(end >= 0, $"Markdown report does not contain '{nextHeading}' after '{heading}'.");
		return markdown[start..end];
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
