using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Htmxor.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue191PersistedStateParityTests
{
	[Fact]
	public async Task Candidate_selected_interactive_server_component_preserves_persisted_state_representation()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.Path);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.Path);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.Contains("data-issue-191-state=\"rendered\"", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Server-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-WebAssembly-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_omits_javascript_initializers_for_enhanced_navigation_like_stock()
	{
		await using var pair = await Issue191HostPair.CreateAsync("[{\"identifier\":\"issue-191\"}]");
		using var stockRequest = CreateEnhancedNavigationRequest();
		using var candidateRequest = CreateEnhancedNavigationRequest();
		using var stockResponse = await pair.Stock.Client.SendAsync(stockRequest);
		using var candidateResponse = await pair.Candidate.Client.SendAsync(candidateRequest);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.DoesNotContain("<!--Blazor-Web-Initializers:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
	}

	[Fact]
	public async Task Candidate_selected_interactive_webassembly_component_preserves_persisted_state_representation()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.WebAssemblyPath);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.WebAssemblyPath);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.Contains("<!--Blazor-WebAssembly-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Server-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_selected_interactive_auto_component_preserves_persisted_state_representation()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.AutoPath);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.AutoPath);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
		Assert.Contains("<!--Blazor-WebAssembly:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Server-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-WebAssembly-Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_persisted_state_remains_request_scoped_during_sequential_and_concurrent_requests()
	{
		await using var pair = await Issue191HostPair.CreateAsync();

		var paths = new[] { Issue191HostPair.Path, Issue191HostPair.WebAssemblyPath, Issue191HostPair.AutoPath };
		foreach (var path in paths)
		{
			using var response = await pair.Candidate.Client.GetAsync(path);
			var snapshot = await Issue187ResponseSnapshot.CreateAsync(response);
			Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
			Assert.Contains("<!--Blazor-Server-Component-State:", snapshot.Body, StringComparison.Ordinal);
			Assert.Contains("<!--Blazor-WebAssembly-Component-State:", snapshot.Body, StringComparison.Ordinal);
		}

		var responses = await Task.WhenAll(paths.SelectMany(path => Enumerable.Repeat(path, 4))
			.Select(path => pair.Candidate.Client.GetAsync(path)));
		foreach (var response in responses)
		{
			using (response)
			{
				var snapshot = await Issue187ResponseSnapshot.CreateAsync(response);
				Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
				Assert.Contains("<!--Blazor-Server-Component-State:", snapshot.Body, StringComparison.Ordinal);
				Assert.Contains("<!--Blazor-WebAssembly-Component-State:", snapshot.Body, StringComparison.Ordinal);
			}
		}
	}

	[Fact]
	public async Task Candidate_persisted_state_does_not_leak_authenticated_request_state()
	{
		await using var pair = await Issue191HostPair.CreateAsync();
		using var alice = CreateRequest("alice");
		using var bob = CreateRequest("bob");
		using var aliceResponse = await pair.Candidate.Client.SendAsync(alice);
		using var bobResponse = await pair.Candidate.Client.SendAsync(bob);

		Assert.Contains("data-issue-191-user=\"alice\"", await aliceResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Contains("data-issue-191-user=\"bob\"", await bobResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);

		var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(async index =>
		{
			using var request = CreateRequest(index % 2 == 0 ? "alice" : "bob");
			using var response = await pair.Candidate.Client.SendAsync(request);
			return (index, body: await response.Content.ReadAsStringAsync());
		}));
		Assert.All(responses, response => Assert.Contains(
			$"data-issue-191-user=\"{(response.index % 2 == 0 ? "alice" : "bob")}\"",
			response.body,
			StringComparison.Ordinal));
	}

	[Fact]
	public async Task Candidate_status_code_reexecution_suppresses_persisted_state_like_stock()
	{
		await using var pair = await Issue191HostPair.CreateAsync();
		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.StatusOriginPath);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.StatusOriginPath);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.NotFound, stock.StatusCode);
		Assert.DoesNotContain("Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_exception_handler_reexecution_suppresses_persisted_state_like_stock()
	{
		await using var pair = await Issue191HostPair.CreateAsync();
		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.ExceptionOriginPath);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.ExceptionOriginPath);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(HttpStatusCode.InternalServerError, stock.StatusCode);
		Assert.DoesNotContain("Component-State:", stock.Body, StringComparison.Ordinal);
		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_duplicate_configured_server_mode_preserves_stock_response()
	{
		await using var pair = await Issue191HostPair.CreateAsync(
			configureEndpoints: endpoints => endpoints.AddInteractiveServerRenderMode());
		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.Path);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.Path);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Equal(stock.StatusCode, candidate.StatusCode);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	[Fact]
	public async Task Candidate_emits_configured_javascript_initializers_in_stock_order()
	{
		const string initializers = "[{\"identifier\":\"issue-191\"}]";
		await using var pair = await Issue191HostPair.CreateAsync(initializers);
		using var stockResponse = await pair.Stock.Client.GetAsync(Issue191HostPair.Path);
		using var candidateResponse = await pair.Candidate.Client.GetAsync(Issue191HostPair.Path);
		var stock = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var candidate = await Issue187ResponseSnapshot.CreateAsync(candidateResponse);

		Assert.Contains("<!--Blazor-Web-Initializers:W3siaWRlbnRpZmllciI6Imlzc3VlLTE5MSJ9XQ==-->", stock.Body, StringComparison.Ordinal);
		Assert.Equal(NormalizeDynamicState(stock.Body), NormalizeDynamicState(candidate.Body));
		Assert.Equal(NormalizeDynamicHeaders(stock.Headers), NormalizeDynamicHeaders(candidate.Headers));
	}

	private static HttpRequestMessage CreateRequest(string user)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, Issue191HostPair.Path);
		request.Headers.Add(Issue187AuthenticationHandler.UserHeaderName, user);
		return request;
	}

	private static HttpRequestMessage CreateEnhancedNavigationRequest()
	{
		var request = new HttpRequestMessage(HttpMethod.Get, Issue191HostPair.Path);
		request.Headers.TryAddWithoutValidation("Accept", "text/html; blazor-enhanced-nav=on");
		return request;
	}

	private static string NormalizeDynamicState(string body)
	{
		var normalized = Regex.Replace(body, "\\\"prerenderId\\\":\\\"[^\\\"]+\\\"", "\\\"prerenderId\\\":\\\"<dynamic>\\\"");
		normalized = Regex.Replace(normalized, "\\\"descriptor\\\":\\\"[^\\\"]+\\\"", "\\\"descriptor\\\":\\\"<dynamic>\\\"");
		normalized = Regex.Replace(normalized, "(?<=<!--Blazor-Server-Component-State:)[^-]+(?=-->)", "<dynamic>");
		return Regex.Replace(normalized, "(?<=<!--Blazor-WebAssembly-Component-State:)[^-]+(?=-->)", "<dynamic>");
	}

	private static IReadOnlyDictionary<string, string> NormalizeDynamicHeaders(IReadOnlyDictionary<string, string> headers)
		=> headers.ToDictionary(
			pair => pair.Key,
			pair => pair.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
				? Regex.Replace(pair.Value, "(?<==)[^;]+", "<dynamic>")
				: pair.Value,
			StringComparer.OrdinalIgnoreCase);
}

