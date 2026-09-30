using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

// A minimal in-process TestServer host for #260's inherited-streaming and non-streaming-quiescence parity
// cases, compiled for both net10.0 and net11.0 (see the csproj's net11.0 Compile/RazorComponent Include). It
// deliberately does not reuse Issue187ParityHost: that host's own file compiles only for net10.0 today, and
// widening its much larger surface (session, authentication, forms, TempData) to net11.0 would pull in
// behavior this issue's cases never touch. This host carries only what #260 needs, and reuses
// Issue264StreamingBodyReader for incremental reading rather than adding a second copy of that logic.
internal sealed class Issue260Host(WebApplication app) : IAsyncDisposable
{
	public HttpClient Client { get; } = app.GetTestClient();

	public IServiceProvider Services => app.Services;

	public static async Task<Issue260Host> CreateAsync<TRootComponent>(
		bool htmxor, Action<WebApplication>? configurePipeline = null)
		where TRootComponent : IComponent
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(TRootComponent).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddSingleton<Issue260Gate>();
		var razorComponents = builder.Services.AddRazorComponents();
		if (htmxor)
		{
			razorComponents.AddHtmxor();
		}

		var app = builder.Build();
		configurePipeline?.Invoke(app);
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<TRootComponent>();
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new Issue260Host(app);
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await app.DisposeAsync();
	}
}
