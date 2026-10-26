using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Htmxor.AspNetCore10;

public sealed class Issue291Host : IAsyncDisposable
{
	private readonly WebApplication app;
	private readonly HttpClient client;
	public Issue291Probe Probe { get; } = new();
	public Issue291Logs Logs { get; } = new();

	private Issue291Host(bool htmxor)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Logging.ClearProviders();
		builder.Logging.AddProvider(Logs);
		builder.Services.AddSingleton(Probe);
		var components = builder.Services.AddRazorComponents();
		if (htmxor)
			components.AddHtmxor();
		app = builder.Build();
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue291App>();
		if (htmxor)
			endpoints.AddHtmxorEndpoints();
		client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
	}

	public static async Task<Issue291Host> StartAsync(bool htmxor = true)
	{
		var host = new Issue291Host(htmxor);
		await host.app.StartAsync();
		host.client.BaseAddress = new Uri(host.app.Services.GetRequiredService<IServer>()
			.Features.Get<IServerAddressesFeature>()!.Addresses.Single());
		return host;
	}

	public async Task<Issue291Response> GetAsync(string query, bool htmx, ITestOutputHelper output)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-291/status?" + query);
		if (htmx)
			request.Headers.Add("HX-Request", "true");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		// Host shutdown drains request processing before the absence of exception logs is asserted.
		await app.StopAsync();
		output.WriteLine($"GET {request.RequestUri}, htmx={htmx}: {(int)response.StatusCode}, body={body}, Location={response.Headers.Location}");
		foreach (var entry in Logs.Entries)
			output.WriteLine(entry);
		return new(response.StatusCode, body, response.Headers.Location?.OriginalString,
			response.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase));
	}

	public async ValueTask DisposeAsync()
	{
		client.Dispose();
		await app.DisposeAsync();
	}
}

public sealed record Issue291Response(HttpStatusCode Status, string Body, string? Location, Dictionary<string, string> Headers);

public sealed class Issue291Probe
{
	public Exception? Rejection { get; set; }
	public Exception? EmptyBodyGuard { get; set; }
	public Issue291State? Before { get; set; }
	public Issue291State? After { get; set; }
}

public sealed record Issue291State(int Status, KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] Headers,
	KeyValuePair<object, object?>[] Items, Stream Body, bool HasStarted)
{
	public static Issue291State Capture(HttpContext context) => new(context.Response.StatusCode,
		context.Response.Headers.ToArray(), context.Items.ToArray(), context.Response.Body, context.Response.HasStarted);
}

public sealed class Issue291Logs : ILoggerProvider
{
	public ConcurrentQueue<string> Entries { get; } = new();
	public ConcurrentQueue<Exception> Exceptions { get; } = new();
	public ConcurrentQueue<string> Errors { get; } = new();
	public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);
	public void Dispose() { }

	private sealed class CaptureLogger(Issue291Logs owner, string category) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			var entry = $"{category}[{eventId.Id}] {logLevel}: {formatter(state, exception)} {exception}";
			owner.Entries.Enqueue(entry);
			if (exception is not null)
				owner.Exceptions.Enqueue(exception);
			if (logLevel >= LogLevel.Error)
				owner.Errors.Enqueue(entry);
		}
	}
}
