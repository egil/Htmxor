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
		// explicit assignment redden this test. Acceptance criterion 1 only requires a non-current
		// result; a design that shared Drift's exit code 1 for an unresolved watch, distinguished
		// instead in the JSON report, the Markdown report and the review issue (criterion 3), would
		// also have been compliant. `3` pins the exit code the delivered implementation actually
		// chose, not a requirement the acceptance criteria impose on their own.
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
		// Program.cs aggregates a multi-framework run's exit code by letting InfrastructureError win
		// outright and falling back to an ordinal Max over MonitorStatus for every other
		// combination; the sibling test below pins the InfrastructureError half. This one pins the
		// fallback half: a run must not exit 0 (Current) merely because one configured framework
		// happened to be clean while another carried an unresolved watch, and 3 is the unresolved
		// framework's own documented exit code (docs/agents/testing.md).
		//
		// The fixture must make the two frameworks genuinely differ. Stubbing WrongFilePath's
		// contents at net10.0's reviewed commit makes net10.0 Current: upstream has not moved past
		// its baseline and the watch resolves there. net11.0's tag resolves past its own baseline
		// and WrongFilePath has no contents stub there, so it stays UnresolvedWatch — provided its
		// issue write is stubbed below, or the run converts it to InfrastructureError before the
		// aggregation under test ever sees it. net10.0 is listed first, so this fixture also rules
		// out an aggregation that returns the first framework's result: that would exit 0.
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
		// Program.RunAsync lets InfrastructureError win outright and falls back to an ordinal Max
		// over MonitorStatus only for the rest, so an unresolved-but-known finding in one
		// framework cannot outrank a genuine provider/tool failure in another. A naive fix that
		// only appended the new status after InfrastructureError in the MonitorStatus declaration,
		// leaving a plain ordinal Max in place, would have reversed the two. Exit code 2 is
		// InfrastructureError's documented meaning (docs/agents/testing.md), not an ordinal
		// position: this asserts the existing outcome still wins when both are present, without
		// pinning how (a non-sequential enum value would satisfy it too).
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
		// GitHubIssueUpserter.UpsertAsync gates the actual write on `result.Status`, a second,
		// independent status check in a different file from the one that populates
		// MonitorResult.Issues (MonitorReports.Create). A fix that only widened
		// MonitorReports.Create would leave this path silently unresolved: the in-memory Issues
		// would be populated but never posted, so the review issue would never exist on GitHub — a
		// state that is computed but never written does not satisfy that the monitor reports an
		// unresolved path.
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
		// fails, because the reports embed the status, so the rebuild has to carry
		// result.UnresolvedWatchPaths across: otherwise a run that found this exact unresolved
		// path and then failed to write its issue silently drops the one thing the persisted JSON
		// and Markdown reports must name. No create stub for the unresolved-path issue's POST: the
		// create 404s, GitHubApi.WriteAsync throws, and UpsertAsync's outer catch turns that into a
		// non-null issueWrite.Error, which is what forces Program.cs onto the reconstruction branch
		// under test rather than returning the already-correct `result` unchanged.
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
