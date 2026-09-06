using Htmxor.UpstreamMonitor;
using static Htmxor.UpstreamMonitor.Tests.PrefixInventoryFixture;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class EffectivePartialTypeTests
{
	private const string ImplicitAccessibility = "partial class EndpointHtmlRenderer { }";

	[Fact]
	public async Task Adding_an_empty_partial_with_implicit_accessibility_reports_only_source_drift()
	{
		var transport = Transport("added");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource), (Auxiliary, ImplicitAccessibility));

		var result = await Fixture.Application(transport).RunAsync(SubclassRequest());

		AssertSourceOnly(result, "added");
	}

	[Fact]
	public async Task Removing_an_empty_partial_with_implicit_accessibility_reports_only_source_drift()
	{
		var transport = Transport("removed");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource), (Auxiliary, ImplicitAccessibility));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource));

		var result = await Fixture.Application(transport).RunAsync(SubclassRequest());

		AssertSourceOnly(result, "removed");
	}

	[Fact]
	public async Task Adding_the_only_partial_declaration_reports_a_real_type_opportunity()
	{
		var transport = Transport("added");
		Revision(transport, Fixture.BaselineCommit);
		Revision(transport, Fixture.TargetCommit, (Auxiliary, Partial()));

		var result = await Fixture.Application(transport).RunAsync(SubclassRequest());

		ReportAssertions.Equal(result, Report("added", "extensibility-opportunity",
			new ApiReportRow("EndpointHtmlRenderer", "added", "type", "internal partial class EndpointHtmlRenderer", "extensibility-opportunity")));
		Assert.Contains("Extensibility opportunity | added | type | EndpointHtmlRenderer",
			Assert.IsType<IssueUpsertInput>(result.Issue).Body);
	}

	[Fact]
	public async Task Removing_the_only_partial_declaration_reports_a_real_type_compatibility_risk()
	{
		var transport = Transport("removed");
		Revision(transport, Fixture.BaselineCommit, (Auxiliary, Partial()));
		Revision(transport, Fixture.TargetCommit);

		var result = await Fixture.Application(transport).RunAsync(SubclassRequest());

		ReportAssertions.Equal(result, Report("removed", "compatibility-risk",
			new ApiReportRow("EndpointHtmlRenderer", "removed", "type", "internal partial class EndpointHtmlRenderer", "compatibility-risk")));
		Assert.Contains("Compatibility risk | removed | type | EndpointHtmlRenderer",
			Assert.IsType<IssueUpsertInput>(result.Issue).Body);
	}

	private static MonitorRequest SubclassRequest() => ProviderInventoryTests.Request(
		Fixture.Watch(Prefix, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses));

	private static void AssertSourceOnly(MonitorResult result, string kind)
	{
		ReportAssertions.Equal(result, Report(kind, "implementation-review"));
		Assert.Empty(result.ApiChanges);
		var issue = Assert.IsType<IssueUpsertInput>(result.Issue);
		Assert.Contains($"Implementation review | {kind} | {Auxiliary}", issue.Body);
		Assert.DoesNotContain("Extensibility opportunity", issue.Body);
		Assert.DoesNotContain("Compatibility risk", issue.Body);
	}

	private static ReportExpectation Report(string kind, string classification, params ApiReportRow[] apis) => new("drift",
		new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
		[new(Auxiliary, kind, classification)], apis, null);
}
