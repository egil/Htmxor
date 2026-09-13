#if NET11_0_OR_GREATER
using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue210NativeSecurityTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Token_only_configuration_does_not_add_DELETE_token_validation(bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync(disableNative: true);
		using var request = Issue210SecurityHost.Request("DELETE", form: form);

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-action=\"DELETE\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Token_middleware_preserves_native_DELETE_allowance_without_a_token(bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request("DELETE", form: form);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-action=\"DELETE\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Token_middleware_preserves_native_DELETE_denial_despite_a_valid_token(bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request("DELETE", form: form);
		await host.AddCredentialsAsync(request);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		request.Headers.Add("Origin", "https://untrusted.example");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Fact]
	public async Task Token_middleware_preserves_native_headerless_DELETE_allowance_on_actionless_routes()
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request("DELETE", "actionless", form: true);

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-actionless", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Initializations);
		Assert.Equal(0, host.Probe.Callbacks);
	}

	[Theory]
	[MemberData(nameof(Issue210TokenSecurityTests.UnsafeRequests), MemberType = typeof(Issue210TokenSecurityTests))]
	public async Task Native_same_origin_runs_generated_callbacks_without_tokens(string method, bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request(method, form: form);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");
		using var response = await host.Client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		Assert.Contains($"data-action=\"{method}\"", html, StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[MemberData(nameof(Issue210TokenSecurityTests.UnsafeRequests), MemberType = typeof(Issue210TokenSecurityTests))]
	public async Task Native_cross_site_rejects_generated_callbacks_before_application_effects(string method, bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request(method, form: form);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData("none", null, null)]
	[InlineData("cross-site", "https://trusted.example", "trusted")]
	[InlineData("same-site", "https://trusted.example", "trusted")]
	[InlineData(null, "http://localhost", null)]
	[InlineData(null, null, null)]
	public async Task Native_allowed_headers_and_group_cors_policy_reach_generated_callback(
		string? fetchSite, string? origin, string? cors)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false, cors: cors);
		using var request = Issue210SecurityHost.Request("DELETE");
		AddOriginHeaders(request, fetchSite, origin);

		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData("same-site", null, null)]
	[InlineData("unexpected", null, null)]
	[InlineData("cross-site", "https://trusted.example", "wildcard")]
	[InlineData(null, "https://untrusted.example", null)]
	[InlineData(null, "null", null)]
	public async Task Native_denied_headers_and_wildcard_cors_reject_before_application_effects(
		string? fetchSite, string? origin, string? cors)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false, cors: cors);
		using var request = Issue210SecurityHost.Request("DELETE");
		AddOriginHeaders(request, fetchSite, origin);

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Fact]
	public async Task Native_same_origin_submits_the_ordinary_form_without_a_token()
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request("POST", "form", form: true, direct: false);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-saved=\"True\">submitted", html, StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Fact]
	public async Task Native_cross_site_rejects_the_ordinary_form_before_application_effects()
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request("POST", "form", form: true, direct: false);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	[InlineData("DELETE")]
	public async Task Native_denial_stops_actionless_routes_before_lifecycle(string method)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request(method, "actionless", form: true);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Fact]
	public async Task Native_opt_out_runs_generated_callback_despite_cross_site_headers()
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request("DELETE", "exempt");
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Fact]
	public async Task Native_protection_on_actionless_POST_reaches_named_form_dispatch()
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request("POST", "actionless", form: true);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Contains("does not specify which form is being submitted", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Equal((1, 1, 0), (host.Probe.Bindings, host.Probe.Initializations, host.Probe.Callbacks));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Native_rendering_does_not_issue_an_unused_antiforgery_cookie(bool direct)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false);
		using var request = Issue210SecurityHost.Request("GET", "form", direct: direct);
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.False(response.Headers.Contains("Set-Cookie"));
		Assert.DoesNotContain("__RequestVerificationToken", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task Streaming_token_issuance_matches_the_configured_stock_pipeline(bool htmxor, bool tokens)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: tokens, htmxor: htmxor, groupPrefix: "");
		using var request = Issue210SecurityHost.Request("GET", "streaming", direct: false);
		request.RequestUri = new Uri("/issue-210/streaming", UriKind.Relative);
		using var response = await host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(TimeSpan.FromSeconds(20));
		host.StreamGate.Completion.TrySetResult();
		var html = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		Assert.Contains("data-stream=\"complete\"", html, StringComparison.Ordinal);
		Assert.Equal(tokens, response.Headers.Contains("Set-Cookie"));
	}

	private static void AddOriginHeaders(HttpRequestMessage request, string? fetchSite, string? origin)
	{
		if (fetchSite is not null)
		{
			request.Headers.Add("Sec-Fetch-Site", fetchSite);
		}

		if (origin is not null)
		{
			request.Headers.Add("Origin", origin);
		}
	}
}
#endif
