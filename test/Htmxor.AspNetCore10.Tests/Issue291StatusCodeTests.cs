using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Htmxor.AspNetCore10;

public sealed class Issue291StatusCodeTests(ITestOutputHelper output)
{
	[Theory]
	[InlineData(204, false, false)]
	[InlineData(204, true, false)]
	[InlineData(204, false, true)]
	[InlineData(204, true, true)]
	[InlineData(304, false, false)]
	[InlineData(304, true, false)]
	[InlineData(304, false, true)]
	[InlineData(304, true, true)]
	public async Task Body_forbidden_API_status_sends_headers_without_logged_exceptions(int status, bool htmx, bool empty)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={status}&Empty={empty}", htmx, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)status, response.Status),
			() => Assert.Empty(response.Body),
			() => Assert.Equal("retained", response.Headers["X-Status-Probe"]),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors),
			() => Assert.Equal(empty && !htmx, host.Probe.EmptyBodyGuard is InvalidOperationException)
		);
	}

	[Theory]
	[InlineData(301, false)]
	[InlineData(301, true)]
	[InlineData(302, false)]
	[InlineData(302, true)]
	[InlineData(303, false)]
	[InlineData(303, true)]
	[InlineData(307, false)]
	[InlineData(307, true)]
	[InlineData(308, false)]
	[InlineData(308, true)]
	public async Task Redirect_API_status_rejects_before_mutation_and_preserves_visible_body(int status, bool htmx)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={status}&Mode=reject", htmx, output);

		AssertRejection(host.Probe);
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.Accepted, response.Status),
			() => Assert.Null(response.Location),
			() => Assert.Contains("<p data-status-body>Component output.</p>", response.Body, StringComparison.Ordinal),
			() => Assert.Equal("existing-event", response.Headers["HX-Trigger"]),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	[Theory]
	[InlineData(301, "empty")]
	[InlineData(301, "navigation")]
	[InlineData(302, "empty")]
	[InlineData(302, "navigation")]
	[InlineData(303, "empty")]
	[InlineData(303, "navigation")]
	[InlineData(307, "empty")]
	[InlineData(307, "navigation")]
	[InlineData(308, "empty")]
	[InlineData(308, "navigation")]
	public async Task Redirect_rejection_preserves_existing_body_suppression(int status, string seed)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={status}&Mode=reject&Seed={seed}", true, output);

		AssertRejection(host.Probe);
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.Accepted, response.Status),
			() => Assert.Empty(response.Body),
			() => Assert.Equal(seed == "navigation", response.Headers.ContainsKey("HX-Location")),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	[Theory]
	[InlineData(200, false)]
	[InlineData(200, true)]
	[InlineData(202, false)]
	[InlineData(202, true)]
	[InlineData(286, false)]
	[InlineData(286, true)]
	[InlineData(300, false)]
	[InlineData(300, true)]
	[InlineData(305, false)]
	[InlineData(305, true)]
	[InlineData(306, false)]
	[InlineData(306, true)]
	[InlineData(404, false)]
	[InlineData(404, true)]
	[InlineData(422, false)]
	[InlineData(422, true)]
	public async Task Other_API_statuses_preserve_component_output(int status, bool htmx)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={status}", htmx, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)status, response.Status),
			() => Assert.Contains("<p data-status-body>Component output.</p>", response.Body, StringComparison.Ordinal),
			() => Assert.Equal("retained", response.Headers["X-Status-Probe"]),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	[Theory]
	[InlineData(204, 200, false)]
	[InlineData(204, 200, true)]
	[InlineData(204, 422, false)]
	[InlineData(204, 422, true)]
	[InlineData(304, 200, false)]
	[InlineData(304, 200, true)]
	[InlineData(304, 422, false)]
	[InlineData(304, 422, true)]
	public async Task Later_allowed_API_status_restores_component_output(int initial, int final, bool htmx)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={initial}&FinalStatus={final}&Mode=transition", htmx, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)final, response.Status),
			() => Assert.Contains("<p data-status-body>Component output.</p>", response.Body, StringComparison.Ordinal),
			() => Assert.Equal("retained", response.Headers["X-Status-Probe"]),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	[Theory]
	[InlineData(204, 200, "empty")]
	[InlineData(204, 200, "navigation")]
	[InlineData(204, 422, "empty")]
	[InlineData(204, 422, "navigation")]
	[InlineData(304, 200, "empty")]
	[InlineData(304, 200, "navigation")]
	[InlineData(304, 422, "empty")]
	[InlineData(304, 422, "navigation")]
	public async Task Later_allowed_API_status_retains_independent_body_suppression(int initial, int final, string seed)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={initial}&FinalStatus={final}&Mode=transition&Seed={seed}", true, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)final, response.Status),
			() => Assert.Empty(response.Body),
			() => Assert.Equal("retained", response.Headers["X-Status-Probe"]),
			() => Assert.Equal(seed == "navigation", response.Headers.ContainsKey("HX-Location")),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	[Theory]
	[InlineData(204, 304, false)]
	[InlineData(204, 304, true)]
	[InlineData(304, 204, false)]
	[InlineData(304, 204, true)]
	public async Task Raw_forbidden_status_overriding_API_status_retains_stock_write_rejection(int initial, int final, bool htmx)
	{
		await using var stock = await Issue291Host.StartAsync(htmxor: false);
		await using var candidate = await Issue291Host.StartAsync();

		var expected = await stock.GetAsync($"Status={initial}&FinalStatus={final}&Mode=mixed", htmx, output);
		var actual = await candidate.GetAsync($"Status={initial}&FinalStatus={final}&Mode=mixed", htmx, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)final, expected.Status),
			() => Assert.Equal(expected.Status, actual.Status),
			() => Assert.Equal(expected.Body, actual.Body),
			() => Assert.Empty(actual.Body),
			() => Assert.Equal("retained", actual.Headers["X-Status-Probe"])
		);
		var message = $"Writing to the response body is invalid for responses with status code {final}.";
		Assert.Multiple(
			() => Assert.Contains(stock.Logs.Exceptions, exception => exception is InvalidOperationException && exception.Message == message),
			() => Assert.Contains(candidate.Logs.Exceptions, exception => exception is InvalidOperationException && exception.Message == message)
		);
	}

	[Theory]
	[InlineData(204, 304, "empty")]
	[InlineData(204, 304, "navigation")]
	[InlineData(304, 204, "empty")]
	[InlineData(304, 204, "navigation")]
	public async Task Raw_forbidden_status_overriding_API_status_retains_independent_suppression(int initial, int final, string seed)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync($"Status={initial}&FinalStatus={final}&Mode=mixed&Seed={seed}", true, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)final, response.Status),
			() => Assert.Empty(response.Body),
			() => Assert.Equal("retained", response.Headers["X-Status-Probe"]),
			() => Assert.Equal(seed == "navigation", response.Headers.ContainsKey("HX-Location")),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	[Theory]
	[InlineData(204, false)]
	[InlineData(204, true)]
	[InlineData(304, false)]
	[InlineData(304, true)]
	public async Task Raw_body_forbidden_status_retains_stock_write_rejection(int status, bool htmx)
	{
		await using var stock = await Issue291Host.StartAsync(htmxor: false);
		await using var candidate = await Issue291Host.StartAsync();

		var expected = await stock.GetAsync($"Status={status}&Mode=raw", htmx, output);
		var actual = await candidate.GetAsync($"Status={status}&Mode=raw", htmx, output);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)status, expected.Status),
			() => Assert.Equal(expected.Status, actual.Status),
			() => Assert.Equal(expected.Body, actual.Body),
			() => Assert.Empty(actual.Body)
		);
		var message = $"Writing to the response body is invalid for responses with status code {status}.";
		Assert.Multiple(
			() => Assert.Contains(stock.Logs.Exceptions, exception => exception is InvalidOperationException && exception.Message == message),
			() => Assert.Contains(candidate.Logs.Exceptions, exception => exception is InvalidOperationException && exception.Message == message)
		);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Raw_redirect_with_Location_remains_an_escape_hatch(bool htmx)
	{
		await using var host = await Issue291Host.StartAsync();

		var response = await host.GetAsync("Status=302&Mode=raw-redirect", htmx, output);

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.Found, response.Status),
			() => Assert.Equal("/destination", response.Location),
			() => Assert.Contains("<p data-status-body>Component output.</p>", response.Body, StringComparison.Ordinal),
			() => Assert.Empty(host.Logs.Exceptions),
			() => Assert.Empty(host.Logs.Errors)
		);
	}

	private static void AssertRejection(Issue291Probe probe)
	{
		Assert.Multiple(
			() => Assert.IsType<ArgumentException>(probe.Rejection),
			() => Assert.IsType<Issue291State>(probe.Before),
			() => Assert.IsType<Issue291State>(probe.After));
		var exception = (ArgumentException)probe.Rejection!;
		Assert.Multiple(
			() => Assert.Contains("Location(", exception.Message, StringComparison.Ordinal),
			() => Assert.Contains("Redirect(", exception.Message, StringComparison.Ordinal)
		);
		var before = probe.Before!;
		var after = probe.After!;
		Assert.Multiple(
			() => Assert.Equal(before.Status, after.Status),
			() => Assert.Equal(before.Headers, after.Headers),
			() => Assert.Equal(before.Items, after.Items),
			() => Assert.Same(before.Body, after.Body),
			() => Assert.Equal(before.HasStarted, after.HasStarted)
		);
	}
}
