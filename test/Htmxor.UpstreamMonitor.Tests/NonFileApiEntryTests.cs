using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #244 (https://github.com/egil/Htmxor/issues/244): a symlink, submodule, or GitHub's legacy
// submodule-as-file listing reaches API-surface comparison and must be reported per watch, using
// #240's finding vocabulary, while every other finding in the same run is still reported. Decision:
// https://github.com/egil/Htmxor/issues/244#issuecomment-5892255228, corrected by
// https://github.com/egil/Htmxor/issues/244#issuecomment-5893089626 (the target revision alone
// decides; a symlink or submodule contributes no API source at either revision).
public sealed class NonFileApiEntryTests
{
	// A real upstream directory reused from WrongKindWatchTests, for a plausible file-watch path.
	private const string CacheViewDirectory = "src/Components/Endpoints/src/CacheView";
	private const string CacheViewParent = "src/Components/Endpoints/src";
	private const string WrongKindPath = CacheViewDirectory + "/LinkedEntry";

	// The verified live dotnet/aspnetcore example from comment 5804645684 (a5383385).
	private const string SubmodulesParent = "src/submodules";
	private const string SubmodulePath = SubmodulesParent + "/googletest";

	// issuecomment-5893089626: the target revision alone decides whether a `file` watch is
	// unresolved. Every same-kind row (baseline and target share a kind) and every differing-kind
	// row (a symlink or submodule at only one revision, or an added path) ends the same way: the
	// wrong-kind watch is reported with the target's kind, and the driving watch's own drift, API
	// changes and review issue all survive in the same run.
	[Theory]
	[InlineData("modified", "symlink", "symlink", false, "exists-as-symlink")]
	[InlineData("modified", "symlink", "symlink", true, "exists-as-symlink")]
	[InlineData("modified", "submodule", "submodule", false, "exists-as-submodule")]
	[InlineData("modified", "submodule", "submodule", true, "exists-as-submodule")]
	[InlineData("modified", "file", "symlink", false, "exists-as-symlink")]
	[InlineData("modified", "file", "symlink", true, "exists-as-symlink")]
	[InlineData("modified", "file", "submodule", false, "exists-as-submodule")]
	[InlineData("added", null, "symlink", false, "exists-as-symlink")]
	[InlineData("modified", "symlink", "submodule", false, "exists-as-submodule")]
	public async Task File_watch_whose_target_kind_is_a_non_file_is_reported_unresolved_regardless_of_the_baseline(
		string compareStatus, string? baselineKind, string targetKind, bool wrongKindListedFirst, string expectedFinding)
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
					new { filename = WrongKindPath, status = compareStatus },
				},
			}));
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent("source/target/IRazorComponentEndpointInvoker.cs"));
		if (baselineKind == "file")
		{
			transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit),
				Fixture.GitHubContentText("internal class LinkedEntry { public void Before() { } }"));
		}
		else if (baselineKind is not null)
		{
			transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent(baselineKind));
		}
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.TargetCommit), WrongKindContent(targetKind));
		var request = new MonitorRequest(Fixture.Manifest(targets), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		// The InfrastructureError text is asserted before Status, so a regression's exact cause is
		// visible from the failure output alone rather than only from a bare status mismatch.
		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
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

	// The same decision's other half, over both non-file kinds: a symlink or submodule contributes no
	// API source at either revision, reading as absent at the baseline. A `file` watch whose target
	// resolves to a real file is ordinary drift, not a finding, whichever kind its baseline was.
	[Theory]
	[InlineData("symlink")]
	[InlineData("submodule")]
	public async Task File_watch_whose_baseline_is_a_non_file_kind_and_whose_target_is_a_real_file_is_ordinary_drift(string baselineKind)
	{
		var watch = Fixture.Watch(WrongKindPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = WrongKindPath, status = "modified" } } }));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent(baselineKind));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal class LinkedEntry { public void After() { } }"));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == WrongKindPath);
		Assert.Contains(result.ApiChanges, change => change.TypeName == "LinkedEntry" && change.Kind == ChangeKind.Added);
	}

	// The same rule's other half again, over both non-file kinds: a `file` watch whose target is
	// absent (the path was removed) is also ordinary drift, not a finding.
	[Theory]
	[InlineData("symlink")]
	[InlineData("submodule")]
	public async Task File_watch_whose_baseline_is_a_non_file_kind_and_whose_target_is_removed_is_ordinary_drift(string baselineKind)
	{
		var watch = Fixture.Watch(WrongKindPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = WrongKindPath, status = "removed" } } }));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent(baselineKind));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == WrongKindPath);
		Assert.Empty(result.ApiChanges);
	}

	// The prefix-directory counterpart to a wrong-kind file watch (issuecomment-5892255228), beside a
	// matching file: the wrong-kind sibling is not an API candidate, but the watch's real matching
	// file is still compared for its own API surface, and no unresolved-watch issue appears.
	[Fact]
	public async Task Changed_symlink_beside_a_matching_file_in_a_prefix_watchs_directory_is_still_compared_over_its_files()
	{
		const string symlinkSibling = CacheViewDirectory + ".Alias.cs";
		const string fileSibling = CacheViewDirectory + ".cs";
		var drivingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new object[]
				{
					new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" },
					new { filename = symlinkSibling, status = "modified" },
					new { filename = fileSibling, status = "modified" },
				},
			}));
		var listingJson = JsonSerializer.Serialize(new object[]
		{
			PrefixInventoryFixture.Entry(CacheViewDirectory, "dir"),
			PrefixInventoryFixture.Entry(symlinkSibling, "symlink"),
			PrefixInventoryFixture.Entry(fileSibling),
		});
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class CacheView { public void Before() { } }"));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class CacheView { public void After() { } }"));
		var request = new MonitorRequest(Fixture.Manifest(drivingWatch, prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		Assert.Contains(result.SourceChanges, change => change.Path == symlinkSibling);
		Assert.Contains(result.ApiChanges, change => change.TypeName == "CacheView");
		Assert.DoesNotContain(result.Issues, issue => issue.Identity == "aspnetcore-10-unresolved-watch");
	}

	// issuecomment-5893089626: a prefix watch the compare has already matched, left with no matching
	// file at the target, is reported unresolved with the precedence kind, and the symlink's own
	// source change still stands.
	[Fact]
	public async Task Changed_symlink_alone_in_a_prefix_watchs_directory_is_reported_unresolved_with_its_kind()
	{
		const string symlinkSibling = CacheViewDirectory + ".Alias.cs";
		var drivingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new object[]
				{
					new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" },
					new { filename = symlinkSibling, status = "modified" },
				},
			}));
		var listingJson = JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(symlinkSibling, "symlink") });
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
		var request = new MonitorRequest(Fixture.Manifest(drivingWatch, prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		Assert.Contains(result.SourceChanges, change => change.Path == symlinkSibling);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == CacheViewDirectory);
		Assert.Equal("exists-as-symlink", row.GetProperty("finding").GetString());
	}

	// issuecomment-5893089626's precedence, pinned separately from the fact above: directory outranks
	// symlink over the target listing's kinds, even though the entry that actually changed is the
	// symlink. Target resolution applies only when the prefix watch reaches API-surface comparison,
	// which requires a changed path directly in the prefix's directory (issuecomment-5893677679);
	// this fact's changed symlink sibling is exactly that. A change only beneath a matching
	// subdirectory never reaches API comparison and keeps #240's ordinary drift, still pinned by
	// WrongKindWatchTests.Mixed_run_with_a_file_change_beneath_a_prefix_watchs_directory_and_a_
	// drifting_watch_does_not_abort_the_run's directory-only rows.
	[Fact]
	public async Task Changed_symlink_beside_a_matching_directory_in_a_prefix_watchs_directory_is_reported_unresolved_by_precedence()
	{
		const string symlinkSibling = CacheViewDirectory + ".Alias.cs";
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = symlinkSibling, status = "modified" } } }));
		var listingJson = JsonSerializer.Serialize(new object[]
		{
			PrefixInventoryFixture.Entry(CacheViewDirectory, "dir"),
			PrefixInventoryFixture.Entry(symlinkSibling, "symlink"),
		});
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
		var request = new MonitorRequest(Fixture.Manifest(prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == symlinkSibling);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == CacheViewDirectory);
		Assert.Equal("exists-as-directory", row.GetProperty("finding").GetString());
	}

	// The same issuecomment-5893089626 gap reached a different way: the prefix's last real file is
	// removed at the target rather than there never having been one, leaving only a non-file entry
	// behind. Kept on this issue's declared API-surface route, as every other prefix fact here is.
	[Fact]
	public async Task Prefix_watch_whose_last_real_file_is_removed_at_the_target_is_reported_unresolved_with_its_kind()
	{
		const string fileSibling = CacheViewDirectory + ".cs";
		const string symlinkSibling = CacheViewDirectory + ".Alias.cs";
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = fileSibling, status = "removed" } } }));
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(fileSibling) }));
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}",
			JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(symlinkSibling, "symlink") }));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class CacheView { public void Before() { } }"));
		var request = new MonitorRequest(Fixture.Manifest(prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == fileSibling);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == CacheViewDirectory);
		Assert.Equal("exists-as-symlink", row.GetProperty("finding").GetString());
	}

	// issuecomment-5893089626's own named example: a compare-matched prefix watch whose baseline
	// listing holds only a non-file match gains a real matching file at the target, and is ordinary
	// drift, not a finding, because resolution looks only at the target.
	[Fact]
	public async Task Prefix_watch_whose_baseline_holds_only_a_non_file_match_and_gains_a_real_file_at_the_target_is_ordinary_drift()
	{
		const string aliasPath = CacheViewDirectory + ".Alias.cs";
		var drivingWatch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker);
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new object[]
				{
					new { filename = ExpectedMonitorArtifacts.Invoker, status = "removed" },
					new { filename = aliasPath, status = "modified" },
				},
			}));
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(aliasPath, "symlink") }));
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}",
			JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(aliasPath) }));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(aliasPath, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class CacheView { public void After() { } }"));
		var request = new MonitorRequest(Fixture.Manifest(drivingWatch, prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == ExpectedMonitorArtifacts.Invoker);
		Assert.Contains(result.SourceChanges, change => change.Path == aliasPath);
		Assert.Contains(result.ApiChanges, change => change.TypeName == "CacheView" && change.Kind == ChangeKind.Added);
		using var json = JsonDocument.Parse(result.JsonReport);
		Assert.False(json.RootElement.TryGetProperty("unresolvedWatches", out _));
	}

	// Comment 5804645684's resolution half: a prefix whose only match is GitHub's legacy
	// submodule-as-file listing shape is reported exists-as-submodule, not treated as resolved.
	[Fact]
	public async Task Prefix_watch_whose_only_match_is_a_legacy_listed_submodule_is_reported_exists_as_submodule()
	{
		var watch = Fixture.Watch(SubmodulePath, WatchMatch.Prefix);
		var transport = ProviderInventoryTests.UnrelatedChangeTransport();
		transport.AddRepeatingJson(
			$"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new[] { PrefixInventoryFixture.LegacySubmoduleEntry(SubmodulePath) }));
		var request = ProviderInventoryTests.Request(watch);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = Assert.Single(json.RootElement.GetProperty("unresolvedWatches").EnumerateArray());
		Assert.Equal(SubmodulePath, row.GetProperty("path").GetString());
		Assert.Equal("exists-as-submodule", row.GetProperty("finding").GetString());
	}

	// Comment 5804645684's candidacy half: the submodule need not itself change, only share a prefix
	// watch's directory with a file that does (issuecomment-5892255228, issuecomment-5893089626). The
	// fake answers the submodule's own single-path GET the way GitHub actually does (type: submodule).
	[Fact]
	public async Task Prefix_watch_beside_a_legacy_listed_submodule_still_compares_its_changed_file()
	{
		const string siblingFile = SubmodulePath + ".Overrides.cs";
		var watch = Fixture.Watch(SubmodulePath, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = siblingFile, status = "modified" } } }));
		var listingJson = JsonSerializer.Serialize(new object[]
		{
			PrefixInventoryFixture.LegacySubmoduleEntry(SubmodulePath),
			PrefixInventoryFixture.Entry(siblingFile),
		});
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(siblingFile, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void Before() { } }"));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(siblingFile, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void After() { } }"));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.BaselineCommit), FaithfulSubmoduleContent());
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.TargetCommit), FaithfulSubmoduleContent());
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == siblingFile);
		Assert.Contains(result.ApiChanges, change => change.TypeName == "googletest");
		Assert.DoesNotContain(result.Issues, issue => issue.Identity == "aspnetcore-10-unresolved-watch");
	}

	// Comment 5791910991's warning about a partial fix: excluding a changed compare path only by its
	// raw listing type (literally "symlink" or "submodule") still takes this legacy entry for a file,
	// since its raw type is "file". Given an API surface, alone the watch is compare-matched with
	// nothing real left (issuecomment-5893089626); beside a real file, its own API surface is still
	// compared.
	[Fact]
	public async Task Changed_legacy_listed_submodule_alone_in_a_prefix_watchs_directory_is_reported_unresolved_with_its_kind()
	{
		var prefixWatch = Fixture.Watch(SubmodulePath, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = SubmodulePath, status = "modified" } } }));
		var listingJson = JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.LegacySubmoduleEntry(SubmodulePath) });
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.BaselineCommit), FaithfulSubmoduleContent());
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.TargetCommit), FaithfulSubmoduleContent());
		var request = new MonitorRequest(Fixture.Manifest(prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == SubmodulePath);
		Assert.Equal("exists-as-submodule", row.GetProperty("finding").GetString());
	}

	[Fact]
	public async Task Changed_legacy_listed_submodule_beside_a_matching_file_in_a_prefix_watchs_directory_is_still_compared_over_its_files()
	{
		const string fileSibling = SubmodulePath + ".Overrides.cs";
		var prefixWatch = Fixture.Watch(SubmodulePath, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new
			{
				files = new[]
				{
					new { filename = SubmodulePath, status = "modified" },
					new { filename = fileSibling, status = "modified" },
				},
			}));
		var listingJson = JsonSerializer.Serialize(new object[]
		{
			PrefixInventoryFixture.LegacySubmoduleEntry(SubmodulePath),
			PrefixInventoryFixture.Entry(fileSibling),
		});
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddRepeatingJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void Before() { } }"));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void After() { } }"));
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.BaselineCommit), FaithfulSubmoduleContent());
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.TargetCommit), FaithfulSubmoduleContent());
		var request = new MonitorRequest(Fixture.Manifest(prefixWatch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == SubmodulePath);
		Assert.Contains(result.SourceChanges, change => change.Path == fileSibling);
		Assert.Contains(result.ApiChanges, change => change.TypeName == "googletest");
		Assert.DoesNotContain(result.Issues, issue => issue.Identity == "aspnetcore-10-unresolved-watch");
	}

	// A genuine infrastructure defect distinct from a symlink or submodule shape - a "type: file"
	// object with no readable body at all - fails the run as infrastructure (issuecomment-5892255228),
	// so a fix broad enough to swallow any SourceAsync failure as a finding is rejected.
	// PartialInventoryFailureTests.Directory_inventory_omitting_a_known_existing_changed_partial_is_
	// incomplete already pins the omission guard's own half of this; this is the SourceAsync half.
	[Fact]
	public async Task A_file_watchs_unreadable_source_body_still_fails_the_run_as_infrastructure()
	{
		var watch = Fixture.Watch(ExpectedMonitorArtifacts.Invoker, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = ExpectedMonitorArtifacts.Invoker, status = "modified" } } }));
		var malformed = JsonSerializer.Serialize(new { type = "file", size = 123 });
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(ExpectedMonitorArtifacts.Invoker, Fixture.BaselineCommit), malformed);
		transport.AddRepeatingJson(PrefixInventoryFixture.ContentsUrl(ExpectedMonitorArtifacts.Invoker, Fixture.TargetCommit), malformed);
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.False(string.IsNullOrWhiteSpace(result.InfrastructureError));
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Empty(result.Issues);
	}

	private static string WrongKindContent(string kind) => kind switch
	{
		"symlink" => JsonSerializer.Serialize(new { type = "symlink", target = "../CacheView.cs" }),
		"submodule" => JsonSerializer.Serialize(new { type = "submodule", submodule_git_url = "https://github.com/example/vendor" }),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unrecognized non-file kind."),
	};

	private static string FaithfulSubmoduleContent() =>
		JsonSerializer.Serialize(new { type = "submodule", submodule_git_url = "https://github.com/google/googletest" });
}
