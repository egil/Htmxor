using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class SourceChangeTests
{
	private const string Invoker = "src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs";
	private const string RendererPrefix = "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer";
	private const string StaticRenderer = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs";

	[Fact]
	public async Task Watched_files_and_prefixes_report_added_removed_and_changed_paths()
	{
		var transport = DriftTransport("github/compare-watched-files.json");
		var application = Fixture.Application(transport);
		var manifest = Fixture.Manifest(
			Fixture.Watch(Invoker),
			Fixture.Watch(RendererPrefix, WatchMatch.Prefix),
			Fixture.Watch(StaticRenderer));
		var request = new MonitorRequest(
			manifest,
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Equal(new UpstreamRevision("v10.0.12", Fixture.TargetCommit), result.Upstream);
		Assert.Equal(
			[
				new SourceChange(Invoker, ChangeKind.Changed),
				new SourceChange($"{RendererPrefix}.Diagnostics.cs", ChangeKind.Added),
				new SourceChange($"{RendererPrefix}.PrerenderingState.cs", ChangeKind.Removed),
				new SourceChange($"{RendererPrefix}.Streaming.cs", ChangeKind.Changed),
				new SourceChange(StaticRenderer, ChangeKind.Removed),
			],
			result.SourceChanges);
		Assert.Contains(Fixture.BaselineCommit, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(Fixture.TargetCommit, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(RendererPrefix, result.MarkdownReport, StringComparison.Ordinal);
		Assert.Contains("added", result.MarkdownReport, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("removed", result.MarkdownReport, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("changed", result.MarkdownReport, StringComparison.OrdinalIgnoreCase);
	}

	internal static FakeGitHubTransport DriftTransport(string compareFixture)
	{
		var transport = new FakeGitHubTransport();
		transport.AddJson(
			"/repos/dotnet/aspnetcore/git/ref/tags/v10.0.12",
			Fixture.Read("github/ref-v10.0.12-direct.json"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			Fixture.Read(compareFixture));
		return transport;
	}
}
