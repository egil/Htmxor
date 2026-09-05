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
				new SourceChange(Invoker, ChangeKind.Changed, ReviewClassification.ParityRequired),
				new SourceChange($"{RendererPrefix}.Diagnostics.cs", ChangeKind.Added, ReviewClassification.ParityRequired),
				new SourceChange($"{RendererPrefix}.PrerenderingState.cs", ChangeKind.Removed, ReviewClassification.ParityRequired),
				new SourceChange($"{RendererPrefix}.Streaming.cs", ChangeKind.Changed, ReviewClassification.ParityRequired),
				new SourceChange(StaticRenderer, ChangeKind.Removed, ReviewClassification.ParityRequired),
			],
			result.SourceChanges);
		ReportAssertions.EqualDriftReport(result);
	}

	[Fact]
	public async Task Watched_base_implementation_change_without_API_change_requires_implementation_review()
	{
		const string renderer = "src/Components/Components/src/RenderTree/Renderer.cs";
		var transport = DriftTransport("github/compare-renderer-implementation.json");
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{renderer}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent("source/baseline/Renderer.cs"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{renderer}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent("source/target/Renderer.cs"));
		var application = Fixture.Application(transport);
		var request = new MonitorRequest(
			Fixture.Manifest(Fixture.Watch(
				renderer,
				apiSurface: ApiSurface.Subclass,
				relationship: WatchRelationship.Subclasses)),
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.Equal(
			[new SourceChange(renderer, ChangeKind.Changed, ReviewClassification.ImplementationReview)],
			result.SourceChanges);
		Assert.Empty(result.ApiChanges);
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
