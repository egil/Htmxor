using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ConsoleBoundaryTests
{
	[Fact]
	public async Task Default_invocation_discovers_latest_stable_release_with_environment_token()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();
		transport.AddJson(
			"/repos/dotnet/aspnetcore/releases?per_page=100",
			Fixture.Read("github/releases.json"));
		transport.AddJson(
			"/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11",
			Fixture.Read("github/ref-v10.0.11-direct.json"));

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(
			new ConsoleObservation(
				0,
				string.Join('\n',
					"GET /repos/dotnet/aspnetcore/releases?per_page=100 Bearer fixture-token",
					"GET /repos/dotnet/aspnetcore/git/ref/tags/v10.0.11 Bearer fixture-token"),
				"current",
				"current"),
			observation);
	}

	[Fact]
	public async Task Explicit_tag_and_baseline_write_both_reports_and_exit_nonzero_for_drift()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = SourceChangeTests.DriftTransport("github/compare-watched-files.json");
		var jsonPath = Path.Combine(workspace.Path, "custom", "drift.json");
		var markdownPath = Path.Combine(workspace.Path, "custom", "drift.md");

		var observation = await RunAsync(
			workspace,
			transport,
			[
				"--tag", "v10.0.12",
				"--baseline", Fixture.BaselineCommit,
				"--json", jsonPath,
				"--markdown", markdownPath,
			]);

		Assert.Equal(1, observation.ExitCode);
		Assert.Contains($"GET /repos/dotnet/aspnetcore/git/ref/tags/v10.0.12 Bearer fixture-token", observation.Requests, StringComparison.Ordinal);
		Assert.Contains($"GET /repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit} Bearer fixture-token", observation.Requests, StringComparison.Ordinal);
		Assert.Equal("drift", observation.JsonStatus);
		Assert.Equal("drift", observation.MarkdownStatus);
	}

	[Fact]
	public async Task Provider_failure_exits_as_infrastructure_error_and_still_owns_both_reports()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();
		transport.AddStatus(
			"/repos/dotnet/aspnetcore/releases?per_page=100",
			System.Net.HttpStatusCode.ServiceUnavailable);

		var observation = await RunAsync(workspace, transport, []);

		Assert.Equal(
			new ConsoleObservation(
				2,
				"GET /repos/dotnet/aspnetcore/releases?per_page=100 Bearer fixture-token",
				"infrastructure-error",
				"infrastructure-error"),
			observation);
	}

	[Fact]
	public async Task Command_line_token_is_rejected_before_network_or_file_writes()
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();

		var observation = await RunAsync(workspace, transport, ["--token", "must-not-be-accepted"]);

		Assert.Equal(
			new ConsoleObservation(
				2,
				string.Empty,
				null,
				null,
				string.Empty,
				"Tokens are accepted only through the GH_TOKEN environment variable."),
			observation);
	}

	private static async Task<ConsoleObservation> RunAsync(
		TemporaryMonitorWorkspace workspace,
		FakeGitHubTransport transport,
		IReadOnlyList<string> arguments)
	{
		using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.github.test") };
		using var standardOutput = new StringWriter();
		using var standardError = new StringWriter();
		var exitCode = await Program.RunAsync(
			arguments,
			name => name == "GH_TOKEN" ? "fixture-token" : null,
			client,
			workspace.Path,
			standardOutput,
			standardError);
		var jsonPath = ArgumentPath(arguments, "--json") ?? Path.Combine(workspace.Path, "upstream-monitor.json");
		var markdownPath = ArgumentPath(arguments, "--markdown") ?? Path.Combine(workspace.Path, "upstream-monitor.md");

		return new ConsoleObservation(
			exitCode,
			string.Join('\n', transport.Requests.Select(request =>
				$"{request.Method} {request.PathAndQuery} {request.Authorization}")),
			ReadJsonStatus(jsonPath),
			ReadMarkdownStatus(markdownPath),
			standardOutput.ToString().Trim(),
			standardError.ToString().Trim());
	}

	private static string? ArgumentPath(IReadOnlyList<string> arguments, string name)
	{
		var index = arguments.ToList().IndexOf(name);
		return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : null;
	}

	private static string? ReadJsonStatus(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
		return document.RootElement.GetProperty("status").GetString();
	}

	private static string? ReadMarkdownStatus(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		return File.ReadLines(path)
			.First(line => line.StartsWith("Status:", StringComparison.OrdinalIgnoreCase))
			.Split(':', 2)[1]
			.Trim()
			.ToLowerInvariant();
	}

	private sealed record ConsoleObservation(
		int ExitCode,
		string Requests,
		string? JsonStatus,
		string? MarkdownStatus,
		string StandardOutput = "",
		string StandardError = "");

	private sealed class TemporaryMonitorWorkspace : IDisposable
	{
		public TemporaryMonitorWorkspace()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-monitor-console-{Guid.NewGuid():N}");
			Directory.CreateDirectory(System.IO.Path.Combine(Path, "eng", "Htmxor.UpstreamMonitor"));
			File.WriteAllText(
				System.IO.Path.Combine(Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json"),
				"{\"repository\":\"dotnet/aspnetcore\",\"reviewed\":{\"tag\":\"v10.0.11\",\"commit\":\"a5383385245bdacc20ec19f30e46090a8154d8da\"},\"watches\":[]}");
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
