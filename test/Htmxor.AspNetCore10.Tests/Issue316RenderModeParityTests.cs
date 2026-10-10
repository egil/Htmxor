using System.Net;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

/// <summary>
/// Design decision 6103041533's <c>ConfiguredRenderModesMetadata</c> stock-metadata item (#316): a
/// chained <c>AddInteractiveServerRenderMode()</c> reaches a generated <c>HtmxRoute</c> endpoint
/// exactly as it reaches a stock <c>@page</c> endpoint. Observed through the approved inactive
/// candidate adapter (#188), whose form services read <c>ConfiguredRenderModesMetadata</c> to reject
/// an unconfigured interactive-server component before rendering it. Modeled on
/// <c>Issue191PersistedStateParityTests.Candidate_rejects_unconfigured_interactive_server_mode_like_stock</c>,
/// with an <c>HtmxRoute</c> candidate added alongside the stock one.
/// </summary>
public sealed class Issue316RenderModeParityTests
{
	[Fact]
	public async Task Candidate_rejects_unconfigured_interactive_server_HtmxRoute_component_like_stock()
	{
		var options = new Issue187ParityHostOptions
		{
			ConfigureRazorComponents = builder => builder.AddInteractiveServerComponents(),
		};
		await using var stock = await Issue187ParityHost.CreateAsync<Issue316RenderModeApp>(
			useHtmxor: false,
			options: options);
		await using var candidate = await Issue187ParityHost.CreateAsync<Issue316RenderModeApp>(
			useHtmxor: true,
			HtmxorEndpointCandidateServices.Add,
			options);

		using var stockResponse = await stock.Client.GetAsync("/issue-316-rendermode/stock");
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-316-rendermode/htmx/1");
		request.Headers.TryAddWithoutValidation("HX-Request", "true");
		request.Headers.TryAddWithoutValidation("HX-Request-Type", "partial");
		using var htmxResponse = await candidate.Client.SendAsync(request);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxBody = await htmxResponse.Content.ReadAsStringAsync();

		Assert.True(
			stockResponse.StatusCode == HttpStatusCode.InternalServerError &&
				stockBody.Contains("AddInteractiveServerRenderMode", StringComparison.Ordinal),
			$"Stock twin expected 500 naming AddInteractiveServerRenderMode, got {(int)stockResponse.StatusCode}. Body: {stockBody}");
		Assert.True(
			htmxResponse.StatusCode == stockResponse.StatusCode &&
				htmxBody.Contains("AddInteractiveServerRenderMode", StringComparison.Ordinal),
			$"HtmxRoute candidate expected to match its stock twin's rejection, got {(int)htmxResponse.StatusCode}. Body: {htmxBody}");
	}
}
