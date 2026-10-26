using System.Collections.Concurrent;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10.Issue375.Browser;

public sealed class Issue211Probe(bool tokens)
{
	public bool Tokens { get; } = tokens;
	public ConcurrentQueue<Issue211Callback> Callbacks { get; } = new();
	public ConcurrentQueue<string> Requests { get; } = new();
	public ConcurrentQueue<Issue211BrowserOrigin> Origins { get; } = new();
}

public sealed record Issue211Callback(string Method, string Instance);
public sealed record Issue211BrowserOrigin(string Origin, string FetchSite);

internal static class Issue211Host
{
	public static WebApplication Create(Issue211Probe probe, string? trustedOrigin = null, bool wildcard = false, bool allowCredentials = false)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue211Host).Assembly.GetName().Name,
			ContentRootPath = AppContext.BaseDirectory,
		});
		builder.WebHost.UseStaticWebAssets().UseKestrel().UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		builder.Services.AddRazorComponents().AddHtmxor();
		builder.Services.AddSingleton(probe);
		ConfigureCors(builder.Services, trustedOrigin, wildcard, allowCredentials);
		var app = builder.Build();
		app.UseRouting();
		app.Use(async (context, next) =>
		{
			probe.Requests.Enqueue(context.Request.Method);
			if (HttpMethods.IsPost(context.Request.Method))
			{
				probe.Origins.Enqueue(new(context.Request.Headers.Origin.ToString(), context.Request.Headers["Sec-Fetch-Site"].ToString()));
			}
			await next(context);
		});
		app.UseCors();
		if (probe.Tokens)
		{
			app.UseAntiforgery();
			app.Use(ValidateDeleteAsync);
		}
		app.MapStaticAssets();
		var components = app.MapRazorComponents<Issue56App>();
		components.Add(PreserveOwnHttpMethodMetadata);
		components.AddHtmxorEndpoints().RequireCors("browser");
		return app;
	}

	private static void PreserveOwnHttpMethodMetadata(EndpointBuilder builder)
	{
		// CORS endpoint construction mutates the last method metadata. Copy the framework's
		// shared instance so this host cannot alter another in-process host (as in #246).
		var current = builder.Metadata.OfType<IHttpMethodMetadata>().LastOrDefault();
		if (current is not null)
		{
			builder.Metadata.Add(new HttpMethodMetadata(current.HttpMethods, current.AcceptCorsPreflight));
		}
	}

	private static void ConfigureCors(IServiceCollection services, string? trustedOrigin, bool wildcard, bool allowCredentials)
	{
		services.AddCors(options => options.AddPolicy("browser", policy =>
		{
			policy.AllowAnyHeader().AllowAnyMethod();
			if (wildcard)
			{
				policy.AllowAnyOrigin();
			}
			else if (trustedOrigin is not null)
			{
				policy.WithOrigins(trustedOrigin);
				if (allowCredentials)
				{
					policy.AllowCredentials();
				}
			}
		}));
	}

	private static async Task ValidateDeleteAsync(HttpContext context, RequestDelegate next)
	{
		// .NET 11 token middleware omits DELETE; this application explicitly requires its stock token validator.
		if (HttpMethods.IsDelete(context.Request.Method))
		{
			var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
			if (!await antiforgery.IsRequestValidAsync(context))
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return;
			}
		}
		await next(context);
	}
}
