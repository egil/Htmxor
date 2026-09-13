#if NET11_0_OR_GREATER
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10;

public sealed class Issue214StateParityTests
{
	[Theory]
	[InlineData("server", false)]
	[InlineData("wasm", false)]
	[InlineData("auto", false)]
	[InlineData("server", true)]
	[InlineData("wasm", true)]
	[InlineData("auto", true)]
	public async Task Ordinary_and_enhanced_responses_preserve_state_options_and_metadata(string mode, bool enhanced)
	{
		await using var stock = await Issue214Host.CreateAsync(false);
		await using var candidate = await Issue214Host.CreateAsync(true);

		var expected = await ReadAsync(stock, "state", mode, enhanced, "alice", "first");
		var actual = await ReadAsync(candidate, "state", mode, enhanced, "alice", "first");

		Assert.Equal(HttpStatusCode.OK, expected.Status);
		Assert.Contains("data-asset=\"assets/issue-214.fingerprint.js\"", expected.Body, StringComparison.Ordinal);
		Assert.Contains("<!--Blazor-Configuration:", expected.Body, StringComparison.Ordinal);
		Assert.Contains("\"applicationCulture\":\"is-IS\"", expected.Body, StringComparison.Ordinal);
		Assert.Contains("\"reconnectionMaxRetries\":7", expected.Body, StringComparison.Ordinal);
		Assert.Contains("\"disableDomPreservation\":false", expected.Body, StringComparison.Ordinal);
		Assert.Equal(!enhanced, expected.Body.Contains("<!--Blazor-Web-Initializers:WyJpc3N1ZS0yMTQiXQ==-->", StringComparison.Ordinal));
		Assert.Contains("first", expected.Body, StringComparison.Ordinal);
		Assert.Contains("issue214", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("server", false)]
	[InlineData("wasm", false)]
	[InlineData("auto", false)]
	[InlineData("server", true)]
	[InlineData("wasm", true)]
	[InlineData("auto", true)]
	public async Task Status_reexecution_preserves_the_final_endpoints_state(string mode, bool enhanced)
	{
		await using var stock = await Issue214Host.CreateAsync(false);
		await using var candidate = await Issue214Host.CreateAsync(true);

		var expected = await ReadAsync(stock, "status", mode, enhanced, "alice", "status-value");
		var actual = await ReadAsync(candidate, "status", mode, enhanced, "alice", "status-value");

		Assert.Equal(HttpStatusCode.NotFound, expected.Status);
		Assert.Contains("issue214", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("server", false)]
	[InlineData("wasm", false)]
	[InlineData("auto", false)]
	[InlineData("server", true)]
	[InlineData("wasm", true)]
	[InlineData("auto", true)]
	public async Task Exception_handling_preserves_its_independent_stock_state_contract(string mode, bool enhanced)
	{
		await using var stock = await Issue214Host.CreateAsync(false);
		await using var candidate = await Issue214Host.CreateAsync(true);

		var expected = await ReadAsync(stock, "exception", mode, enhanced, "alice", "error-value");
		var actual = await ReadAsync(candidate, "exception", mode, enhanced, "alice", "error-value");

		Assert.Equal(HttpStatusCode.InternalServerError, expected.Status);
		Assert.Contains("data-user=\"alice\"", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("server")]
	[InlineData("wasm")]
	[InlineData("auto")]
	public async Task Sequential_and_concurrent_requests_keep_their_own_persisted_data_and_options(string mode)
	{
		await using var stock = await Issue214Host.CreateAsync(false);
		await using var candidate = await Issue214Host.CreateAsync(true);

		var first = await ReadPairAsync(stock, candidate, mode, 0);
		var second = await ReadPairAsync(stock, candidate, mode, 1);
		var concurrent = await ReadOverlappingPairsAsync(stock, candidate, mode);

		Assert.All(concurrent.Prepend(second).Prepend(first), AssertIsolated);
	}

	[Fact]
	public async Task Separately_configured_applications_retain_their_own_browser_options()
	{
		await using var stock = await Issue214Host.CreateAsync(false, "second-app");
		await using var first = await Issue214Host.CreateAsync(true, "first-app");
		await using var second = await Issue214Host.CreateAsync(true, "second-app");

		var responses = await Task.WhenAll(
			ReadAsync(first, "state", "auto", false, "alice", "first"),
			ReadAsync(second, "state", "auto", false, "bob", "second"));
		var expected = await ReadAsync(stock, "state", "auto", false, "bob", "second");

		Assert.Contains("\"environmentName\":\"first-app\"", responses[0].Body, StringComparison.Ordinal);
		Assert.Contains("\"environmentName\":\"second-app\"", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected, responses[1]);
	}

	[Theory]
	[InlineData("server")]
	[InlineData("wasm")]
	[InlineData("auto")]
	public async Task Nested_explicit_modes_inherit_the_ancestor_boundary_and_persisted_state_store(string mode)
	{
		await using var stock = await Issue214Host.CreateAsync(false);
		await using var candidate = await Issue214Host.CreateAsync(true);

		var expected = await ReadAsync(stock, "nested", mode, false, "alice", "nested-value");
		var actual = await ReadAsync(candidate, "nested", mode, false, "alice", "nested-value");

		Assert.Equal(HttpStatusCode.OK, expected.Status);
		Assert.Equal(2, Regex.Matches(expected.Body, "<!--Blazor:\\{").Count);
		Assert.Contains("issue214={\"user\":\"alice\",\"value\":\"nested-value\"}", expected.Body, StringComparison.Ordinal);
		Assert.Equal(expected, actual);
	}

	private static async Task<PairedResponse[]> ReadOverlappingPairsAsync(Issue214Host stock, Issue214Host candidate, string mode)
	{
		stock.Overlap.HoldRequests(6);
		candidate.Overlap.HoldRequests(6);
		var requests = Enumerable.Range(2, 6).Select(index => ReadPairAsync(stock, candidate, mode, index)).ToArray();
		var responses = Task.WhenAll(requests);
		try
		{
			var arrivals = await Task.WhenAll(stock.Overlap.WaitForArrivalsAsync(), candidate.Overlap.WaitForArrivalsAsync());
			var expected = Enumerable.Range(2, 6).Select(index => new KeyValuePair<string, string>($"user-{index}", $"value-{index}"));
			Assert.Equal(expected, arrivals[0]);
			Assert.Equal(expected, arrivals[1]);
			Assert.All(requests, request => Assert.False(request.IsCompleted));
		}
		finally
		{
			stock.Overlap.Release();
			candidate.Overlap.Release();
			await responses.WaitAsync(TimeSpan.FromSeconds(20));
		}

		return await responses;
	}

	private static async Task<PairedResponse> ReadPairAsync(Issue214Host stock, Issue214Host candidate, string mode, int index)
	{
		var user = $"user-{index}";
		var value = $"value-{index}";
		var expected = ReadAsync(stock, "state", mode, false, user, value);
		var actual = ReadAsync(candidate, "state", mode, false, user, value);
		await Task.WhenAll(expected, actual);
		return new(await expected, await actual, user, value);
	}

	private static void AssertIsolated(PairedResponse pair)
	{
		Assert.Contains($"\"user\":\"{pair.User}\",\"value\":\"{pair.Value}\"", pair.Expected.Body, StringComparison.Ordinal);
		Assert.Equal(pair.Expected, pair.Actual);
	}

	private static async Task<Response> ReadAsync(Issue214Host host, string path, string mode, bool enhanced, string user, string value)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, $"/issue-214/{path}?mode={mode}&value={value}");
		request.Headers.Add("X-Issue-214-User", user);
		request.Headers.Add("X-Issue-214-Mode", mode);
		request.Headers.Add("X-Issue-214-Value", value);
		if (enhanced)
		{
			request.Headers.TryAddWithoutValidation("Accept", "text/html; blazor-enhanced-nav=on");
		}

		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		var normalized = NormalizeBody(body, host.Protection);
		var headers = string.Join("\n", response.Headers.Concat(response.Content.Headers)
			.OrderBy(header => header.Key, StringComparer.Ordinal)
			.Select(header => $"{header.Key}: {NormalizeHeader(header.Key, string.Join(",", header.Value))}"));
		return new(response.StatusCode, headers, normalized);
	}

	private static string NormalizeBody(string body, IDataProtectionProvider protection)
	{
		var normalized = Regex.Replace(body, "\"(prerenderId|descriptor)\":\"[^\"]+\"", "\"$1\":\"<dynamic>\"");
		normalized = Regex.Replace(normalized, "<!--Blazor-Configuration:(.*?)-->", match =>
			$"<!--Blazor-Configuration:{Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[1].Value))}-->");
		return Regex.Replace(normalized, "<!--Blazor-(Server|WebAssembly)-Component-State:(.*?)-->", match =>
		{
			var bytes = Convert.FromBase64String(match.Groups[2].Value);
			if (match.Groups[1].Value == "Server")
			{
				bytes = protection.CreateProtector("Microsoft.AspNetCore.Components.Server.State").Unprotect(bytes);
			}

			var state = JsonSerializer.Deserialize<SortedDictionary<string, byte[]>>(bytes)!;
			var decoded = string.Join(";", state.Select(item => $"{item.Key}={NormalizePersistedValue(Encoding.UTF8.GetString(item.Value))}"));
			return $"<!--Blazor-{match.Groups[1].Value}-Component-State:{decoded}-->";
		});
	}

	private static string NormalizePersistedValue(string value)
		=> Regex.Replace(value, "\"value\":\"[^\"]+\"(?=,\"formFieldName\":\"__RequestVerificationToken\")", "\"value\":\"<antiforgery-token>\"");

	private static string NormalizeHeader(string name, string value)
		=> name.ToLowerInvariant() switch
		{
			"date" => "<date>",
			"ssr-framing" => "<framing-id>",
			"set-cookie" => Regex.Replace(value, "^([^=]+=)[^;]+", "$1<cookie-value>"),
			_ => value,
		};

	private sealed record Response(HttpStatusCode Status, string Headers, string Body);
	private sealed record PairedResponse(Response Expected, Response Actual, string User, string Value);
}
#endif
