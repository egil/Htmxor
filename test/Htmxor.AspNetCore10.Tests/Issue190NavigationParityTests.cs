using System.Net;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue190NavigationParityTests
{
	[Fact]
	public async Task Candidate_selected_component_navigation_before_output_has_stock_redirect_parity()
	{
		await using var stock = await Issue190NavigationHost.CreateAsync(useCandidate: false);
		await using var candidate = await Issue190NavigationHost.CreateAsync(useCandidate: true);

		using var stockResponse = await stock.Client.GetAsync(Issue190NavigationHost.Path);
		using var candidateResponse = await candidate.Client.GetAsync(Issue190NavigationHost.Path);
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidateSnapshot = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.Found, stockSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.StatusCode, candidateSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Headers, candidateSnapshot.Headers);
		Assert.Equal(stockSnapshot.Body, candidateSnapshot.Body);
		Assert.Equal(["initialized"], stock.Lifecycle.Events);
		Assert.Equal(stock.Lifecycle.Events, candidate.Lifecycle.Events);
		Assert.DoesNotContain("data-issue-190-output", stockSnapshot.Body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Candidate_selected_component_external_navigation_before_output_has_stock_redirect_parity()
	{
		await using var stock = await Issue190NavigationHost.CreateAsync(useCandidate: false);
		await using var candidate = await Issue190NavigationHost.CreateAsync(useCandidate: true);

		using var stockResponse = await SendEnhancedNavigationRequestAsync(stock.Client);
		using var candidateResponse = await SendEnhancedNavigationRequestAsync(candidate.Client);
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidateSnapshot = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.Found, stockSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.StatusCode, candidateSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Headers["blazor-enhanced-nav"], candidateSnapshot.Headers["blazor-enhanced-nav"]);
		Assert.Equal(stockSnapshot.Body, candidateSnapshot.Body);
		Assert.Equal(["external-initialized"], stock.Lifecycle.Events);
		Assert.Equal(stock.Lifecycle.Events, candidate.Lifecycle.Events);
		Assert.DoesNotContain("data-issue-190-external-output", stockSnapshot.Body, StringComparison.Ordinal);

		await AssertRedirectDestinationAsync(stock.Client, stockSnapshot.Headers["Location"]);
		await AssertRedirectDestinationAsync(candidate.Client, candidateSnapshot.Headers["Location"]);
		Assert.Equal(stockSnapshot.Headers, candidateSnapshot.Headers);
	}

	private static async Task<HttpResponseMessage> SendEnhancedNavigationRequestAsync(HttpClient client)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, Issue190ExternalNavigationContract.Path);
		request.Headers.Add("blazor-enhanced-nav", "allow");
		return await client.SendAsync(request);
	}

	private static async Task AssertRedirectDestinationAsync(HttpClient client, string location)
	{
		if (location == Issue190ExternalNavigationContract.Destination)
		{
			return;
		}

		Assert.StartsWith("_framework/opaque-redirect?url=", location, StringComparison.Ordinal);
		using var redirectResponse = await client.GetAsync(new Uri(client.BaseAddress!, location));
		Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
		Assert.Equal(Issue190ExternalNavigationContract.Destination, redirectResponse.Headers.Location?.OriginalString);
	}
}

internal static class Issue190ExternalNavigationContract
{
	public const string Destination = "https://example.invalid/issue-190/external-destination?source=navigation";
	public const string Path = "/issue-190/navigate-external";
}

internal sealed class Issue190NavigationHost(Issue187ParityHost host, Issue190LifecycleJournal lifecycle) : IAsyncDisposable
{
	public const string Path = "/issue-190/navigate";

	public HttpClient Client => host.Client;

	public Issue190LifecycleJournal Lifecycle { get; } = lifecycle;

	public static async Task<Issue190NavigationHost> CreateAsync(bool useCandidate)
	{
		var options = new Issue187ParityHostOptions
		{
			ConfigureServices = services => services.AddSingleton<Issue190LifecycleJournal>(),
			BeforeAntiforgery = app => app.Use(CaptureEndpointFailureAsync),
		};
		var host = await Issue187ParityHost.CreateAsync(
			useHtmxor: useCandidate,
			configureHtmxorServices: useCandidate ? HtmxorEndpointCandidateServices.Add : null,
			options: options);
		return new(host, host.App.Services.GetRequiredService<Issue190LifecycleJournal>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();

	private static async Task CaptureEndpointFailureAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception exception) when (!context.Response.HasStarted)
		{
			context.Response.Clear();
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			await context.Response.WriteAsync(exception.GetType().FullName!);
		}
	}
}

internal sealed class Issue190LifecycleJournal
{
	private readonly List<string> events = [];

	public IReadOnlyList<string> Events => events;

	public void Record(string value) => events.Add(value);
}
