using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #244: #240 made a watch that does not resolve to the kind of thing it claims report itself
// per watch, without losing the rest of the run. This issue owns the one abort shape #240 split off:
// a symlink or submodule that API-surface comparison itself assumes is a readable file. The root
// cause is shared by three routes, all pinned here per the delivery checkpoint's decision
// (https://github.com/egil/Htmxor/issues/244#issuecomment-5892255228):
//
//   - a `file` watch whose path is a symlink or submodule reaches UpstreamRepository.SourceAsync,
//     which reads "encoding" from a GitHub object that does not have one and throws
//     KeyNotFoundException, which UpstreamMonitorApplication.RunAsync turns into an
//     InfrastructureError with no sources and no issues (Tests below: File_watch_naming_a_...);
//   - a `prefix` watch's changed non-file entry, directly in its directory, is missing from
//     UpstreamRepository.PrefixSourcePaths (only files survive that filter) while still being
//     "expected" by UpstreamMonitorApplication.ApiSourceAsync's omission guard, which throws a
//     MonitorFailure for the same InfrastructureError outcome (Tests below:
//     Changed_symlink_directly_in_a_prefix_watchs_directory_...);
//   - a submodule that GitHub's directory listing types as "file" (issue comment 5804645684) is
//     taken for a real file by UpstreamRepository.EntryKind, so it resolves silently instead of
//     reporting exists-as-submodule, and is handed to SourceAsync whenever a sibling file changes
//     (Tests below: Prefix_watch_whose_only_match_is_a_legacy_listed_submodule_... and
//     Legacy_listed_submodule_is_not_handed_to_SourceAsync_...).
//
// The decided fix keeps every one of these a per-watch UnresolvedWatch finding (#240's existing
// finding vocabulary, no new status), while every other finding in the same run is still reported.
public sealed class NonFileApiEntryTests
{
	// A real upstream directory reused from WrongKindWatchTests, for a plausible file-watch path.
	private const string CacheViewDirectory = "src/Components/Endpoints/src/CacheView";
	private const string CacheViewParent = "src/Components/Endpoints/src";
	private const string WrongKindPath = CacheViewDirectory + "/LinkedEntry";

	// #240's own precedence tests (WrongKindWatchTests.Prefix_watch_whose_matches_are_a_symlink_...
	// and _include_a_directory_a_symlink_and_a_submodule_...) already pin directory > symlink >
	// submodule for a prefix watch resolved with no matching file in the compare at all. That
	// resolution path (UpstreamRepository.ResolveAsync) never inspects ApiSurface, so adding one to
	// the watch cannot change it; repeating it here would duplicate an already-covered, already-green
	// fact rather than add red evidence for this issue's own root cause.

