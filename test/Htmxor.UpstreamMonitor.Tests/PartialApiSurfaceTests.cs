using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class PartialApiSurfaceTests
{
	private const string Prefix = "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer";
	private const string First = Prefix + ".Events.cs";
	private const string Second = Prefix + ".Rendering.cs";
	private const string Moved = "public void Render()";
	private const string Removed = "protected void Retired()";
	private const string Added = "public void Introduced()";

	[Theory]
	[InlineData("public")]
	[InlineData("protected")]
	public async Task Moving_a_member_between_changed_partials_reports_only_source_drift(string visibility)
	{
		var transport = Transport((First, "modified"), (Second, "modified"));
		AddSources(transport, First, Partial($"{visibility} void Render() {{ }}"), Partial());
		AddSources(transport, Second, Partial(), Partial($"{visibility} void Render() {{ }}"));

		var result = await Fixture.Application(transport).RunAsync(Request());

		Assert.Empty(result.ApiChanges);
		ReportAssertions.Equal(result, DriftReport(
			[new(First, "changed", "parity-required"), new(Second, "changed", "parity-required")], []));
		Assert.DoesNotContain("| member |", Assert.IsType<IssueUpsertInput>(result.Issue).Body);
	}

	[Fact]
	public async Task Moving_an_entire_partial_to_a_new_file_preserves_the_aggregate_API()
	{
		var transport = Transport((First, "removed"), (Second, "added"));
		transport.AddJson(SourceUrl(First, Fixture.BaselineCommit), Fixture.GitHubContentText(Partial($"{Moved} {{ }}")));
		transport.AddJson(SourceUrl(Second, Fixture.TargetCommit), Fixture.GitHubContentText(Partial($"{Moved} {{ }}")));

		var result = await Fixture.Application(transport).RunAsync(Request());

		Assert.Empty(result.ApiChanges);
		ReportAssertions.Equal(result, DriftReport(
			[new(First, "removed", "parity-required"), new(Second, "added", "parity-required")], []));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Genuine_aggregate_changes_have_exact_stable_rows_without_the_moved_member(bool reverseInventory)
	{
		var files = new[] { (First, "modified"), (Second, "modified") };
		var transport = Transport(reverseInventory ? files.Reverse().ToArray() : files);
		AddSources(transport, First, Partial($"{Moved} {{ }}"), Partial());
		AddSources(transport, Second, Partial($"{Removed} {{ }}"), Partial($"{Moved} {{ }} {Added} {{ }}"));

		var result = await Fixture.Application(transport).RunAsync(Request());

		Assert.Equal(new[]
		{
			new ApiChange("EndpointHtmlRenderer", ChangeKind.Added, ApiSymbolKind.Member, Added, ReviewClassification.ExtensibilityOpportunity),
			new ApiChange("EndpointHtmlRenderer", ChangeKind.Removed, ApiSymbolKind.Member, Removed, ReviewClassification.CompatibilityRisk),
		}, result.ApiChanges);
		ReportAssertions.Equal(result, DriftReport(
			[new(First, "changed", "parity-required"), new(Second, "changed", "parity-required")],
			[new("EndpointHtmlRenderer", "added", "member", Added, "extensibility-opportunity"),
			 new("EndpointHtmlRenderer", "removed", "member", Removed, "compatibility-risk")]));
	}

	[Fact]
	public async Task Exact_file_watch_still_reports_a_member_leaving_its_watched_file()
	{
		var transport = Transport((First, "modified"), (Second, "modified"));
		AddSources(transport, First, Partial($"{Moved} {{ }}"), Partial());
		AddSources(transport, Second, Partial(), Partial($"{Moved} {{ }}"));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(First, apiSurface: ApiSurface.Subclass)));

		ReportAssertions.Equal(result, DriftReport([new(First, "changed", "parity-required")],
			[new("EndpointHtmlRenderer", "removed", "member", Moved, "compatibility-risk")]));
	}

	[Fact]
	public async Task Separate_prefix_watches_do_not_cancel_independent_API_changes()
	{
		const string otherPrefix = "src/Other/EndpointHtmlRenderer";
		const string otherPath = otherPrefix + ".cs";
		var transport = Transport((First, "modified"), (otherPath, "modified"));
		AddSources(transport, First, Partial($"{Moved} {{ }}"), Partial());
		AddSources(transport, otherPath, Partial(), Partial($"{Moved} {{ }}"));
		var request = new MonitorRequest(Fixture.Manifest(
			Fixture.Watch(Prefix, WatchMatch.Prefix, ApiSurface.Subclass),
			Fixture.Watch(otherPrefix, WatchMatch.Prefix, ApiSurface.Subclass)), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		ReportAssertions.Equal(result, DriftReport(
			[new(First, "changed", "parity-required"), new(otherPath, "changed", "parity-required")],
			[new("EndpointHtmlRenderer", "added", "member", Moved, "extensibility-opportunity"),
			 new("EndpointHtmlRenderer", "removed", "member", Moved, "compatibility-risk")]));
	}

	[Theory]
	[InlineData(Fixture.BaselineCommit)]
	[InlineData(Fixture.TargetCommit)]
	public async Task Unavailable_partial_revision_reports_infrastructure_failure_without_partial_drift(string failedRevision)
	{
		var transport = Transport((First, "modified"), (Second, "modified"));
		AddSources(transport, First, Partial($"{Moved} {{ }}"), Partial());
		AddSources(transport, Second, Partial(), Partial($"{Moved} {{ }}"));
		transport.ReplaceWithFailure(SourceUrl(Second, failedRevision));

		var result = await Fixture.Application(transport).RunAsync(Request());

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Equal("GitHub API returned 503 Service Unavailable.", result.InfrastructureError);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Null(result.Issue);
		ReportAssertions.Equal(result, ProviderInventoryTests.FailureReport("GitHub API returned 503 Service Unavailable."));
	}

	private static MonitorRequest Request() => ProviderInventoryTests.Request(Fixture.Watch(Prefix, WatchMatch.Prefix, ApiSurface.Subclass));

	private static FakeGitHubTransport Transport(params (string Path, string Status)[] files)
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson($"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = files.Select(file => new { filename = file.Path, status = file.Status }) }));
		foreach (var directory in files.GroupBy(file => file.Path[..file.Path.LastIndexOf('/')]))
		{
			PrefixInventoryFixture.Listing(transport, directory.Key, Fixture.BaselineCommit,
				directory.Where(file => file.Status != "added").Select(file => file.Path));
			PrefixInventoryFixture.Listing(transport, directory.Key, Fixture.TargetCommit,
				directory.Where(file => file.Status != "removed").Select(file => file.Path));
		}
		return transport;
	}

	private static void AddSources(FakeGitHubTransport transport, string path, string before, string after)
	{
		transport.AddJson(SourceUrl(path, Fixture.BaselineCommit), Fixture.GitHubContentText(before));
		transport.AddJson(SourceUrl(path, Fixture.TargetCommit), Fixture.GitHubContentText(after));
	}

	private static string SourceUrl(string path, string revision) => $"/repos/dotnet/aspnetcore/contents/{path}?ref={revision}";
	private static string Partial(string members = "") => $"internal partial class EndpointHtmlRenderer {{ {members} }}";

	private static ReportExpectation DriftReport(SourceReportRow[] sources, ApiReportRow[] apis) => new("drift",
		new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit), sources, apis, null);
}
