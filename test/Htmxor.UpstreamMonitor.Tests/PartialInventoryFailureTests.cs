using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.PrefixInventoryFixture;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class PartialInventoryFailureTests
{
	[Theory]
	[InlineData(Fixture.BaselineCommit, RenderingDirectory)]
	[InlineData(Fixture.TargetCommit, RenderingDirectory)]
	[InlineData(Fixture.BaselineCommit, Main)]
	[InlineData(Fixture.TargetCommit, Main)]
	public async Task Unavailable_directory_or_unchanged_partial_cannot_produce_a_drift_report(string revision, string path)
	{
		var transport = ModifiedPartial();
		transport.ReplaceWithFailure(ContentsUrl(path, revision));

		var result = await Fixture.Application(transport).RunAsync(Request());

		AssertInfrastructureFailure(result);
		Assert.Equal("GitHub API returned 503 Service Unavailable.", result.InfrastructureError);
	}

	[Theory]
	[InlineData(Fixture.BaselineCommit)]
	[InlineData(Fixture.TargetCommit)]
	public async Task Directory_inventory_omitting_a_known_existing_changed_partial_is_incomplete(string revision)
	{
		var transport = ModifiedPartial();
		transport.ReplaceJson(ContentsUrl(RenderingDirectory, revision), JsonSerializer.Serialize(new[] { Entry(Main) }));

		var result = await Fixture.Application(transport).RunAsync(Request());

		AssertInfrastructureFailure(result);
	}

	[Theory]
	[InlineData(Fixture.BaselineCommit)]
	[InlineData(Fixture.TargetCommit)]
	public async Task A_file_response_cannot_stand_in_for_a_complete_directory_inventory(string revision)
	{
		var transport = ModifiedPartial();
		transport.ReplaceJson(ContentsUrl(RenderingDirectory, revision), Fixture.GitHubContentText(MainSource));

		var result = await Fixture.Application(transport).RunAsync(Request());

		AssertInfrastructureFailure(result);
	}

	private static void AssertInfrastructureFailure(MonitorResult result)
	{
		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.False(string.IsNullOrWhiteSpace(result.InfrastructureError));
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Null(result.Issue);
		ReportAssertions.Equal(result, ProviderInventoryTests.FailureReport(result.InfrastructureError!));
	}
}
