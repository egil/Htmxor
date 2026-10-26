using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Htmxor.AspNetCore10.Issue375.Browser;

internal static class NamedSelectionHost
{
	public static WebApplication Create(Issue50CacheProbe? cacheProbe = null, Issue171LifecycleProbe? issue171Probe = null)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(NamedSelectionHost).Assembly.GetName().Name,
			ContentRootPath = AppContext.BaseDirectory,
			EnvironmentName = Environments.Production,
		});
		builder.WebHost.UseStaticWebAssets().UseKestrel().UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddOutputCache();
		builder.Services.AddSingleton(cacheProbe ?? new Issue50CacheProbe());
		builder.Services.AddSingleton(issue171Probe ?? new Issue171LifecycleProbe());
		builder.Services.AddScoped<Issue171RequestWork>();
		var app = builder.Build();
		Assert.True(app.Environment.IsProduction());
		app.UseAntiforgery();
		app.UseOutputCache();
		app.MapStaticAssets();
		app.MapRazorComponents<Issue56App>().AddHtmxorEndpoints();
		return app;
	}
}
