using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// A per-host wrapper over Issue260Host, carrying #269's own interactive Server/WebAssembly render-mode
// services and endpoint conventions through Issue260Host's configureRazorComponents/configureEndpoints hooks,
// the same way Issue261PostStartNotFoundParityTests' own Issue261Host wraps it for #261's gate -- without a
// second copy of Issue260Host's own TestServer/data-protection/antiforgery/gate wiring. #270 reuses the same
// wrapper for its own interactive-component-in-a-streaming-page cases.
internal sealed class Issue269Host(Issue260Host host) : IAsyncDisposable
{
	public HttpClient Client => host.Client;

	public IServiceProvider Services => host.Services;

	public IDataProtectionProvider Protection => host.Services.GetRequiredService<IDataProtectionProvider>();

	public static async Task<Issue269Host> CreateAsync(bool htmxor, Action<WebApplication>? configurePipeline = null)
	{
		var host = await Issue260Host.CreateAsync<Issue269App>(
			htmxor,
			configurePipeline,
			configureRazorComponents: razorComponents =>
			{
				razorComponents.AddInteractiveServerComponents();
				razorComponents.AddInteractiveWebAssemblyComponents();
			},
			configureEndpoints: endpoints =>
			{
				endpoints.AddInteractiveServerRenderMode();
				endpoints.AddInteractiveWebAssemblyRenderMode();
			});
		return new Issue269Host(host);
	}

	public async ValueTask DisposeAsync() => await host.DisposeAsync();
}
