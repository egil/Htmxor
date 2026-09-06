using System.Text.Json;
using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.PrefixInventoryFixture;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class CompletePartialInventoryTests
{
	[Fact]
	public async Task Adding_one_partial_reports_its_new_member_while_the_unchanged_type_remains()
	{
		var transport = Transport("added");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource), (Auxiliary, Partial("public void Introduced() { }")));

		var result = await Fixture.Application(transport).RunAsync(Request());

		ReportAssertions.Equal(result, Drift("added",
			new ApiReportRow("EndpointHtmlRenderer", "added", "member", "public void Introduced()", "extensibility-opportunity")));
		var issue = Assert.IsType<IssueUpsertInput>(result.Issue);
		Assert.Contains("Extensibility opportunity | added | member | EndpointHtmlRenderer | public void Introduced()", issue.Body);
		Assert.DoesNotContain("| type |", issue.Body);
	}

	[Fact]
	public async Task Removing_an_empty_partial_keeps_the_unchanged_type_and_reports_only_source_drift()
	{
		var transport = Transport("removed");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource), (Auxiliary, Partial()));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource));

		var result = await Fixture.Application(transport).RunAsync(Request());

		ReportAssertions.Equal(result, Drift("removed"));
		Assert.DoesNotContain("| type |", Assert.IsType<IssueUpsertInput>(result.Issue).Body);
	}

	[Fact]
	public async Task Removing_one_partial_reports_its_lost_member_while_the_unchanged_type_remains()
	{
		var transport = Transport("removed");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource), (Auxiliary, Partial("protected void Retired() { }")));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource));

		var result = await Fixture.Application(transport).RunAsync(Request());

		ReportAssertions.Equal(result, Drift("removed",
			new ApiReportRow("EndpointHtmlRenderer", "removed", "member", "protected void Retired()", "compatibility-risk")));
		var issue = Assert.IsType<IssueUpsertInput>(result.Issue);
		Assert.Contains("Compatibility risk | removed | member | EndpointHtmlRenderer | protected void Retired()", issue.Body);
		Assert.DoesNotContain("| type |", issue.Body);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Directory_inventory_order_preserves_exact_aggregate_member_rows(bool reverseInventory)
	{
		var transport = Transport("added");
		var baseline = new[] { (Main, MainSource), (Prefix + ".Stable.cs", Partial("protected void Stable() { }")) };
		var target = baseline.Append((Auxiliary, Partial("public void Zebra() { } public void Alpha() { }"))).ToArray();
		Revision(transport, Fixture.BaselineCommit, reverseInventory ? baseline.Reverse().ToArray() : baseline);
		Revision(transport, Fixture.TargetCommit, reverseInventory ? target.Reverse().ToArray() : target);

		var result = await Fixture.Application(transport).RunAsync(Request());

		ReportAssertions.Equal(result, Drift("added",
			new ApiReportRow("EndpointHtmlRenderer", "added", "member", "public void Alpha()", "extensibility-opportunity"),
			new ApiReportRow("EndpointHtmlRenderer", "added", "member", "public void Zebra()", "extensibility-opportunity")));
	}

	[Fact]
	public async Task Unrelated_directory_entries_do_not_enter_the_watched_partial_surface()
	{
		var transport = Transport("added");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource), (Auxiliary, Partial("public void Introduced() { }")));
		var unrelated = new[] { Entry(RenderingDirectory + "/Other.cs"), Entry(Prefix + ".Nested", "dir") };
		transport.ReplaceJson(ContentsUrl(RenderingDirectory, Fixture.BaselineCommit),
			JsonSerializer.Serialize(new[] { Entry(Main) }.Concat(unrelated)));
		transport.ReplaceJson(ContentsUrl(RenderingDirectory, Fixture.TargetCommit),
			JsonSerializer.Serialize(new[] { Entry(Main), Entry(Auxiliary) }.Concat(unrelated.Reverse())));

		var result = await Fixture.Application(transport).RunAsync(Request());

		ReportAssertions.Equal(result, Drift("added",
			new ApiReportRow("EndpointHtmlRenderer", "added", "member", "public void Introduced()", "extensibility-opportunity")));
	}
}
