using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Issue286;

public sealed class Issue286CombinedConstraintTests : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddRazorComponents().AddHtmxor();
		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue286App>().AddHtmxorEndpoints();
		await app.StartAsync();

		var addressesFeature = app.Services
			.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
			.Features
			.Get<IServerAddressesFeature>()!;
		var boundAddress = addressesFeature.Addresses.Single();
		client = new HttpClient { BaseAddress = new Uri(boundAddress) };
	}

	[Fact]
	public async Task Every_stock_constraint_and_a_multiconstraint_segment_bind_the_same_as_their_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-362-286/constraints/htmx/" + MatchingSegments, ConstraintsExpected);
		await AssertStockBodyContainsAsync("/issue-362-286/constraints/stock/" + MatchingSegments, ConstraintsExpected);
	}

	[Fact]
	public async Task Non_matching_constraint_value_rejects_with_the_same_status_as_its_stock_twin()
	{
		var nonMatchingSegments = "abc" + MatchingSegments["42".Length..];

		using var htmxRequest = new HttpRequestMessage(HttpMethod.Get, "/issue-362-286/constraints/htmx/" + nonMatchingSegments);
		htmxRequest.Headers.Add("HX-Request", "true");
		htmxRequest.Headers.Add("HX-Request-Type", "partial");
		using var htmxResponse = await client.SendAsync(htmxRequest);
		using var stockResponse = await client.GetAsync("/issue-362-286/constraints/stock/" + nonMatchingSegments);

		Assert.Multiple(
			() => Assert.True(
			htmxResponse.StatusCode != HttpStatusCode.OK,
			$"Expected a non-matching IntVal segment to reject the HtmxRoute twin, got {(int)htmxResponse.StatusCode}."),
			() => Assert.Equal(stockResponse.StatusCode, htmxResponse.StatusCode));
	}

	private const string MatchingSegments =
		"42/true/2024-01-01/1.5/2.5/3.5/11111111-1111-1111-1111-111111111111/123456789012/" +
		"abcd/ab/abc/abcd/5/5/5/abc/abc/x/x/a.txt/5";
	private const string ConstraintsExpected =
		"42|True|2024-01-01|1.5|2.5|3.5|11111111-1111-1111-1111-111111111111|123456789012|" +
		"abcd|ab|abc|abcd|5|5|5|abc|abc|x|x|a.txt|5";

	private async Task AssertHtmxBodyContainsAsync(string path, string expected)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected GET {path} to answer 200 OK, received {(int)response.StatusCode} " +
			$"{response.StatusCode}. Body: {body}"),
			() => Assert.Contains(expected, body, StringComparison.Ordinal));
	}

	private async Task AssertStockBodyContainsAsync(string path, string expected)
	{
		using var response = await client.GetAsync(path);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected GET {path} to answer 200 OK, received {(int)response.StatusCode} " +
			$"{response.StatusCode}. Body: {body}"),
			() => Assert.Contains(expected, body, StringComparison.Ordinal));
	}

	public async Task DisposeAsync()
	{
		client.Dispose();
		await app.DisposeAsync();
	}
}
