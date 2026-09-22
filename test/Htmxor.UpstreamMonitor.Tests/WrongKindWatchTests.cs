using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #240: #232 made a watch resolve only to the kind of thing it claims to watch, but every
// watch that does not resolve is reported with #232's absent-path wording even when the path
// exists as a directory, a symlink, or a submodule. These tests pin the design decision recorded
// on the issue: a wrong-kind watch stays the same UnresolvedWatch status, exit code, and review
// issue identity, but every report surface now carries what was actually found, and a prefix
// watch whose listing holds both a matching directory and matching files still resolves.
public sealed class WrongKindWatchTests
{
	// A real upstream directory: the wrong kind of thing for a `match: file` watch to name.
	private const string DirectoryPath = "src/Components/Endpoints/src/CacheView";
	private const string SymlinkPath = "src/Components/Endpoints/src/CacheView/CacheViewSymlink.cs";
	private const string SubmodulePath = "src/Components/Endpoints/src/CacheView/VendorModule";

	// A shared stem for the prefix-ordering cases: distinct from DirectoryPath's own directory so
	// its contents-listing stub cannot collide with the file-watch fixtures above.
	private const string OrderPrefix = "src/Components/Endpoints/src/CacheView/Linked";
	private const string OrderParent = "src/Components/Endpoints/src/CacheView";
	private const string OrderSymlinkPath = OrderPrefix + ".cs";
	private const string OrderSubmodulePath = OrderPrefix + "Module";

	[Fact]
	public async Task File_watch_wrong_kinds_are_each_named_by_their_own_finding_word_in_the_json_report()
	{
		var (transport, request) = MixedWrongKindFixture();

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		using var json = JsonDocument.Parse(result.JsonReport);
		var rows = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Select(element => (Path: element.GetProperty("path").GetString()!, Finding: element.GetProperty("finding").GetString()!))
			.ToArray();
		Assert.Equal(
			new[]
			{
				(DirectoryPath, "exists-as-directory"),
				(SymlinkPath, "exists-as-symlink"),
				(SubmodulePath, "exists-as-submodule"),
			}.OrderBy(row => row.Item1, StringComparer.Ordinal).ToArray(),
			rows);
	}

	[Fact]
	public async Task File_watch_wrong_kinds_are_each_named_by_their_own_finding_word_in_the_markdown_report()
	{
		var (transport, request) = MixedWrongKindFixture();

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Contains($"- exists-as-directory | {DirectoryPath}", result.MarkdownReport, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-symlink | {SymlinkPath}", result.MarkdownReport, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-submodule | {SubmodulePath}", result.MarkdownReport, StringComparison.Ordinal);
	}

	[Fact]
	public async Task File_watch_wrong_kinds_are_each_named_by_their_own_finding_word_in_the_review_issue()
	{
		var (transport, request) = MixedWrongKindFixture();

		var result = await Fixture.Application(transport).RunAsync(request);

		var issue = Assert.Single(result.Issues);
		Assert.Equal("Htmxor upstream watches do not resolve at v10.0.12", issue.Title);
		Assert.Contains("## ASP.NET Core watches that do not resolve upstream", issue.Body, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-directory | [{DirectoryPath}]", issue.Body, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-symlink | [{SymlinkPath}]", issue.Body, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-submodule | [{SubmodulePath}]", issue.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Prefix_watch_whose_only_matches_are_directories_is_named_exists_as_directory()
	{
		const string parent = "src/Components/Endpoints/src";
		var watch = Fixture.Watch(DirectoryPath, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[]
			{
				PrefixInventoryFixture.Entry(DirectoryPath, "dir"),
				PrefixInventoryFixture.Entry($"{parent}/Unrelated.cs"),
			}));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == DirectoryPath);
		Assert.Equal("exists-as-directory", row.GetProperty("finding").GetString());
		Assert.Contains($"- exists-as-directory | {DirectoryPath}", result.MarkdownReport, StringComparison.Ordinal);
	}

