using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #240: #232 made a watch resolve only to the kind of thing it claims to watch, but every
// watch that does not resolve is reported with #232's absent-path wording even when the path
// exists as a directory, a symlink, or a submodule. These tests pin the design decision recorded
// on the issue: a wrong-kind watch stays the same UnresolvedWatch status, exit code, and review
// issue identity, but every report surface now carries what was actually found, and a prefix
// watch whose listing holds both a matching directory and matching files still resolves. They
// also pin the issue's later correction
// (https://github.com/egil/Htmxor/issues/240#issuecomment-5781847223): a prefix watch naming a
// directory, with a non-`none` API surface, must not abort the whole run when a file beneath that
// directory changes.
public sealed class WrongKindWatchTests
{
	// A real upstream directory: the wrong kind of thing for a `match: file` watch to name.
	private const string DirectoryPath = "src/Components/Endpoints/src/CacheView";
	private const string SymlinkPath = "src/Components/Endpoints/src/CacheView/CacheViewSymlink.cs";
	private const string SubmodulePath = "src/Components/Endpoints/src/CacheView/VendorModule";

	// A path genuinely absent upstream: #219's exact wrong path, unchanged from #232.
	private const string AbsentPath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";

	// The shared stem for the prefix-ordering cases, whose parent listing is DirectoryPath itself.
	private const string OrderPrefix = DirectoryPath + "/Linked";
	private const string OrderParent = DirectoryPath;

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

