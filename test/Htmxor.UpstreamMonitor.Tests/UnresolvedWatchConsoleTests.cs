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
		// Program.cs:57 returns `(int)status` directly — a pre-existing, documented contract this
		// test does not touch or extend. `!= 0` is required directly by acceptance criterion 1
		// ("a non-current result"). `!= 2` is defense-in-depth at this same CLI surface for a
		// counter-implementation MonitorOutcomeTests.Current_or_infrastructure_outcome_never_
		// writes_an_issue already forbids at the application level (an unresolved path silently
		// misclassified as InfrastructureError, which never populates Issue). No exclusion for
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

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.NotEqual(0, observation.ExitCode);
		Assert.NotEqual(2, observation.ExitCode);
	}

	[Fact]
	public async Task Unresolved_watch_path_in_one_framework_is_not_masked_by_a_current_framework_in_the_same_run()
	{
		// Program.cs:48 aggregates a multi-framework run's exit code with `results.Max(...)`, an
		// ordinal comparison across MonitorStatus. No existing console-level test exercises that
		// aggregation across differing statuses from multiple frameworks in one invocation. This
		// does not pin where the new status sits in the enum declaration or a specific integer —
		// only that a run cannot exit 0 (Current) merely because one configured framework
		// happened to be clean while another carried an unresolved watch.
		using var workspace = MultiTargetWorkspace(WrongFilePath, api: "none", relationship: "reimplements");
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/releases?per_page=100", Fixture.Read("github/releases.json"));
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11", Fixture.Read("github/ref-v10.0.11-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/git/ref/tags/{Fixture.Net11ReviewedTag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Fixture.TargetCommit } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.Net11ReviewedCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));

		var observation = await RunAsync(workspace, transport, []);

		Assert.NotEqual(0, observation.ExitCode);
	}

	[Fact]
	public async Task Infrastructure_error_in_one_framework_is_not_masked_by_an_unresolved_watch_in_another_framework_in_the_same_run()
	{
		// The amended Verification contract's row for Program.cs:49,57 covers the
		// InfrastructureError special case alongside the exit-code cast. Program.cs:48's ordinal
		// `Max` means a naive fix that simply appends the new status after InfrastructureError in
		// the MonitorStatus declaration — the least-disruptive-looking change, since it leaves
		// Current/Drift/InfrastructureError's existing 0/1/2 exit codes untouched for every other
		// already-passing test — would let an unresolved-but-known finding in one framework
		// outrank a genuine provider/tool failure in another framework for exit-code purposes.
		// Exit code 2 is InfrastructureError's pre-existing, documented meaning (docs/agents/
		// testing.md), not the new status's own ordinal: this asserts the existing outcome must
		// still win when both are present in one run, without saying how the Implementor achieves
		// that (a non-sequential explicit enum value, or an aggregation that checks
		// InfrastructureError before falling back to ordinal Max, are both compatible with this
		// assertion).
		using var workspace = MultiTargetWorkspace(WrongFilePath, api: "none", relationship: "reimplements");
		var transport = new FakeGitHubTransport();
		transport.AddStatus("/repos/dotnet/aspnetcore/releases?per_page=100", System.Net.HttpStatusCode.ServiceUnavailable);
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/git/ref/tags/{Fixture.Net11ReviewedTag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Fixture.TargetCommit } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.Net11ReviewedCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(2, observation.ExitCode);
	}

	[Fact]
	public async Task Unresolved_watch_path_review_issue_is_created_on_github()
	{
		// GitHubIssueUpserter.UpsertAsync (GitHubIssueUpserter.cs:9) gates the actual write on
		// `result.Status == MonitorStatus.Drift`, a second, independent status check in a
		// different file from the one that populates MonitorResult.Issue
		// (MonitorReports.Create). A fix that only widens MonitorReports.Create would leave this
		// path silently unresolved: the in-memory Issue would be populated but never posted, so
		// criterion 3's review issue would never exist on GitHub. This is the exact gap the
		// amended Verification contract's Observation seam now names explicitly: "whether the
		// review issue is actually written through GitHubIssueUpserter... a state that is
		// computed but never written does not satisfy 'the upstream monitor reports it'."
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
