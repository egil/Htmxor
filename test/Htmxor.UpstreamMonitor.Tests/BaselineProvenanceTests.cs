using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class BaselineProvenanceTests
{
	[Fact]
	public async Task Custom_baseline_JSON_identifies_the_unresolved_tag_and_exact_commit()
	{
		var result = await CustomBaselineDriftAsync();

		using var report = JsonDocument.Parse(result.JsonReport);
		var baseline = report.RootElement.GetProperty("baseline");
		Assert.Equal(new[] { "unresolved", Fixture.BaselineCommit },
			new[] { baseline.GetProperty("tag").GetString(), baseline.GetProperty("commit").GetString() });
	}

	[Fact]
	public async Task Custom_baseline_Markdown_identifies_the_unresolved_tag_and_exact_commit()
	{
		var result = await CustomBaselineDriftAsync();

		Assert.Contains($"Baseline: unresolved ({Fixture.BaselineCommit})", result.MarkdownReport);
		Assert.DoesNotContain($"Baseline: v10.0.11 ({Fixture.BaselineCommit})", result.MarkdownReport);
	}

	[Fact]
	public async Task Custom_baseline_issue_input_identifies_the_unresolved_tag_and_exact_commit()
	{
		var result = await CustomBaselineDriftAsync();

		var issue = Assert.IsType<IssueUpsertInput>(result.Issue);
		Assert.Contains($"- Previous: [unresolved ({Fixture.BaselineCommit})](https://github.com/dotnet/aspnetcore/tree/{Fixture.BaselineCommit})", issue.Body);
		Assert.DoesNotContain($"v10.0.11 ({Fixture.BaselineCommit})", issue.Body);
		Assert.Contains($"/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}", issue.Body);
	}

	[Fact]
	public async Task Explicit_baseline_equal_to_reviewed_commit_preserves_the_reviewed_tag()
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11",
			Fixture.Read("github/ref-v10.0.11-direct.json"));

		var result = await Fixture.Application(transport).RunAsync(new MonitorRequest(Fixture.Manifest(), 10,
			RequestedTag: "v10.0.11", BaselineCommit: Fixture.ReviewedCommit));

		ReportAssertions.Equal(result, ExpectedMonitorArtifacts.CurrentReport());
	}

	private static async Task<MonitorResult> CustomBaselineDriftAsync()
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-watched-files.json");

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(ExpectedMonitorArtifacts.Invoker)));

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Null(result.InfrastructureError);
		return result;
	}
}
