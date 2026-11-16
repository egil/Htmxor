using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// One browser's journey through a real hosted pipeline: a token request that also establishes the Session
// cookie, the redirecting request, and a fresh following request that reads what survived. Cookies are carried
// by hand because the TestServer client keeps none.
internal sealed class Issue230Host(Issue260Host host) : IAsyncDisposable
{
	public const string RedirectPath = "/issue-230/redirect";
	public const string ActionPath = "/issue-230/action";
	public const string ReadPath = "/issue-230/read";
	private readonly CookieContainer cookies = new();
	private string? token;

	public static async Task<Issue230Host> StartAsync(bool htmxor)
		=> new(await Issue260Host.CreateAsync<Issue260App>(htmxor, ConfigurePipeline, ConfigureServices));

	private static void ConfigureServices(IServiceCollection services)
	{
#if NET11_0_OR_GREATER
		services.AddDistributedMemoryCache();
		services.AddSession(options => options.Cookie.Name = "issue230-session");
#endif
	}

	private static void ConfigurePipeline(WebApplication app)
	{
#if NET11_0_OR_GREATER
		app.UseSession();
#endif
	}

	public async Task<Issue230Snapshot> SendAsync(
		string method, string mode, bool htmx, string? destination = null, bool enhancedNavigation = false)
	{
		token ??= await ReadTokenAsync();
		var query = $"?Mode={mode}" + (destination is null ? "" : $"&Destination={Uri.EscapeDataString(destination)}");
		var path = (mode == "action" ? ActionPath : RedirectPath) + query;
		using var request = Issue264SwitchOnRequests.Create(new HttpMethod(method), path, htmx, enhancedNavigation);
		AddUnsafeRequestProof(request, token);
		using var response = await SendWithCookiesAsync(request);
		return new(
			response.StatusCode,
			response.Headers.Location is { } location ? ToComparable(location) : null,
			Header(response, "HX-Redirect"),
			response.Headers.Contains("blazor-enhanced-nav-redirect-location"),
			await response.Content.ReadAsStringAsync());
	}

	// The form field answers a POST and the header answers a generated action.
	private static void AddUnsafeRequestProof(HttpRequestMessage request, string token)
	{
		if (request.Method == HttpMethod.Get)
		{
			return;
		}

		request.Headers.Add("RequestVerificationToken", token);
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "issue-230-save",
			["__RequestVerificationToken"] = token,
		});
	}

	// A fresh following request, with no htmx or enhanced-navigation headers on either host.
	public async Task<Issue230Survivors> ReadAsync()
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, ReadPath);
		using var response = await SendWithCookiesAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		return new(Attribute(body, "data-issue-230-message"), Attribute(body, "data-issue-230-value"));
	}

	private async Task<string> ReadTokenAsync()
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-230/token");
		using var response = await SendWithCookiesAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		return WebUtility.HtmlDecode(Attribute(body, "name=\"__RequestVerificationToken\" value"));
	}

	private async Task<HttpResponseMessage> SendWithCookiesAsync(HttpRequestMessage request)
	{
		var baseAddress = host.Client.BaseAddress!;
		var cookieHeader = cookies.GetCookieHeader(baseAddress);
		if (cookieHeader.Length > 0)
		{
			request.Headers.Add("Cookie", cookieHeader);
		}

		var response = await host.Client.SendAsync(request);
		if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
		{
			foreach (var cookie in setCookies)
			{
				cookies.SetCookies(baseAddress, cookie);
			}
		}

		return response;
	}

	// Each host has its own base address, so a same-origin Location compares by path and query.
	private string ToComparable(Uri location)
		=> location.IsAbsoluteUri && location.Authority == host.Client.BaseAddress!.Authority
			? location.PathAndQuery
			: location.OriginalString;

	private static string? Header(HttpResponseMessage response, string name)
		=> response.Headers.TryGetValues(name, out var values) ? values.Single() : null;

	private static string Attribute(string body, string name)
	{
		var match = Regex.Match(body, name + "=\"([^\"]*)\"", RegexOptions.CultureInvariant);
		Assert.True(match.Success, $"Expected {name} in: {body}");
		return match.Groups[1].Value;
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();
}

internal sealed record Issue230Snapshot(
	HttpStatusCode Status, string? Location, string? HxRedirect, bool HasEnhancedNavigationLocation, string Body);

internal sealed record Issue230Survivors(string Message, string Value);
