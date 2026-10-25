using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	public static IEnumerable<object?[]> InvalidNavigationDestinations()
	{
		foreach (var destination in new string?[] { null, "", " ", " /orders/42", "/orders/42 ", "/orders/\n42", "http://[::1" })
			foreach (var operation in new[] { "location", "push", "redirect", "replace" })
				yield return new object?[] { operation, destination };
	}

	[Theory]
	[MemberData(nameof(InvalidNavigationDestinations))]
	public void Navigation_destinations_are_validated_before_the_marker_guard_without_mutation(string operation, string? destination)
	{
		var context = CreateNavigationContext(validMarker: false);
		var response = context.GetHtmxContext().Response;
		Assert.ThrowsAny<ArgumentException>(() => ApplyNavigation(response, operation, destination!));
		AssertNavigationStateUnchanged(context);
	}

	[Theory]
	[InlineData("push", "true")]
	[InlineData("push", "false")]
	[InlineData("replace", "true")]
	[InlineData("replace", "false")]
	[InlineData("location", "https://other.example/orders/42")]
	[InlineData("push", "https://other.example/orders/42")]
	[InlineData("replace", "https://other.example/orders/42")]
	[InlineData("location", "//other.example/orders/42")]
	[InlineData("push", "//other.example/orders/42")]
	[InlineData("replace", "//other.example/orders/42")]
	[InlineData("location", "ftp://reports.internal.example/orders/42")]
	[InlineData("push", "ftp://reports.internal.example/orders/42")]
	[InlineData("replace", "ftp://reports.internal.example/orders/42")]
	[InlineData("redirect", "mailto:user@example.com")]
	public void Navigation_destinations_enforce_history_scheme_and_origin_rules(string operation, string destination)
	{
		AssertRejectedNavigation(response => ApplyNavigation(response, operation, destination));
	}

	[Theory]
	[InlineData("push", "TRUE")]
	[InlineData("push", "False")]
	[InlineData("replace", "TRUE")]
	[InlineData("replace", "False")]
	public void Case_distinct_history_destinations_remain_relative_references(string operation, string destination)
	{
		var context = CreateNavigationContext(validMarker: true);
		var response = context.GetHtmxContext().Response;
		Assert.Multiple(
			() => Assert.Same(response, ApplyNavigation(response, operation, destination)),
			() => Assert.Equal(destination, Assert.Single(GetNavigationHeaders(context)).Value.ToString()));
	}

	[Fact]
	public void Redirect_allows_an_external_http_destination()
	{
		var context = CreateNavigationContext(validMarker: true);
		var response = context.GetHtmxContext().Response;
		Assert.Multiple(
			() => Assert.Same(response, response.Redirect("https://idp.example/login")),
			() => Assert.Equal("https://idp.example/login", context.Response.Headers[HtmxResponseHeaderNames.Redirect]));
	}

	[Theory]
	[InlineData("push")]
	[InlineData("redirect")]
	[InlineData("replace")]
	public void Navigation_operations_preserve_Uri_text(string operation)
	{
		var destination = new Uri("https://REPORTS.internal.example:443/a/../orders%20archive", UriKind.Absolute);
		Assert.NotEqual(destination.OriginalString, destination.ToString());
		var context = CreateNavigationContext(validMarker: true);
		var response = context.GetHtmxContext().Response;
		Assert.Multiple(
			() => Assert.Same(response, ApplyUriNavigation(response, operation, destination)),
			() => Assert.Equal(destination.OriginalString, Assert.Single(GetNavigationHeaders(context)).Value.ToString()));
	}

	[Fact]
	public void Navigation_operations_use_one_last_call_wins_header()
	{
		var context = CreateNavigationContext(validMarker: true);
		var response = context.GetHtmxContext().Response;
		var result = response.Location("/location").Redirect("/redirect").Refresh().PushUrl("/push")
			.ReplaceUrl("/replace").PreventBrowserHistoryUpdate().PreventBrowserCurrentUrlUpdate();
		var header = Assert.Single(GetNavigationHeaders(context));
		Assert.Multiple(
			() => Assert.Same(response, result),
			() => Assert.Equal(HtmxResponseHeaderNames.ReplaceUrl, header.Key),
			() => Assert.Equal("false", header.Value),
			() => Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode));
	}

	private static HtmxResponse ApplyNavigation(HtmxResponse response, string operation, string destination) => operation switch
	{
		"location" => response.Location(destination),
		"push" => response.PushUrl(destination),
		"redirect" => response.Redirect(destination),
		"replace" => response.ReplaceUrl(destination),
		_ => throw new ArgumentOutOfRangeException(nameof(operation)),
	};

	private static HtmxResponse ApplyUriNavigation(HtmxResponse response, string operation, Uri destination) => operation switch
	{
		"push" => response.PushUrl(destination),
		"redirect" => response.Redirect(destination),
		"replace" => response.ReplaceUrl(destination),
		_ => throw new ArgumentOutOfRangeException(nameof(operation)),
	};

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
			body.Contains("data-issue-154-navigation", StringComparison.Ordinal)),
			() => { if (!expectsBody) Assert.Equal(string.Empty, body); }
		);
	}

}
