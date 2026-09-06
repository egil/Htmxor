using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class SourcePathEncodingTests
{
	[Theory]
	[InlineData("src/folder name/Renderer.cs", "src/folder%20name/Renderer.cs")]
	[InlineData("src/folder#name/Renderer.cs", "src/folder%23name/Renderer.cs")]
	[InlineData("src/folder?name/Renderer.cs", "src/folder%3Fname/Renderer.cs")]
	[InlineData("src/folder%23name/Renderer.cs", "src/folder%2523name/Renderer.cs")]
	[InlineData("src/folder name#?%23/Renderer.Part #?%23.cs", "src/folder%20name%23%3F%2523/Renderer.Part%20%23%3F%2523.cs")]
	public async Task Reserved_source_path_segments_retrieve_exact_revision_content(string path, string encodedPath)
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson($"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = path, status = "modified" } } }));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{encodedPath}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContentText("public class Renderer { protected void Render(int value) { } }"));
		transport.AddJson($"/repos/dotnet/aspnetcore/contents/{encodedPath}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContentText("public class Renderer { protected void Render(string value) { } }"));

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(
			Fixture.Watch(path, apiSurface: ApiSurface.Subclass, relationship: WatchRelationship.Subclasses)));

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Null(result.InfrastructureError);
		ReportAssertions.Equal(result, new ReportExpectation("drift",
			new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
			[new(path, "changed", "compatibility-risk")],
			[new("Renderer", "added", "member", "protected void Render(string value)", "extensibility-opportunity"),
			 new("Renderer", "removed", "member", "protected void Render(int value)", "compatibility-risk")], null));
	}
}
