using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class BaseTypeSyntaxPolicyTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string ComponentPath = "src/Components/Components/src/ComponentBase.cs";
	private const string InputPath = "src/Components/Web/src/Forms/InputBase.cs";
	private const string UnrelatedPath = "src/Components/Components/src/RenderTree/Renderer.cs";

	public static TheoryData<string, string> MappedDeclarations => new()
	{
		{ "base-call-imported", ComponentPath },
		{ "base-call-qualified", ComponentPath },
		{ "base-call-alias", ComponentPath },
		{ "base-call-namespace-alias", ComponentPath },
		{ "generic-base-imported", InputPath },
		{ "generic-base-qualified", InputPath },
		{ "generic-base-alias", InputPath },
		{ "generic-base-namespace-alias", InputPath },
		{ "generic-base-distinct-arity", InputPath },
	};

	public static TheoryData<string, string> UnmappedDeclarations => new()
	{
		{ "generic-unmapped-one", "Microsoft.AspNetCore.SignalR.IHubContext`1" },
		{ "generic-unmapped-two", "Microsoft.AspNetCore.SignalR.IHubContext`2" },
	};

	[Theory]
	[MemberData(nameof(MappedDeclarations))]
	public void Framework_base_syntax_requires_its_canonical_source_watch(string fixture, string upstreamPath)
	{
		using var repository = new DeclarationRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, upstreamPath, WatchRelationship.Subclasses)], untracked);
	}

	[Theory]
	[MemberData(nameof(MappedDeclarations))]
	public void Unrelated_watch_does_not_cover_a_framework_base(string fixture, string upstreamPath)
	{
		using var repository = new DeclarationRepository(fixture);
		var manifest = Fixture.Manifest(Watch(UnrelatedPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, upstreamPath, WatchRelationship.Subclasses)], untracked);
	}

	[Theory]
	[MemberData(nameof(MappedDeclarations))]
	public void Canonical_watch_covers_a_framework_base_regardless_of_source_spelling(string fixture, string upstreamPath)
	{
		using var repository = new DeclarationRepository(fixture);
		var manifest = Fixture.Manifest(Watch(upstreamPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	[Theory]
	[MemberData(nameof(UnmappedDeclarations))]
	public void Unmapped_generic_framework_interface_reports_its_metadata_arity(string fixture, string identity)
	{
		using var repository = new DeclarationRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, $"unresolved:{identity}", WatchRelationship.Implements)], untracked);
	}

	[Theory]
	[MemberData(nameof(UnmappedDeclarations))]
	public void Unrelated_watch_cannot_hide_an_unmapped_generic_interface(string fixture, string identity)
	{
		using var repository = new DeclarationRepository(fixture);
		var manifest = Fixture.Manifest(Watch(UnrelatedPath));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(LocalPath, $"unresolved:{identity}", WatchRelationship.Implements)], untracked);
	}

	[Theory]
	[InlineData("base-call-local")]
	[InlineData("generic-base-local")]
	[InlineData("generic-base-local-alias")]
	[InlineData("base-syntax-examples")]
	public void Local_bases_and_source_examples_do_not_create_framework_dependencies(string fixture)
	{
		using var repository = new DeclarationRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Empty(untracked);
	}

	private static WatchTarget Watch(string upstreamPath) => Fixture.Watch(
		upstreamPath, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses,
		dependencies: [LocalPath]);

	private sealed class DeclarationRepository : IDisposable
	{
		public DeclarationRepository(string fixture)
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-base-syntax-{Guid.NewGuid():N}");
			var destination = System.IO.Path.Combine(Path, LocalPath);
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
			File.Copy(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "dependencies", fixture, "Dependency.cs"), destination);
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
