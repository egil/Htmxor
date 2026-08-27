using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

public sealed class Issue85FormTests : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue85FormTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddRazorComponents().AddHtmx();
		builder.Services.AddSingleton<Issue85ApplicationProbe>();
		builder.Services.AddScoped<Issue85RequestProbe>();

		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>()
			.AddHtmxorComponentEndpoints(app);

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task Valid_htmx_form_submission_uses_one_request_component()
	{
		var pageEndpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == typeof(Issue85Page));
		Assert.Single(pageEndpoints);

		using var normalResponse = await client.GetAsync("/issue-85");
		var normalBody = await normalResponse.Content.ReadAsStringAsync();
		Assert.True(normalResponse.StatusCode == HttpStatusCode.OK, normalBody);
		Assert.Contains("data-stock-shell", normalBody, StringComparison.Ordinal);
		Assert.Contains("hx-post=\"/issue-85\"", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-binding-count=\"0\"", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-initialization-count=\"1\"", normalBody, StringComparison.Ordinal);
		Assert.Contains("data-callback-count=\"0\"", normalBody, StringComparison.Ordinal);
		Assert.Equal("issue-85-form", ExtractAttribute(normalBody, "name=\"_handler\"", "value"));
		Assert.Equal("Input.Value", ExtractAttribute(normalBody, "data-issue-85-input", "name"));
		var token = ExtractAttribute(normalBody, "name=\"__RequestVerificationToken\"", "value");
		var cookie = ExtractAntiforgeryCookie(normalResponse);
		var normalRequestId = ExtractAttribute(normalBody, "data-issue-85-result", "data-request-id");

		using var request = CreatePostRequest(cookie, token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-bound-value=\"accepted-value\"", body, StringComparison.Ordinal);
		Assert.Contains("data-submitted-value=\"accepted-value\"", body, StringComparison.Ordinal);
		Assert.Contains("data-binding-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains("data-initialization-count=\"1\"", body, StringComparison.Ordinal);
		Assert.Contains("data-callback-count=\"1\"", body, StringComparison.Ordinal);
		Assert.NotEqual(normalRequestId, ExtractAttribute(body, "data-issue-85-result", "data-request-id"));
		Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("data-stock-shell", body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Missing_antiforgery_token_rejects_before_form_binding_or_callback()
	{
		using var request = CreatePostRequest(cookie: null, token: null);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		var probe = app.Services.GetRequiredService<Issue85ApplicationProbe>();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.DoesNotContain("data-issue-85-result", body, StringComparison.Ordinal);
		Assert.Equal(0, probe.BindingCount);
		Assert.Equal(0, probe.InitializationCount);
		Assert.Equal(0, probe.CallbackCount);
	}

	private static HttpRequestMessage CreatePostRequest(string? cookie, string? token)
	{
		var fields = new Dictionary<string, string>
		{
			["_handler"] = "issue-85-form",
			["Input.Value"] = "accepted-value",
		};
		if (token is not null)
		{
			fields["__RequestVerificationToken"] = token;
		}

		var request = new HttpRequestMessage(HttpMethod.Post, "/issue-85")
		{
			Content = new FormUrlEncodedContent(fields),
		};
		request.Headers.Add("HX-Request", "true");
		if (cookie is not null)
		{
			request.Headers.Add("Cookie", cookie);
		}

		return request;
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

internal sealed class Issue85ApplicationProbe
{
	private int bindingCount;
	private int initializationCount;
	private int callbackCount;

	public int BindingCount => Volatile.Read(ref bindingCount);

	public int InitializationCount => Volatile.Read(ref initializationCount);

	public int CallbackCount => Volatile.Read(ref callbackCount);

	public void RecordBinding() => Interlocked.Increment(ref bindingCount);

	public void RecordInitialization() => Interlocked.Increment(ref initializationCount);

	public void RecordCallback() => Interlocked.Increment(ref callbackCount);
}

internal sealed class Issue85RequestProbe(Issue85ApplicationProbe applicationProbe)
{
	public string Id { get; } = Guid.NewGuid().ToString("D");

	public int BindingCount { get; private set; }

	public int InitializationCount { get; private set; }

	public int CallbackCount { get; private set; }

	public void RecordBinding()
	{
		BindingCount++;
		applicationProbe.RecordBinding();
	}

	public void RecordInitialization()
	{
		InitializationCount++;
		applicationProbe.RecordInitialization();
	}

	public void RecordCallback()
	{
		CallbackCount++;
		applicationProbe.RecordCallback();
	}
}
