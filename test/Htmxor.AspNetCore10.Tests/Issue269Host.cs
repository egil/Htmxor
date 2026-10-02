using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

// A minimal in-process TestServer host for #269's interactive-component-in-a-streaming-page cases, compiled for
// both net10.0 and net11.0 (see the csproj's net11.0 Compile/RazorComponent Include). Scoped only to what #269
// (and #270's reuse of the same interactive-boundary fixture) needs -- interactive Server/WebAssembly render
// modes with persisted component state inside a [StreamRendering] page -- following the same per-issue scoped
// host convention as Issue260Host, Issue214Host and Issue187ParityHost, rather than widening one of those
// hosts' own already-documented surface. Reuses Issue260Gate for named waypoints instead of adding a fifth
// gate type, and Issue264StreamingBodyReader for incremental reading. `configurePipeline` lets a case add its
// own re-execution middleware (see Issue260ReexecutionParityTests' own ConfigurePipeline) ahead of
// MapRazorComponents without a second copy of this host's own service wiring.
internal sealed class Issue269Host(WebApplication app) : IAsyncDisposable
{
	public HttpClient Client { get; } = app.GetTestClient();

	public IServiceProvider Services => app.Services;

	public IDataProtectionProvider Protection => app.Services.GetRequiredService<IDataProtectionProvider>();

	public static async Task<Issue269Host> CreateAsync(bool htmxor, Action<WebApplication>? configurePipeline = null)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue269App).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddSingleton<Issue260Gate>();
		var razorComponents = builder.Services.AddRazorComponents();
		razorComponents.AddInteractiveServerComponents();
		razorComponents.AddInteractiveWebAssemblyComponents();
		if (htmxor)
		{
			razorComponents.AddHtmxor();
		}

		var app = builder.Build();
		configurePipeline?.Invoke(app);
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue269App>()
			.AddInteractiveServerRenderMode()
			.AddInteractiveWebAssemblyRenderMode();
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new Issue269Host(app);
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await app.DisposeAsync();
	}
}
