using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.ConsoleBoundaryTests;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #240, LR-324c38a-P007: every other wrong-kind test drives UpstreamMonitorApplication
// directly and reads MonitorResult. The exit code the workflow acts on and the JSON/Markdown
// files it uploads are produced only by Program.RunAsync, and Program.RunMonitorAsync rebuilds
// both reports from result.UnresolvedWatchPaths when the issue write itself fails — a second,
// independent path that must also carry the finding word once it exists, not just the path
// string the field carries today. These tests close that gap at the console boundary
// UnresolvedWatchConsoleTests already exercises for the absent-path shape.
[Collection("Process environment")]
public sealed class WrongKindWatchConsoleTests
{
	private const string DirectoryPath = "src/Components/Endpoints/src/CacheView";

	[Fact]
	public async Task Wrong_kind_watch_and_a_drifting_watch_exit_3_with_the_finding_word_in_both_persisted_reports()
	{
		using var workspace = Workspace(Watch(DirectoryPath), Watch(ExpectedMonitorArtifacts.Invoker));
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" } } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryPath}/Nested.cs") }));
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":43,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Equal(3, observation.ExitCode);
		Assert.Contains("exists-as-directory", observation.JsonReport, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-directory | {DirectoryPath}", observation.MarkdownReport, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Failed_issue_write_still_names_the_wrong_kind_finding_in_the_persisted_reports()
	{
		using var workspace = Workspace(Watch(DirectoryPath));
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryPath}/Nested.cs") }));
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		// Deliberately no create stub: the unresolved-path issue's POST 404s, which is what forces
		// Program.cs onto the InfrastructureError reconstruction branch under test.

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Equal(2, observation.ExitCode);
		Assert.Contains("exists-as-directory", observation.JsonReport, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-directory | {DirectoryPath}", observation.MarkdownReport, StringComparison.Ordinal);
	}

	private static object Watch(string path) =>
		new { path, match = "file", api = "none", relationship = "reimplements", dependencies = Array.Empty<string>() };

	private static TemporaryMonitorWorkspace Workspace(params object[] watches)
	{
		var workspace = new TemporaryMonitorWorkspace();
		var manifestPath = Path.Combine(workspace.Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
		{
			repository = Fixture.Repository,
			reviewed = new { tag = "v10.0.11", commit = Fixture.ReviewedCommit },
			watches,
		}));
		return workspace;
	}
}
