using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ProviderInventoryTests
{
	private const string Prefix = "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer";
	private const string Compare = "/repos/dotnet/aspnetcore/compare/" + Fixture.BaselineCommit + "..." + Fixture.TargetCommit;

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Capped_compare_cannot_establish_current_or_complete_drift(bool includesWatchedFile)
	{
		var transport = TargetTransport();
		var files = Enumerable.Range(0, 300).Select(index => new
		{
			filename = includesWatchedFile && index == 0 ? ExpectedMonitorArtifacts.Invoker : $"src/Unrelated/File{index}.cs",
			status = "modified",
		});
		transport.AddJson(Compare, JsonSerializer.Serialize(new { status = "ahead", files }));

		var result = await Fixture.Application(transport).RunAsync(Request(Fixture.Watch(ExpectedMonitorArtifacts.Invoker)));

		Assert.Equal("GitHub compare file inventory reached the 300-file limit; completeness is unknown.", result.InfrastructureError);
		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Null(result.Issue);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		ReportAssertions.Equal(result, FailureReport(result.InfrastructureError!));
		Assert.DoesNotContain(transport.Requests, request => request.Method != HttpMethod.Get);
	}

	[Theory]
	[InlineData("exact-out")]
	[InlineData("exact-in")]
	[InlineData("prefix-in")]
	[InlineData("prefix-out")]
	[InlineData("prefix-within")]
	public async Task Rename_records_preserve_watched_old_removal_and_new_addition(string scenario)
	{
		var (previous, current, watch, expected) = scenario.StartsWith("exact-", StringComparison.Ordinal) ? ExactRename(scenario) : PrefixRename(scenario);
		var transport = TargetTransport();
		transport.AddJson(Compare, JsonSerializer.Serialize(new
		{
			status = "ahead",
			files = new[] { new { filename = current, previous_filename = previous, status = "renamed" } },
		}));

		var result = await Fixture.Application(transport).RunAsync(Request(watch));

		Assert.Equal(expected, result.SourceChanges.OrderBy(change => change.Path, StringComparer.Ordinal));
		Assert.Equal(MonitorStatus.Drift, result.Status);
		ReportAssertions.Equal(result, new ReportExpectation("drift",
			new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
			expected.Select(change => new SourceReportRow(change.Path, change.Kind.ToString().ToLowerInvariant(), "parity-required")).ToArray(), [], null));
	}

	private static (string, string, WatchTarget, SourceChange[]) ExactRename(string scenario) => scenario == "exact-in"
		? ("src/Elsewhere.cs", ExpectedMonitorArtifacts.Invoker, Fixture.Watch(ExpectedMonitorArtifacts.Invoker), [Added(ExpectedMonitorArtifacts.Invoker)])
		: (ExpectedMonitorArtifacts.Invoker, "src/Elsewhere.cs", Fixture.Watch(ExpectedMonitorArtifacts.Invoker), [Removed(ExpectedMonitorArtifacts.Invoker)]);

	private static (string, string, WatchTarget, SourceChange[]) PrefixRename(string scenario)
	{
		var oldPartial = $"{Prefix}.Old.cs";
		var newPartial = $"{Prefix}.New.cs";
		return scenario switch
		{
			"prefix-in" => ("src/Elsewhere.cs", newPartial, Fixture.Watch(Prefix, WatchMatch.Prefix), [Added(newPartial)]),
			"prefix-out" => (oldPartial, "src/Elsewhere.cs", Fixture.Watch(Prefix, WatchMatch.Prefix), [Removed(oldPartial)]),
			_ => (oldPartial, newPartial, Fixture.Watch(Prefix, WatchMatch.Prefix), [Added(newPartial), Removed(oldPartial)]),
		};
	}

	private static SourceChange Added(string path) => new(path, ChangeKind.Added, ReviewClassification.ParityRequired);
	private static SourceChange Removed(string path) => new(path, ChangeKind.Removed, ReviewClassification.ParityRequired);

	internal static FakeGitHubTransport TargetTransport()
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12", Fixture.Read("github/ref-v10.0.12-direct.json"));
		return transport;
	}

	internal static MonitorRequest Request(WatchTarget watch) => new(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

	internal static ReportExpectation FailureReport(string error) => new("infrastructure-error",
		new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit), [], [], error);
}