	// The design decision names an order for a prefix watch whose matching entries are all
	// non-files: directory, then symlink, then submodule. No directory entry is present here, so
	// this pins that a symlink match outranks a submodule match.
	[Fact]
	public async Task Prefix_watch_whose_matches_are_a_symlink_and_a_submodule_is_named_exists_as_symlink()
	{
		var watch = Fixture.Watch(OrderPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{OrderParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[]
			{
				PrefixInventoryFixture.Entry(OrderSubmodulePath, "submodule"),
				PrefixInventoryFixture.Entry(OrderSymlinkPath, "symlink"),
			}));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == OrderPrefix);
		Assert.Equal("exists-as-symlink", row.GetProperty("finding").GetString());
	}

	// The other half of the same order: a directory match outranks both a symlink and a submodule
	// match at the same prefix.
	[Fact]
	public async Task Prefix_watch_whose_matches_include_a_directory_a_symlink_and_a_submodule_is_named_exists_as_directory()
	{
		var watch = Fixture.Watch(OrderPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{OrderParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[]
			{
				PrefixInventoryFixture.Entry(OrderSubmodulePath, "submodule"),
				PrefixInventoryFixture.Entry(OrderSymlinkPath, "symlink"),
				PrefixInventoryFixture.Entry(OrderPrefix, "dir"),
			}));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == OrderPrefix);
		Assert.Equal("exists-as-directory", row.GetProperty("finding").GetString());
	}

	// Work item 3's decided answer: a sibling directory sharing a prefix watch's stem is outside
	// what the watch claims, but the files sharing that same stem in the same listing still
	// resolve it. This is not a finding, and it is already the delivered behavior; it protects
	// against a plausible counter-fix that stops resolving a prefix once any non-file entry
	// shares its stem.
	[Fact]
	public async Task Prefix_watch_whose_listing_holds_both_a_matching_directory_and_matching_files_resolves()
	{
		const string parent = "src/Components/Endpoints/src";
		var watch = Fixture.Watch(DirectoryPath, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[]
			{
				PrefixInventoryFixture.Entry(DirectoryPath, "dir"),
				PrefixInventoryFixture.Entry($"{DirectoryPath}.cs"),
			}));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.Current, result.Status);
		Assert.Empty(result.Issues);
	}

	// Criterion 2 and the issue's own risk statement: a wrong-kind watch and a genuinely
	// drifting watch in the same run must both be reported, and the manifest's listed order must
	// not change that. DirectoryPath never appears in the compare (a directory cannot drift as a
	// file would), so it is resolved rather than matched; ExpectedMonitorArtifacts.Invoker is
	// removed in the compare, giving a real SourceChange and a real drift issue alongside the
	// unresolved one, exactly as UnresolvedWatchPathTests.Mixed_run_reports_the_unresolved_watch_
	// without_losing_a_different_watchs_drift_from_the_reports already pins for the absent-path
	// shape of this same risk.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Mixed_run_reports_a_wrong_kind_watch_and_a_drifting_watch_in_both_manifest_orderings(bool wrongKindListedFirst)
	{
		var driftingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var wrongKindWatch = Fixture.Watch(DirectoryPath);
		var targets = wrongKindListedFirst ? new[] { wrongKindWatch, driftingWatch } : new[] { driftingWatch, wrongKindWatch };
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" } } }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryPath}/Nested.cs") }));
		var request = new MonitorRequest(Fixture.Manifest(targets), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		Assert.Equal(2, result.Issues.Count);
		var driftIssue = Assert.Single(result.Issues, issue => issue.Body.Contains(ExpectedMonitorArtifacts.Invoker, StringComparison.Ordinal));
		var unresolvedIssue = Assert.Single(result.Issues, issue => issue.Identity == "aspnetcore-10-unresolved-watch");
		Assert.NotEqual(driftIssue.Identity, unresolvedIssue.Identity);
		Assert.DoesNotContain(DirectoryPath, driftIssue.Body, StringComparison.Ordinal);
		Assert.DoesNotContain(ExpectedMonitorArtifacts.Invoker, unresolvedIssue.Body, StringComparison.Ordinal);

		Assert.Equal("Htmxor upstream watches do not resolve at v10.0.12", unresolvedIssue.Title);
		Assert.Contains("## ASP.NET Core watches that do not resolve upstream", unresolvedIssue.Body, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-directory | [{DirectoryPath}]", unresolvedIssue.Body, StringComparison.Ordinal);

		using var json = JsonDocument.Parse(result.JsonReport);
		var unresolvedRows = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Select(element => (element.GetProperty("path").GetString()!, element.GetProperty("finding").GetString()!)).ToArray();
		Assert.Equal(new[] { (DirectoryPath, "exists-as-directory") }, unresolvedRows);
		Assert.Contains($"- exists-as-directory | {DirectoryPath}", result.MarkdownReport, StringComparison.Ordinal);
	}

	// Acceptance criterion 4, and #240's second comment "How this meets acceptance criterion 4":
	// an absent-only run keeps #232's exact title, heading, body sentence and unchanged Markdown
	// row. Distinct from UnresolvedWatchPathTests, which pins that the run is unresolved rather
	// than current or drift but never pinned the exact rendered wording; this closes that gap
	// without repeating UnresolvedWatchPathTests's own coverage of the status and distinguishability.
	[Fact]
	public async Task Absent_only_run_keeps_the_existing_review_issue_title_heading_body_and_markdown_row()
	{
		const string wrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";
		var watch = Fixture.Watch(wrongFilePath);
		var transport = UnrelatedChangeTransport();
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		var issue = Assert.Single(result.Issues);
		Assert.Equal("Htmxor upstream watch paths do not exist at v10.0.12", issue.Title);
		Assert.Contains("## ASP.NET Core watch paths that do not exist upstream", issue.Body, StringComparison.Ordinal);
		Assert.Contains("These dependencies are unmonitored", issue.Body, StringComparison.Ordinal);
		Assert.Contains($"- [{wrongFilePath}](", issue.Body, StringComparison.Ordinal);
		Assert.Contains($"- does-not-exist-upstream | {wrongFilePath}", result.MarkdownReport, StringComparison.Ordinal);
	}

	private static (FakeGitHubTransport Transport, MonitorRequest Request) MixedWrongKindFixture()
	{
		var directoryWatch = Fixture.Watch(DirectoryPath);
		var symlinkWatch = Fixture.Watch(SymlinkPath);
		var submoduleWatch = Fixture.Watch(SubmodulePath);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryPath}/Nested.cs") }));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{SymlinkPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(PrefixInventoryFixture.Entry(SymlinkPath, "symlink")));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{SubmodulePath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(PrefixInventoryFixture.Entry(SubmodulePath, "submodule")));
		var request = new MonitorRequest(Fixture.Manifest(directoryWatch, symlinkWatch, submoduleWatch), 10, "v10.0.12", Fixture.BaselineCommit);
		return (transport, request);
	}

	// The wrong-kind path can never appear in a real GitHub compare response, so an unrelated
	// changed file is the faithful shape of what the provider actually returns. Mirrors
	// UnresolvedWatchPathTests's own private helper of the same name and shape.
	private static FakeGitHubTransport UnrelatedChangeTransport()
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		return transport;
	}
}
