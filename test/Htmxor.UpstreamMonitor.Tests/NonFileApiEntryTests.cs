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

	// Decision (issuecomment-5893089626): the target revision alone decides. A `file` watch whose
	// target is a symlink or submodule is reported unresolved with that kind, regardless of the
	// baseline's kind or whether the path exists there at all.
	[Theory]
	[InlineData("modified", "file", "symlink", false)]
	[InlineData("modified", "file", "symlink", true)]
	[InlineData("modified", "file", "submodule", false)]
	[InlineData("added", null, "symlink", false)]
	[InlineData("modified", "symlink", "submodule", false)]
	public async Task File_watch_whose_target_kind_is_a_non_file_is_reported_unresolved_regardless_of_the_baseline(
		string compareStatus, string? baselineKind, string targetKind, bool wrongKindListedFirst)
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
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent("source/baseline/IRazorComponentEndpointInvoker.cs"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{ExpectedMonitorArtifacts.InvokerInterface}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent("source/target/IRazorComponentEndpointInvoker.cs"));
		if (baselineKind == "file")
		{
			transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit),
				Fixture.GitHubContentText("internal class Placeholder { }"));
		}
		else if (baselineKind is not null)
		{
			transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent(baselineKind));
		}
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.TargetCommit), WrongKindContent(targetKind));
		var request = new MonitorRequest(Fixture.Manifest(targets), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		var expectedFinding = targetKind switch
		{
			"symlink" => "exists-as-symlink",
			"submodule" => "exists-as-submodule",
			_ => throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Unrecognized target kind."),
		};
		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == WrongKindPath);
		using var json = JsonDocument.Parse(result.JsonReport);
		var row = json.RootElement.GetProperty("unresolvedWatches").EnumerateArray()
			.Single(element => element.GetProperty("path").GetString() == WrongKindPath);
		Assert.Equal(expectedFinding, row.GetProperty("finding").GetString());
	}

	// The same decision's other half: a symlink or submodule contributes no API source at either
	// revision, reading as absent at the baseline. A `file` watch whose target resolves to a real
	// file is ordinary drift, not a finding, even though its baseline was a symlink.
	[Fact]
	public async Task File_watch_whose_baseline_is_a_symlink_and_whose_target_is_a_real_file_is_ordinary_drift()
	{
		var watch = Fixture.Watch(WrongKindPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = WrongKindPath, status = "modified" } } }));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent("symlink"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal class LinkedEntry { public void After() { } }"));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == WrongKindPath);
		Assert.Contains(result.ApiChanges, change => change.TypeName == "LinkedEntry" && change.Kind == ChangeKind.Added);
	}

	// The same rule's other half again: a `file` watch whose target is absent (the path was removed)
	// is also ordinary drift, not a finding, even though its baseline was a symlink.
	[Fact]
	public async Task File_watch_whose_baseline_is_a_symlink_and_whose_target_is_removed_is_ordinary_drift()
	{
		var watch = Fixture.Watch(WrongKindPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = WrongKindPath, status = "removed" } } }));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(WrongKindPath, Fixture.BaselineCommit), WrongKindContent("symlink"));
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Null(result.InfrastructureError);
		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(result.SourceChanges, change => change.Path == WrongKindPath);
		Assert.Empty(result.ApiChanges);
	}

	// Decision row 1's prefix-directory counterpart, beside a matching file: the wrong-kind sibling
	// is not an API candidate, but the watch's real matching file is still compared for its own API
	// surface, and no unresolved-watch issue appears for this shape.
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
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class CacheView { public void Before() { } }"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.TargetCommit),
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

	// Decision row 3: a prefix watch the compare has already matched, left with no matching file at
	// the target, is reported unresolved with the precedence kind, and the symlink's own source
	// change still stands.
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
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
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

	// Decision row 3's precedence, pinned separately from the fact above: directory outranks symlink
	// over the target listing's kinds, even though the entry that actually changed is the symlink.
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
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}", listingJson);
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

	// The same decision row 3 gap reached a different way: the prefix's last real file is removed at
	// the target rather than there never having been one, leaving only a non-file entry behind.
	[Fact]
	public async Task Prefix_watch_whose_last_real_file_is_removed_at_the_target_is_reported_unresolved_with_its_kind()
	{
		const string fileSibling = CacheViewDirectory + ".cs";
		const string symlinkSibling = CacheViewDirectory + ".Alias.cs";
		var prefixWatch = Fixture.Watch(CacheViewDirectory, WatchMatch.Prefix);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = fileSibling, status = "removed" } } }));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.BaselineCommit}",
			JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(fileSibling) }));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{CacheViewParent}?ref={Fixture.TargetCommit}",
			JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.Entry(symlinkSibling, "symlink") }));
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

	// Comment 5804645684's resolution half: a prefix whose only match is GitHub's legacy
	// submodule-as-file listing shape is reported exists-as-submodule, not treated as resolved.
	[Fact]
	public async Task Prefix_watch_whose_only_match_is_a_legacy_listed_submodule_is_reported_exists_as_submodule()
	{
		var watch = Fixture.Watch(SubmodulePath, WatchMatch.Prefix);
		var transport = ProviderInventoryTests.UnrelatedChangeTransport();
		transport.AddJson(
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

	// Comment 5804645684's candidacy half, decision row 1: the submodule need not itself change,
	// only share a prefix watch's directory with a file that does. The fake answers the submodule's
	// own single-path GET the way GitHub actually does (type: submodule).
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
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(siblingFile, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void Before() { } }"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(siblingFile, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void After() { } }"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.BaselineCommit), FaithfulSubmoduleContent());
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.TargetCommit), FaithfulSubmoduleContent());
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
	// nothing real left (decision row 3); beside a real file, its own API surface is still compared.
	[Fact]
	public async Task Changed_legacy_listed_submodule_alone_in_a_prefix_watchs_directory_is_reported_unresolved_with_its_kind()
	{
		var prefixWatch = Fixture.Watch(SubmodulePath, WatchMatch.Prefix, ApiSurface.Subclass, WatchRelationship.Subclasses);
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = SubmodulePath, status = "modified" } } }));
		var listingJson = JsonSerializer.Serialize(new object[] { PrefixInventoryFixture.LegacySubmoduleEntry(SubmodulePath) });
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.BaselineCommit), FaithfulSubmoduleContent());
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.TargetCommit), FaithfulSubmoduleContent());
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
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.BaselineCommit}", listingJson);
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{SubmodulesParent}?ref={Fixture.TargetCommit}", listingJson);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.BaselineCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void Before() { } }"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(fileSibling, Fixture.TargetCommit),
			Fixture.GitHubContentText("internal partial class googletest { public void After() { } }"));
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.BaselineCommit), FaithfulSubmoduleContent());
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(SubmodulePath, Fixture.TargetCommit), FaithfulSubmoduleContent());
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
	// object with no readable body at all - fails the run as infrastructure (decision 5892255228 row
	// 5), so a fix broad enough to swallow any SourceAsync failure as a finding is rejected.
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
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(ExpectedMonitorArtifacts.Invoker, Fixture.BaselineCommit), malformed);
		transport.AddJson(PrefixInventoryFixture.ContentsUrl(ExpectedMonitorArtifacts.Invoker, Fixture.TargetCommit), malformed);
		var request = new MonitorRequest(Fixture.Manifest(watch), 10, "v10.0.12", Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
		Assert.False(string.IsNullOrWhiteSpace(result.InfrastructureError));
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		Assert.Empty(result.Issues);
	}

	private static string WrongKindContent(string kind) => kind == "symlink"
		? JsonSerializer.Serialize(new { type = "symlink", target = "../CacheView.cs" })
		: JsonSerializer.Serialize(new { type = "submodule", submodule_git_url = "https://github.com/example/vendor" });

	private static string FaithfulSubmoduleContent() =>
		JsonSerializer.Serialize(new { type = "submodule", submodule_git_url = "https://github.com/google/googletest" });
}
