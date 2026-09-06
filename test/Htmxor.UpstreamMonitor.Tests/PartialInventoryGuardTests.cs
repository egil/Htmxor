using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.PrefixInventoryFixture;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class PartialInventoryGuardTests
{
	[Fact]
	public async Task A_directory_at_the_provider_limit_cannot_establish_a_complete_surface()
	{
		var result = await RunWithExtraEntriesAsync(UnrelatedEntries(998));

		AssertInfrastructureFailure(result);
	}

	[Fact]
	public async Task A_directory_below_the_provider_limit_preserves_the_exact_drift_report()
	{
		var result = await RunWithExtraEntriesAsync(UnrelatedEntries(997));

		ReportAssertions.Equal(result, Drift("changed",
			new ApiReportRow("EndpointHtmlRenderer", "added", "member", "public void Introduced()", "extensibility-opportunity"),
			new ApiReportRow("EndpointHtmlRenderer", "removed", "member", "public void Retired()", "compatibility-risk")));
	}

	[Fact]
	public async Task A_repeated_directory_path_cannot_establish_a_complete_surface()
	{
		var result = await RunWithExtraEntriesAsync([Entry(Main)]);

		AssertInfrastructureFailure(result);
	}

	[Theory]
	[InlineData("src/Other.cs")]
	[InlineData(RenderingDirectory + "/Nested/Other.cs")]
	public async Task A_path_outside_the_requested_directory_cannot_establish_a_complete_surface(string path)
	{
		var result = await RunWithExtraEntriesAsync([Entry(path)]);

		AssertInfrastructureFailure(result);
	}

	[Fact]
	public async Task An_unsupported_entry_kind_cannot_establish_a_complete_surface()
	{
		var result = await RunWithExtraEntriesAsync([Entry(RenderingDirectory + "/Other.cs", "unknown")]);

		AssertInfrastructureFailure(result);
	}

	[Theory]
	[InlineData("{\"type\":\"file\"}")]
	[InlineData("{\"path\":null,\"type\":\"file\"}")]
	[InlineData("{\"path\":42,\"type\":\"file\"}")]
	[InlineData("{\"path\":\"$directory/Other.cs\"}")]
	[InlineData("{\"path\":\"$directory/Other.cs\",\"type\":null}")]
	[InlineData("{\"path\":\"$directory/Other.cs\",\"type\":true}")]
	public async Task Malformed_required_entry_data_reports_an_actionable_inventory_error(string entryJson)
	{
		using var entry = JsonDocument.Parse(entryJson.Replace("$directory", RenderingDirectory, StringComparison.Ordinal));
		var result = await RunWithExtraEntriesAsync([entry.RootElement]);

		AssertInfrastructureFailure(result);
		Assert.Equal("GitHub directory inventory is invalid or reached the 1000-entry limit; completeness is unknown.", result.InfrastructureError);
	}

	private static IEnumerable<object> UnrelatedEntries(int count) =>
		Enumerable.Range(0, count).Select(index => Entry(RenderingDirectory + $"/Other{index}.cs"));

	private static async Task<MonitorResult> RunWithExtraEntriesAsync(IEnumerable<object> entries)
	{
		var transport = ModifiedPartial();
		transport.ReplaceJson(ContentsUrl(RenderingDirectory, Fixture.TargetCommit),
			JsonSerializer.Serialize(new[] { Entry(Main), Entry(Auxiliary) }.Concat(entries)));
		return await Fixture.Application(transport).RunAsync(Request());
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