internal sealed class Issue191HostPair(Issue187ParityHost stock, Issue187ParityHost candidate) : IAsyncDisposable
{
	public const string Path = "/issue-191/state";
	public const string WebAssemblyPath = "/issue-191/webassembly-state";
	public const string AutoPath = "/issue-191/auto-state";
	public const string StatusOriginPath = "/issue-191/status-origin";
	public const string ExceptionOriginPath = "/issue-191/exception-origin";

	public Issue187ParityHost Stock { get; } = stock;

	public Issue187ParityHost Candidate { get; } = candidate;

	public static async Task<Issue191HostPair> CreateAsync(
		string? javaScriptInitializers = null,
		Action<RazorComponentsEndpointConventionBuilder>? configureEndpoints = null)
	{
		var protection = new EphemeralDataProtectionProvider();
		var options = new Issue187ParityHostOptions
		{
			BeforeSession = app =>
			{
				app.UseExceptionHandler(Path);
				app.UseStatusCodePagesWithReExecute(Path);
				app.Use(async (context, next) =>
				{
					if (context.Request.Path == StatusOriginPath)
					{
						context.Response.StatusCode = StatusCodes.Status404NotFound;
						return;
					}
					if (context.Request.Path == ExceptionOriginPath)
					{
						throw new InvalidOperationException("issue-191 exception origin");
					}

					await next(context);
				});
			},
			ConfigureRazorComponents = builder =>
			{
				builder.AddInteractiveServerComponents();
				builder.AddInteractiveWebAssemblyComponents();
			},
			ConfigureRazorComponentOptions = options =>
			{
				if (javaScriptInitializers is not null)
				{
					ConfigureJavaScriptInitializers(options, javaScriptInitializers);
				}
			},
			ConfigureEndpoints = endpoints =>
			{
				endpoints.AddInteractiveServerRenderMode();
				endpoints.AddInteractiveWebAssemblyRenderMode();
				configureEndpoints?.Invoke(endpoints);
			},
			ConfigureServices = services => services.AddSingleton<IDataProtectionProvider>(protection),
		};
		var stock = await Issue187ParityHost.CreateAsync<Issue191App>(false, options: options);
		var candidate = await Issue187ParityHost.CreateAsync<Issue191App>(
			true,
			HtmxorEndpointCandidateServices.Add,
			options);
		return new(stock, candidate);
	}

	private static void ConfigureJavaScriptInitializers(RazorComponentsServiceOptions options, string initializers)
	{
		var property = typeof(RazorComponentsServiceOptions).GetProperty(
			"JavaScriptInitializers",
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
			?? throw new InvalidOperationException("The ASP.NET Core test fixture requires RazorComponentsServiceOptions.JavaScriptInitializers.");
		property.SetValue(options, initializers);
	}

	public async ValueTask DisposeAsync()
	{
		await Candidate.DisposeAsync();
		await Stock.DisposeAsync();
	}
}
