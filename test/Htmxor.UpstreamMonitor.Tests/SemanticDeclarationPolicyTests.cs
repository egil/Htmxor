using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class SemanticDeclarationPolicyTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string ComponentPath = "src/Components/Components/src/ComponentBase.cs";
	private const string InterfacePath = "src/Components/Components/src/IComponent.cs";

	public static TheoryData<string, string, string> FrameworkDeclarations => new()
	{
		{ "alias-enclosing-import", ComponentPath, "Subclasses" },
		{ "relative-namespace-import", ComponentPath, "Subclasses" },
		{ "relative-type-alias", ComponentPath, "Subclasses" },
		{ "alias-declaration-context", ComponentPath, "Subclasses" },
		{ "fully-qualified-alias", ComponentPath, "Subclasses" },
		{ "record-struct-braced", InterfacePath, "Implements" },
		{ "record-class-primary", InterfacePath, "Implements" },
		{ "record-struct-primary-semicolon", InterfacePath, "Implements" },
		{ "record-class-semicolon", InterfacePath, "Implements" },
	};

	[Theory]
	[MemberData(nameof(FrameworkDeclarations))]
	public void Bound_framework_declaration_requires_its_canonical_source_watch(
		string fixture, string upstreamPath, string relationshipName)
	{
		using var repository = new DeclarationRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, upstreamPath, Enum.Parse<WatchRelationship>(relationshipName))], untracked);
	}

	[Theory]
	[MemberData(nameof(FrameworkDeclarations))]
	public void Exact_watch_covers_the_bound_framework_declaration(
		string fixture, string upstreamPath, string relationshipName)
	{
		using var repository = new DeclarationRepository(fixture);
		var manifest = Fixture.Manifest(Fixture.Watch(upstreamPath,
			relationship: Enum.Parse<WatchRelationship>(relationshipName), dependencies: [LocalPath]));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	[Theory]
	[InlineData("alias-local-target")]
	[InlineData("alias-sibling-nonleakage")]
	[InlineData("record-local-interfaces")]
	[InlineData("inert-records")]
	public void Local_bindings_and_inert_records_do_not_create_framework_dependencies(string fixture)
	{
		using var repository = new DeclarationRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Empty(untracked);
	}

	private sealed class DeclarationRepository : IDisposable
	{
		public DeclarationRepository(string fixture)
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-semantic-declarations-{Guid.NewGuid():N}");
			var destination = System.IO.Path.Combine(Path, "src", "Htmxor");
			Directory.CreateDirectory(destination);
			foreach (var source in Directory.EnumerateFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "semantic-dependencies", fixture), "*.cs"))
			{
				File.Copy(source, System.IO.Path.Combine(destination, System.IO.Path.GetFileName(source)));
			}
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
