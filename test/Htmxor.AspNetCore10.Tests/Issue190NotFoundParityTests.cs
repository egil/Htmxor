using System.Net;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue190NotFoundParityTests
{
	[Fact]
	public async Task Candidate_selected_component_router_not_found_has_stock_http_flush_and_lifecycle_parity()
	{
		await using var stock = await Issue190NotFoundHost.CreateAsync(useCandidate: false);
		await using var candidate = await Issue190NotFoundHost.CreateAsync(useCandidate: true);

		using var stockResponse = await stock.Client.GetAsync(Issue190NotFoundHost.Path);
		using var candidateResponse = await candidate.Client.GetAsync(Issue190NotFoundHost.Path);
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidateSnapshot = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.NotFound, stockSnapshot.StatusCode);
		Assert.DoesNotContain(stockSnapshot.Headers, header =>
			header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
		Assert.Empty(stockSnapshot.Body);
		Assert.Equal(stockSnapshot.StatusCode, candidateSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Headers, candidateSnapshot.Headers);
		Assert.Equal(stockSnapshot.Body, candidateSnapshot.Body);
		Assert.Equal(["origin:initialized", "response:completed:False"], stock.Journal.Events);
		Assert.Equal(stock.Journal.Events, candidate.Journal.Events);
	}
}

internal sealed class Issue190NotFoundHost(Issue187ParityHost host, Issue190NotFoundJournal journal) : IAsyncDisposable
{
	public const string Path = "/issue-190/router-not-found-origin";

	public HttpClient Client => host.Client;

	public Issue190NotFoundJournal Journal { get; } = journal;

	public static async Task<Issue190NotFoundHost> CreateAsync(bool useCandidate)
	{
		var options = new Issue187ParityHostOptions
		{
			ConfigureServices = services => services.AddSingleton<Issue190NotFoundJournal>(),
			AfterAntiforgery = app => app.Use(ObserveResponseAsync),
		};
		var host = await Issue187ParityHost.CreateAsync<Issue190NotFoundApp>(
			useHtmxor: useCandidate,
			configureHtmxorServices: useCandidate ? HtmxorEndpointCandidateServices.Add : null,
			options: options);
		return new(host, host.App.Services.GetRequiredService<Issue190NotFoundJournal>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();

	private static async Task ObserveResponseAsync(HttpContext context, RequestDelegate next)
	{
		var journal = context.RequestServices.GetRequiredService<Issue190NotFoundJournal>();
		context.Response.OnStarting(() =>
		{
			journal.Record($"response:starting:{context.Response.StatusCode}:{context.Response.ContentType}");
			return Task.CompletedTask;
		});
		await next(context);
		journal.Record($"response:completed:{context.Response.HasStarted}");
	}
}

internal sealed class Issue190NotFoundJournal
{
	private readonly List<string> events = [];

	public IReadOnlyList<string> Events => events;

	public void Record(string value) => events.Add(value);
}
