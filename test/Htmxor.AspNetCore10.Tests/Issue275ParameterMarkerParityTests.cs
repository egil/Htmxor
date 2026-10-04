using System.Net;

namespace Htmxor.AspNetCore10;

// #275's parameterized interactive-marker parity cases, compiled for both net10.0 and net11.0 (see the csproj).
// Every case pairs a stock host against an AddHtmxor candidate host built by Issue269Host (its interactive
// Server and WebAssembly render modes also satisfy Auto), requesting Issue275ParametersPage, whose one
// interactive child always carries a string parameter, a null-valued parameter and a complex parameter with a
// null member.
public sealed class Issue275ParameterMarkerParityTests
{
	[Theory]
	[InlineData("webassembly")]
	[InlineData("auto")]
	public async Task Decoded_parameter_definitions_and_values_match_stock_for_a_string_null_and_complex_parameter(string mode)
	{
		await using var stock = await Issue269Host.CreateAsync(htmxor: false);
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true);
		var path = $"/issue-275/parameters/{mode}";

		using var stockResponse = await stock.Client.GetAsync(path);
		using var candidateResponse = await candidate.Client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);

		using var stockMarker = Issue275ComponentMarker.ExtractStartMarker(await stockResponse.Content.ReadAsStringAsync());
		using var candidateMarker = Issue275ComponentMarker.ExtractStartMarker(await candidateResponse.Content.ReadAsStringAsync());

		var stockDefinitions = Issue275ComponentMarker.DecodeBase64Utf8(Issue275ComponentMarker.GetStringField(stockMarker, "parameterDefinitions"));
		var stockValues = Issue275ComponentMarker.DecodeBase64Utf8(Issue275ComponentMarker.GetStringField(stockMarker, "parameterValues"));

		// Stock's own oracle, so this comparison cannot pass vacuously by both hosts sharing one bug: names are
		// camelCase, and Subtitle's own null value means its definition has no typeName or assembly member to
		// write, so stock omits them entirely rather than writing them as null.
		Assert.Contains("\"name\":\"Title\"", stockDefinitions, StringComparison.Ordinal);
		Assert.Contains("\"name\":\"Subtitle\"", stockDefinitions, StringComparison.Ordinal);
		Assert.DoesNotContain("\"Name\"", stockDefinitions, StringComparison.Ordinal);
		Assert.DoesNotContain("\"typeName\":null", stockDefinitions, StringComparison.Ordinal);
		Assert.DoesNotContain("\"assembly\":null", stockDefinitions, StringComparison.Ordinal);
		Assert.Contains("\"label\":\"primary\"", stockValues, StringComparison.Ordinal);
		Assert.DoesNotContain("\"Label\"", stockValues, StringComparison.Ordinal);
		Assert.DoesNotContain("\"note\":null", stockValues, StringComparison.Ordinal);

		var candidateDefinitions = Issue275ComponentMarker.DecodeBase64Utf8(Issue275ComponentMarker.GetStringField(candidateMarker, "parameterDefinitions"));
		var candidateValues = Issue275ComponentMarker.DecodeBase64Utf8(Issue275ComponentMarker.GetStringField(candidateMarker, "parameterValues"));

		Assert.Equal(stockDefinitions, candidateDefinitions);
		Assert.Equal(stockValues, candidateValues);
	}

	[Theory]
	[InlineData("auto")]
	[InlineData("server")]
	public async Task Unprotected_descriptor_matches_stock_verbatim_for_a_string_null_and_complex_parameter(string mode)
	{
		await using var stock = await Issue269Host.CreateAsync(htmxor: false);
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true);
		var path = $"/issue-275/parameters/{mode}";

		using var stockResponse = await stock.Client.GetAsync(path);
		using var candidateResponse = await candidate.Client.GetAsync(path);

		using var stockMarker = Issue275ComponentMarker.ExtractStartMarker(await stockResponse.Content.ReadAsStringAsync());
		using var candidateMarker = Issue275ComponentMarker.ExtractStartMarker(await candidateResponse.Content.ReadAsStringAsync());

		var stockDescriptor = Issue275ComponentMarker.DecodeDescriptor(
			Issue275ComponentMarker.GetStringField(stockMarker, "descriptor"), stock.Protection);

		// Stock's own oracle: the descriptor's own field names are camelCase (ServerComponentSerializationSettings),
		// so this comparison cannot pass vacuously by both hosts sharing one bug.
		Assert.Contains("\"assemblyName\":", stockDescriptor, StringComparison.Ordinal);
		Assert.Contains("\"parameterDefinitions\":", stockDescriptor, StringComparison.Ordinal);
		Assert.DoesNotContain("\"AssemblyName\":", stockDescriptor, StringComparison.Ordinal);

		var candidateDescriptor = Issue275ComponentMarker.DecodeDescriptor(
			Issue275ComponentMarker.GetStringField(candidateMarker, "descriptor"), candidate.Protection);

		Assert.Equal(stockDescriptor, candidateDescriptor);
	}

	[Theory]
	[InlineData("webassembly")]
	[InlineData("auto")]
	[InlineData("server")]
	public async Task Response_matches_stocks_status_headers_and_whole_body_for_a_string_null_and_complex_parameter(string mode)
	{
		await using var stock = await Issue269Host.CreateAsync(htmxor: false);
		await using var candidate = await Issue269Host.CreateAsync(htmxor: true);
		var path = $"/issue-275/parameters/{mode}";

		using var stockResponse = await stock.Client.GetAsync(path);
		using var candidateResponse = await candidate.Client.GetAsync(path);
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var candidateBody = await candidateResponse.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);
		Assert.Contains("data-issue-275-page=\"rendered\"", stockBody, StringComparison.Ordinal);
		Assert.Equal(stockResponse.StatusCode, candidateResponse.StatusCode);
		Assert.Equal(Issue260Snapshot.NormalizeHeaders(stockResponse), Issue260Snapshot.NormalizeHeaders(candidateResponse));

		// The shared Issue272PersistedState decoder already blanks the marker's own random prerenderId and
		// data-protected descriptor to a fixed placeholder (neither can match byte-for-byte across two
		// separately keyed, separately invoked hosts) and unprotects each host's own persisted-state store with
		// that host's own data-protection provider, the same normalization #269's own whole-body comparison
		// relies on.
		Assert.Equal(
			Issue272PersistedState.Decode(stockBody, stock.Protection),
			Issue272PersistedState.Decode(candidateBody, candidate.Protection));
	}
}
