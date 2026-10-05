using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Htmxor.AspNetCore10;

/// <summary>
/// #175 red: the behavior-neutral seam from commit 82fc101 (<see cref="DisableHtmxDirectRoutingAttribute"/>
/// with no wiring) is staged. The direct-request cases below assert the brief's approved target
/// outcome (404, no lifecycle work) for a marked component authored as .razor, .razor.cs, and
/// all-C#, so they fail today for the expected reason: <c>HtmxorDirectEndpointMatcherPolicy</c> and
/// <c>ConfigureEndpoint</c>'s defense in depth do not yet invalidate a marked candidate, so the
/// request is still selected and the component's lifecycle still runs. The stock-page, dual-route,
/// and metadata cells assert behavior that is already correct and must stay that way (regression
/// characterization).
///
/// There is no runtime action cell here. Per #172 point 5.4, any action on a marked component is a
/// build error; a marked component that also compiles an inferred binding is therefore not a state
/// this project can hold once that rule lands, so a runtime fixture for it would either not compile
/// (once fixed) or never meaningfully exercise the matcher policy (an action-less marked component
/// has no unsafe method registered at all, so an unsafe request against it already gets a stock 405
/// today, independent of the marker). The build-error side of 5.4 is covered where it belongs:
/// <see cref="Generators.Tests.HtmxorRouteDeclarationAnalyzerTests.Inferred_binding_on_a_DisableHtmxDirectRouting_marked_component_is_a_build_error"/>.
/// </summary>
public sealed class Issue175NormalOnlyReachabilityTests : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue175NormalOnlyReachabilityTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton<Issue175RequestProbe>();

		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.WithMetadata(Issue175MetadataSentinel.Instance)
			.AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
	}

	public static IEnumerable<object[]> MarkedPages()
	{
		yield return new object[] { "/issue-175/razor", typeof(Issue175RazorPage), "razor" };
		yield return new object[] { "/issue-175/codebehind", typeof(Issue175CodeBehindPage), "codebehind" };
		yield return new object[] { "/issue-175/csharp", typeof(Issue175CSharpPage), "csharp" };
	}

	[Theory]
	[MemberData(nameof(MarkedPages))]
	public async Task Normal_boosted_and_full_requests_all_get_the_stock_page_with_preserved_metadata(
		string path,
		Type componentType,
		string form)
	{
		AssertSingleMarkedEndpoint(path, componentType);

		await AssertStockResponseAsync(path, form, new HttpRequestMessage(HttpMethod.Get, path));

		using var boosted = new HttpRequestMessage(HttpMethod.Get, path);
		boosted.Headers.Add("HX-Request", "true");
		boosted.Headers.Add("HX-Boosted", "true");
		boosted.Headers.Add("HX-Request-Type", "full");
		await AssertStockResponseAsync(path, form, boosted);

		using var full = new HttpRequestMessage(HttpMethod.Get, path);
		full.Headers.Add("HX-Request", "true");
		full.Headers.Add("HX-Request-Type", "full");
		await AssertStockResponseAsync(path, form, full);
	}

	[Theory]
	[MemberData(nameof(MarkedPages))]
	public async Task Direct_partial_GET_is_not_selected_and_gets_404_with_no_lifecycle_work(
		string path,
		Type componentType,
		string form)
	{
		_ = componentType;
		_ = form;

		// #175 red: today HtmxorDirectEndpointMatcherPolicy still selects this candidate, so the
		// request succeeds and OnInitialized runs. Once the policy (and ConfigureEndpoint's defense
		// in depth) recognize the marker, the candidate is invalidated before any component work,
		// and this assertion starts passing.
		var probe = app.Services.GetRequiredService<Issue175RequestProbe>();
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal(0, probe.InitializationCount);
	}

	private void AssertSingleMarkedEndpoint(string path, Type componentType)
	{
		var endpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == componentType);
		var endpoint = Assert.Single(endpoints);

		Assert.Equal(path, endpoint.RoutePattern.RawText);
		Assert.Same(Issue175MetadataSentinel.Instance, endpoint.Metadata.GetMetadata<Issue175MetadataSentinel>());

		// The marker is a plain class attribute; stock Blazor's own endpoint-metadata convention
		// attaches it exactly as it attaches [Authorize] or [Host(...)] elsewhere in this suite,
		// independent of anything Htmxor wires for it. This is already correct today.
		Assert.NotNull(endpoint.Metadata.GetMetadata<DisableHtmxDirectRoutingAttribute>());
	}

	private async Task AssertStockResponseAsync(string path, string form, HttpRequestMessage request)
	{
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK for {path}, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("data-stock-shell", body, StringComparison.Ordinal);
		Assert.Contains($"data-issue-175-page=\"{form}\"", body, StringComparison.Ordinal);
		Assert.Contains("data-route-metadata=\"preserved\"", body, StringComparison.Ordinal);
	}

	public async Task DisposeAsync()
	{
		client?.Dispose();
		if (app is not null)
		{
			await app.DisposeAsync();
		}
	}
}

internal sealed class Issue175RequestProbe
{
	public int InitializationCount { get; private set; }

	public void RecordInitialization() => InitializationCount++;
}

internal sealed record Issue175MetadataSentinel(string Value)
{
	public static Issue175MetadataSentinel Instance { get; } = new("preserved");
}