	[Theory]
	[InlineData(false, "symlink", "exists-as-symlink")]
	[InlineData(true, "symlink", "exists-as-symlink")]
	[InlineData(false, "submodule", "exists-as-submodule")]
	[InlineData(true, "submodule", "exists-as-submodule")]
	public async Task File_watch_naming_a_non_file_kind_with_an_api_surface_does_not_abort_a_run_beside_a_drifting_watch(
		bool wrongKindListedFirst, string kind, string expectedFinding)
	{
		var drivingWatch = Fixture.Watch(
			ExpectedMonitorArtifacts.InvokerInterface, apiSurface: ApiSurface.Interface, relationship: WatchRelationship.Implements);
		var wrongKindWatch = Fixture.Watch(WrongKindPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var targets = wrongKindListedFirst ? new[] { wrongKindWatch, drivingWatch } : new[] { drivingWatch, wrongKindWatch };
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new object[]
				{
					new { filename = ExpectedMonitorArtifacts.InvokerInterface, status = "modified" },
					new { filename = WrongKindPath, status = "modified" },
				},
			}));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent("source/target/IRazorComponentEndpointInvoker.cs"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent(kind));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.TargetCommit), WrongKindContent(kind));
		var request = new MonitorRequest(Fixture.Manifest(targets), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		// The design decision: UnresolvedWatch already outranks Drift and carries every source
		// change, API change and issue this run found, so the wrong-kind watch's own finding and the
		// driving watch's real drift both surface from the same run rather than the run aborting.
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Null(result.InfrastructureError);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.InvokerInterface);
		Assert.Contains(result.SourceChanges, change => change.Path == WrongKindPath);
		Assert.Equal(ExpectedMonitorArtifacts.InterfaceApiChanges(), result.ApiChanges);
		Assert.Contains(result.Issues, issue => issue.Identity == "aspnetcore-10-upstream-drift");

		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == WrongKindPath);
		Assert.Equal(expectedFinding, row.GetProperty("finding").GetString());
		Assert.Contains($"- {expectedFinding} | {WrongKindPath}", result.MarkdownReport, StringComparison.Ordinal);
	}

	// Scope-widening comment 5791910991's root cause: PrefixSourcePaths already drops a properly
	// typed symlink or submodule listing entry, but ApiSourceAsync's omission guard computes what it
	// "expected" from the raw compare instead, so the dropped entry still looks omitted and the guard
	// throws. Same throw whether that entry is the only change under the directory or sits beside a
	// real file that also changed; a driving watch alongside it loses its own drift too, because the
	// throw happens before UpstreamMonitorApplication.CompareWatchedAsync ever gets to record any
	// source change for this run. One kind (symlink) is enough to pin the shared root cause; the
	// finding wording itself is already pinned per kind above.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Changed_symlink_directly_in_a_prefix_watchs_directory_does_not_abort_the_run(bool besideMatchingFile)
	{
		const string symlinkSibling = CacheViewDirectory + ".Alias.cs";
		const string fileSibling = CacheViewDirectory + ".cs";
		var drivingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		var compareFiles = new List<object>
		{
			new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" },
			new { filename = symlinkSibling, status = "modified" },
		};
		if (besideMatchingFile)
		{
			compareFiles.Add(new { filename = fileSibling, status = "modified" });
		}
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = compareFiles }));
		var entries = new List<object>
		{
			PrefixInventoryFixture.Entry(CacheViewDirectory, "dir"),
			PrefixInventoryFixture.Entry(symlinkSibling, "symlink"),
		};
		if (besideMatchingFile)
		{
			entries.Add(PrefixInventoryFixture.Entry(fileSibling));
		}
		var listingJson = JsonSerializer.Serialize(entries);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
		if (besideMatchingFile)
		{
			transport.AddJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.BaselineCommit),
				Fixture.GitHubContentText("internal partial class CacheView { public void Before() { } }"));
			transport.AddJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.TargetCommit),
				Fixture.GitHubContentText("internal partial class CacheView { public void After() { } }"));
		}
		var request = new MonitorRequest(Fixture.Manifest(drivingWatch, prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.NotEqual(MonitorStatus.InfrastructureError, result.Status);
		Assert.Null(result.InfrastructureError);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		if (besideMatchingFile)
		{
			// The design decision's other half: the wrong-kind sibling is not an API candidate, but
			// the watch's real matching file is still compared for its own API surface.
			Assert.Contains(result.ApiChanges, change => change.TypeName == "CacheView");
		}
	}

	// Scope-widening comment 5804645684: GitHub's directory listing types a submodule as "file", so
	// UpstreamRepository.EntryKind currently takes it for a real file both for resolution and for API
	// candidacy. This is the resolution half: a prefix whose only match at the reviewed commit is
	// such an entry currently resolves as fine (WatchFinding returns null, kinds.Contains(null) is
	// true) instead of reporting exists-as-submodule.
	[Fact]
	public async Task Prefix_watch_whose_only_match_is_a_legacy_listed_submodule_is_reported_exists_as_submodule()
	{
		const string parent = "src/submodules";
		const string submodulePath = parent + "/googletest";
		var watch = Fixture.Watch(submodulePath, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.LegacySubmoduleEntry(submodulePath) }));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = Assert.Single(json.RootElement.GetProperty("unresolvedWatches").EnumerateArray());
		Assert.Equal(submodulePath, row.GetProperty("path").GetString());
		Assert.Equal("exists-as-submodule", row.GetProperty("finding").GetString());
	}

	// The other half of the same misclassification: a listed submodule need not even change itself.
	// It only has to share a prefix watch's directory with a file that does, for
	// UpstreamRepository.PrefixSourcePathsAsync to hand its path to SourceAsync alongside the real
	// file's. No fake response is registered for the submodule's own single-path contents URL: a
	// production fix that still fetches it (rather than excluding it before ever calling SourceAsync)
	// fails this test on the unmapped-request 404 instead of a live submodule shape, which pins that
	// the call itself must not happen, not merely that its particular response must be tolerated.
	[Fact]
	public async Task Legacy_listed_submodule_is_not_handed_to_SourceAsync_when_a_sibling_file_changes()
	{
		const string parent = "src/submodules";
		const string submodulePath = parent + "/googletest";
		const string siblingFile = submodulePath + ".Overrides.cs";
		var watch = Fixture.Watch(submodulePath, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = siblingFile, status = "modified" } } }));
		var listingJson = JsonSerializer.Serialize(new object[]
		{
			PrefixInventoryFixture.LegacySubmoduleEntry(submodulePath),
			PrefixInventoryFixture.Entry(siblingFile),
		});
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{parent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(siblingFile, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void Before() { } }"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(siblingFile, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void After() { } }"));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.NotEqual(MonitorStatus.InfrastructureError, result.Status);
		Assert.Null(result.InfrastructureError);
		Assert.DoesNotContain(transport.Requests, observed => observed.PathAndQuery.StartsWith(
			$"/repos/dotnet/aspnetcore/contents/{submodulePath}?", StringComparison.Ordinal));
		Assert.Contains(result.ApiChanges, change => change.TypeName == "googletest");
	}

	// Preservation (must stay green now and after): a genuine infrastructure defect distinct from a
	// symlink or submodule shape - a "type: file" object with no readable body at all - must keep
	// failing the run as infrastructure. This guards against a fix broad enough to swallow any
	// SourceAsync failure as a per-watch finding rather than only the documented wrong-kind shapes.
	// PartialInventoryFailureTests.Directory_inventory_omitting_a_known_existing_changed_partial_is_
	// incomplete already pins the omission guard's own preservation half for a changed real FILE the
	// listing omits; this is the SourceAsync half the same principle requires.
	[Fact]
	public async Task A_file_watchs_unreadable_source_body_still_fails_the_run_as_infrastructure()
	{
		var watch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "modified" } } }));
		var malformed = JsonSerializer.Serialize(new { type = "file", size = 123 });
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(ExpectedMonitorArtifacts.Invoker, Fixture.BaselineCommit), malformed);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(ExpectedMonitorArtifacts.Invoker, Fixture.TargetCommit), malformed);
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.Equal("Upstream monitor infrastructure failed.", result.InfrastructureError);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Empty(result.Issues);
	}

	private static string WrongKindContent(string kind) => kind == "symlink"
		? JsonSerializer.Serialize(new { type = "symlink", target = "../CacheView.cs" })
		: JsonSerializer.Serialize(new { type = "submodule", submodule_git_url = "https://github.com/example/vendor" });

	// An unrelated changed file keeps every watch in these fixtures out of the compare, so each is
	// resolved against the baseline commit rather than matched as a change. Mirrors
	// WrongKindWatchTests's and UnresolvedWatchPathTests's own private helper of the same name and shape.
	private static FakeGitHubTransport UnrelatedChangeTransport()
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		return transport;
	}
}
