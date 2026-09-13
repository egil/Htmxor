using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue210TokenSecurityTests
{
	public static TheoryData<string, bool> UnsafeRequests => new()
	{
		{ "POST", false }, { "POST", true },
		{ "PUT", false }, { "PUT", true },
		{ "PATCH", false }, { "PATCH", true },
		{ "DELETE", false }, { "DELETE", true },
	};

	public static TheoryData<string, bool> TokenValidatedRequests => new()
	{
		{ "POST", false }, { "POST", true },
		{ "PUT", false }, { "PUT", true },
		{ "PATCH", false }, { "PATCH", true },
#if !NET11_0_OR_GREATER
		{ "DELETE", false }, { "DELETE", true },
#endif
	};

	[Theory]
	[MemberData(nameof(TokenValidatedRequests))]
	public async Task Valid_token_runs_the_generated_callback_even_with_cross_site_headers(string method, bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request(method, form: form);
		await host.AddCredentialsAsync(request);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		request.Headers.Add("Origin", "https://untrusted.example");

		using var response = await host.Client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		Assert.Contains($"data-action=\"{method}\"", html, StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[MemberData(nameof(TokenValidatedRequests))]
	public async Task Missing_token_rejects_before_application_effects_even_for_same_origin(string method, bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request(method, form: form);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Fact]
	public async Task Valid_token_submits_the_ordinary_form()
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request("POST", "form", form: true, direct: false);
		await host.AddCredentialsAsync(request);

		using var response = await host.Client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-saved=\"True\">submitted", html, StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Fact]
	public async Task Missing_token_rejects_the_ordinary_form_before_application_effects()
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request("POST", "form", form: true, direct: false);

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("PATCH")]
#if !NET11_0_OR_GREATER
	[InlineData("DELETE")]
#endif
	public async Task Actionless_unsafe_route_rejects_missing_token_before_lifecycle(string method)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request(method, "actionless", form: true);
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Explicit_opt_out_allows_generated_callback_without_a_token(bool tokens)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: tokens, disableNative: !tokens);
		using var request = Issue210SecurityHost.Request("DELETE", "exempt");
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Ordinary_form_opt_out_preserves_application_choice(bool tokens)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: tokens);
		using var request = Issue210SecurityHost.Request("POST", "exempt-form", form: true, direct: false);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	public async Task Corrupted_token_rejects_before_binding_lifecycle_or_callback(string method)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request(method, form: true);
		await host.AddCredentialsAsync(request);
		request.Headers.Remove("RequestVerificationToken");
		request.Headers.Add("RequestVerificationToken", "corrupted");
		request.Headers.Add("Sec-Fetch-Site", "same-origin");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	[InlineData("DELETE")]
	public async Task Valid_protection_renders_actionless_unsafe_routes(string method)
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request(method, "actionless", form: true);
		await host.AddCredentialsAsync(request);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("data-actionless", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Equal(1, host.Probe.Initializations);
		Assert.Equal(0, host.Probe.Callbacks);
	}

	[Fact]
	public async Task Valid_token_on_actionless_POST_reaches_named_form_dispatch()
	{
		await using var host = await Issue210SecurityHost.StartAsync();
		using var request = Issue210SecurityHost.Request("POST", "actionless", form: true);
		await host.AddCredentialsAsync(request);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Contains("does not specify which form is being submitted", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
		Assert.Equal((1, 1, 0), (host.Probe.Bindings, host.Probe.Initializations, host.Probe.Callbacks));
	}

	[Theory]
	[InlineData("actions", "DELETE", false)]
	[InlineData("actionless", "PUT", true)]
	[InlineData("form", "POST", true)]
	public async Task Missing_protection_middleware_is_a_configuration_failure_before_effects(string page, string method, bool form)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: false, disableNative: true);
		using var request = Issue210SecurityHost.Request(method, page, form, direct: page != "form");
		using var response = await host.Client.SendAsync(request);
		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.Contains("antiforgery", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
		host.Probe.AssertUntouched();
	}
}
