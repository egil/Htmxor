using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class UnresolvedWatchPathTests
{
	// The exact wrong path #219 shipped for two releases before it was noticed: see issue #232.
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";
	private const string UnmatchedPrefix = "src/Components/Endpoints/src/CacheView/Retired";

	[Fact]
	public async Task File_watch_at_a_path_that_never_existed_upstream_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(WrongFilePath);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, WrongFilePath);
	}

	[Fact]
	public async Task Prefix_watch_matching_no_upstream_files_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(UnmatchedPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, UnmatchedPrefix);
	}

	[Fact]
	public async Task Unresolved_watch_path_is_not_reported_as_current_when_upstream_has_not_moved()
	{
		// RunAsync returns Current before examining any watch whenever upstream has not moved
		// past the reviewed commit (UpstreamMonitorApplication.cs:14-17) — the common steady
		// state, and the state most runs are in. A fix placed only inside CompareWatchedAsync
		// would leave this path unresolved: criterion 1 is stated against the reviewed commit,
		// not against the existence of drift.
		var watch = Fixture.Watch(WrongFilePath);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11", Fixture.Read("github/ref-v10.0.11-direct.json"));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, RequestedTag: "v10.0.11");

		var result = await Fixture.Application(transport).RunAsync(request);

		AssertUnresolvedIsDistinguishableFromOrdinaryDrift(result, WrongFilePath);
	}

	// Acceptance criterion 3 requires the unresolved state to be distinguishable from ordinary
	// drift in the JSON report, the Markdown report, and the review issue — not merely "not
	// Current". Excluding Drift directly rules out the counter-implementation that funnels an
	// unresolved watch through an ordinary SourceChange and the existing Drift pipeline, which
	// would produce output shaped exactly like drift in a file that does exist. Requiring a
	// populated Issue rules out the other counter-implementation that classifies an unresolved
	// path as InfrastructureError (a natural shape if resolution failure is thrown and caught by
	// RunAsync's existing catch): MonitorReports.Create populates Issue only for Drift today
	// (MonitorOutcomeTests.Current_or_infrastructure_outcome_never_writes_an_issue pins that an
	// InfrastructureError result never writes one), so that shape would suppress the review
	// issue on every unresolved-path run while still passing a bare "not Current" check.
	private static void AssertUnresolvedIsDistinguishableFromOrdinaryDrift(MonitorResult result, string path)
	{
		Assert.NotEqual(MonitorStatus.Current, result.Status);
		Assert.NotEqual(MonitorStatus.Drift, result.Status);
		Assert.Contains(path, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(path, result.MarkdownReport, StringComparison.Ordinal);
		Assert.NotNull(result.Issue);
		Assert.Contains(path, result.Issue.Body, StringComparison.Ordinal);
	}

	// The wrong or unmatched watch path can never appear in a real GitHub compare response, so an
	// unrelated changed file is the faithful shape of what the provider actually returns.
	private static FakeGitHubTransport UnrelatedChangeTransport()
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		return transport;
	}
}
