using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue318FormActionTests
{
	[Theory]
	[InlineData("PUT", false)]
	[InlineData("PUT", true)]
	[InlineData("PATCH", false)]
	[InlineData("PATCH", true)]
	public async Task Generated_action_binds_form_body_without_a_named_form(string method, bool multipart)
	{
		await using var host = await Issue318Host.StartAsync();
		using var request = Issue318Host.Request(method, multipart);
		await host.AddCredentialsAsync(request);

		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains($"data-action=\"{method}\"", body, StringComparison.Ordinal);
		Assert.Contains("Current count: 6", body, StringComparison.Ordinal);
		Assert.Equal(5, host.Probe.BoundCount);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DELETE_action_does_not_bind_form_body(bool multipart)
	{
		await using var host = await Issue318Host.StartAsync();
		using var request = Issue318Host.Request("DELETE", multipart);
		await host.AddCredentialsAsync(request);

		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains("data-action=\"DELETE\"", body, StringComparison.Ordinal);
		Assert.Contains("Current count: 1", body, StringComparison.Ordinal);
		Assert.Equal(0, host.Probe.BoundCount);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData("PUT", false)]
	[InlineData("PUT", true)]
	[InlineData("PATCH", false)]
	[InlineData("PATCH", true)]
	[InlineData("DELETE", false)]
	[InlineData("DELETE", true)]
	public async Task Non_action_non_POST_request_does_not_bind_form_body(string method, bool multipart)
	{
		await using var host = await Issue318Host.StartAsync();
		using var request = Issue318Host.Request(method, multipart, action: false);
		await host.AddCredentialsAsync(request);

		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains("Current count: 0", body, StringComparison.Ordinal);
		Assert.Equal(0, host.Probe.BoundCount);
		Assert.Equal(0, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	public async Task Endpoint_antiforgery_opt_out_allows_action_without_token(string method)
	{
		await using var host = await Issue318Host.StartAsync(disableValidation: true);
		using var request = Issue318Host.Request(method);

		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.False(host.Probe.RequiresValidation);
		Assert.Null(host.Probe.MiddlewareValid);
		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains($"data-action=\"{method}\"", body, StringComparison.Ordinal);
		Assert.Contains("Current count: 6", body, StringComparison.Ordinal);
	}
}
