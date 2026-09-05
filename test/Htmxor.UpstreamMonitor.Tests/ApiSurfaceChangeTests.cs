using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ApiSurfaceChangeTests
{
	private const string StaticRendererPath = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs";
	private const string InvokerPath = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs";

	[Fact]
	public async Task Subclass_and_interface_surfaces_report_public_and_protected_API_changes()
	{
		var transport = SourceChangeTests.DriftTransport("github/compare-api-files.json");
		AddSourceResponses(transport, StaticRendererPath, "StaticHtmlRenderer.cs");
		AddSourceResponses(transport, InvokerPath, "IRazorComponentEndpointInvoker.cs");
		var application = Fixture.Application(transport);
		var manifest = Fixture.Manifest(
			Fixture.Watch(StaticRendererPath, apiSurface: ApiSurface.Subclass),
			Fixture.Watch(InvokerPath, apiSurface: ApiSurface.Interface));
		var request = new MonitorRequest(
			manifest,
			10,
			RequestedTag: "v10.0.12",
			BaselineCommit: Fixture.BaselineCommit);

		var result = await application.RunAsync(request);

		Assert.Equal(MonitorStatus.Drift, result.Status);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.BaseType, "Renderer"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.BaseType, "RendererV2"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Constraint, "where T : class"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Constraint, "where T : notnull, IDisposable"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Constructor, "protected StaticHtmlRenderer(IServiceProvider services)"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Constructor, "public StaticHtmlRenderer(IServiceProvider services)"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Removed, ApiSymbolKind.Member, "protected virtual Task RenderAsync(T value)"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("StaticHtmlRenderer<T>", ChangeKind.Added, ApiSymbolKind.Member, "protected abstract ValueTask RenderAsync(T value)"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Removed, ApiSymbolKind.Member, "Task InvokeAsync(HttpContext context)"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "ValueTask InvokeAsync(HttpContext context)"),
			result.ApiChanges);
		Assert.Contains(
			new ApiChange("IRazorComponentEndpointInvoker", ChangeKind.Added, ApiSymbolKind.Member, "Task WarmAsync(CancellationToken cancellationToken)"),
			result.ApiChanges);
		Assert.Contains("public/protected API", result.MarkdownReport, StringComparison.OrdinalIgnoreCase);
	}

	private static void AddSourceResponses(
		FakeGitHubTransport transport,
		string upstreamPath,
		string fixtureName)
	{
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{upstreamPath}?ref={Fixture.BaselineCommit}",
			Fixture.GitHubContent($"source/baseline/{fixtureName}"));
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/contents/{upstreamPath}?ref={Fixture.TargetCommit}",
			Fixture.GitHubContent($"source/target/{fixtureName}"));
	}
}
