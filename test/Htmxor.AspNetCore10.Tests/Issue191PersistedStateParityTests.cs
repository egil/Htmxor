using System.Net;
using System.Text.RegularExpressions;
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
		Assert.Contains("<!--Blazor-WebAssembly-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_selected_interactive_webassembly_component_preserves_persisted_state_representation()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.WebAssemblyPath);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.WebAssemblyPath);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.Contains("<!--Blazor-WebAssembly-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Server-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_selected_interactive_auto_component_preserves_persisted_state_representation()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.AutoPath);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.AutoPath);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.Contains("<!--Blazor-WebAssembly:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Server-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-WebAssembly-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_persisted_state_remains_request_scoped_during_sequential_and_concurrent_requests()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		var paths = new[] { Issue191HostPair.Path, Issue191HostPair.WebAssemblyPath, Issue191HostPair.AutoPath };
		foreach (var path in paths)
		{
			using var response = await pair.Candidate.Client.GetAsync(path);
			var snapshot = await Issue187ResponseSnapshot.CreateAsync(response);
			Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
			Assert.Contains("<!--Blazor-Server-Component-State:", snapshot.Body, StringComparison.Ordinal);
			Assert.Contains("<!--Blazor-WebAssembly-Component-State:", snapshot.Body, StringComparison.Ordinal);
		}

		var responses = await Task.WhenAll(paths.SelectMany(path => Enumerable.Repeat(path, 4))
			.Select(path => pair.Candidate.Client.GetAsync(path)));
		foreach (var response in responses)
		{
			using (response)
			{
				var snapshot = await Issue187ResponseSnapshot.CreateAsync(response);
				Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
				Assert.Contains("<!--Blazor-Server-Component-State:", snapshot.Body, StringComparison.Ordinal);
				Assert.Contains("<!--Blazor-WebAssembly-Component-State:", snapshot.Body, StringComparison.Ordinal);
			}
		}
	}

	private static string NormalizeDynamicState(string body)
	{
		var normalized = Regex.Replace(body, "\\\"prerenderId\\\":\\\"[^\\\"]+\\\"", "\\\"prerenderId\\\":\\\"<dynamic>\\\"");
		normalized = Regex.Replace(normalized, "\\\"descriptor\\\":\\\"[^\\\"]+\\\"", "\\\"descriptor\\\":\\\"<dynamic>\\\"");
		normalized = Regex.Replace(normalized, "(?<=<!--Blazor-Server-Component-State:)[^-]+(?=-->)", "<dynamic>");
		return Regex.Replace(normalized, "(?<=<!--Blazor-WebAssembly-Component-State:)[^-]+(?=-->)", "<dynamic>");
	}

	private static IReadOnlyDictionary<string, string> NormalizeDynamicHeaders(IReadOnlyDictionary<string, string> headers)
		=> headers.ToDictionary(
			pair => pair.Key,
			pair => pair.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
				? Regex.Replace(pair.Value, "(?<==)[^;]+", "<dynamic>")
				: pair.Value,
			StringComparer.OrdinalIgnoreCase);
}

internal sealed class Issue191HostPair(Issue187ParityHost stock, Issue187ParityHost candidate) : IAsyncDisposable
{
	public const string Path = "/issue-191/state";
	public const string WebAssemblyPath = "/issue-191/webassembly-state";
	public const string AutoPath = "/issue-191/auto-state";

	public Issue187ParityHost Stock { get; } = stock;

	public Issue187ParityHost Candidate { get; } = candidate;

	public static async Task<Issue191HostPair> CreateAsync()
	{
		var protection = new EphemeralDataProtectionProvider();
		var options = new Issue187ParityHostOptions
		{
			ConfigureRazorComponents = builder =>
			{
				builder.AddInteractiveServerComponents();
				builder.AddInteractiveWebAssemblyComponents();
			},
			ConfigureEndpoints = endpoints =>
			{
				endpoints.AddInteractiveServerRenderMode();
				endpoints.AddInteractiveWebAssemblyRenderMode();
			},
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
