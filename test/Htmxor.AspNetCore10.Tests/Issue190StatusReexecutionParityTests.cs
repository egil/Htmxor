using System.Net;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue190StatusReexecutionParityTests
{
	[Fact]
	public async Task Candidate_selected_component_status_code_reexecution_has_stock_http_and_lifecycle_parity()
	{
		await using var stock = await Issue190StatusReexecutionHost.CreateAsync(useCandidate: false);
		await using var candidate = await Issue190StatusReexecutionHost.CreateAsync(useCandidate: true);

		using var stockResponse = await stock.Client.GetAsync(Issue190StatusReexecutionHost.OriginPath);
		using var candidateResponse = await candidate.Client.GetAsync(Issue190StatusReexecutionHost.OriginPath);
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidateSnapshot = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.NotFound, stockSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.StatusCode, candidateSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Body, candidateSnapshot.Body);
		Assert.Contains("data-issue-190-status-page", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains($"data-original-path=\"{Issue190StatusReexecutionHost.OriginPath}\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains($"data-request-path=\"{Issue190StatusReexecutionHost.StatusPath}\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Contains("data-original-status=\"404\"", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.DoesNotContain("data-issue-190-status-origin", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Equal([$"status:{Issue190StatusReexecutionHost.OriginPath}:{Issue190StatusReexecutionHost.StatusPath}:404"], stock.Lifecycle.Events);
		Assert.Equal(stock.Lifecycle.Events, candidate.Lifecycle.Events);
		Assert.Equal(stockSnapshot.Headers, candidateSnapshot.Headers);
	}
}

internal sealed class Issue190StatusReexecutionHost(Issue187ParityHost host, Issue190StatusReexecutionJournal lifecycle) : IAsyncDisposable
{
	public const string OriginPath = "/issue-190/status-origin";
	public const string StatusPath = "/issue-190/status";

	public HttpClient Client => host.Client;

	public Issue190StatusReexecutionJournal Lifecycle { get; } = lifecycle;

	public static async Task<Issue190StatusReexecutionHost> CreateAsync(bool useCandidate)
	{
		var options = new Issue187ParityHostOptions
		{
			ConfigureServices = services => services.AddSingleton<Issue190StatusReexecutionJournal>(),
			BeforeSession = app =>
			{
				app.UseStatusCodePagesWithReExecute(StatusPath);
				app.Use(StatusOriginAsync);
				app.UseRouting();
			},
		};
		var host = await Issue187ParityHost.CreateAsync(
			useHtmxor: useCandidate,
			configureHtmxorServices: useCandidate ? HtmxorEndpointCandidateServices.Add : null,
			options: options);
		return new(host, host.App.Services.GetRequiredService<Issue190StatusReexecutionJournal>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();

	private static Task StatusOriginAsync(HttpContext context, RequestDelegate next)
	{
		if (context.Request.Path == OriginPath)
		{
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			return Task.CompletedTask;
		}

		return next(context);
	}
}

internal sealed class Issue190StatusReexecutionJournal
{
	private readonly List<string> events = [];

	public IReadOnlyList<string> Events => events;

	public void Record(string value) => events.Add(value);
}
