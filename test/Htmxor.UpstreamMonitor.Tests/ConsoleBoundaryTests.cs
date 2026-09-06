using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

[Collection("Process environment")]
public sealed class ConsoleBoundaryTests
{
	[Fact]
	public async Task Default_invocation_discovers_latest_stable_release_with_environment_token()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/releases?per_page=100", Fixture.Read("github/releases.json"));
		transport.AddJson(
			"/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11",
			Fixture.Read("github/ref-v10.0.11-direct.json"));

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(0, observation.ExitCode);
		Assert.Equal(
			[
				Get("/repos/dotnet/aspnetcore/releases?per_page=100"),
				Get("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11"),
			],
			observation.Requests);
		ReportAssertions.Equal(observation.JsonReport!, observation.MarkdownReport!, ExpectedMonitorArtifacts.CurrentReport());
	}

	[Fact]
	public async Task Explicit_drift_writes_complete_reports_and_authenticated_identity_safe_issue()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = SourceChangeTests.DriftTransport("github/compare-watched-files.json");
		transport.AddJson("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100", "[]");
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");
		var jsonPath = Path.Combine(workspace.Path, "custom", "drift.json");
		var markdownPath = Path.Combine(workspace.Path, "custom", "drift.md");

		var observation = await RunAsync(
			workspace,
			transport,
			["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit, "--json", jsonPath, "--markdown", markdownPath]);

		Assert.Equal(1, observation.ExitCode);
		Assert.Equal(ExpectedDriftRequests(), observation.Requests);
		ReportAssertions.Equal(
			observation.JsonReport!,
			observation.MarkdownReport!,
			ExpectedMonitorArtifacts.SingleFileDriftReport());
	}

	[Fact]
	public async Task Provider_failure_exits_as_infrastructure_error_and_writes_complete_reports()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();
		transport.AddStatus(
			"/repos/dotnet/aspnetcore/releases?per_page=100",
			System.Net.HttpStatusCode.ServiceUnavailable);

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(2, observation.ExitCode);
		Assert.Equal([Get("/repos/dotnet/aspnetcore/releases?per_page=100")], observation.Requests);
		ReportAssertions.Equal(
			observation.JsonReport!,
			observation.MarkdownReport!,
			ExpectedMonitorArtifacts.InfrastructureReport());
	}

	[Fact]
	public async Task Command_line_token_is_rejected_before_network_or_file_writes()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();

		var observation = await RunAsync(workspace, transport, ["--token", "must-not-be-accepted"]);

		Assert.Equal(2, observation.ExitCode);
		Assert.Empty(observation.Requests);
		Assert.Null(observation.JsonReport);
		Assert.Null(observation.MarkdownReport);
		Assert.Equal(string.Empty, observation.StandardOutput);
		Assert.Equal("Tokens are accepted only through the GH_TOKEN environment variable.", observation.StandardError);
	}

	[Theory]
	[InlineData("--tag")]
	[InlineData("--baseline")]
	[InlineData("--json")]
	[InlineData("--markdown")]
	public async Task Duplicate_option_is_rejected_with_concrete_usage_error_before_network_access(string option)
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();
		var arguments = new[] { option, "first-value", option, "second-value" };

		var observation = await RunAsync(workspace, transport, arguments);

		Assert.Equal(2, observation.ExitCode);
		Assert.Empty(observation.Requests);
		Assert.Null(observation.JsonReport);
		Assert.Null(observation.MarkdownReport);
		Assert.Equal(string.Empty, observation.StandardOutput);
		Assert.Equal($"Option '{option}' may only be specified once.", observation.StandardError);
	}

	private static IReadOnlyList<ConsoleRequestObservation> ExpectedDriftRequests() =>
	[
		Get("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12"),
		Get($"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}"),
		Get("/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100"),
		new(
			HttpMethod.Post,
			"/repos/egil/Htmxor/issues",
			IssueCreateBody(ExpectedMonitorArtifacts.SingleFileIssue()),
			"Bearer fixture-token"),
	];

	private static ConsoleRequestObservation Get(string path) =>
		new(HttpMethod.Get, path, null, "Bearer fixture-token");

	private static string IssueCreateBody(IssueUpsertInput issue) => CanonicalJson(
		System.Text.Json.JsonSerializer.Serialize(new
		{
			title = issue.Title,
			body = issue.Body,
			labels = new[] { "upstream-monitor" },
		}));

	private static string CanonicalJson(string json) =>
		System.Text.Json.JsonSerializer.Serialize(System.Text.Json.JsonDocument.Parse(json).RootElement);

	internal static async Task<ConsoleObservation> RunAsync(
		TemporaryMonitorWorkspace workspace,
		FakeGitHubTransport transport,
		IReadOnlyList<string> arguments,
		string? token = "fixture-token")
	{
		using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.github.test") };
		using var standardOutput = new StringWriter();
		using var standardError = new StringWriter();
		using var process = new MonitorProcessScope(workspace.Path, token, client, standardOutput, standardError);
		var exitCode = await Program.Main(arguments.ToArray());
		var jsonPath = ArgumentPath(arguments, "--json") ?? Path.Combine(workspace.Path, "upstream-monitor.json");
		var markdownPath = ArgumentPath(arguments, "--markdown") ?? Path.Combine(workspace.Path, "upstream-monitor.md");

		return new ConsoleObservation(
			exitCode,
			transport.Requests.Select(Observe).ToArray(),
			Read(jsonPath),
			Read(markdownPath),
			standardOutput.ToString().Trim(),
			standardError.ToString().Trim());
	}

	private static string? ArgumentPath(IReadOnlyList<string> arguments, string name)
	{
		var index = arguments.ToList().IndexOf(name);
		return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : null;
	}

	private static string? Read(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

	private static ConsoleRequestObservation Observe(ObservedRequest request) => new(
		request.Method,
		request.PathAndQuery,
		request.Body is null ? null : CanonicalJson(request.Body),
		request.Authorization);

	internal sealed record ConsoleRequestObservation(
		HttpMethod Method,
		string PathAndQuery,
		string? Body,
		string? Authorization);

	internal sealed record ConsoleObservation(
		int ExitCode,
		IReadOnlyList<ConsoleRequestObservation> Requests,
		string? JsonReport,
		string? MarkdownReport,
		string StandardOutput = "",
		string StandardError = "");

	internal sealed class TemporaryMonitorWorkspace : IDisposable
	{
		public TemporaryMonitorWorkspace()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-monitor-console-{Guid.NewGuid():N}");
			Directory.CreateDirectory(System.IO.Path.Combine(Path, "eng", "Htmxor.UpstreamMonitor"));
			File.WriteAllText(
				System.IO.Path.Combine(Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json"),
				ManifestJson());
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);

		private static string ManifestJson() =>
			$$"""
			{
			  "repository": "dotnet/aspnetcore",
			  "reviewed": {
			    "tag": "v10.0.11",
			    "commit": "{{Fixture.ReviewedCommit}}"
			  },
			  "watches": [
			    {
			      "path": "{{ExpectedMonitorArtifacts.Invoker}}",
			      "match": "file",
			      "api": "none",
			      "relationship": "reimplements",
			      "dependencies": []
			    }
			  ]
			}
			""";
	}
}
