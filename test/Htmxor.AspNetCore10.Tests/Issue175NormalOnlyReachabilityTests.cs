using System.Net;
using System.Text.RegularExpressions;
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
/// with no wiring) is staged. These tests pin today's -- still wrong -- runtime behavior for a marked
/// component authored as .razor, .razor.cs, and all-C#, matching the issue's own "Meaningful red"
/// paragraph. They are expected to start failing once the matcher policy, <c>ConfigureEndpoint</c>,
/// and the generator/analyzer recognize the marker, at which point green-baseline flips each
/// assertion to its approved outcome (404 for the direct cells, a build error for the action cell).
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
		builder.Services.AddScoped<Issue175RequestProbe>();

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
	public async Task Direct_partial_GET_still_answers_with_component_output(
		string path,
		Type componentType,
		string form)
	{
		_ = componentType;

		// #175 red: HtmxorDirectEndpointMatcherPolicy does not invalidate a marked candidate yet,
		// so a direct partial GET is still selected, runs the component's lifecycle, and renders
		// its fragment -- the brief requires 404 with no lifecycle work once the policy recognizes
		// the marker.
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		Assert.Contains($"data-issue-175-page=\"{form}\"", body, StringComparison.Ordinal);
		Assert.Contains("data-initialization-count=\"1\"", body, StringComparison.Ordinal);
		Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", body, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("/issue-175/razor", "razor")]
	[InlineData("/issue-175/codebehind", "codebehind")]
	public async Task Direct_POST_still_invokes_the_marked_components_inferred_action(string path, string form)
	{
		_ = form;

		// #175 red: today's generator/analyzer do not report HTMXOR002 for an action on a marked
		// component (pinned separately in HtmxorRouteDeclarationAnalyzerTests), and the matcher
		// policy does not 404 the marked endpoint, so this unsafe request still reaches the
		// component instance and its callback still runs.
		var (token, cookie) = await GetAntiforgeryCredentialsAsync(path);

		using var request = new HttpRequestMessage(HttpMethod.Put, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("Cookie", cookie);
		request.Headers.Add("RequestVerificationToken", token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
		Assert.Contains("data-initialization-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains("data-callback-count=\"1\"", body, StringComparison.Ordinal);
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

	private async Task<(string Token, string Cookie)> GetAntiforgeryCredentialsAsync(string path)
	{
		using var pageResponse = await client.GetAsync(path);
		var pageBody = await pageResponse.Content.ReadAsStringAsync();
		Assert.True(pageResponse.StatusCode == HttpStatusCode.OK, pageBody);

		return (
			ExtractAttribute(pageBody, "name=\"__RequestVerificationToken\"", "value"),
			ExtractAntiforgeryCookie(pageResponse));
	}

	private static string ExtractAttribute(string html, string elementMarker, string attributeName)
	{
		var markerIndex = html.IndexOf(elementMarker, StringComparison.Ordinal);
		Assert.True(markerIndex >= 0, $"Expected an element containing '{elementMarker}'.");
		var elementStart = html.LastIndexOf('<', markerIndex);
		var elementEnd = html.IndexOf('>', markerIndex);
		Assert.True(elementStart >= 0 && elementEnd > elementStart, $"Expected complete markup around '{elementMarker}'.");
		var element = html[elementStart..(elementEnd + 1)];
		var match = Regex.Match(
			element,
			$"(?:^|\\s){Regex.Escape(attributeName)}=\"(?<value>[^\"]*)\"",
			RegexOptions.CultureInvariant);
		Assert.True(match.Success, $"Expected attribute '{attributeName}' in '{element}'.");
		return WebUtility.HtmlDecode(match.Groups["value"].Value);
	}

	private static string ExtractAntiforgeryCookie(HttpResponseMessage response)
	{
		var cookie = Assert.Single(
			response.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
		return cookie.Split(';', 2)[0];
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

	public int CallbackCount { get; private set; }

	public void RecordInitialization() => InitializationCount++;

	public void RecordCallback() => CallbackCount++;
}

internal sealed record Issue175MetadataSentinel(string Value)
{
	public static Issue175MetadataSentinel Instance { get; } = new("preserved");
}
