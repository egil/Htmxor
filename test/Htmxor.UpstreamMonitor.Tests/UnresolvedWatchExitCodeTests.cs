using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.ConsoleBoundaryTests;

namespace Htmxor.UpstreamMonitor.Tests;

[Collection("Process environment")]
public sealed class UnresolvedWatchExitCodeTests
{
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";

	[Fact]
	public async Task Exit_code_for_an_unresolved_watch_path_is_not_the_current_exit_code()
	{
		// docs/agents/testing.md documents exit 0 as meaning Current, and Program.cs:57 returns
		// `(int)status` directly — a pre-existing, documented contract this test does not touch.
		// This does not invent a value for the fourth status the issue requires; it only forces
		// that an unresolved-path run cannot silently exit as if nothing needs attention. The
		// Verification contract names the exit code in its Observation seam explicitly.
		using var workspace = SingleWatchWorkspace(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.NotEqual(0, observation.ExitCode);
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
		using var workspace = MultiFrameworkWorkspace(WrongFilePath);
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

	private static TemporaryMonitorWorkspace SingleWatchWorkspace(string path)
	{
		var workspace = new TemporaryMonitorWorkspace();
		File.WriteAllText(ManifestPath(workspace), JsonSerializer.Serialize(new
		{
			repository = Fixture.Repository,
			reviewed = new { tag = "v10.0.11", commit = Fixture.ReviewedCommit },
			watches = new[] { new { path, match = "file", api = "none", relationship = "reimplements", dependencies = Array.Empty<string>() } },
		}));
		return workspace;
	}

	private static TemporaryMonitorWorkspace MultiFrameworkWorkspace(string path)
	{
		var workspace = new TemporaryMonitorWorkspace();
		File.WriteAllText(ManifestPath(workspace), JsonSerializer.Serialize(new
		{
			repository = Fixture.Repository,
			frameworks = new[]
			{
				new { targetFramework = "net10.0", majorVersion = 10, allowsPrerelease = false, referencePackVersion = "10.0.11", reviewed = new { tag = "v10.0.11", commit = Fixture.ReviewedCommit } },
				new { targetFramework = "net11.0", majorVersion = 11, allowsPrerelease = true, referencePackVersion = "11.0.0-rc.1.26425.128", reviewed = new { tag = Fixture.Net11ReviewedTag, commit = Fixture.Net11ReviewedCommit } },
			},
			watches = new[] { new { path, match = "file", api = "none", relationship = "reimplements", dependencies = Array.Empty<string>() } },
		}));
		return workspace;
	}

	private static string ManifestPath(TemporaryMonitorWorkspace workspace) =>
		Path.Combine(workspace.Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");
}
