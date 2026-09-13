#if NET11_0_OR_GREATER
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal sealed class Issue214Host(WebApplication app, string directory) : IAsyncDisposable
{
	public HttpClient Client { get; } = app.GetTestClient();
	public IDataProtectionProvider Protection => app.Services.GetRequiredService<IDataProtectionProvider>();
	public Issue214OverlapGate Overlap => app.Services.GetRequiredService<Issue214OverlapGate>();

	public static async Task<Issue214Host> CreateAsync(bool htmxor, string environment = "issue214")
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue214App).Assembly.GetName().Name,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddSingleton<Issue214OverlapGate>();
		builder.Services.AddCascadingAuthenticationState();
		var components = builder.Services.AddRazorComponents();
		components.AddInteractiveServerComponents();
		components.AddInteractiveWebAssemblyComponents();
		if (htmxor)
		{
			components.AddHtmxor();
		}

		var directory = Path.Combine(Path.GetTempPath(), $"htmxor-issue-214-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, $"{builder.Environment.ApplicationName}.modules.json"), "[\"issue-214\"]");
		builder.Environment.WebRootFileProvider = new PhysicalFileProvider(directory);
		var app = builder.Build();
		app.UseExceptionHandler("/issue-214/state");
		app.UseStatusCodePagesWithReExecute("/issue-214/state");
		app.Use(HandleRequestAsync);
		app.UseAntiforgery();
		var endpoints = app.MapRazorComponents<Issue214App>()
			.AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode()
			.WithBrowserOptions(options =>
			{
				options.StaticServer.PreserveDom = true;
				options.InteractiveServer.ReconnectionMaxRetries = 7;
				options.InteractiveWebAssembly.ApplicationCulture = "is-IS";
				options.InteractiveWebAssembly.EnvironmentName = environment;
			});
		endpoints.WithMetadata(new ResourceAssetCollection([
			new ResourceAsset("assets/issue-214.fingerprint.js", [new ResourceAssetProperty("label", "issue-214.js")]),
		]));
		if (htmxor)
		{
			endpoints.AddHtmxorEndpoints();
		}

		await app.StartAsync();
		return new(app, directory);
	}

	private static async Task HandleRequestAsync(HttpContext context, RequestDelegate next)
	{
		if (context.Request.Path == "/issue-214/status")
		{
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			return;
		}

		if (context.Request.Path == "/issue-214/exception")
		{
			throw new InvalidOperationException("issue-214 exception origin");
		}

		context.User = new ClaimsPrincipal(new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, context.Request.Headers["X-Issue-214-User"].ToString())], "issue214"));
		await next(context);
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await app.DisposeAsync();
		Directory.Delete(directory, recursive: true);
	}
}
#endif
