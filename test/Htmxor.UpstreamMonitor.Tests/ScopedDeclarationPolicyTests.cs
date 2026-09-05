using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ScopedDeclarationPolicyTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string ComponentPath = "src/Components/Components/src/ComponentBase.cs";
	private const string InputPath = "src/Components/Web/src/Forms/InputBase.cs";

	public static TheoryData<string, string> MappedDeclarations => new()
	{
		{ "global-import", ComponentPath },
		{ "global-alias", ComponentPath },
		{ "block-aliases", ComponentPath },
		{ "file-alias-inner-alias", ComponentPath },
		{ "nested-alias", ComponentPath },
		{ "global-alias-inner-alias", ComponentPath },
		{ "file-import-inner-import", ComponentPath },
		{ "global-import-inner-import", ComponentPath },
		{ "file-scoped-import", ComponentPath },
		{ "semicolon-base", ComponentPath },
		{ "semicolon-primary", ComponentPath },
		{ "semicolon-generic", InputPath },
	};

	[Theory]
	[MemberData(nameof(MappedDeclarations))]
	public void Framework_base_in_its_declaration_scope_requires_its_source_watch(string fixture, string upstreamPath)
	{
		using var repository = new ScopedRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, upstreamPath, WatchRelationship.Subclasses)], untracked);
	}

	[Theory]
	[MemberData(nameof(MappedDeclarations))]
	public void Exact_watch_covers_the_framework_base_in_its_declaration_scope(string fixture, string upstreamPath)
	{
		using var repository = new ScopedRepository(fixture);
		var manifest = Fixture.Manifest(Fixture.Watch(upstreamPath, apiSurface: ApiSurface.Subclass,
			relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	[Theory]
	[InlineData("semicolon-unmapped-one", "Microsoft.AspNetCore.SignalR.IHubContext`1")]
	[InlineData("semicolon-unmapped-two", "Microsoft.AspNetCore.SignalR.IHubContext`2")]
	public void Semicolon_interface_requires_its_unmapped_framework_identity(string fixture, string identity)
	{
		using var repository = new ScopedRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, $"unresolved:{identity}", WatchRelationship.Implements)], untracked);
	}

	[Theory]
	[InlineData("block-alias-unused")]
	[InlineData("file-import-does-not-leak")]
	[InlineData("global-import-local-homonym")]
	[InlineData("global-import-inner-local")]
	[InlineData("global-alias-inner-local")]
	[InlineData("semicolon-local")]
	[InlineData("semicolon-local-generic")]
	[InlineData("semicolon-unrelated-namespace")]
	[InlineData("inert-scope-syntax")]
	public void Local_bases_and_inert_declarations_do_not_inherit_unrelated_imports(string fixture)
	{
		using var repository = new ScopedRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Empty(untracked);
	}

	private sealed class ScopedRepository : IDisposable
	{
		public ScopedRepository(string fixture)
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-scoped-declarations-{Guid.NewGuid():N}");
			var destination = System.IO.Path.Combine(Path, "src", "Htmxor");
			Directory.CreateDirectory(destination);
			foreach (var source in Directory.EnumerateFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "scoped-dependencies", fixture), "*.cs"))
			{
				File.Copy(source, System.IO.Path.Combine(destination, System.IO.Path.GetFileName(source)));
			}
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
