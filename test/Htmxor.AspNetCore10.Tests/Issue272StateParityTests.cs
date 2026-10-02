using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

// #272's non-streamed render-mode-inference parity cases, compiled for both net10.0 and net11.0 (see the
// csproj). Every case pairs a stock host against an AddHtmxor candidate host built by the shared Issue260Host,
// configured with both interactive Server and WebAssembly so persistence goes through the composite store
// (HtmxorEndpointCandidateRenderer.WritePersistedStateAsync's multi-mode branch). Each payload is decoded by
// Issue272PersistedState rather than blanked, so a dropped or misrouted entry is visible instead of hidden
// behind an opaque blob, unlike Issue191PersistedStateParityTests' own NormalizeDynamicState. Three shapes
// are covered: a usage-site boundary's own direct child, a component with its own explicit mode nested
// deeper than that, and a plain component with no render mode of its own nested deeper still.
public sealed class Issue272StateParityTests
{
	[Theory]
	[InlineData("server")]
	[InlineData("wasm")]
	[InlineData("auto")]
	public async Task Usage_site_rendermode_child_persists_its_own_state_like_stock(string mode)
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		var stockResult = await ReadAsync(stock, "/issue-272/state", mode);
		var candidateResult = await ReadAsync(candidate, "/issue-272/state", mode);

		Assert.Equal(HttpStatusCode.OK, stockResult.Status);

		// Stock's own oracle: the child's entry is filed under the selected mode's own store(s), so neither a
		// dropped entry nor a fixture that ignored the mode header can hide behind the equality assertion
		// below.
		AssertEntryInModeStore(stockResult.DecodedBody, mode, Entry("issue272-probe"));

		Assert.Equal(stockResult.Status, candidateResult.Status);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(stockResult.DecodedBody, candidateResult.DecodedBody);
	}

	[Theory]
	[InlineData("server")]
	[InlineData("wasm")]
	[InlineData("auto")]
	public async Task Nested_explicit_rendermode_child_joins_the_ancestor_boundary_like_stock(string mode)
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		var stockResult = await ReadAsync(stock, "/issue-272/nested", mode);
		var candidateResult = await ReadAsync(candidate, "/issue-272/nested", mode);

		Assert.Equal(HttpStatusCode.OK, stockResult.Status);

		// Stock's own oracle: exactly one boundary (its open and close marker) and the child's entry filed
		// under the ancestor's own mode -- never the fixed InteractiveWebAssembly mode declared at
		// Issue272NestedContent's own usage site -- is what "joins the ancestor boundary instead of opening a
		// second one" means observably, for every ancestor mode.
		Assert.Equal(2, Regex.Matches(stockResult.DecodedBody, "<!--Blazor:\\{").Count);
		AssertEntryInModeStore(stockResult.DecodedBody, mode, Entry("issue272-probe"));

		Assert.Equal(stockResult.Status, candidateResult.Status);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(stockResult.DecodedBody, candidateResult.DecodedBody);
	}

	[Theory]
	[InlineData("server")]
	[InlineData("wasm")]
	[InlineData("auto")]
	public async Task Deep_plain_descendant_without_its_own_rendermode_persists_its_own_state_like_stock(string mode)
	{
		await using var stock = await CreateHostAsync(htmxor: false);
		await using var candidate = await CreateHostAsync(htmxor: true);

		var stockResult = await ReadAsync(stock, "/issue-272/deep", mode);
		var candidateResult = await ReadAsync(candidate, "/issue-272/deep", mode);

		Assert.Equal(HttpStatusCode.OK, stockResult.Status);

		// Stock's own oracle, same shape as the other two cases. Issue272Child here carries no render mode of
		// its own and sits two levels below Issue272DeepWrapper, the boundary's own direct child -- the shape
		// a fix that only widens the immediate-parent check (rather than walking to the closest ancestor
		// boundary) still drops.
		AssertEntryInModeStore(stockResult.DecodedBody, mode, Entry("issue272-deep-probe"));

		Assert.Equal(stockResult.Status, candidateResult.Status);
		Assert.Equal(stockResult.Headers, candidateResult.Headers);
		Assert.Equal(stockResult.DecodedBody, candidateResult.DecodedBody);
	}

	private static string Entry(string persistenceKey) => $"{persistenceKey}=\"issue272-persisted\"";

	private static void AssertEntryInModeStore(string decodedBody, string mode, string persistedEntry)
	{
		var serverStore = ExtractStore(decodedBody, "Server");
		var webAssemblyStore = ExtractStore(decodedBody, "WebAssembly");
		switch (mode)
		{
			case "server":
				Assert.Contains(persistedEntry, serverStore, StringComparison.Ordinal);
				Assert.DoesNotContain(persistedEntry, webAssemblyStore, StringComparison.Ordinal);
				break;
			case "wasm":
				Assert.DoesNotContain(persistedEntry, serverStore, StringComparison.Ordinal);
				Assert.Contains(persistedEntry, webAssemblyStore, StringComparison.Ordinal);
				break;
			default:
				Assert.Contains(persistedEntry, serverStore, StringComparison.Ordinal);
				Assert.Contains(persistedEntry, webAssemblyStore, StringComparison.Ordinal);
				break;
		}
	}

	private static string ExtractStore(string decodedBody, string store)
	{
		var match = Regex.Match(decodedBody, $"<!--Blazor-{store}-Component-State:(.*?)-->");
		return match.Success ? match.Groups[1].Value : string.Empty;
	}

	private static Task<Issue260Host> CreateHostAsync(bool htmxor)
		=> Issue260Host.CreateAsync<Issue272App>(
			htmxor,
			configureRazorComponents: builder =>
			{
				builder.AddInteractiveServerComponents();
				builder.AddInteractiveWebAssemblyComponents();
			},
			configureEndpoints: endpoints =>
			{
				endpoints.AddInteractiveServerRenderMode();
				endpoints.AddInteractiveWebAssemblyRenderMode();
			});

	private static async Task<Response> ReadAsync(Issue260Host host, string path, string mode)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("X-Issue-272-Mode", mode);
		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		var decoded = Issue272PersistedState.Decode(body, host.Services.GetRequiredService<IDataProtectionProvider>());
		var headers = string.Join("\n", response.Headers.Concat(response.Content.Headers)
			.OrderBy(header => header.Key, StringComparer.Ordinal)
			.Select(header => $"{header.Key}: {NormalizeHeader(header.Key, string.Join(",", header.Value))}"));
		return new(response.StatusCode, headers, decoded);
	}

	// Date varies on every response. The framework's antiforgery state provider calls
	// IAntiforgery.GetAndStoreTokens when interactive persisted state is written, which issues an
	// ".AspNetCore.Antiforgery.*" cookie (not something either fixture page persists itself) whose value is
	// random per request and keyed per host, exactly like Issue214StateParityTests' own NormalizeHeader. No
	// other header varies for these non-streamed, session-free pages.
	private static string NormalizeHeader(string name, string value)
		=> name.ToLowerInvariant() switch
		{
			"date" => "<date>",
			"set-cookie" => Regex.Replace(value, "^([^=]+=)[^;]+", "$1<cookie-value>"),
			_ => value,
		};

	private sealed record Response(HttpStatusCode Status, string Headers, string DecodedBody);
}
