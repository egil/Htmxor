using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ManifestDependencyPolicyTests
{
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

		public void Write(string relativePath)
		{
			var path = System.IO.Path.Combine(Path, relativePath);
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
			File.WriteAllText(path, string.Empty);
		}

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
