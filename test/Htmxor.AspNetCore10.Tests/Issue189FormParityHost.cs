using System.Net;
using System.Text.RegularExpressions;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

internal sealed class Issue189HostPair(Issue187ParityHost stock, Issue187ParityHost candidate) : IAsyncDisposable
{
	public const string FormPath = "/issue-189/form";
	public const string CookieName = "issue189-antiforgery";

	public Issue187ParityHost Stock { get; } = stock;

	public Issue187ParityHost Candidate { get; } = candidate;

	public static async Task<Issue189HostPair> CreateAsync(
		string environment = "Development",
		bool detailedErrors = false,
		bool invokerValidation = false,
		int collectionLimit = 1024,
		bool configureServerRenderMode = false)
	{
		var protection = new EphemeralDataProtectionProvider();
		var options = new Issue187ParityHostOptions
		{
			EnvironmentName = environment,
			ConfigureRazorComponents = builder => ConfigureRenderModeServices(builder, configureServerRenderMode),
			ConfigureEndpoints = endpoints => ConfigureRenderModeEndpoints(endpoints, configureServerRenderMode),
			ConfigureBuilder = builder => builder.Configuration[WebHostDefaults.DetailedErrorsKey] = detailedErrors.ToString(),
			ConfigureServices = services => Configure(services, protection, collectionLimit),
			BeforeAntiforgery = app => app.Use(CaptureExceptionAsync),
			AfterAntiforgery = app => ConfigureValidationObservation(app, invokerValidation),
		};
		var stock = await Issue187ParityHost.CreateAsync(false, options: options);
		var candidate = await Issue187ParityHost.CreateAsync(true, HtmxorEndpointCandidateServices.Add, options);
		return new(stock, candidate);
	}

	public Issue189Observation Observe(Issue187ParityHost host, string requestId)
		=> host.App.Services.GetRequiredService<Issue189Journal>().For(requestId);

	public async Task<Issue189Tokens> GetTokensAsync()
	{
		using var request = Request(HttpMethod.Get, FormPath, "token-provisioning");
		using var response = await Stock.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var token = Regex.Match(body, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
		Assert.True(token.Success, "The stock form must issue a real antiforgery request token.");
		var cookie = response.Headers.GetValues("Set-Cookie")
			.Single(value => value.StartsWith(CookieName + "=", StringComparison.Ordinal)).Split(';')[0];
		return new(WebUtility.HtmlDecode(token.Groups[1].Value), cookie);
	}

	public static HttpRequestMessage Request(HttpMethod method, string path, string requestId)
	{
		var request = new HttpRequestMessage(method, path);
		request.Headers.Add("X-Issue-189-Request", requestId);
		return request;
	}

	public static HttpRequestMessage Post(
		string path, string requestId, Issue189Tokens tokens, IEnumerable<KeyValuePair<string, string>> values)
	{
		var request = Request(HttpMethod.Post, path, requestId);
		request.Headers.Add("Cookie", tokens.Cookie);
		request.Content = new FormUrlEncodedContent(values.Append(new("__RequestVerificationToken", tokens.Token)));
		return request;
	}

	public async ValueTask DisposeAsync()
	{
		await Candidate.DisposeAsync();
		await Stock.DisposeAsync();
	}

	private static void ConfigureRenderModeServices(IRazorComponentsBuilder builder, bool configureServer)
	{
		if (configureServer)
		{
			builder.AddInteractiveServerComponents();
		}
	}

	private static void ConfigureRenderModeEndpoints(RazorComponentsEndpointConventionBuilder endpoints, bool configureServer)
	{
		if (configureServer)
		{
			endpoints.AddInteractiveServerRenderMode();
		}
	}

	private static void Configure(
		IServiceCollection services, IDataProtectionProvider protection, int collectionLimit)
	{
		services.AddSingleton(protection);
		services.AddAntiforgery(options => options.Cookie.Name = CookieName);
		services.Configure<RazorComponentsServiceOptions>(options =>
		{
			options.MaxFormMappingCollectionSize = collectionLimit;
		});
		services.AddSingleton<Issue189Journal>();
		services.AddSingleton<Issue189OverlapGate>();
		services.AddScoped<Issue189RequestProbe>();
		Decorate<IAntiforgery>(services, (inner, provider) =>
			new Issue189ObservedAntiforgery(inner, provider.GetRequiredService<Issue189Journal>()));
		Decorate<IFormValueMapper>(services, (inner, provider) =>
			new Issue189ObservedMapper(inner, provider.GetRequiredService<Issue189RequestProbe>()));
		Decorate<IRazorComponentEndpointInvoker>(services, (inner, provider) =>
			new Issue189ObservedInvoker(inner, provider.GetRequiredService<Issue189RequestProbe>()));
	}

	private static void Decorate<T>(IServiceCollection services, Func<T, IServiceProvider, T> create) where T : class
	{
		var descriptor = services.Single(service => service.ServiceType == typeof(T));
		services.Remove(descriptor);
		services.Add(new ServiceDescriptor(typeof(T), provider =>
			create((T)CreateService(descriptor, provider), provider), descriptor.Lifetime));
	}

	private static object CreateService(ServiceDescriptor descriptor, IServiceProvider provider)
		=> descriptor.ImplementationInstance
			?? descriptor.ImplementationFactory?.Invoke(provider)
			?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);

	private static void ConfigureValidationObservation(WebApplication app, bool invokerValidation)
		=> app.Use(async (context, next) =>
		{
			var observation = context.RequestServices.GetRequiredService<Issue189RequestProbe>().Observation;
			var feature = context.Features.Get<IAntiforgeryValidationFeature>();
			observation.Operations.Enqueue($"middleware-feature:{feature?.IsValid}");
			if (invokerValidation)
			{
				// Retain real middleware execution and endpoint metadata, but expose the invoker's no-result branch.
				context.Features.Set<IAntiforgeryValidationFeature>(null);
			}

			await next(context);
			ObserveLateToken(context, observation);
		});

	private static void ObserveLateToken(HttpContext context, Issue189Observation observation)
	{
		if (context.Request.Headers.ContainsKey("X-Issue-189-Late-Token"))
		{
			// Observe the provider after ordinary endpoint output, without adding response content or re-execution.
			observation.Operations.Enqueue($"late-response-started:{context.Response.HasStarted}");
			var provider = context.RequestServices.GetRequiredService<AntiforgeryStateProvider>();
			observation.Operations.Enqueue($"late-token:{provider.GetAntiforgeryToken() is not null}");
		}
	}

	private static async Task CaptureExceptionAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (InvalidOperationException exception)
		{
			// Capture the stock exception without developer-page stack noise or error-page re-execution.
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.Response.ContentType = "text/plain";
			await context.Response.WriteAsync(exception.GetType().FullName + "\n" + exception.Message);
		}
	}

	private sealed class Issue189ObservedInvoker(IRazorComponentEndpointInvoker inner, Issue189RequestProbe probe)
		: IRazorComponentEndpointInvoker
	{
		public Task Render(HttpContext context)
		{
			probe.Observation.Operations.Enqueue("invoker:" + inner.GetType().Name);
			return inner.Render(context);
		}
	}
}

internal sealed record Issue189Tokens(string Token, string Cookie);
