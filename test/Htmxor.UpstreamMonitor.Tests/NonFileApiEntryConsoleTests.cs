using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.ConsoleBoundaryTests;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #244 at the console boundary. #240's own WrongKindWatchConsoleTests covers a wrong-kind
// watch the compare never matches (resolved through UpstreamRepository.ResolveAsync), not this
// issue's own route: a `file` watch with an API surface whose path the compare does match. Program.
// RunAsync, the exit code, and the persisted reports are exercised only here for that route.
[Collection("Process environment")]
public sealed class NonFileApiEntryConsoleTests
{
	private const string WrongKindPath = "src/Components/Endpoints/src/CacheView/LinkedEntry";

	[Fact]
	public async Task Symlink_named_file_watch_beside_a_drifting_watch_exits_3_and_upserts_both_issues()
	{
		using var workspace = WrongKindWatchConsoleTests.Workspace(
			Watch(ExpectedMonitorArtifacts.InvokerInterface, "interface", "implements"),
			Watch(WrongKindPath, "subclass", "subclasses"));
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new object[]
				{
					new { filename = ExpectedMonitorArtifacts.InvokerInterface, status = "modified" },
					new { filename = WrongKindPath, status = "modified" },
				},
			}));
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent("source/target/IRazorComponentEndpointInvoker.cs"));
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{WrongKindPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new { type = "symlink", target = "../CacheView.cs" }));
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{WrongKindPath}?ref={Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { type = "symlink", target = "../CacheView.cs" }));
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":43,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, ["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Equal(3, observation.ExitCode);
		Assert.Contains("exists-as-symlink", observation.JsonReport, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-symlink | {WrongKindPath}", observation.MarkdownReport, StringComparison.Ordinal);
		Assert.Equal(
			2,
			observation.Requests.Count(request => request.Method == HttpMethod.Post && request.PathAndQuery == "/repos/egil/Htmxor/issues"));
	}

	private static object Watch(string path, string api, string relationship) =>
		new { path, match = "file", api, relationship, dependencies = Array.Empty<string>() };
}
