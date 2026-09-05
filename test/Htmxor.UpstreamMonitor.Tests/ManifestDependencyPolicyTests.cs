using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ManifestDependencyPolicyTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string StaticRendererPath = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs";
	private const string InvokerInterfacePath = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs";

	public static TheoryData<string, string, string> DependencyDeclarations => new()
	{
		{ "qualified-renderer", StaticRendererPath, "Subclasses" },
		{ "partial-renderer", "src/Components/Components/src/RenderTree/Renderer.cs", "Subclasses" },
		{ "imported-interface", InvokerInterfacePath, "Implements" },
		{ "primary-constructor", InvokerInterfacePath, "Implements" },
		{ "aliased-renderer", StaticRendererPath, "Subclasses" },
		{ "mirrored-source", "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs", "Mirrors" },
		{ "private-access-source", "src/Components/Endpoints/src/Forms/Provider.cs", "PrivateAccesses" },
		{ "reimplemented-source", "src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs", "Reimplements" },
	};

	public static TheoryData<string, string, string, string> AdditionalFrameworkDeclarations => new()
	{
		{ "component-base", "Microsoft.AspNetCore.Components.ComponentBase", "src/Components/Components/src/ComponentBase.cs", "Subclasses" },
		{ "component-interface", "Microsoft.AspNetCore.Components.IComponent", "src/Components/Components/src/IComponent.cs", "Implements" },
		{ "navigation-manager", "Microsoft.AspNetCore.Components.NavigationManager", "src/Components/Components/src/NavigationManager.cs", "Subclasses" },
	};

	[Theory]
	[MemberData(nameof(AdditionalFrameworkDeclarations))]
	public void Additional_framework_identity_is_reported_without_any_watch(
		string fixture, string identity, string upstreamPath, string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		AssertAdditionalDependency(untracked, identity, upstreamPath, relationshipName);
	}

	[Theory]
	[MemberData(nameof(AdditionalFrameworkDeclarations))]
	public void Unrelated_watch_cannot_hide_an_additional_framework_identity(
		string fixture, string identity, string upstreamPath, string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);
		var manifest = Fixture.Manifest(Watch(StaticRendererPath, WatchRelationship.Subclasses, LocalPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		AssertAdditionalDependency(untracked, identity, upstreamPath, relationshipName);
	}

	[Theory]
	[MemberData(nameof(AdditionalFrameworkDeclarations))]
	public void Exact_watch_covers_an_additional_framework_identity(
		string fixture, string identity, string upstreamPath, string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);
		var manifest = Fixture.Manifest(Watch(upstreamPath, Enum.Parse<WatchRelationship>(relationshipName), LocalPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.True(untracked.Count == 0, $"{identity} is covered by its exact source watch: {string.Join(", ", untracked)}");
	}

	private static void AssertAdditionalDependency(IReadOnlyList<LocalFrameworkDependency> untracked,
		string identity, string upstreamPath, string relationshipName)
	{
		var dependency = Assert.Single(untracked);
		Assert.Equal(LocalPath, dependency.LocalPath);
		Assert.Equal(Enum.Parse<WatchRelationship>(relationshipName), dependency.Relationship);
		Assert.Contains(dependency.UpstreamPath, new[] { upstreamPath, $"unresolved:{identity}" });
	}

	[Theory]
	[InlineData("using Microsoft.AspNetCore.Http; public class Dependency : IResult { }", false)]
	[InlineData("using Microsoft.AspNetCore.Http; public class Dependency : IResult { }", true)]
	[InlineData("public class Dependency : global::Microsoft.AspNetCore.Http.IResult { }", false)]
	public void Trusted_framework_identity_without_a_reviewed_source_map_is_reported_as_unresolved(
		string source, bool unrelatedWatch)
	{
		using var repository = new TemporaryRepository();
		repository.Write(LocalPath, source);
		var manifest = unrelatedWatch
			? Fixture.Manifest(Watch(StaticRendererPath, WatchRelationship.Subclasses, LocalPath))
			: Fixture.Manifest();

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath,
			"unresolved:Microsoft.AspNetCore.Http.IResult", WatchRelationship.Implements)], untracked);
	}

	[Theory]
	[InlineData("// Htmxor upstream dependency: src/Components/Endpoints/src/Forms/Provider.cs | private-accesses trailing")]
	[InlineData("// Example: Htmxor upstream dependency: src/Components/Endpoints/src/Forms/Provider.cs | private-accesses")]
	[InlineData("class Dependency { string marker = \"// Htmxor upstream dependency: src/Components/Endpoints/src/Forms/Provider.cs | private-accesses\"; }")]
	public void Private_access_provenance_requires_an_exact_comment_marker(string source)
	{
		using var repository = new TemporaryRepository();
		repository.Write(LocalPath, source);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Empty(untracked);
	}

	[Fact]
	public void JSON_manifest_reads_private_access_relationship_without_a_public_API_surface()
	{
		using var repository = new TemporaryRepository();
		repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", """
			{
			  "repository": "dotnet/aspnetcore",
			  "reviewed": { "tag": "v10.0.11", "commit": "a5383385245bdacc20ec19f30e46090a8154d8da" },
			  "watches": [{
			    "path": "src/Components/Endpoints/src/Forms/Provider.cs",
			    "match": "file", "api": "none", "relationship": "private-accesses",
			    "dependencies": ["src/Htmxor/Dependency.cs"]
			  }]
			}
			""");

		var manifest = WatchManifestFile.Read(repository.Path);

		Assert.Equal(Fixture.Repository, manifest.Repository);
		Assert.Equal("v10.0.11", manifest.ReviewedTag);
		Assert.Equal(Fixture.ReviewedCommit, manifest.ReviewedCommit);
		var watch = Assert.Single(manifest.Targets);
		Assert.Equal("src/Components/Endpoints/src/Forms/Provider.cs", watch.Path);
		Assert.Equal(WatchMatch.File, watch.Match);
		Assert.Equal(ApiSurface.None, watch.ApiSurface);
		Assert.Equal(WatchRelationship.PrivateAccesses, watch.Relationship);
		Assert.Equal([LocalPath], watch.LocalDependencies);
	}

	[Fact]
	public void Committed_manifest_covers_every_local_framework_dependency()
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
		var manifestPath = Path.Combine(repositoryRoot, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");

		Assert.True(File.Exists(manifestPath), "The upstream monitor manifest must be committed.");
		var manifest = WatchManifestFile.Read(repositoryRoot);

		Assert.Empty(ManifestDependencyPolicy.FindUntrackedDependencies(repositoryRoot, manifest));
	}

	[Fact]
	public void Missing_local_manifest_dependency_is_reported_without_network_access()
	{
		using var repository = new TemporaryRepository();
		repository.Write("src/Htmxor/Present.cs");
		var manifest = Fixture.Manifest(Fixture.Watch(
			"src/Components/Components/src/Rendering/ComponentState.cs",
			apiSurface: ApiSurface.Subclass,
			dependencies:
			[
				"src/Htmxor/Present.cs",
				"src/Htmxor/Missing.cs",
			]));

		var missing = ManifestDependencyPolicy.FindMissingDependencies(repository.Path, manifest);

		Assert.Equal(["src/Htmxor/Missing.cs"], missing);
	}

	[Theory]
	[MemberData(nameof(DependencyDeclarations))]
	public void Local_framework_dependency_absent_from_existing_watch_is_reported(
		string fixture,
		string upstreamPath,
		string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);
		var relationship = Enum.Parse<WatchRelationship>(relationshipName);
		var manifest = Fixture.Manifest(Watch(upstreamPath, relationship));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, upstreamPath, relationship)], untracked);
	}

	[Theory]
	[MemberData(nameof(DependencyDeclarations))]
	public void Local_framework_dependency_is_discovered_with_an_empty_manifest(
		string fixture,
		string upstreamPath,
		string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);
		var manifest = Fixture.Manifest();

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal(
			[new LocalFrameworkDependency(LocalPath, upstreamPath, Enum.Parse<WatchRelationship>(relationshipName))],
			untracked);
	}

	[Theory]
	[MemberData(nameof(DependencyDeclarations))]
	public void An_unrelated_watch_for_the_same_local_file_does_not_cover_a_framework_dependency(
		string fixture,
		string upstreamPath,
		string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);
		var manifest = Fixture.Manifest(Watch(
			"src/Components/Components/src/ComponentBase.cs", WatchRelationship.Subclasses, LocalPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal(
			[new LocalFrameworkDependency(LocalPath, upstreamPath, Enum.Parse<WatchRelationship>(relationshipName))],
			untracked);
	}

	[Theory]
	[MemberData(nameof(DependencyDeclarations))]
	public void A_watch_covering_the_declared_dependency_has_no_omission(
		string fixture,
		string upstreamPath,
		string relationshipName)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture(fixture);
		var manifest = Fixture.Manifest(Watch(upstreamPath, Enum.Parse<WatchRelationship>(relationshipName), LocalPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	[Theory]
	[InlineData("comments")]
	[InlineData("strings")]
	[InlineData("imports-and-usages")]
	[InlineData("local-homonyms")]
	public void Source_without_a_framework_relationship_does_not_add_spurious_dependencies(string fixture)
	{
		using var repository = new TemporaryRepository();
		repository.WriteFixture("qualified-renderer");
		repository.WriteFixture(fixture);
		var manifest = Fixture.Manifest();

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, StaticRendererPath, WatchRelationship.Subclasses)], untracked);
	}

	private static WatchTarget Watch(string upstreamPath, WatchRelationship relationship, params string[] dependencies) =>
		Fixture.Watch(
			upstreamPath,
			apiSurface: relationship switch
			{
				WatchRelationship.Subclasses => ApiSurface.Subclass,
				WatchRelationship.Implements => ApiSurface.Interface,
				_ => ApiSurface.None,
			},
			relationship: relationship,
			dependencies: dependencies);

	private sealed class TemporaryRepository : IDisposable
	{
		public TemporaryRepository()
		{
			Path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				$"htmxor-upstream-monitor-{Guid.NewGuid():N}");
			Directory.CreateDirectory(Path);
		}

		public string Path { get; }

		public void WriteFixture(string fixture)
		{
			var directory = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "dependencies", fixture);
			foreach (var file in Directory.GetFiles(directory, "*.cs"))
			{
				Write($"src/Htmxor/{System.IO.Path.GetFileName(file)}", File.ReadAllText(file));
			}
		}

		public void Write(string relativePath, string contents = "")
		{
			var path = System.IO.Path.Combine(Path, relativePath);
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
			File.WriteAllText(path, contents);
		}

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
