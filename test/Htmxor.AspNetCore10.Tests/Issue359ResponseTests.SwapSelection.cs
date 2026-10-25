using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	public static IEnumerable<object?[]> InvalidSwapSelectionValues()
	{
		foreach (var value in new string?[] { null, "", " ", " value", "value ", "value\ninside", "value\u007Finside" })
			foreach (var operation in new[] { "reswap", "retarget", "reselect" })
				yield return new object?[] { operation, value };
	}

	[Theory]
	[MemberData(nameof(InvalidSwapSelectionValues))]
	public void Swap_and_selection_values_are_validated_before_the_marker_without_response_mutation(string operation, string? value)
	{
		var context = CreateNavigationContext(validMarker: false);
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers["X-Application"] = "retained";
		context.Response.Headers.Append(HtmxResponseHeaderNames.Reswap, "existing-reswap");
		context.Response.Headers.Append(HtmxResponseHeaderNames.Retarget, "existing-retarget");
		context.Response.Headers.Append(HtmxResponseHeaderNames.Reselect, "existing-reselect");
		var expectedHeaders = context.Response.Headers.ToDictionary(
			static header => header.Key,
			static header => header.Value,
			StringComparer.OrdinalIgnoreCase);
		var response = context.GetHtmxContext().Response;

		Assert.ThrowsAny<ArgumentException>(() => ApplySwapSelection(response, operation, value!));

		Assert.Multiple(
			() => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
			() => Assert.Equal(expectedHeaders.Count, context.Response.Headers.Count),
			() => Assert.Equal(expectedHeaders, context.Response.Headers.ToDictionary(header => header.Key, header => header.Value, StringComparer.OrdinalIgnoreCase)));
	}

	private static HtmxResponse ApplySwapSelection(HtmxResponse response, string operation, string value) => operation switch
	{
		"reswap" => response.Reswap(value),
		"retarget" => response.Retarget(value),
		"reselect" => response.Reselect(value),
		_ => throw new ArgumentOutOfRangeException(nameof(operation)),
	};

	[Fact]
	public void Swap_and_selection_operations_preserve_exact_open_values_and_coexist()
	{
		var context = CreateNavigationContext(validMarker: true);
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers["X-Application"] = "retained";
		context.Response.Headers.Append(HtmxResponseHeaderNames.Reswap, "discarded-reswap");
		context.Response.Headers.Append(HtmxResponseHeaderNames.Retarget, "discarded-retarget");
		context.Response.Headers.Append(HtmxResponseHeaderNames.Reselect, "discarded-reselect");
		var response = context.GetHtmxContext().Response;

		var result = response
			.Reswap("innerHTML")
			.Reswap("acmeMorph settle:25ms")
			.Retarget("#discarded-target")
			.Retarget("closest [data-acme-target]")
			.Reselect("#discarded-selection")
			.Reselect(":scope > [data-acme-selection]");

		Assert.Multiple(
			() => Assert.Same(response, result),
			() => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
			() => Assert.Equal("retained", context.Response.Headers["X-Application"]),
			() => Assert.Equal(
			"acmeMorph settle:25ms",
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Reswap])),
			() => Assert.Equal(
			"closest [data-acme-target]",
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Retarget])),
			() => Assert.Equal(
			":scope > [data-acme-selection]",
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Reselect]))
		);
	}

	[Theory]
	[InlineData("retain", true, false)]
	[InlineData("empty", false, false)]
	[InlineData("navigation", false, true)]
	public async Task Swap_and_selection_component_preserves_status_body_and_other_headers(
		string operation,
		bool expectsBody,
		bool expectsNavigation)
	{
		using var request = CreateMarkerRequest(
			$"{SwapSelectionPageRequestPath}?operation={operation}",
			["true"]);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode),
			() => Assert.Equal(
			"acmeMorph settle:25ms",
			Assert.Single(response.Headers.GetValues(HtmxResponseHeaderNames.Reswap))),
			() => Assert.Equal(
			"closest [data-issue-154-target]",
			Assert.Single(response.Headers.GetValues(HtmxResponseHeaderNames.Retarget))),
			() => Assert.Equal(
			":scope > [data-issue-154-selection]",
			Assert.Single(response.Headers.GetValues(HtmxResponseHeaderNames.Reselect))),
			() => Assert.Equal(
			expectsNavigation,
			response.Headers.Contains(HtmxResponseHeaderNames.Location)),
			() => Assert.Equal(
			expectsBody,
			body.Contains("data-issue-154-swap-selection", StringComparison.Ordinal)),
			() => { if (!expectsBody) Assert.Equal(string.Empty, body); }
		);
	}

}
