using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ManifestDependencyPolicyTests
{
	[Fact]
	public void Committed_manifest_covers_every_local_framework_dependency()
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
		var manifestPath = Path.Combine(repositoryRoot, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");

		Assert.True(File.Exists(manifestPath), "The upstream monitor manifest must be committed.");
		using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
		var manifest = ReadManifest(document.RootElement);

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
	[InlineData(
		"internal sealed class UnlistedRenderer : Microsoft.AspNetCore.Components.Web.HtmlRendering.StaticHtmlRenderer { }",
		"src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs",
		"Subclasses")]
	[InlineData(
		"internal sealed class UnlistedInvoker : Microsoft.AspNetCore.Components.Endpoints.IRazorComponentEndpointInvoker { }",
		"src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs",
		"Implements")]
	[InlineData(
		"// Htmxor upstream dependency: src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs | mirrors",
		"src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs",
		"Mirrors")]
	[InlineData(
		"// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements",
		"src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs",
		"Reimplements")]
	public void Local_framework_dependency_absent_from_manifest_is_reported(
		string source,
		string upstreamPath,
		string relationshipName)
	{
		using var repository = new TemporaryRepository();
		const string localPath = "src/Htmxor/UnlistedFrameworkDependency.cs";
		repository.Write(localPath, source);
		var relationship = Enum.Parse<WatchRelationship>(relationshipName);
		var manifest = Fixture.Manifest(Fixture.Watch(
			upstreamPath,
			apiSurface: relationship switch
			{
				WatchRelationship.Subclasses => ApiSurface.Subclass,
				WatchRelationship.Implements => ApiSurface.Interface,
				_ => ApiSurface.None,
			},
			relationship: relationship));

		var untracked = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal([new LocalFrameworkDependency(localPath, upstreamPath, relationship)], untracked);
	}

	private static WatchManifest ReadManifest(JsonElement manifest)
	{
		var reviewed = manifest.GetProperty("reviewed");
		return new WatchManifest(
			manifest.GetProperty("repository").GetString()!,
			reviewed.GetProperty("tag").GetString()!,
			reviewed.GetProperty("commit").GetString()!,
			manifest.GetProperty("watches").EnumerateArray().Select(ReadWatch).ToArray());
	}

	private static WatchTarget ReadWatch(JsonElement watch) => new(
		watch.GetProperty("path").GetString()!,
		Enum.Parse<WatchMatch>(watch.GetProperty("match").GetString()!, ignoreCase: true),
		Enum.Parse<ApiSurface>(watch.GetProperty("api").GetString()!, ignoreCase: true),
		Enum.Parse<WatchRelationship>(watch.GetProperty("relationship").GetString()!, ignoreCase: true),
		watch.GetProperty("dependencies").EnumerateArray().Select(value => value.GetString()!).ToArray());

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

		public void Write(string relativePath, string contents = "")
		{
			var path = System.IO.Path.Combine(Path, relativePath);
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
			File.WriteAllText(path, contents);
		}

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
