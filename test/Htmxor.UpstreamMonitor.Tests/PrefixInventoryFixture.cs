using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

internal static class PrefixInventoryFixture
{
	public const string RenderingDirectory = "src/Components/Endpoints/src/Rendering";
	public const string Prefix = RenderingDirectory + "/EndpointHtmlRenderer";
	public const string Main = Prefix + ".cs";
	public const string Auxiliary = Prefix + ".Auxiliary.cs";
	public const string MainSource = "internal partial class EndpointHtmlRenderer { public void Preserved() { } }";

	public static MonitorRequest Request() => ProviderInventoryTests.Request(
		Fixture.Watch(Prefix, WatchMatch.Prefix, ApiSurface.Subclass));

	public static FakeGitHubTransport Transport(string status)
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson($"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = Auxiliary, status } } }));
		return transport;
	}

	public static FakeGitHubTransport ModifiedPartial()
	{
		var transport = Transport("modified");
		Revision(transport, Fixture.BaselineCommit, (Main, MainSource), (Auxiliary, Partial("public void Retired() { }")));
		Revision(transport, Fixture.TargetCommit, (Main, MainSource), (Auxiliary, Partial("public void Introduced() { }")));
		return transport;
	}

	public static void Revision(FakeGitHubTransport transport, string revision, params (string Path, string Source)[] files)
	{
		Listing(transport, RenderingDirectory, revision, files.Select(file => file.Path));
		foreach (var file in files)
		{
			transport.AddJson(ContentsUrl(file.Path, revision), Fixture.GitHubContentText(file.Source));
		}
	}

	public static void Listing(FakeGitHubTransport transport, string directory, string revision, IEnumerable<string> paths) =>
		transport.AddJson(ContentsUrl(directory, revision), JsonSerializer.Serialize(paths.Select(path => Entry(path))));

	public static object Entry(string path, string type = "file") => new { name = Path.GetFileName(path), path, type };
	public static string ContentsUrl(string path, string revision) => $"/repos/dotnet/aspnetcore/contents/{path}?ref={revision}";
	public static string Partial(string members = "") => $"internal partial class EndpointHtmlRenderer {{ {members} }}";

	public static ReportExpectation Drift(string sourceKind, params ApiReportRow[] apis) => new("drift",
		new("unresolved", Fixture.BaselineCommit), new("v10.0.12", Fixture.TargetCommit),
		[new(Auxiliary, sourceKind, "parity-required")], apis, null);
}
