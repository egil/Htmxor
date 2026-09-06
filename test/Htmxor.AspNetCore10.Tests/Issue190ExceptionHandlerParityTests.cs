using System.Net;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Htmxor.AspNetCore10;

public sealed class Issue190ExceptionHandlerParityTests
{
	[Fact]
	public async Task Candidate_selected_component_precommit_exception_has_stock_error_handler_parity()
	{
		await using var stock = await Issue190ExceptionHandlerHost.CreateAsync(useCandidate: false);
		await using var candidate = await Issue190ExceptionHandlerHost.CreateAsync(useCandidate: true);

		using var stockResponse = await stock.Client.GetAsync(Issue190ExceptionHandlerHost.OriginPath);
		using var candidateResponse = await candidate.Client.GetAsync(Issue190ExceptionHandlerHost.OriginPath);
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidateSnapshot = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.InternalServerError, stockSnapshot.StatusCode);
		Assert.Equal(
			$"exception-handler:{Issue190ExceptionHandlerHost.OriginPath}:InvalidOperationException",
			stockSnapshot.Body);
		Assert.Equal("pre-commit", stockSnapshot.Headers["X-Issue-190-Exception-Handler"]);
		Assert.Equal(["origin", $"handler:{Issue190ExceptionHandlerHost.OriginPath}:InvalidOperationException"], stock.Lifecycle.Events);
		Assert.Equal(stockSnapshot.StatusCode, candidateSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Headers, candidateSnapshot.Headers);
		Assert.Equal(stockSnapshot.Body, candidateSnapshot.Body);
		Assert.Equal(stock.Lifecycle.Events, candidate.Lifecycle.Events);
	}
}

internal sealed class Issue190ExceptionHandlerHost(Issue187ParityHost host, Issue190ExceptionHandlerJournal lifecycle) : IAsyncDisposable
{
	public const string OriginPath = "/issue-190/exception-origin";

	public HttpClient Client => host.Client;

	public Issue190ExceptionHandlerJournal Lifecycle { get; } = lifecycle;

	public static async Task<Issue190ExceptionHandlerHost> CreateAsync(bool useCandidate)
	{
		var options = new Issue187ParityHostOptions
		{
			EnvironmentName = Environments.Production,
			ConfigureServices = services => services.AddSingleton<Issue190ExceptionHandlerJournal>(),
			BeforeSession = app =>
			{
				app.UseExceptionHandler(errorApp => errorApp.Run(HandleExceptionAsync));
			},
		};
		var host = await Issue187ParityHost.CreateAsync(
			useHtmxor: useCandidate,
			configureHtmxorServices: useCandidate ? HtmxorEndpointCandidateServices.Add : null,
			options: options);
		return new(host, host.App.Services.GetRequiredService<Issue190ExceptionHandlerJournal>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();

	private static async Task HandleExceptionAsync(HttpContext context)
	{
		var feature = context.Features.Get<IExceptionHandlerPathFeature>()
			?? throw new InvalidOperationException("The exception handler feature was not available.");
		var errorType = feature.Error.GetType().Name;
		context.RequestServices.GetRequiredService<Issue190ExceptionHandlerJournal>()
			.Record($"handler:{feature.Path}:{errorType}");
		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		context.Response.ContentType = "text/plain";
		context.Response.Headers["X-Issue-190-Exception-Handler"] = "pre-commit";
		await context.Response.WriteAsync($"exception-handler:{feature.Path}:{errorType}");
	}
}

internal sealed class Issue190ExceptionHandlerJournal
{
	private readonly List<string> events = [];

	public IReadOnlyList<string> Events => events;

	public void Record(string value) => events.Add(value);
}