		var section = MarkdownSection(result.MarkdownReport, "## Unresolved watch paths", nextHeading: null);
		Assert.Contains($"- exists-as-directory | {DirectoryPath}", section, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-symlink | {SymlinkPath}", section, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-submodule | {SubmodulePath}", section, StringComparison.Ordinal);
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
		// #232's absent-path sentence makes an "unmonitored" claim that the design decided the
		// wrong-kind variant must not repeat, because a prefix's deeper source drift and a wrong-kind
		// file watch's own directory are both still visible, unlike a path that is genuinely absent.
		Assert.DoesNotContain("These dependencies are unmonitored", issue.Body, StringComparison.Ordinal);
		// The design decision's own replacement sentence, pinned positively: DoesNotContain above
		// only rules out the retired claim, and would still pass for any other wording placed in its
		// position, including a different false claim about upstream.
		Assert.Contains("- These watches do not resolve to the kind of thing they claim at the reviewed commit.", issue.Body, StringComparison.Ordinal);
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
	// non-files: directory, then symlink, then submodule. Listing order alone (submodule, symlink)
	// would let "take the last listed entry" pass, and path order alone would let "take the
	// ordinally-first path" pass (".../LinkedM..." sorts after ".../LinkedA..."). The listed-first
	// and ordinally-first entry here is a submodule, the listed-last entry is a different
	// submodule, and the expected symlink winner is the listed-middle, ordinally-middle entry, so
	// neither wrong rule can produce "exists-as-symlink" by accident.
	[Fact]
	public async Task Prefix_watch_whose_matches_are_a_symlink_and_a_submodule_is_named_exists_as_symlink()
	{
		var watch = Fixture.Watch(OrderPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{OrderParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[]
			{
				PrefixInventoryFixture.Entry($"{OrderPrefix}AConfig", "submodule"),
				PrefixInventoryFixture.Entry($"{OrderPrefix}MTarget", "symlink"),
				PrefixInventoryFixture.Entry($"{OrderPrefix}ZModule", "submodule"),
			}));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == OrderPrefix);
		Assert.Equal("exists-as-symlink", row.GetProperty("finding").GetString());
	}

	// The other half of the same order: a directory match outranks both a symlink and a submodule
	// match at the same prefix. Same discrimination as the test above, with the expected directory
	// winner listed and sorted in the middle, a symlink at both "first" positions and a different
	// submodule at both "last" positions, so neither wrong rule can produce "exists-as-directory"
	// by accident either.
	[Fact]
	public async Task Prefix_watch_whose_matches_include_a_directory_a_symlink_and_a_submodule_is_named_exists_as_directory()
	{
		var watch = Fixture.Watch(OrderPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{OrderParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[]
			{
				PrefixInventoryFixture.Entry($"{OrderPrefix}ALink", "symlink"),
				PrefixInventoryFixture.Entry($"{OrderPrefix}MDir", "dir"),
				PrefixInventoryFixture.Entry($"{OrderPrefix}ZVendor", "submodule"),
			}));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == OrderPrefix);
		Assert.Equal("exists-as-directory", row.GetProperty("finding").GetString());
	}

	// Work item 3's decided answer for the steady state: a sibling directory sharing a prefix
	// watch's stem is outside what the watch claims, but the files sharing that same stem in the
	// same listing still resolve it, and this is not a finding. This is already the delivered
	// behavior; it protects against a plausible counter-fix that stops resolving a prefix once any
	// non-file entry shares its stem. The same answer for a watch that also carries an API surface,
	// and whose matching files actually changed, is pinned separately below
	// (Mixed_run_with_a_file_change_beneath_a_prefix_watchs_directory_and_a_drifting_watch_does_not_abort_the_run),
	// because that shape reaches a different, currently broken code path
	// (UpstreamMonitorApplication.ApiSourceAsync) that this steady-state fixture never exercises.
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
	// shape of this same risk. The exact wording each finding uses is the seam tests' own contract
	// above, not this one's: this theory keeps only the JSON row's path-to-finding pairing, which
	// is the one check that still discriminates a per-order regression from the seam tests already
	// covering the wording itself.
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

		using var json = JsonDocument.Parse(result.JsonReport);
		var unresolvedRows = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Select(element => (element.GetProperty("path").GetString()!, element.GetProperty("finding").GetString()!)).ToArray();
		Assert.Equal(new[] { (DirectoryPath, "exists-as-directory") }, unresolvedRows);
	}

	// Acceptance criterion 2's mirror image: the design's "any wrong-kind watch present" trigger
	// must also hold when it is beside an absent watch rather than beside a drifting one, and every
	// row — including the absent watch's own row — must be labelled, not only the wrong-kind row.
	[Fact]
	public async Task Mixed_run_with_an_absent_watch_and_a_wrong_kind_watch_uses_the_wrong_kind_variant_and_labels_every_row()
	{
		var absentWatch = Fixture.Watch(AbsentPath);
		var wrongKindWatch = Fixture.Watch(DirectoryPath);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryPath}/Nested.cs") }));
		var request = new MonitorRequest(Fixture.Manifest(absentWatch, wrongKindWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		var issue = Assert.Single(result.Issues);
		Assert.Equal("Htmxor upstream watches do not resolve at v10.0.12", issue.Title);
		var section = MarkdownSection(result.MarkdownReport, "## Unresolved watch paths", nextHeading: null);
		Assert.Contains($"- does-not-exist-upstream | {AbsentPath}", section, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-directory | {DirectoryPath}", section, StringComparison.Ordinal);
		Assert.Contains($"- does-not-exist-upstream | [{AbsentPath}]", issue.Body, StringComparison.Ordinal);
		Assert.Contains($"- exists-as-directory | [{DirectoryPath}]", issue.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("These dependencies are unmonitored", issue.Body, StringComparison.Ordinal);
		Assert.Contains("- These watches do not resolve to the kind of thing they claim at the reviewed commit.", issue.Body, StringComparison.Ordinal);
	}

	// Acceptance criterion 4, and #240's second comment "How this meets acceptance criterion 4":
	// an absent-only run keeps #232's exact title, heading, body and rows. Distinct from
	// UnresolvedWatchPathTests, which pins that the run is unresolved rather than current or drift
	// but never pinned the exact rendered wording; this closes that gap without repeating
	// UnresolvedWatchPathTests's own coverage of the status and distinguishability. The body is
	// compared exactly rather than by fragment, because #240 edits exactly the function that
	// renders it, and a fragment match cannot see a change to the parts it does not check (the
	// second explanatory line, the "### Unresolved watch paths" subheading, or the review
	// checklist).
	[Fact]
	public async Task Absent_only_run_keeps_the_existing_review_issue_title_heading_body_and_markdown_row()
	{
		var result = await AbsentOnlyResultAsync();

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		var issue = Assert.Single(result.Issues);
		Assert.Equal("Htmxor upstream watch paths do not exist at v10.0.12", issue.Title);
		Assert.Equal(ExpectedAbsentOnlyIssueBody(AbsentPath), issue.Body);
		Assert.Contains($"- does-not-exist-upstream | {AbsentPath}", result.MarkdownReport, StringComparison.Ordinal);

		using var json = JsonDocument.Parse(result.JsonReport);
		var row = Assert.Single(json.RootElement.GetProperty("unresolvedWatches").EnumerateArray());
		Assert.Equal(AbsentPath, row.GetProperty("path").GetString());
		Assert.Equal("does-not-exist-upstream", row.GetProperty("finding").GetString());
	}

	// LR-324c38a-P002: GitHubIssueUpserter.Matches finds an existing issue only through an
	// `Identity: {identity}` line in the persisted body; it never reads IssueUpsertInput.Identity
	// directly. A wrong-kind body that dropped or altered that line would still pass every seam
	// test above (which reads only the in-memory Issues property) while opening a new tracker issue
	// on every scheduled run instead of updating the open one, the exact orphaning the design's
	// identity choice exists to prevent.
	[Fact]
	public async Task Wrong_kind_issue_persists_its_identity_line_when_upserted()
	{
		var result = await SingleWrongKindResultAsync();
		var transport = new RecordingIssueTransport();
		using var client = new HttpClient(transport, disposeHandler: false) { BaseAddress = new Uri("https://api.github.test") };
		var upserter = new GitHubIssueUpserter(client);

		await upserter.UpsertAsync(result);

		var persisted = Assert.Single(transport.Issues);
		Assert.Contains("Identity: aspnetcore-10-unresolved-watch", persisted.Body, StringComparison.Ordinal);
	}

	// The wrong-kind counterpart to IssueIdentityTests.Two_upserts_carrying_divergent_findings_
	// leave_both_discoverable_in_the_tracker: that test only ever upserts an absent-path unresolved
	// result, so "neither issue body erases the other" (acceptance criterion 2) has never been
	// observed against persisted tracker state for the wrong-kind variant, only against one run's
	// in-memory drafts.
	[Theory]
	[InlineData("wrong-kind-then-drift")]
	[InlineData("drift-then-wrong-kind")]
	public async Task Wrong_kind_and_drift_issues_upserted_in_either_order_persist_both_without_erasure(string ordering)
	{
		var wrongKind = await SingleWrongKindResultAsync();
		var drift = await DriftOnlyResultAsync();
		var (first, second) = ordering == "wrong-kind-then-drift" ? (wrongKind, drift) : (drift, wrongKind);

		var transport = new RecordingIssueTransport();
		using var client = new HttpClient(transport, disposeHandler: false) { BaseAddress = new Uri("https://api.github.test") };
		var upserter = new GitHubIssueUpserter(client);

		await upserter.UpsertAsync(first);
		await upserter.UpsertAsync(second);

		Assert.Equal(
			new[] { first.Issues[0].Body, second.Issues[0].Body },
			transport.Issues.Select(issue => issue.Body));
	}

	// The design depends on an already-open absent-variant issue being updated in place once a
	// wrong-kind watch appears in the same manifest slot, not orphaned in favour of a new one: both
	// share the identity `aspnetcore-10-unresolved-watch`, and GitHubIssueUpserter.Matches only
	// reads that line, so the second write should match and PATCH the first issue rather than POST
	// a second one.
	[Fact]
	public async Task An_open_absent_variant_issue_is_updated_in_place_once_a_wrong_kind_watch_appears()
	{
		var absentOnly = await AbsentOnlyResultAsync();
		var wrongKind = await SingleWrongKindResultAsync();
		var transport = new RecordingIssueTransport();
		using var client = new HttpClient(transport, disposeHandler: false) { BaseAddress = new Uri("https://api.github.test") };
		var upserter = new GitHubIssueUpserter(client);

		var firstWrite = await upserter.UpsertAsync(absentOnly);
		var secondWrite = await upserter.UpsertAsync(wrongKind);

		Assert.Equal(IssueWriteAction.Created, firstWrite.Action);
		Assert.Equal(IssueWriteAction.Updated, secondWrite.Action);
		Assert.Equal(firstWrite.IssueNumber, secondWrite.IssueNumber);
		var persisted = Assert.Single(transport.Issues);
		Assert.Equal(wrongKind.Issues[0].Title, persisted.Title);
		Assert.Equal(wrongKind.Issues[0].Body, persisted.Body);
	}

	// LR-324c38a-P001, corrected by the design decision's follow-up comment
	// (https://github.com/egil/Htmxor/issues/240#issuecomment-5781847223). A prefix watch's API
	// surface must only ever be checked against files directly in the one directory its listing
	// covers; a changed file beneath a matching subdirectory (the CacheView shape every fixture in
	// this file uses) is source drift only, and must not be handed to that listing at all. Today it
	// still is: ApiSourceAsync's own incomplete-listing guard treats the deep file as an omission
	// from that listing and throws, and the exception unwinds UpstreamMonitorApplication.RunAsync's
	// try/catch, discarding both this watch's own drift and ExpectedMonitorArtifacts.Invoker's
	// unrelated drift for the entire run. Both manifest orderings, and both the directory-only
	// shape and the Work item 3 shape (a sibling file, CacheView.cs, also matches the same parent
	// listing), must stop aborting.
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Mixed_run_with_a_file_change_beneath_a_prefix_watchs_directory_and_a_drifting_watch_does_not_abort_the_run(
		bool prefixWatchListedFirst, bool listingAlsoHoldsAMatchingSiblingFile)
	{
		const string parent = "src/Components/Endpoints/src";
		var deepChangedFile = $"{DirectoryPath}/Nested.cs";
		var siblingFile = $"{DirectoryPath}.cs";
		var driftingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var prefixWatch = Fixture.Watch(DirectoryPath, WatchMatch.Prefix, ApiSurface.Subclass);
		var targets = prefixWatchListedFirst ? new[] { prefixWatch, driftingWatch } : new[] { driftingWatch, prefixWatch };
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new[]
				{
					new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" },
					new { filename = deepChangedFile, status = "modified" },
				},
			}));
		var entries = new List<object> { PrefixInventoryFixture.Entry(DirectoryPath, "dir") };
		if (listingAlsoHoldsAMatchingSiblingFile)
		{
			entries.Add(PrefixInventoryFixture.Entry(siblingFile));
		}
		var listingJson = JsonSerializer.Serialize(entries);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.TargetCommit}", listingJson);
		if (listingAlsoHoldsAMatchingSiblingFile)
		{
			var siblingSource = Fixture.GitHubContentText("internal partial class CacheView { }");
			transport.AddJson($"/repos/dotnet/aspnetcore/contents/{siblingFile}?ref={Fixture.BaselineCommit}", siblingSource);
			transport.AddJson($"/repos/dotnet/aspnetcore/contents/{siblingFile}?ref={Fixture.TargetCommit}", siblingSource);
		}
		var request = new MonitorRequest(Fixture.Manifest(targets), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.NotEqual(MonitorStatus.InfrastructureError, result.Status);
		Assert.Null(result.InfrastructureError);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		Assert.Contains(result.SourceChanges, change => change.Path == deepChangedFile);
	}

	// Preservation: a changed file directly in the prefix's own listed directory that the listing
	// omits must still fail the run as infrastructure. PartialInventoryFailureTests.Directory_
	// inventory_omitting_a_known_existing_changed_partial_is_incomplete already pins this exact
	// ApiSourceAsync guard for the Rendering/EndpointHtmlRenderer shape; this repeats it only for
	// the CacheView-directory shape the rest of this file uses, so the fix above cannot be
	// implemented by widening the guard to also excuse a direct sibling it should still catch.
	[Fact]
	public async Task A_direct_sibling_the_listing_omits_still_fails_the_run_as_infrastructure()
	{
		const string parent = "src/Components/Endpoints/src";
		var siblingFile = $"{DirectoryPath}.cs";
		var prefixWatch = Fixture.Watch(DirectoryPath, WatchMatch.Prefix, ApiSurface.Subclass);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = siblingFile, status = "modified" } } }));
		var listingWithoutSibling = JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry(DirectoryPath, "dir") });
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.BaselineCommit}", listingWithoutSibling);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.TargetCommit}", listingWithoutSibling);
		var request = new MonitorRequest(Fixture.Manifest(prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.Issues);
	}

	// Watches listed out of both path order and their own declaration order, so a container that
	// merely preserved manifest or insertion order (rather than sorting by path, as the design
	// decides) would produce a different sequence than the OrderBy'd expectation the JSON seam test
	// above compares against.
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
		var request = new MonitorRequest(Fixture.Manifest(submoduleWatch, directoryWatch, symlinkWatch), 10, "v10.0.12", Fixture.BaselineCommit);
		return (transport, request);
	}

	private static async Task<MonitorResult> SingleWrongKindResultAsync()
	{
		var watch = Fixture.Watch(DirectoryPath);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{DirectoryPath}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.Entry($"{DirectoryPath}/Nested.cs") }));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);
		return await Fixture.Application(transport).RunAsync(request);
	}

	private static async Task<MonitorResult> DriftOnlyResultAsync()
	{
		var watch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" } } }));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);
		return await Fixture.Application(transport).RunAsync(request);
	}

	private static async Task<MonitorResult> AbsentOnlyResultAsync()
	{
		var watch = Fixture.Watch(AbsentPath);
		var transport = UnrelatedChangeTransport();
		var request = ProviderInventoryTests.Request(watch);
		return await Fixture.Application(transport).RunAsync(request);
	}

	// The exact body MonitorReports.UnresolvedIssue renders for a single absent watch, built from
	// the same literal template pieces the design decision quotes, independently of production
	// code, so this is a genuine comparison rather than the function checking itself.
	private static string ExpectedAbsentOnlyIssueBody(string path)
	{
		const string url = "https://github.com/dotnet/aspnetcore";
		const string identity = "aspnetcore-10-unresolved-watch";
		return string.Join('\n',
		[
			"## ASP.NET Core watch paths that do not exist upstream", string.Empty, $"Identity: {identity}", string.Empty,
			$"- Reviewed: [unresolved ({Fixture.BaselineCommit})]({url}/tree/{Fixture.BaselineCommit})",
			$"- Current: [v10.0.12 ({Fixture.TargetCommit})]({url}/tree/{Fixture.TargetCommit})",
			"- These dependencies are unmonitored: a path that cannot resolve never appears in a compare,",
			"  so drift in it is reported as current on every run until the manifest is corrected.",
			string.Empty, "### Unresolved watch paths", string.Empty,
			$"- [{path}]({url}/tree/{Fixture.BaselineCommit}/{path})",
			string.Empty, "### Review checklist", string.Empty,
			"- [ ] Correct or remove each path above", "- [ ] Confirm the corrected path is the right upstream dependency",
			"- [ ] Re-run the monitor and confirm the watch reports against real content",
		]);
	}

	// Isolates one heading's own body from a single concatenated Markdown string, so a row found
	// "in the Markdown report" can be pinned to the specific section that names it. Mirrors
	// UnresolvedWatchPathTests's own private helper of the same name and shape.
	private static string MarkdownSection(string markdown, string heading, string? nextHeading)
	{
		var start = markdown.IndexOf(heading, StringComparison.Ordinal);
		Assert.True(start >= 0, $"Markdown report does not contain '{heading}'.");
		var end = nextHeading is null ? markdown.Length : markdown.IndexOf(nextHeading, start, StringComparison.Ordinal);
		Assert.True(end >= 0, $"Markdown report does not contain '{nextHeading}' after '{heading}'.");
		return markdown[start..end];
	}

	// An unrelated changed file keeps every watch in these fixtures out of the compare, so each is
	// resolved against the baseline commit rather than matched as a change. Mirrors
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
