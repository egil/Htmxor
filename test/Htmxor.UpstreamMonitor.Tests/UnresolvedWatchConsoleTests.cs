using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.ConsoleBoundaryTests;
using static Htmxor.UpstreamMonitor.Tests.ConsoleTargetFrameworkTests;

namespace Htmxor.UpstreamMonitor.Tests;

[Collection("Process environment")]
public sealed class UnresolvedWatchConsoleTests
{
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";

	[Fact]
	public async Task Exit_code_for_an_unresolved_watch_path_is_neither_current_nor_infrastructure_error()
	{
		// docs/agents/testing.md documents exit 0/1/2 as Current/Drift/InfrastructureError, and
		// Program.RunAsync returns `(int)status` directly — a pre-existing, documented contract this
		// test does not touch or extend. `!= 0` is required directly by acceptance criterion 1
		// ("a non-current result"). `!= 2` is defense-in-depth at this same CLI surface for a
		// counter-implementation MonitorOutcomeTests.Current_or_infrastructure_outcome_never_
		// writes_an_issue already forbids at the application level (an unresolved path silently
		// misclassified as InfrastructureError, which never populates Issues). No exclusion for
		// exit code 1 (Drift): acceptance criterion 3 requires distinguishability in the JSON
		// report, the Markdown report, and the review issue — not the exit code — so a design
		// where the new status deliberately shares Drift's exit code 1 (both meaning "requires
		// review" at the CLI surface) while still rendering distinguishable report content remains
		// compliant and is not foreclosed here.
		using var workspace = SingleWatchWorkspace(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		// WrongFilePath is deliberately never resolved (that is this test's whole point), so #232's
		// fix now writes a review issue for it. Without these stubs the unstubbed issue-list/create
		// endpoints 404, UpsertAsync's own catch turns that into an unrelated write failure, and
		// Program.cs converts the run to InfrastructureError (exit 2) for the wrong reason.
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.NotEqual(0, observation.ExitCode);
		Assert.NotEqual(2, observation.ExitCode);
	}

	[Fact]
	public async Task Exit_code_for_an_unresolved_watch_path_matches_the_pinned_ordinal()
	{
		// MonitorContracts.cs gives MonitorStatus explicit values specifically because Program.cs
		// returns `(int)status` directly and QualityCommand.cs hardcodes the same integer a second
		// time in a different assembly — UpstreamCommandDispatchTests feeds that integer to a fake
		// runner as a literal and never derives it from the real enum, so it cannot catch a drift
		// between the two. Nothing end to end confirmed a real unresolved-path run's actual exit
		// code is still the value the enum declares. The literal `3` below is deliberate, not a
		// shortcut for `(int)MonitorStatus.UnresolvedWatch`: deriving the expected value from the
		// same enum the real run also derives its value from would make this assertion vacuous —
		// both sides would drift together under a reorder. A hard-coded literal, matching exactly
		// what QualityCommand.cs itself hard-codes, is what actually lets a reorder or a removed
		// explicit assignment redden this test.
		using var workspace = SingleWatchWorkspace(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Equal(3, observation.ExitCode);
	}

	[Fact]
	public async Task Unresolved_watch_path_in_one_framework_is_not_masked_by_a_current_framework_in_the_same_run()
	{
		// Program.cs's multi-framework branch aggregates the run's exit code across frameworks with an
		// ordinal Max over MonitorStatus. No existing console-level test exercises that
		// aggregation across differing statuses from multiple frameworks in one invocation. This
		// does not pin where the new status sits in the enum declaration or a specific integer —
		// only that a run cannot exit 0 (Current) merely because one configured framework
		// happened to be clean while another carried an unresolved watch.
		//
		// PR #239 review (Copilot, correct): as originally written, WrongFilePath had no contents
		// stub at either framework's own reviewed commit, so BOTH frameworks resolved to
		// UnresolvedWatch — the "current framework" this test's name names never existed, and a
		// broken aggregation that simply propagated one framework's result unchanged would still
		// have passed. Stubbing WrongFilePath's contents at net10.0's own reviewed commit
		// (Fixture.ReviewedCommit) makes net10.0 genuinely Current: upstream has not moved past
		// its baseline, and the watch now resolves there. net11.0's own resolution setup is left
		// exactly as before — its tag still resolves past its own baseline, and WrongFilePath
		// still has no contents stub at net11.0's baseline — so it still resolves to
		// UnresolvedWatch, provided its issue write below is stubbed so the run does not convert
		// it to InfrastructureError before the aggregation this test exists to weigh ever sees it.
		// net10.0 is listed first in the manifest, ahead of the UnresolvedWatch net11.0 result, so
		// this fixture also rules out a broken aggregation that simply returns the first
		// framework's result outright: that would report Current (exit 0), which this test's
		// assertion catches. (net11.0 first, net10.0 second, would not: Max() and First() agree
		// whenever the first entry already happens to be the worse status.)
		using var workspace = MultiTargetWorkspace(WrongFilePath, api: "none", relationship: "reimplements");
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/releases?per_page=100", Fixture.Read("github/releases.json"));
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11", Fixture.Read("github/ref-v10.0.11-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{WrongFilePath}?ref={Fixture.ReviewedCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/git/ref/tags/{Fixture.Net11ReviewedTag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Fixture.TargetCommit } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.Net11ReviewedCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		// net11.0's unresolved watch files a review issue, so the issue endpoints must be stubbed.
		// Unstubbed they 404, UpsertAsync's catch turns that into a write failure, and Program
		// rebuilds net11.0's result as InfrastructureError before the aggregation ever sees the
		// UnresolvedWatch status this test exists to weigh.
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(3, observation.ExitCode);
	}

	[Fact]
	public async Task Infrastructure_error_in_one_framework_is_not_masked_by_an_unresolved_watch_in_another_framework_in_the_same_run()
	{
		// The amended Verification contract's row for Program.RunAsync's InfrastructureError
		// precedence covers that special case alongside the exit-code cast. Program.RunAsync's
		// ordinal `Max` fallback means a naive fix that simply appended the new status after
		// InfrastructureError in the MonitorStatus declaration — the least-disruptive-looking
		// change, since it leaves Current/Drift/InfrastructureError's existing 0/1/2 exit codes
		// untouched for every other already-passing test — would let an unresolved-but-known
		// finding in one framework outrank a genuine provider/tool failure in another framework
		// for exit-code purposes. Exit code 2 is InfrastructureError's pre-existing, documented
		// meaning (docs/agents/testing.md), not the new status's own ordinal: this asserts the
		// existing outcome must still win when both are present in one run, without saying how
		// the Implementor achieves that (a non-sequential explicit enum value, or an aggregation
		// that checks InfrastructureError before falling back to ordinal Max, are both
		// compatible with this assertion).
		using var workspace = MultiTargetWorkspace(WrongFilePath, api: "none", relationship: "reimplements");
		var transport = new FakeGitHubTransport();
		transport.AddStatus("/repos/dotnet/aspnetcore/releases?per_page=100", System.Net.HttpStatusCode.ServiceUnavailable);
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/git/ref/tags/{Fixture.Net11ReviewedTag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Fixture.TargetCommit } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.Net11ReviewedCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		// net11.0 must stay UnresolvedWatch for the two statuses to actually compete: without these
		// stubs its issue write 404s and Program rebuilds it as InfrastructureError, leaving both
		// frameworks at 2, where plain ordinal Max would answer 2 as well.
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(2, observation.ExitCode);
		using var report = JsonDocument.Parse(observation.JsonReport!);
		var frameworks = report.RootElement.GetProperty("frameworks").EnumerateArray().ToArray();
		Assert.Equal("infrastructure-error", FrameworkStatus(frameworks, "net10.0"));
		Assert.Equal("unresolved-watch", FrameworkStatus(frameworks, "net11.0"));
	}

	[Fact]
	public async Task Unresolved_watch_path_review_issue_is_created_on_github()
	{
		// GitHubIssueUpserter.UpsertAsync gates the actual write on
		// `result.Status`, a second, independent status check in a different file from the one
		// that populates MonitorResult.Issues (MonitorReports.Create). A fix that only widens
		// MonitorReports.Create would leave this path silently unresolved: the in-memory Issues
		// would be populated but never posted, so criterion 3's review issue would never exist on
		// GitHub. This is the exact gap the amended Verification contract's Observation seam now
		// names explicitly: "whether the review issue is actually written through
		// GitHubIssueUpserter... a state that is computed but never written does not satisfy 'the
		// upstream monitor reports it'."
		using var workspace = SingleWatchWorkspace(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Contains(observation.Requests, request => request.Method == HttpMethod.Post && request.PathAndQuery == "/repos/egil/Htmxor/issues");
	}

	[Fact]
	public async Task Failed_issue_write_still_names_the_unresolved_path_in_the_persisted_reports()
	{
		// Program.cs's RunMonitorAsync rebuilds the reports when the GitHub issue write itself
		// fails, because the reports embed the status (MonitorReports.Create(...,
		// MonitorStatus.InfrastructureError, ...)). Before 3e25419 that reconstruction call site
		// omitted result.UnresolvedWatchPaths, so a run that found this exact unresolved path and
		// then failed to write its issue silently dropped the one thing acceptance criterion 1
		// requires the persisted JSON and Markdown reports to name. No create stub for the
		// unresolved-path issue's POST: the create 404s, GitHubApi.WriteAsync throws, and
		// UpsertAsync's outer catch turns that into a non-null issueWrite.Error, which is what
		// forces Program.cs onto the reconstruction branch under test rather than returning the
		// already-correct `result` unchanged.
		using var workspace = SingleWatchWorkspace(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		// Deliberately no create stub: the unresolved-path issue's POST 404s.

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Equal(2, observation.ExitCode);
		Assert.Contains(WrongFilePath, observation.JsonReport, StringComparison.Ordinal);
		Assert.Contains(WrongFilePath, observation.MarkdownReport, StringComparison.Ordinal);
	}

	private static string FrameworkStatus(IReadOnlyList<JsonElement> frameworks, string targetFramework) =>
		frameworks.Single(framework => framework.GetProperty("targetFramework").GetString() == targetFramework)
			.GetProperty("report").GetProperty("status").GetString()!;

	private static TemporaryMonitorWorkspace SingleWatchWorkspace(string path)
	{
		var workspace = new TemporaryMonitorWorkspace();
		var manifestPath = Path.Combine(workspace.Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
		{
			repository = Fixture.Repository,
			reviewed = new { tag = "v10.0.11", commit = Fixture.ReviewedCommit },
			watches = new[] { new { path, match = "file", api = "none", relationship = "reimplements", dependencies = Array.Empty<string>() } },
		}));
		return workspace;
	}
}
