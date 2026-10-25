using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	[Fact]
	public void Navigation_destinations_are_validated_before_the_marker_guard_without_mutation()
	{
		string?[] invalidDestinations =
		[
			null,
			"",
			" ",
			" /orders/42",
			"/orders/42 ",
			"/orders/\n42",
			"http://[::1",
		];

		foreach (var destination in invalidDestinations)
		{
			foreach (var operation in GetNavigationOperations(destination!))
			{
				var context = CreateNavigationContext(validMarker: false);
				var response = context.GetHtmxContext().Response;

				Assert.ThrowsAny<ArgumentException>(() => operation(response));
				AssertNavigationStateUnchanged(context);
			}
		}
	}

	[Fact]
	public void Navigation_destinations_enforce_history_scheme_and_origin_rules()
	{
		foreach (var literal in new[] { "true", "false" })
		{
			AssertRejectedNavigation(response => response.PushUrl(literal));
			AssertRejectedNavigation(response => response.ReplaceUrl(literal));
		}

		foreach (var relativeReference in new[] { "TRUE", "False" })
		{
			foreach (var operation in new Func<HtmxResponse, HtmxResponse>[]
			{
				response => response.PushUrl(relativeReference),
				response => response.ReplaceUrl(relativeReference),
			})
			{
				var context = CreateNavigationContext(validMarker: true);
				var response = context.GetHtmxContext().Response;

				Assert.Multiple(
					() => Assert.Same(response, operation(response)),
					() => Assert.Equal(
					relativeReference,
					Assert.Single(GetNavigationHeaders(context)).Value.ToString())
				);
			}
		}

		foreach (var destination in new[]
		{
			"https://other.example/orders/42",
			"//other.example/orders/42",
			"ftp://reports.internal.example/orders/42",
		})
		{
			AssertRejectedNavigation(response => response.Location(destination));
			AssertRejectedNavigation(response => response.PushUrl(destination));
			AssertRejectedNavigation(response => response.ReplaceUrl(destination));
		}

		var redirectContext = CreateNavigationContext(validMarker: true);
		var redirectResponse = redirectContext.GetHtmxContext().Response;
		Assert.Multiple(
			() => Assert.Same(redirectResponse, redirectResponse.Redirect("https://idp.example/login")),
			() => Assert.Equal(
			"https://idp.example/login",
			redirectContext.Response.Headers[HtmxResponseHeaderNames.Redirect])
		);

		AssertRejectedNavigation(response => response.Redirect("mailto:user@example.com"));
	}

	[Fact]
	public void Navigation_operations_preserve_Uri_text_and_use_one_last_call_wins_header()
	{
		var destination = new Uri(
			"https://REPORTS.internal.example:443/a/../orders%20archive",
			UriKind.Absolute);
		Assert.NotEqual(destination.OriginalString, destination.ToString());

		foreach (var operation in new Func<HtmxResponse, Uri, HtmxResponse>[]
		{
			(response, uri) => response.PushUrl(uri),
			(response, uri) => response.Redirect(uri),
			(response, uri) => response.ReplaceUrl(uri),
		})
		{
			var context = CreateNavigationContext(validMarker: true);
			var response = context.GetHtmxContext().Response;

			Assert.Multiple(
				() => Assert.Same(response, operation(response, destination)),
				() => Assert.Equal(destination.OriginalString, Assert.Single(GetNavigationHeaders(context)).Value.ToString())
			);
		}

		var overwriteContext = CreateNavigationContext(validMarker: true);
		var overwriteResponse = overwriteContext.GetHtmxContext().Response;
		var result = overwriteResponse
			.Location("/location")
			.Redirect("/redirect")
			.Refresh()
			.PushUrl("/push")
			.ReplaceUrl("/replace")
			.PreventBrowserHistoryUpdate()
			.PreventBrowserCurrentUrlUpdate();

		Assert.Same(overwriteResponse, result);
		var header = Assert.Single(GetNavigationHeaders(overwriteContext));
		Assert.Multiple(
			() => Assert.Equal(HtmxResponseHeaderNames.ReplaceUrl, header.Key),
			() => Assert.Equal("false", header.Value),
			() => Assert.Equal(StatusCodes.Status200OK, overwriteContext.Response.StatusCode)
		);
	}

	[Theory]
	[InlineData("location", HtmxResponseHeaderNames.Location, "/orders/42", false)]
	[InlineData(
		"location-uri",
		HtmxResponseHeaderNames.Location,
		"http://REPORTS.internal.example:80/a/../orders%20archive",
		false)]
	[InlineData("push", HtmxResponseHeaderNames.PushUrl, "/orders?page=2", true)]
	[InlineData("prevent-push", HtmxResponseHeaderNames.PushUrl, "false", true)]
	[InlineData("redirect", HtmxResponseHeaderNames.Redirect, "/login", false)]
	[InlineData("refresh", HtmxResponseHeaderNames.Refresh, "true", false)]
	[InlineData("replace", HtmxResponseHeaderNames.ReplaceUrl, "?page=2", true)]
	[InlineData("prevent-replace", HtmxResponseHeaderNames.ReplaceUrl, "false", true)]
	[InlineData("redirect-then-replace", HtmxResponseHeaderNames.ReplaceUrl, "?page=2", true)]
	[InlineData("replace-then-redirect", HtmxResponseHeaderNames.Redirect, "/login", false)]
	[InlineData("empty-then-replace", HtmxResponseHeaderNames.ReplaceUrl, "?page=2", false)]
	public async Task Navigation_component_applies_one_header_and_the_documented_body_effect(
		string operation,
		string expectedHeader,
		string expectedValue,
		bool expectsBody)
	{
		using var request = CreateMarkerRequest(
			$"{NavigationPageRequestPath}?operation={operation}",
			["true"]);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
		var header = Assert.Single(response.Headers, header =>
			NavigationHeaderNames.Contains(header.Key, StringComparer.OrdinalIgnoreCase));
		Assert.Multiple(
			() => Assert.Equal(expectedHeader, header.Key, StringComparer.OrdinalIgnoreCase),
			() => Assert.Equal(expectedValue, Assert.Single(header.Value)),
			() => Assert.Equal(
			expectsBody,
			body.Contains("data-issue-154-navigation", StringComparison.Ordinal))
		);
		if (!expectsBody)
		{
			Assert.Equal(string.Empty, body);
		}
	}

}
