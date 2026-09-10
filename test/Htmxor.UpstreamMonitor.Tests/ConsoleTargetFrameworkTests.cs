using System.Text.Json;
using static Htmxor.UpstreamMonitor.Tests.ConsoleBoundaryTests;

namespace Htmxor.UpstreamMonitor.Tests;

[Collection("Process environment")]
public sealed class ConsoleTargetFrameworkTests
{
	[Fact]
	public async Task All_configured_targets_report_their_own_exact_current_identity()
	{
		using var workspace = MultiTargetWorkspace();
		var transport = new FakeGitHubTransport();
		transport.AddJson(Releases, Fixture.Read("github/releases.json"));
		transport.AddJson(Releases, """[{"tag_name":"v11.0.0-rc.1.26425.128","draft":false,"prerelease":true}]""");
		transport.AddJson(Tag("v10.0.11"), Fixture.Read("github/ref-v10.0.11-direct.json"));
		transport.AddJson(Tag(Fixture.Net11ReviewedTag), Commit(Fixture.Net11ReviewedCommit));

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(0, observation.ExitCode);
		Assert.Equal("Current", observation.StandardOutput);
		Assert.Equal(
			[
				("net10.0", "v10.0.11", Fixture.ReviewedCommit),
				("net11.0", Fixture.Net11ReviewedTag, Fixture.Net11ReviewedCommit),
			],
			Reports(observation.JsonReport!).Select(result => Identity(result.Target, result.Report)));
	}

	[Fact]
	public async Task Selected_target_reports_source_and_API_drift_with_its_own_reviewed_identity()
	{
		using var workspace = MultiTargetWorkspace();
		var transport = new FakeGitHubTransport();
		const string tag = "v11.0.0-rc.1.999";
		transport.AddJson(Tag(tag), Commit(Fixture.TargetCommit));
		transport.AddJson(Compare(Fixture.Net11ReviewedCommit), Fixture.Read("github/compare-api-files.json"));
		AddApiSources(transport, ExpectedMonitorArtifacts.InvokerInterface);
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");

		var observation = await RunAsync(workspace, transport, ["--framework", "net11.0", "--tag", tag]);

		Assert.Equal(1, observation.ExitCode);
		using var report = JsonDocument.Parse(observation.JsonReport!);
		Assert.Equal(Fixture.Net11ReviewedTag, report.RootElement.GetProperty("baseline").GetProperty("tag").GetString());
		Assert.Equal(Fixture.Net11ReviewedCommit, report.RootElement.GetProperty("baseline").GetProperty("commit").GetString());
		Assert.Equal(tag, report.RootElement.GetProperty("upstream").GetProperty("tag").GetString());
		Assert.Equal(Fixture.TargetCommit, report.RootElement.GetProperty("upstream").GetProperty("commit").GetString());
		Assert.NotEmpty(report.RootElement.GetProperty("sourceChanges").EnumerateArray());
		Assert.NotEmpty(report.RootElement.GetProperty("apiChanges").EnumerateArray());
	}

	[Fact]
	public async Task Unknown_target_fails_before_network_or_report_writes()
	{
		using var workspace = MultiTargetWorkspace();
		var observation = await RunAsync(workspace, new FakeGitHubTransport(), ["--framework", "net12.0"]);

		Assert.Equal(2, observation.ExitCode);
		Assert.Equal("The requested target framework is not configured for upstream monitoring.", observation.StandardError);
		Assert.Empty(observation.Requests);
		Assert.Null(observation.JsonReport);
		Assert.Null(observation.MarkdownReport);
	}

	[Fact]
	public async Task Provider_failure_is_recorded_for_each_configured_target()
	{
		using var workspace = MultiTargetWorkspace();
		var transport = new FakeGitHubTransport();
		transport.AddStatus(Releases, System.Net.HttpStatusCode.ServiceUnavailable);
		transport.AddStatus(Releases, System.Net.HttpStatusCode.ServiceUnavailable);

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(2, observation.ExitCode);
		Assert.Equal(2, observation.Requests.Count);
		Assert.All(Reports(observation.JsonReport!), result =>
		{
			var report = result.Report;
			Assert.Equal("infrastructure-error", report.GetProperty("status").GetString());
			Assert.Equal(ExpectedMonitorArtifacts.InfrastructureError, report.GetProperty("infrastructureError").GetString());
		});
	}

	private static TemporaryMonitorWorkspace MultiTargetWorkspace()
	{
		var workspace = new TemporaryMonitorWorkspace();
		var path = Path.Combine(workspace.Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");
		File.WriteAllText(path, JsonSerializer.Serialize(new
		{
			repository = "dotnet/aspnetcore",
			frameworks = new[]
			{
				new { targetFramework = "net10.0", majorVersion = 10, allowsPrerelease = false, referencePackVersion = "10.0.11", reviewed = new { tag = "v10.0.11", commit = Fixture.ReviewedCommit } },
				new { targetFramework = "net11.0", majorVersion = 11, allowsPrerelease = true, referencePackVersion = "11.0.0-rc.1.26425.128", reviewed = new { tag = Fixture.Net11ReviewedTag, commit = Fixture.Net11ReviewedCommit } },
			},
			watches = new[] { new { path = ExpectedMonitorArtifacts.InvokerInterface, match = "file", api = "interface", relationship = "implements", dependencies = Array.Empty<string>() } },
		}));
		return workspace;
	}

	private static IEnumerable<(string Target, JsonElement Report)> Reports(string json)
	{
		using var document = JsonDocument.Parse(json);
		return document.RootElement.GetProperty("frameworks").EnumerateArray()
			.Select(result => (result.GetProperty("targetFramework").GetString()!, result.GetProperty("report").Clone())).ToArray();
	}

	private static (string Target, string Tag, string Commit) Identity(string target, JsonElement report) =>
		(target, report.GetProperty("upstream").GetProperty("tag").GetString()!, report.GetProperty("upstream").GetProperty("commit").GetString()!);

	private static void AddApiSources(FakeGitHubTransport transport, string path)
	{
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{path}?ref={Fixture.Net11ReviewedCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{path}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent("source/target/IRazorComponentEndpointInvoker.cs"));
	}

	private const string Releases = "/repos/dotnet/aspnetcore/releases?per_page=100";
	private static string Tag(string tag) => $"/repos/dotnet/aspnetcore/git/ref/tags/{tag}";
	private static string Compare(string baseline) => $"/repos/dotnet/aspnetcore/compare/{baseline}...{Fixture.TargetCommit}";
	private static string Commit(string commit) => JsonSerializer.Serialize(new { @object = new { type = "commit", sha = commit } });
}
