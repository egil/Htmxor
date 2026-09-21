using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

/// <summary>
/// Protects issue #241's amended contract, read against this branch's reachable seam:
/// <c>ManifestDependencyPolicy</c> and the manifest reader, not the (not-yet-present)
/// resolution loop that #232 introduces.
/// </summary>
public sealed class WatchFrameworkScopeTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string ComponentPath = "src/Components/Components/src/ComponentBase.cs";
	private const string NavigationManagerPath = "src/Components/Components/src/NavigationManager.cs";

	// The twelve watches identified in the issue as answering for only one framework's
	// local `#if`-gated dependency. Annotating these is the Implementor's production work;
	// this fixes their expected `frameworks` scope so the test fails until that lands.
	public static TheoryData<string, string> Net11OnlyWatches => new()
	{
		{ "src/Components/Endpoints/src/CacheView/CacheView.cs", "net11.0" },
		{ "src/Components/Endpoints/src/CacheView/CacheViewRenderState.cs", "net11.0" },
		{ "src/Components/Endpoints/src/CacheView/CacheViewService.cs", "net11.0" },
		{ "src/Components/Endpoints/src/DependencyInjection/TempDataService.cs", "net11.0" },
		{ "src/Components/Endpoints/src/Rendering/CacheViewTextWriter.cs", "net11.0" },
		{ "src/Components/Endpoints/src/SessionCascadingValueSupplier.cs", "net11.0" },
		{ "src/Components/Endpoints/src/TempData/TempDataCascadingValueSupplier.cs", "net11.0" },
		{ "src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs", "net11.0" },
		{ "src/Components/Shared/src/ComponentKeyHelper.cs", "net11.0" },
		{ "src/Components/Shared/src/RenderFragmentCapture.cs", "net11.0" },
		{ "src/Shared/MiddlewareInvokedKeys.cs", "net11.0" },
		{ "src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs", "net10.0" },
	};

	// Criterion 1 (green baseline): the thirty-six unannotated watches behave exactly as
	// before. Two ordinary, unscoped watches each cover the dependency discovered for their
	// own framework, exactly as `Covered` already does today, ignoring framework entirely.
	[Fact]
	public void Unannotated_watches_cover_dependencies_discovered_for_every_configured_framework()
	{
		using var repository = new TwoFrameworkRepository();
		var manifest = Fixture.MultiTargetManifest(
			Fixture.Watch(ComponentPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]),
			Fixture.Watch(NavigationManagerPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	// Criterion 1 (schema): the manifest reader must parse an explicit `frameworks` list onto
	// its watch. Nothing parses it yet, so `Frameworks` stays null.
	[Fact]
	public void A_watch_frameworks_list_is_parsed_from_the_manifest()
	{
		using var repository = new TemporaryManifestRepository();
		repository.WriteManifest($$"""
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

	// Criterion 2: an unknown framework name on a watch must be rejected, naming both the
	// watch path and the unknown name. Nothing validates the property today, so it is
	// silently ignored instead of throwing.
	[Fact]
	public void Unknown_framework_name_on_a_watch_is_rejected_naming_the_watch_and_the_unknown_name()
	{
		using var repository = new TemporaryManifestRepository();
		const string watchPath = "src/Components/Endpoints/src/Forms/Provider.cs";
		repository.WriteManifest($$"""
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
			    "frameworks": ["net12.0"]
			  }]
			}
			""");

		var exception = Assert.Throws<MonitorFailure>(() => WatchManifestFile.Read(repository.Path));

		Assert.Contains(watchPath, exception.Message, StringComparison.Ordinal);
		Assert.Contains("net12.0", exception.Message, StringComparison.Ordinal);
	}

	// Criteria 3 and 5: a watch scoped to net11.0 only must not cover a dependency discovered
	// for net10.0, even though its upstream path, relationship, and local dependency all
	// match. `Covered` ignores `Frameworks` entirely today, so the scoped watch silently
	// covers the wrong framework's dependency and this reports nothing.
	[Fact]
	public void Watch_scoped_to_one_framework_does_not_cover_a_dependency_discovered_for_another_framework()
	{
		using var repository = new TwoFrameworkRepository();
		var manifest = Fixture.MultiTargetManifest(
			Fixture.ScopedWatch(ComponentPath, ["net11.0"], relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]),
			Fixture.Watch(NavigationManagerPath, relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, ComponentPath, WatchRelationship.Subclasses)], untracked);
	}

	// Criterion 4: the twelve watches identified in the issue must declare the single
	// framework they actually serve. Annotating `upstream-watch.json` is the Implementor's
	// production work; today none of them carry a `frameworks` property, and the reader does
	// not parse one, so every `Frameworks` reads back null instead of the expected scope.
	[Theory]
	[MemberData(nameof(Net11OnlyWatches))]
	public void Committed_manifest_annotates_the_framework_specific_watch_with_its_scope(string upstreamPath, string expectedFramework)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
		var manifest = WatchManifestFile.Read(repositoryRoot);

		var watch = manifest.Targets.Single(target => target.Path == upstreamPath);

		Assert.Equal([expectedFramework], watch.Frameworks);
	}

	private sealed class TwoFrameworkRepository : IDisposable
	{
		public TwoFrameworkRepository()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-watch-frameworks-{Guid.NewGuid():N}");
			var destination = System.IO.Path.Combine(Path, "src", "Htmxor");
			Directory.CreateDirectory(destination);
			var source = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "target-framework-dependencies", "per-target-symbols", "Dependency.cs");
			File.Copy(source, System.IO.Path.Combine(destination, "Dependency.cs"));
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}

	private sealed class TemporaryManifestRepository : IDisposable
	{
		public TemporaryManifestRepository()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-watch-manifest-{Guid.NewGuid():N}");
			Directory.CreateDirectory(System.IO.Path.Combine(Path, "eng", "Htmxor.UpstreamMonitor"));
		}

		public string Path { get; }

		public void WriteManifest(string json) =>
			File.WriteAllText(System.IO.Path.Combine(Path, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json"), json);

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
