using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(" value")]
	[InlineData("value ")]
	[InlineData("value\ninside")]
	[InlineData("value\u007Finside")]
	public void Swap_and_selection_values_are_validated_before_the_marker_without_response_mutation(
		string? value)
	{
		var operations = new Func<HtmxResponse, string, HtmxResponse>[]
		{
			static (response, candidate) => response.Reswap(candidate),
			static (response, candidate) => response.Retarget(candidate),
			static (response, candidate) => response.Reselect(candidate),
		};

		foreach (var operation in operations)
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

			Assert.ThrowsAny<ArgumentException>(() => operation(response, value!));

			Assert.Multiple(
				() => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
				() => Assert.Equal(expectedHeaders.Count, context.Response.Headers.Count)
			);
			foreach (var expectedHeader in expectedHeaders)
			{
				Assert.Equal(expectedHeader.Value, context.Response.Headers[expectedHeader.Key]);
			}
		}
	}

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
			body.Contains("data-issue-154-swap-selection", StringComparison.Ordinal))
		);
		if (!expectsBody)
		{
			Assert.Equal(string.Empty, body);
		}
	}

}
