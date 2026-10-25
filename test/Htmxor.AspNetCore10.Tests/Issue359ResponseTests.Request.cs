using System.Net;
using System.Reflection;
using Htmxor.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	[Fact]
	public void Valid_core_request_values_preserve_normalized_and_open_text()
	{
		var valid = ParseWireRequest(
			marker: ["true"],
			requestType: ["partial"],
			boosted: ["true"],
			historyRestore: ["true"],
			currentUrl: [" https://EXAMPLE.test/orders?Filter=Open\t"],
			source: [" button#submit\t"],
			target: ["section#result"]);
		Assert.Multiple(
			() => Assert.True(valid.IsHtmxRequest),
			() => Assert.Equal(HtmxRequestType.Partial, valid.RequestType),
			() => Assert.True(valid.IsBoosted),
			() => Assert.True(valid.IsHistoryRestoreRequest),
			() => Assert.Equal(
			"https://EXAMPLE.test/orders?Filter=Open",
			valid.CurrentUrl?.OriginalString),
			() => Assert.Equal("button#submit", valid.Source),
			() => Assert.Equal("section#result", valid.Target)
		);

	}

	[Fact]
	public void Normalized_marker_exposes_the_partial_request()
	{
		var normalizedMarker = ParseWireRequest(marker: ["\ttrue\t"], requestType: ["partial"]);
		Assert.Multiple(
			() => Assert.True(normalizedMarker.IsHtmxRequest),
			() => Assert.Equal(HtmxRequestType.Partial, normalizedMarker.RequestType)
		);
	}

	[Fact]
	public void Normalized_request_type_exposes_partial()
	{
		var normalizedPartial = ParseWireRequest(marker: ["true"], requestType: ["\tpartial\t"]);
		Assert.Equal(HtmxRequestType.Partial, normalizedPartial.RequestType);
	}

	[Fact]
	public void Comma_joined_source_and_target_remain_open_values()
	{
		var commaJoinedOpenValues = ParseWireRequest(
			marker: ["true"],
			requestType: ["partial"],
			source: ["button#source,button#other"],
			target: ["section#target,div#other"]);
		Assert.Multiple(
			() => Assert.Equal("button#source,button#other", commaJoinedOpenValues.Source),
			() => Assert.Equal("section#target,div#other", commaJoinedOpenValues.Target)
		);
	}


	public static IEnumerable<object?[]> InvalidMarkerValues =>
		new string[]?[]
		{
			null,
			[""],
			[" "],
			["false"],
			["TRUE"],
			["true,false"],
			["true\n"],
			["true", "true"],
			["true", "false"],
			[new string('\uD800', 1)],
		}
		.Select(value => new object?[] { value });

	[Theory]
	[MemberData(nameof(InvalidMarkerValues))]
	public void Invalid_marker_hides_all_dependent_request_values(string[]? marker)
	{
		var request = ParseWireRequest(
			marker,
			requestType: ["partial"],
			boosted: ["true"],
			historyRestore: ["true"],
			currentUrl: ["https://example.test/orders"],
			source: ["button#submit"],
			target: ["section#result"]);

		Assert.Multiple(
			() => Assert.False(request.IsHtmxRequest),
			() => Assert.Null(request.RequestType),
			() => Assert.False(request.IsBoosted),
			() => Assert.False(request.IsHistoryRestoreRequest),
			() => Assert.Null(request.CurrentUrl),
			() => Assert.Null(request.Source),
			() => Assert.Null(request.Target)
		);
	}

	public static IEnumerable<object?[]> InvalidRequestTypeValues =>
		new string[]?[]
		{
			null,
			[""],
			["unknown"],
			["partial,full"],
			["partial\n"],
			["partial", "full"],
			[new string('\uD800', 1)],
		}
		.Select(value => new object?[] { value });

	[Theory]
	[MemberData(nameof(InvalidRequestTypeValues))]
	public void Invalid_request_type_is_not_exposed(string[]? requestType)
	{
		var request = ParseWireRequest(marker: ["true"], requestType: requestType);

		Assert.Multiple(
			() => Assert.True(request.IsHtmxRequest),
			() => Assert.Null(request.RequestType)
		);
	}

	public static IEnumerable<object?[]> InvalidBooleanValues =>
		new string[]?[]
		{
			null,
			[""],
			["false"],
			["TRUE"],
			["true,false"],
			["true\n"],
			["true", "true"],
			["true", "false"],
			[new string('\uD800', 1)],
		}
		.Select(value => new object?[] { value });

	[Theory]
	[MemberData(nameof(InvalidBooleanValues))]
	public void Invalid_boolean_values_fail_closed(string[]? booleanValue)
	{
		var request = ParseWireRequest(
			marker: ["true"],
			requestType: ["full"],
			boosted: booleanValue,
			historyRestore: booleanValue);

		Assert.Multiple(
			() => Assert.False(request.IsBoosted),
			() => Assert.False(request.IsHistoryRestoreRequest)
		);
	}

	public static IEnumerable<object?[]> InvalidCurrentUrlValues =>
		new string[]?[]
		{
			null,
			[""],
			[" /orders"],
			["ftp://example.test/orders"],
			["https://example.test/orders\n"],
			["https://example.test/orders", "https://example.test/orders"],
			[new string('\uD800', 1)],
		}
		.Select(value => new object?[] { value });

	[Theory]
	[MemberData(nameof(InvalidCurrentUrlValues))]
	public void Invalid_current_url_is_not_exposed(string[]? currentUrl)
	{
		var request = ParseWireRequest(marker: ["true"], requestType: ["full"], currentUrl: currentUrl);

		Assert.Null(request.CurrentUrl);
	}

	public static IEnumerable<object?[]> InvalidOpenValues =>
		new string[]?[]
		{
			null,
			[""],
			[" "],
			["button\n#submit"],
			["button#submit", "button#submit"],
			[new string('\uD800', 1)],
		}
		.Select(value => new object?[] { value });

	[Theory]
	[MemberData(nameof(InvalidOpenValues))]
	public void Invalid_source_and_target_values_are_not_exposed(string[]? openValue)
	{
		var request = ParseWireRequest(
			marker: ["true"],
			requestType: ["partial"],
			source: openValue,
			target: openValue);

		Assert.Multiple(
			() => Assert.Null(request.Source),
			() => Assert.Null(request.Target)
		);
	}

	[Theory]
	[MemberData(nameof(InvalidHtmxRequestMarkers))]
	public async Task Invalid_marker_cannot_reach_an_htmx_only_route(
		string _,
		string[]? markerValues)
	{
		using var request = CreateMarkerRequest(ReportRequestPath, markerValues);
		request.Headers.Add(HtmxRequestHeaderNames.CurrentUrl, "https://example.test/orders");
		request.Headers.Add(HtmxRequestHeaderNames.Target, "section#patch-result");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-97-report-component", body, StringComparison.Ordinal)
		);
	}

	[Theory]
	[MemberData(nameof(InvalidHtmxRequestMarkers))]
	public async Task Invalid_marker_keeps_the_stock_page_and_rejects_response_operations_without_mutation(
		string _,
		string[]? markerValues)
	{
		using var request = CreateMarkerRequest(
			MarkerPageRequestPath + "?guard-response=true",
			markerValues);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase),
			() => Assert.Contains("data-stock-shell", body, StringComparison.Ordinal),
			() => Assert.Contains("data-issue-154-marker", body, StringComparison.Ordinal),
			() => Assert.Contains("data-rejected-operation-count=\"16\"", body, StringComparison.Ordinal),
			() => Assert.Contains("data-operation-count=\"16\"", body, StringComparison.Ordinal),
			() => Assert.All(
			typeof(HtmxResponseHeaderNames).GetFields(BindingFlags.Public | BindingFlags.Static),
			field => Assert.False(response.Headers.Contains(
				Assert.IsType<string>(field.GetRawConstantValue()))))
		);
	}

	[Fact]
	public async Task One_normalized_true_marker_retains_direct_representation()
	{
		using var request = CreateMarkerRequest(MarkerPageRequestPath, [" true "]);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains("data-issue-154-marker", body, StringComparison.Ordinal),
			() => Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase),
			() => Assert.DoesNotContain("data-stock-shell", body, StringComparison.Ordinal)
		);
	}

	[Theory]
	[MemberData(nameof(InvalidCoreRequestHeaderWireCases))]
	public async Task Invalid_core_request_headers_fail_closed_through_TestServer(
		InvalidCoreRequestHeaderWireCase testCase)
	{
		using var request = CreateWireProbeRequest(testCase.HeaderName, testCase.Values);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		var expected = testCase.Expected;
		var direct = string.Equals(expected.RoutingMode, "Direct", StringComparison.Ordinal);

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"{testCase.CaseName}: expected an OK response, got {response.StatusCode}.\n{body}"),
			() => Assert.Contains(
			"data-issue-154-wire-probe",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-is-htmx-request=\"true\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-request-type=\"{expected.RequestType}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-routing-mode=\"{expected.RoutingMode}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-is-boosted=\"{expected.IsBoosted}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-is-history-restore=\"{expected.IsHistoryRestore}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-current-url=\"{expected.CurrentUrl}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-source=\"{expected.Source}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Contains(
			$"data-target=\"{expected.Target}\"",
			body,
			StringComparison.Ordinal),
			() => Assert.Equal(direct, body.Contains("data-stock-shell", StringComparison.Ordinal) is false),
			() => Assert.Equal(direct, body.Contains("<html", StringComparison.OrdinalIgnoreCase) is false)
		);

	}

}
