using System.Net;

namespace Htmxor.Samples.Tests;

/// <summary>
/// Protects the MinimalHtmxorApp counter migration (#282): a direct partial GET returns the
/// counter fragment rather than the page content that previously depended on conditional rendering.
/// </summary>
public sealed class MinimalHtmxorAppPartialResponseTests
{
	[Fact]
	public async Task Partial_get_counter_returns_only_the_counter_fragment()
	{
		await using var host = await SampleKestrelHost.StartAsync("samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj");
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(host.Address, "counter"));
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		PartialResponseAssertions.IsSingleFragment(body, "div", "role=\"status\"", "<h1>Counter</h1>", id: "counter");
		Assert.Contains("hx-put=\"/counter\"", body, StringComparison.Ordinal);
		Assert.DoesNotContain("This is a htmx enabled counter.", body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Partial_get_weather_returns_the_loading_fragment()
	{
		await using var host = await SampleKestrelHost.StartAsync("samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj");
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(host.Address, "weather"));
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		PartialResponseAssertions.IsSingleFragment(body, "div", "Loading weather data...", "<h1>Weather</h1>");
	}
}
