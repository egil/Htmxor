using System.Net;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue191PersistedStateParityTests
{
	[Fact]
	public async Task Candidate_selected_interactive_server_component_preserves_persisted_state_representation()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.Path);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.Path);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.Contains("data-issue-191-state=\"rendered\"", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Server-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(stock.Headers, candidate.Headers);
		Assert.Equal(stock.Body, candidate.Body);
	}
}

internal sealed class Issue191HostPair(Issue187ParityHost stock, Issue187ParityHost candidate) : IAsyncDisposable
{
	public const string Path = "/issue-191/state";

	public Issue187ParityHost Stock { get; } = stock;

	public Issue187ParityHost Candidate { get; } = candidate;

	public static async Task<Issue191HostPair> CreateAsync()
	{
		var protection = new EphemeralDataProtectionProvider();
		var options = new Issue187ParityHostOptions
		{
			ConfigureRazorComponents = builder => builder.AddInteractiveServerComponents(),
			ConfigureEndpoints = endpoints => endpoints.AddInteractiveServerRenderMode(),
			ConfigureServices = services => services.AddSingleton<IDataProtectionProvider>(protection),
		};
		var stock = await Issue187ParityHost.CreateAsync<Issue191App>(false, options: options);
		var candidate = await Issue187ParityHost.CreateAsync<Issue191App>(
			true,
			HtmxorEndpointCandidateServices.Add,
			options);
		return new(stock, candidate);
	}

	public async ValueTask DisposeAsync()
	{
		await Candidate.DisposeAsync();
		await Stock.DisposeAsync();
	}
}
