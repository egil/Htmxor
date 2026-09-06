using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class TargetFrameworkDeclarationPolicyTests
{
	private const string LocalPath = "src/Htmxor/Dependency.cs";
	private const string ComponentPath = "src/Components/Components/src/ComponentBase.cs";

	[Theory]
	[InlineData("NET")]
	[InlineData("NET10_0")]
	[InlineData("NETCOREAPP")]
	[InlineData("NET5_0_OR_GREATER")]
	[InlineData("NET6_0_OR_GREATER")]
	[InlineData("NET7_0_OR_GREATER")]
	[InlineData("NET8_0_OR_GREATER")]
	[InlineData("NET9_0_OR_GREATER")]
	[InlineData("NET10_0_OR_GREATER")]
	[InlineData("NETCOREAPP1_0_OR_GREATER")]
	[InlineData("NETCOREAPP1_1_OR_GREATER")]
	[InlineData("NETCOREAPP2_0_OR_GREATER")]
	[InlineData("NETCOREAPP2_1_OR_GREATER")]
	[InlineData("NETCOREAPP2_2_OR_GREATER")]
	[InlineData("NETCOREAPP3_0_OR_GREATER")]
	[InlineData("NETCOREAPP3_1_OR_GREATER")]
	public void Active_target_symbol_requires_the_framework_source_watch(string symbol)
	{
		using var repository = new ConditionalRepository("active-symbol", symbol);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, ComponentPath, WatchRelationship.Subclasses)], untracked);
	}

	[Theory]
	[InlineData("NET9_0")]
	[InlineData("NET11_0")]
	[InlineData("NET11_0_OR_GREATER")]
	[InlineData("NETCOREAPP3_1")]
	[InlineData("NETSTANDARD")]
	[InlineData("NETFRAMEWORK")]
	public void Inactive_target_symbol_selects_the_local_else_branch(string symbol)
	{
		using var repository = new ConditionalRepository("inactive-symbol", symbol);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Empty(untracked);
	}

	public static TheoryData<string, string, string> ActiveDeclarations => new()
	{
		{ "global-import", ComponentPath, "Subclasses" },
		{ "global-alias", ComponentPath, "Subclasses" },
		{ "active-unmapped", "unresolved:Microsoft.AspNetCore.SignalR.IHubContext`1", "Implements" },
		{ "active-record", "src/Components/Components/src/IComponent.cs", "Implements" },
		{ "negated-future-symbol", ComponentPath, "Subclasses" },
		{ "inactive-else-framework", ComponentPath, "Subclasses" },
		{ "unguarded", ComponentPath, "Subclasses" },
	};

	[Theory]
	[MemberData(nameof(ActiveDeclarations))]
	public void Active_declaration_requires_its_framework_identity(string fixture, string upstreamPath, string relationshipName)
	{
		using var repository = new ConditionalRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Equal([new LocalFrameworkDependency(LocalPath, upstreamPath, Enum.Parse<WatchRelationship>(relationshipName))], untracked);
	}

	[Fact]
	public void Exact_watch_covers_the_active_framework_declaration()
	{
		using var repository = new ConditionalRepository("active-symbol", "NET10_0");
		var manifest = Fixture.Manifest(Fixture.Watch(ComponentPath,
			relationship: WatchRelationship.Subclasses, dependencies: [LocalPath]));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Empty(untracked);
	}

	[Theory]
	[InlineData("negated-active-symbol")]
	[InlineData("active-else-local")]
	[InlineData("active-local-record")]
	public void Active_local_declaration_does_not_create_a_framework_dependency(string fixture)
	{
		using var repository = new ConditionalRepository(fixture);

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, Fixture.Manifest());

		Assert.Empty(untracked);
	}

	private sealed class ConditionalRepository : IDisposable
	{
		public ConditionalRepository(string fixture, string symbol = "")
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-target-framework-{Guid.NewGuid():N}");
			var destination = System.IO.Path.Combine(Path, "src", "Htmxor");
			Directory.CreateDirectory(destination);
			foreach (var source in Directory.EnumerateFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "target-framework-dependencies", fixture), "*.cs"))
			{
				File.WriteAllText(System.IO.Path.Combine(destination, System.IO.Path.GetFileName(source)),
					File.ReadAllText(source).Replace("TARGET_FRAMEWORK_SYMBOL", symbol, StringComparison.Ordinal));
			}
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
