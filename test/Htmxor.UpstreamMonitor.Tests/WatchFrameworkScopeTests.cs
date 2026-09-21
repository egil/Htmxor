using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

/// <summary>
/// Protects issue #241's amended contract at the seam that decides it here:
/// <c>ManifestDependencyPolicy</c> and the manifest reader.
/// </summary>
public sealed class WatchFrameworkScopeTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string ComponentPath = "src/Components/Components/src/ComponentBase.cs";
	private const string NavigationManagerPath = "src/Components/Components/src/NavigationManager.cs";

	// The twelve upstream dependencies the issue identifies as serving a single framework's
	// local dependency, with the framework each is expected to declare. One upstream path
	// (TempDataProviderServiceCollectionExtensions.cs) resolves to two watches under
	// different relationships, so this theory carries thirteen cases. The
	// WebAssemblySettingsEmitter.cs row pins manifest text only: that file is discovered as a
	// local dependency for neither configured framework, so scoping `Covered` cannot exercise
	// it at this branch's seam; it is annotated because the issue's upstream-tree scan requires
	// it, not because this suite observes its coverage.
	public static TheoryData<string, string, string> FrameworkScopedWatches => new()
	{
		{ "src/Components/Endpoints/src/CacheView/CacheView.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/CacheView/CacheViewRenderState.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/CacheView/CacheViewService.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/DependencyInjection/TempDataService.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/Rendering/CacheViewTextWriter.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/SessionCascadingValueSupplier.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/TempData/TempDataCascadingValueSupplier.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs", "PrivateAccesses", "net11.0" },
		{ "src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs", "Reimplements", "net11.0" },
		{ "src/Components/Shared/src/ComponentKeyHelper.cs", "Mirrors", "net11.0" },
		{ "src/Components/Shared/src/RenderFragmentCapture.cs", "PrivateAccesses", "net11.0" },
		{ "src/Shared/MiddlewareInvokedKeys.cs", "Mirrors", "net11.0" },
		{ "src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs", "Reimplements", "net10.0" },
	};

	// Criterion 1 (green baseline): a watch with no `frameworks` list applies to every
	// configured framework, so each of these ordinary, unscoped watches covers the
	// dependency discovered for its own framework.
	[Fact]
	public void Unannotated_watches_cover_dependencies_discovered_for_every_configured_framework()
	{
		using var repository = new ConditionalRepository("per-target-symbols");
		var manifest = Fixture.MultiTargetManifest(
			Fixture.Watch(ComponentPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]),
			Fixture.Watch(NavigationManagerPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	// Criterion 1 (schema): the manifest reader must parse an explicit `frameworks` list onto
	// its watch.
	[Fact]
	public void A_watch_frameworks_list_is_parsed_from_the_manifest()
	{
		using var repository = new TemporaryRepository();
		repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", $$"""
			{
			  "repository": "dotnet/aspnetcore",
			  "frameworks": [
			    { "targetFramework": "net10.0", "majorVersion": 10, "allowsPrerelease": false, "referencePackVersion": "10.0.11",
			      "reviewed": { "tag": "v10.0.11", "commit": "{{Fixture.ReviewedCommit}}" } },
			    { "targetFramework": "net11.0", "majorVersion": 11, "allowsPrerelease": true, "referencePackVersion": "11.0.0-rc.1.26425.128",
			      "reviewed": { "tag": "{{Fixture.Net11ReviewedTag}}", "commit": "{{Fixture.Net11ReviewedCommit}}" } }
			  ],
			  "watches": [{
			    "path": "src/Components/Endpoints/src/Forms/Provider.cs",
			    "match": "file", "api": "none", "relationship": "reimplements",
			    "dependencies": ["src/Htmxor/Dependency.cs"],
			    "frameworks": ["net11.0"]
			  }]
			}
			""");

		var manifest = WatchManifestFile.Read(repository.Path);

		var watch = Assert.Single(manifest.Targets);
		Assert.Equal(["net11.0"], watch.Frameworks);
	}

	// Criterion 2: every entry of the list is validated, so an unknown name is rejected whether
	// it stands alone or follows a configured one. The second case guards a validator that
	// inspects only the first entry.
	[Theory]
	[InlineData("""["net12.0"]""")]
	[InlineData("""["net10.0", "net12.0"]""")]
	public void Unknown_framework_name_on_a_watch_is_rejected_naming_the_watch_and_the_unknown_name(string frameworks)
	{
		using var repository = new TemporaryRepository();
		const string watchPath = "src/Components/Endpoints/src/Forms/Provider.cs";
		repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", $$"""
			{
			  "repository": "dotnet/aspnetcore",
			  "frameworks": [
			    { "targetFramework": "net10.0", "majorVersion": 10, "allowsPrerelease": false, "referencePackVersion": "10.0.11",
			      "reviewed": { "tag": "v10.0.11", "commit": "{{Fixture.ReviewedCommit}}" } }
			  ],
			  "watches": [{
			    "path": "{{watchPath}}",
			    "match": "file", "api": "none", "relationship": "reimplements",
			    "dependencies": ["src/Htmxor/Dependency.cs"],
			    "frameworks": {{frameworks}}
			  }]
			}
			""");

		var exception = Assert.Throws<MonitorFailure>(() => WatchManifestFile.Read(repository.Path));

		Assert.Contains(watchPath, exception.Message, StringComparison.Ordinal);
		Assert.Contains("net12.0", exception.Message, StringComparison.Ordinal);
	}

	// Criterion 2 (closes LR-24773d2-S001/P001; extended for LR-6d257ca-P001): a frameworks entry
	// that is not a non-empty string is rejected on its own terms rather than silently becoming
	// an unconfigured framework name. The reader used to build `names` with `value.GetString()!`,
	// and `JsonElement.GetString()` returns `null` for a JSON `null`, so `FirstOrDefault`'s
	// "nothing matched" sentinel and "the match is null" were the same value: `[null]` applied to
	// no configured framework instead of being rejected, and in `[null, "net12.0"]` the `null` was
	// found first, so the genuinely unknown `net12.0` beside it was never reported. `[42]` is
	// included because the fix rejects any entry whose `ValueKind` is not `String`, not `null`
	// specifically; without it, narrowing the check to only the `Null` kind would still pass every
	// other case in this file. `[""]` is included because the guard's two grounds are independent:
	// `ValueKind != String` rejects `null` and `42`, but only the separate `{ Length: > 0 }` check
	// rejects an empty string, and no other case here reaches it. The message assertion below
	// therefore checks the malformed-entry wording itself, not just that the watch is named:
	// dropping `Length: > 0` still rejects `""`, naming the watch, but as "declares framework '',
	// which the manifest does not configure" — the unconfigured-name message this theory exists to
	// distinguish from — so a watch-name-only assertion would not notice the clause is unreachable.
	[Theory]
	[InlineData("[null]")]
	[InlineData("[null, \"net12.0\"]")]
	[InlineData("[42]")]
	[InlineData("[\"\"]")]
	public void Malformed_frameworks_entry_on_a_watch_is_rejected_naming_the_watch(string frameworks)
	{
		using var repository = new TemporaryRepository();
		const string watchPath = "src/Components/Endpoints/src/Forms/Provider.cs";
		repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", $$"""
			{
			  "repository": "dotnet/aspnetcore",
			  "frameworks": [
			    { "targetFramework": "net10.0", "majorVersion": 10, "allowsPrerelease": false, "referencePackVersion": "10.0.11",
			      "reviewed": { "tag": "v10.0.11", "commit": "{{Fixture.ReviewedCommit}}" } }
			  ],
			  "watches": [{
			    "path": "{{watchPath}}",
			    "match": "file", "api": "none", "relationship": "reimplements",
			    "dependencies": ["src/Htmxor/Dependency.cs"],
			    "frameworks": {{frameworks}}
			  }]
			}
			""");

		var exception = Assert.Throws<MonitorFailure>(() => WatchManifestFile.Read(repository.Path));

		Assert.Contains(watchPath, exception.Message, StringComparison.Ordinal);
		Assert.Contains("is not a framework name", exception.Message, StringComparison.Ordinal);
	}

	// Criterion 2: an empty `frameworks` list names no framework, so it is rejected rather than
	// read as "every framework" (the meaning of an absent list) or as "no framework" (which
	// would silently stop monitoring the watch). Rejected through the same validation path as
	// an unknown framework name, naming the watch.
	[Fact]
	public void Empty_frameworks_list_on_a_watch_is_rejected_naming_the_watch()
	{
		using var repository = new TemporaryRepository();
		const string watchPath = "src/Components/Endpoints/src/Forms/Provider.cs";
		repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", $$"""
			{
			  "repository": "dotnet/aspnetcore",
			  "frameworks": [
			    { "targetFramework": "net10.0", "majorVersion": 10, "allowsPrerelease": false, "referencePackVersion": "10.0.11",
			      "reviewed": { "tag": "v10.0.11", "commit": "{{Fixture.ReviewedCommit}}" } }
			  ],
			  "watches": [{
			    "path": "{{watchPath}}",
			    "match": "file", "api": "none", "relationship": "reimplements",
			    "dependencies": ["src/Htmxor/Dependency.cs"],
			    "frameworks": []
			  }]
			}
			""");

		var exception = Assert.Throws<MonitorFailure>(() => WatchManifestFile.Read(repository.Path));

		Assert.Contains(watchPath, exception.Message, StringComparison.Ordinal);
	}

	// Criteria 3 and 5: a dependency discovered for every configured framework collapses to one
	// entry before coverage is decided, so a watch scoped to one framework must still leave it
	// uncovered for the other. This is the case that fails if coverage forgets which framework
	// discovered the dependency.
	[Fact]
	public void Watch_scoped_to_one_framework_does_not_cover_a_dependency_discovered_for_every_framework()
	{
		using var repository = new ConditionalRepository("unguarded");
		var manifest = Fixture.MultiTargetManifest(
			Fixture.Watch(ComponentPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]) with { Frameworks = ["net11.0"] });

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, ComponentPath, WatchRelationship.Subclasses)], untracked);
	}

	// Criteria 3 and 5: a watch scoped to net11.0 must not cover the net10.0 dependency that
	// otherwise matches its upstream path, relationship, and local dependency, while a second
	// watch scoped to net11.0 still covers its own framework's dependency.
	[Fact]
	public void Watch_scoped_to_one_framework_does_not_cover_a_dependency_discovered_for_another_framework()
	{
		using var repository = new ConditionalRepository("per-target-symbols");
		var manifest = Fixture.MultiTargetManifest(
			Fixture.Watch(ComponentPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]) with { Frameworks = ["net11.0"] },
			Fixture.Watch(NavigationManagerPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]) with { Frameworks = ["net11.0"] });

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, ComponentPath, WatchRelationship.Subclasses)], untracked);
	}

	// Criterion 2 and 3 (closes LR-1a1c95f-S002/P001): a `frameworks` list naming both
	// configured frameworks must cover the dependency discovered under either, so both
	// validation and coverage walk the whole list rather than only its first entry. A
	// predicate written `watch.Frameworks.Single() == framework` throws on this two-entry
	// list; a predicate that inspects only `Frameworks[0]` would silently miss the second.
	[Fact]
	public void Watch_scoped_to_both_frameworks_covers_a_dependency_discovered_under_either()
	{
		using var repository = new ConditionalRepository("unguarded");
		var manifest = Fixture.MultiTargetManifest(
			Fixture.Watch(ComponentPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]) with { Frameworks = ["net10.0", "net11.0"] });

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	// Criterion 1: only the thirteen watches the issue identifies carry a scope. The other
	// thirty-nine committed watches stay unannotated, which is what keeps each applying to
	// every configured framework; otherwise annotating all fifty-two entries, the shape the
	// issue explicitly rejects, would still satisfy this file. The issue's "forty-eight" and
	// "thirty-six" count distinct upstream paths, not watch entries.
	[Fact]
	public void Committed_manifest_leaves_every_other_watch_unannotated()
	{
		var scoped = new HashSet<(string Path, string Relationship)>(
			FrameworkScopedWatches.Select(row => ((string)row[0]!, (string)row[1]!)));
		var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
		var manifest = WatchManifestFile.Read(repositoryRoot);

		var unexpected = manifest.Targets
			.Where(target => target.Frameworks is not null && !scoped.Contains((target.Path, target.Relationship.ToString())))
			.Select(target => $"{target.Path} | {target.Relationship}").ToArray();

		Assert.Empty(unexpected);
	}

	// Criterion 4: the twelve upstream dependencies identified in the issue must declare the
	// single framework each actually serves. The TempData path selects by relationship as
	// well because it names two distinct watches, one per relationship.
	[Theory]
	[MemberData(nameof(FrameworkScopedWatches))]
	public void Committed_manifest_annotates_the_framework_specific_watch_with_its_scope(
		string upstreamPath, string relationshipName, string expectedFramework)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
		var manifest = WatchManifestFile.Read(repositoryRoot);
		var relationship = Enum.Parse<WatchRelationship>(relationshipName);

		var watch = manifest.Targets.Single(target => target.Path == upstreamPath && target.Relationship == relationship);

		Assert.Equal([expectedFramework], watch.Frameworks);
	}

	// LR-1a1c95f-S001 / LR-1a1c95f-P002 — a decision, recorded here rather than left implicit.
	// The comparison walk (`CompareWatchedAsync`, `CompareApisAsync`) stays framework-blind on
	// purpose: it can only ever report a *true* observation ("this file the watch names changed
	// in that framework's line"), unlike the resolution walk #232 scopes, which can report a
	// *false* one ("this watched path does not exist upstream"). Scoping the comparison walk
	// would suppress a true report — the under-reporting failure mode #219, #232, and this issue
	// all exist to remove — so it is left unscoped deliberately. This case pins that choice: a
	// watch scoped to net11.0 only still reports drift for the net10.0 compare, because the file
	// it names changed in that line too. It is expected to stay green through the
	// implementation; a later change that wants to scope this walk must argue against this test
	// rather than silently narrowing it.
	[Fact]
	public async Task Watch_scoped_to_one_framework_still_reports_drift_in_a_different_frameworks_compare()
	{
		const string path = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs";
		var transport = SourceChangeTests.DriftTransport("github/compare-source-execution-sentinel.json");
		var manifest = Fixture.MultiTargetManifest(
			Fixture.Watch(path, relationship: WatchRelationship.Reimplements) with { Frameworks = ["net11.0"] });
		var request = new MonitorRequest(manifest, 10, RequestedTag: "v10.0.12", BaselineCommit: Fixture.BaselineCommit);

		var result = await Fixture.Application(transport).RunAsync(request);

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Equal([new SourceChange(path, ChangeKind.Changed, ReviewClassification.ParityRequired)], result.SourceChanges);
	}

	// Issue #232's own resolution loop (`UpstreamMonitorApplication.ReportAsync`) does not yet
	// consult `Frameworks` at all: it resolves every watch absent from the compare against
	// whichever framework's baseline the running request measures, regardless of which
	// framework(s) the watch names. A watch scoped to net10.0 only must be resolved against
	// net10.0's baseline, and must not be resolved — in either direction — against net11.0's:
	// both runs are steady state (the requested tag resolves to each framework's own reviewed
	// commit), so no compare stub is needed and the only question is whether resolution runs at
	// all for the framework the watch does not name.
	[Fact]
	public async Task Watch_scoped_to_one_framework_is_resolved_only_against_that_frameworks_baseline()
	{
		const string path = "src/Components/Endpoints/src/CacheView/ScopedOnlyToNet10.cs";
		var watch = Fixture.Watch(path) with { Frameworks = ["net10.0"] };
		var manifest = Fixture.MultiTargetManifest(watch);

		var net10Transport = new FakeGitHubTransport();
		net10Transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11", Fixture.Read("github/ref-v10.0.11-direct.json"));
		var net10Result = await Fixture.Application(net10Transport)
			.RunAsync(new MonitorRequest(manifest, 10, RequestedTag: "v10.0.11"));

		var net11Transport = new FakeGitHubTransport();
		net11Transport.AddJson(
			$"/repos/dotnet/aspnetcore/git/ref/tags/{Fixture.Net11ReviewedTag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Fixture.Net11ReviewedCommit } }));
		var net11Result = await Fixture.Application(net11Transport).RunAsync(new MonitorRequest(manifest, 11));

		// The framework the watch names: absent from the (empty) steady-state compare and
		// unstubbed at its own baseline, so it must still be reported unresolved.
		Assert.Equal(MonitorStatus.UnresolvedWatch, net10Result.Status);
		Assert.Contains(path, net10Result.JsonReport, StringComparison.Ordinal);

		// The framework the watch does not name: the same unstubbed path must never be resolved
		// against net11.0's baseline at all, so the run reports Current rather than a false
		// "does not exist upstream" for a dependency this watch never claimed there.
		Assert.Equal(MonitorStatus.Current, net11Result.Status);
		Assert.DoesNotContain(path, net11Result.JsonReport, StringComparison.Ordinal);
	}

	// The loop reads `Targets.DistinctBy(watch => (watch.Path, watch.Match))` before anything
	// else. Two entries sharing a path and match but differing only in scope must not let
	// whichever survives that collapse decide applicability on its own: applicability has to be
	// decided per watch before the collapse, so that any entry naming the framework being
	// measured keeps the path in the loop even when a differently-scoped sibling for the same
	// path would otherwise stand in for it. `netOtherOnly` is listed first specifically so a fix
	// that filters by `Frameworks` only after `DistinctBy` has already picked its survivor — the
	// literal reading of "decide, then collapse" reversed — keeps `netOtherOnly` (scoped away
	// from net10.0) as that survivor and drops `netThisOnly` unseen, never resolving this path
	// for net10.0 at all. The committed manifest already has two entries sharing a path and
	// match (`TempDataProviderServiceCollectionExtensions.cs`, differing by relationship), which
	// is silent today only because both currently carry the same scope.
	//
	// This is green today for a different reason than the fix below will make it green:
	// ReportAsync does not yet consult Frameworks anywhere, so every non-diffed watch is
	// unconditionally resolved regardless of scope, and resolving the same path and match always
	// answers identically no matter which duplicate entry represents it. A collapse-then-filter
	// implementation is the one shape this test rejects; it must fail once someone reaches for
	// `.Where(watch => Applies(watch, request.Framework))` placed after `DistinctBy` instead of
	// before it.
	[Fact]
	public async Task Applicable_entry_decides_resolution_even_when_a_differently_scoped_duplicate_collapses_first()
	{
		const string path = "src/Components/Endpoints/src/CacheView/DuplicateScopedWatch.cs";
		var netOtherOnly = Fixture.Watch(path) with { Frameworks = ["net11.0"] };
		var netThisOnly = Fixture.Watch(path) with { Frameworks = ["net10.0"] };
		var manifest = Fixture.MultiTargetManifest(netOtherOnly, netThisOnly);
		var transport = new FakeGitHubTransport();
		transport.AddJson("/repos/dotnet/aspnetcore/git/ref/tags/v10.0.11", Fixture.Read("github/ref-v10.0.11-direct.json"));

		var result = await Fixture.Application(transport).RunAsync(new MonitorRequest(manifest, 10, RequestedTag: "v10.0.11"));

		Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
		Assert.Contains(path, result.JsonReport, StringComparison.Ordinal);
	}
}
