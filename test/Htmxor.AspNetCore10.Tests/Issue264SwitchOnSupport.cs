using System.Net;
using System.Text.RegularExpressions;

namespace Htmxor.AspNetCore10;

internal static class Issue264SwitchOnConstants
{
	public const string InternalDestination = "/issue-264/destination";
	public const string ExternalDestination = "https://example.invalid/issue-264/destination";
	public const string EnhancedNavigationAccept = "text/html; blazor-enhanced-nav=on";

	// The test project never references the switch-on host project's assembly (it is spawned as a separate
	// process; see Issue264SwitchOnHostProcess), so this literal is duplicated here rather than shared, and
	// must match Htmxor.AspNetCore10.SwitchOnHost.Issue264SwitchOnHostRuntime.ThrowOnOpaqueRedirectionProtectFlag exactly.
	public const string ThrowOnOpaqueRedirectionProtectFlag = "--throw-on-opaque-redirection-protect";
}

// A read-once-and-normalize snapshot of a switch-on host's response, shared by every Issue264SwitchOnTests
// case so each test states only what it compares, not how a response is read.
internal sealed record Issue264Snapshot(
	HttpStatusCode StatusCode,
	Uri? Location,
	string? HxRedirect,
	string? EnhancedNavigationLocation,
	string? ContentType,
	bool HasSsrFraming,
	string Body)
{
	public static async Task<Issue264Snapshot> CreateAsync(HttpResponseMessage response)
	{
		var body = NormalizeProtectedPayloads(await response.Content.ReadAsStringAsync());
		return new(
			response.StatusCode,
			response.Headers.Location,
			SingleHeaderOrNull(response, "HX-Redirect"),
			SingleHeaderOrNull(response, "blazor-enhanced-nav-redirect-location"),
			response.Content.Headers.ContentType?.ToString(),
			response.Headers.Contains("ssr-framing"),
			body);
	}

	private static string? SingleHeaderOrNull(HttpResponseMessage response, string name)
		=> response.Headers.TryGetValues(name, out var values) ? values.Single() : null;

	// Each switch-on host process has its own randomly keyed ephemeral data-protection provider (see
	// Issue264SwitchOnHostRuntime.ConfigureServices), so an antiforgery token or opaque-redirect payload from
	// one process can never equal another's even when both protect the identical logical value.
	public static string NormalizeProtectedPayloads(string body) => Regex.Replace(
		body,
		"(_framework/opaque-redirect\\?url=|value=\")CfDJ8[^\"<]+",
		"$1<protected>",
		RegexOptions.CultureInvariant);
}

internal static class Issue264SwitchOnRequests
{
	public static HttpRequestMessage Create(HttpMethod method, string path, bool htmx = false, bool enhancedNavigation = false)
	{
		var request = new HttpRequestMessage(method, path);
		if (htmx)
		{
			request.Headers.Add("HX-Request", "true");
		}

		if (enhancedNavigation)
		{
			request.Headers.Add("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		}

		return request;
	}

	public static async Task<Issue264Snapshot> SendAsync(HttpClient client, HttpRequestMessage request)
	{
		using var response = await client.SendAsync(request);
		return await Issue264Snapshot.CreateAsync(response);
	}

	public static async Task<Issue264Snapshot> SubmitFormAsync(
		HttpClient client, string? destination, bool enhancedNavigation = false, bool htmx = false)
	{
		var query = destination is null ? string.Empty : $"?Destination={Uri.EscapeDataString(destination)}";
		using var page = await client.GetAsync("/issue-264/form" + query);
		var pageBody = await page.Content.ReadAsStringAsync();
		var token = ExtractAntiforgeryToken(pageBody);
		using var request = Create(HttpMethod.Post, "/issue-264/form" + query, htmx, enhancedNavigation);
		CopyCookies(page, request);
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "issue-264-go",
			["__RequestVerificationToken"] = token,
		});
		return await SendAsync(client, request);
	}

	// Follows an opaque-redirect value on the same host that protected it: each process has its own ephemeral
	// data-protection key, so the token is meaningful only against the host that issued it.
	public static async Task<Uri?> FollowOpaqueRedirectAsync(Issue264SwitchOnHostProcess host, string opaqueUrl)
	{
		using var response = await host.Client.GetAsync(opaqueUrl);
		return response.Headers.Location;
	}

	public static string ExtractRedirectionTemplateUrl(string body)
	{
		var match = Regex.Match(body, "<template type=\"redirection\">([^<]+)</template>", RegexOptions.CultureInvariant);
		Assert.True(match.Success, $"Expected a stock-shaped redirection template in: {body}");
		return match.Groups[1].Value;
	}

	private static string ExtractAntiforgeryToken(string body)
	{
		var match = Regex.Match(
			body, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
		Assert.True(match.Success, "The switch-on host must issue a real antiforgery request token.");
		return WebUtility.HtmlDecode(match.Groups[1].Value);
	}

	private static void CopyCookies(HttpResponseMessage page, HttpRequestMessage request)
	{
		if (page.Headers.TryGetValues("Set-Cookie", out var cookies))
		{
			request.Headers.Add("Cookie", string.Join("; ", cookies.Select(cookie => cookie.Split(';')[0])));
		}
	}
}
