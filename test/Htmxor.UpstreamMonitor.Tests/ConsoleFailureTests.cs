using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.ConsoleBoundaryTests;

namespace Htmxor.UpstreamMonitor.Tests;

[Collection("Process environment")]
public sealed class ConsoleFailureTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t ")]
	public async Task Missing_environment_token_fails_before_GitHub_with_complete_secret_safe_reports(string? token)
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = new FakeGitHubTransport();

		var observation = await RunAsync(workspace, transport, [], token);

		const string error = "GH_TOKEN environment variable is required.";
		Assert.Equal(error, observation.StandardError);
		Assert.Equal(2, observation.ExitCode);
		Assert.Empty(observation.Requests);
		Assert.Equal(string.Empty, observation.StandardOutput);
		ReportAssertions.Equal(observation.JsonReport!, observation.MarkdownReport!,
			ExpectedMonitorArtifacts.InfrastructureReport() with { InfrastructureError = error });
	}

	[Theory]
	[InlineData("tag-reference")]
	[InlineData("tag-dereference")]
	[InlineData("compare")]
	[InlineData("content")]
	[InlineData("issue-search")]
	[InlineData("issue-create")]
	[InlineData("issue-reopen-and-update")]
	[InlineData("issue-update")]
	public async Task Downstream_provider_failure_reports_infrastructure_and_stops_later_issue_mutation(string boundary)
	{
		using var workspace = new TemporaryMonitorWorkspace();
		var transport = FailureTransport(boundary);
		ConfigureApiWatch(workspace, boundary);

		var observation = await RunAsync(workspace, transport,
			["--tag", "v10.0.12", "--baseline", Fixture.BaselineCommit]);

		Assert.Equal(ExpectedMonitorArtifacts.InfrastructureError, observation.StandardError);
		Assert.Equal(2, observation.ExitCode);
		var expected = ExpectedReport(boundary);
		ReportAssertions.Equal(observation.JsonReport!, observation.MarkdownReport!, expected);
		Assert.Contains(observation.Requests, request => request.PathAndQuery == FailurePath(boundary));
		Assert.All(observation.Requests, request => Assert.Equal("Bearer fixture-token", request.Authorization));
		Assert.Equal(ExpectedMutations(boundary), observation.Requests
			.Where(request => request.Method != HttpMethod.Get).Select(request => $"{request.Method} {request.PathAndQuery}"));
		Assert.DoesNotContain("fixture-token", observation.StandardError + observation.StandardOutput + observation.JsonReport + observation.MarkdownReport);
		AssertReopenedWithFreshContent(boundary, observation.Requests);
	}

	private static FakeGitHubTransport FailureTransport(string boundary)
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-watched-files.json");
		transport.AddJson(Search, Issues(boundary));
		transport.AddJson("/repos/egil/Htmxor/issues", "{\"number\":42,\"state\":\"open\"}");
		ConfigureAnnotatedTag(transport, boundary);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.Invoker}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText("public abstract class RazorComponentEndpointInvoker { }"));
		transport.ReplaceWithFailure(FailurePath(boundary));

		return transport;
	}

	private static void AssertReopenedWithFreshContent(string boundary, IReadOnlyList<ConsoleRequestObservation> requests)
	{
		if (boundary != "issue-reopen-and-update")
		{
			return;
		}

		var write = Assert.Single(requests.Where(request => request.Method == HttpMethod.Patch));
		using var document = System.Text.Json.JsonDocument.Parse(write.Body!);
		var properties = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString());
		var expected = ExpectedMonitorArtifacts.SingleFileIssue();
		Assert.Equal("open", properties["state"]);
		Assert.True(properties.ContainsKey("title"), "The failed reopen PATCH must include the fresh title.");
		Assert.Equal(expected.Title, properties["title"]);
		Assert.True(properties.ContainsKey("body"), "The failed reopen PATCH must include the fresh body.");
		Assert.Equal(expected.Body, properties["body"]);
	}

	private static void ConfigureAnnotatedTag(FakeGitHubTransport transport, string boundary)
	{
		if (boundary != "tag-dereference")
		{
			return;
		}

		transport.ReplaceJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12",
			Fixture.Read("github/ref-v10.0.10-annotated.json").Replace("v10.0.10", "v10.0.12", StringComparison.Ordinal));
	}

	private static void ConfigureApiWatch(TemporaryMonitorWorkspace workspace, string boundary)
	{
		if (boundary != "content")
		{
			return;
		}

		var path = Path.Combine(workspace.Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");
		File.WriteAllText(path, File.ReadAllText(path).Replace("\"api\": \"none\"", "\"api\": \"subclass\"", StringComparison.Ordinal));
	}

	private static string Issues(string boundary) => boundary switch
	{
		"issue-update" => MatchingIssue("open"),
		"issue-reopen-and-update" => MatchingIssue("closed"),
		_ => "[]",
	};

	private static string MatchingIssue(string state) => System.Text.Json.JsonSerializer.Serialize(new[]
	{
		new { number = 42, state, body = ExpectedMonitorArtifacts.SingleFileIssue().Body, labels = new[] { new { name = "upstream-monitor" } } },
	});

	private static string FailurePath(string boundary) => boundary switch
	{
		"tag-reference" => "/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12",
		"tag-dereference" => "/repos/dotnet/aspnetcore/git/tags/dddddddddddddddddddddddddddddddddddddddd",
		"compare" => $"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
		"content" => $"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.Invoker}?ref={Fixture.BaselineCommit}",
		"issue-search" => Search,
		"issue-create" => "/repos/egil/Htmxor/issues",
		_ => Issue,
	};

	private static IReadOnlyList<string> ExpectedMutations(string boundary) => boundary switch
	{
		"issue-create" => ["POST /repos/egil/Htmxor/issues"],
		"issue-reopen-and-update" or "issue-update" => [$"PATCH {Issue}"],
		_ => [],
	};

	private static ReportExpectation ExpectedReport(string boundary)
	{
		var report = ProviderInventoryTests.FailureReport(ExpectedMonitorArtifacts.InfrastructureError);
		if (boundary.StartsWith("tag-", StringComparison.Ordinal))
		{
			return report with { Upstream = null };
		}

		return boundary.StartsWith("issue-", StringComparison.Ordinal)
			? report with { SourceChanges = ExpectedMonitorArtifacts.SingleFileDriftReport().SourceChanges }
			: report;
	}

	private const string Search = "/repos/egil/Htmxor/issues?state=all&labels=upstream-monitor&per_page=100";
	private const string Issue = "/repos/egil/Htmxor/issues/42";
}
