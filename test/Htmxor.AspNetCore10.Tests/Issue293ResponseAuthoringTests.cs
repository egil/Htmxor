using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Htmxor.AspNetCore10;

public sealed class HtmxResponseAuthoringTests
{
	public static TheoryData<string, string?, string> NavigationCases()
	{
		string?[] destinations =
		[
			null, "", " ", " /orders/42", "/orders/42 ", "/orders/\n42",
			"https://app.example/caf\u00e9", "http://[::1", "true", "false", "TRUE", "False",
			"/orders/42", "?page=2", "../orders/%7E42?next=/a/../b",
			"https://APP.example:443/orders/42", "https://other.example/orders/42",
			"http://app.example/orders/42", "https://app.example:444/orders/42",
			"//other.example/orders/42", "ftp://app.example/orders/42", "mailto:user@example.com",
		];
		var cases = new TheoryData<string, string?, string>();
		foreach (var operation in new[] { "Location", "PushUrl", "ReplaceUrl", "Redirect" })
		{
			foreach (var destination in destinations)
			{
				foreach (var seed in new[] { "visible", "empty", "navigation", "status" })
				{
					cases.Add(operation, destination, seed);
				}
			}
		}

		return cases;
	}

	[Theory]
	[MemberData(nameof(NavigationCases))]
	public void Try_navigation_matches_throwing_wire_contract_and_preserves_state_on_rejection(
		string operation, string? destination, string seed)
	{
		var expected = CreateContext();
		var actual = CreateContext();
		Seed(expected, seed);
		Seed(actual, seed);
		var expectedResponse = expected.GetHtmxContext().Response;
		var actualResponse = actual.GetHtmxContext().Response;
		var before = Capture(actual, actualResponse);
		var throwing = RequireOperation<Func<HtmxResponse, string, HtmxResponse>>(operation, typeof(string));
		var attempt = RequireOperation<Func<HtmxResponse, string, bool>>("Try" + operation, typeof(string));

		var rejection = Record.Exception(() => throwing(expectedResponse, destination!));
		var accepted = attempt(actualResponse, destination!);

		Assert.Equal(rejection is null, accepted);
		if (rejection is null)
		{
			AssertEquivalent(expected, expectedResponse, actual, actualResponse);
		}
		else
		{
			Assert.IsAssignableFrom<ArgumentException>(rejection);
			AssertUnchanged(before, actual, actualResponse);
			expectedResponse.Trigger("after-rejection");
			actualResponse.Trigger("after-rejection");
			AssertEquivalent(expected, expectedResponse, actual, actualResponse);
		}
	}

	public static TheoryData<string, string?[]> MarkerCases()
	{
		var cases = new TheoryData<string, string?[]>();
		foreach (var operation in new[] { "TryLocation", "TryPushUrl", "TryReplaceUrl", "TryRedirect" })
		{
			foreach (var marker in new string?[][] { [], [""], [" "], ["false"], ["TRUE"], ["invalid"], ["true,false"], ["true", "true"], ["\rtrue\r"] })
			{
				cases.Add(operation, marker);
			}
		}

		return cases;
	}

	[Theory]
	[MemberData(nameof(MarkerCases))]
	public void Try_navigation_retains_strict_marker_guard_without_mutation(string operation, string?[] marker)
	{
		var context = CreateContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = new StringValues(marker);
		var response = context.GetHtmxContext().Response;
		var before = Capture(context, response);
		var attempt = RequireOperation<Func<HtmxResponse, string, bool>>(operation, typeof(string));

		Assert.Throws<InvalidOperationException>(() => attempt(response, "/orders/42"));

		AssertUnchanged(before, context, response);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("false")]
	[InlineData("TRUE")]
	[InlineData("true")]
	public void Integer_status_matches_enum_on_any_request(string? marker)
	{
		var apply = RequireOperation<Func<HtmxResponse, int, HtmxResponse>>("StatusCode", typeof(int));
		foreach (var status in new[] { 200, 202, 204, 286, 300, 304, 305, 306, 404, StatusCodes.Status422UnprocessableEntity })
		{
			var expected = CreateContext(marker);
			var actual = CreateContext(marker);
			var expectedResponse = expected.GetHtmxContext().Response;
			var actualResponse = actual.GetHtmxContext().Response;

			expectedResponse.StatusCode((HttpStatusCode)status);
			Assert.Same(actualResponse, apply(actualResponse, status));

			Assert.Equal(status, actual.Response.StatusCode);
			AssertEquivalent(expected, expectedResponse, actual, actualResponse);
		}
	}

	[Theory]
	[InlineData(301)]
	[InlineData(302)]
	[InlineData(303)]
	[InlineData(307)]
	[InlineData(308)]
	public void Integer_redirect_status_rejects_with_navigation_guidance_and_no_mutation(int status)
	{
		var apply = RequireOperation<Func<HtmxResponse, int, HtmxResponse>>("StatusCode", typeof(int));
		foreach (var seed in new[] { "visible", "empty", "navigation", "status" })
		{
			var context = CreateContext();
			Seed(context, seed);
			var response = context.GetHtmxContext().Response;
			var before = Capture(context, response);

			var exception = Assert.Throws<ArgumentException>(() => apply(response, status));
			var enumException = Assert.Throws<ArgumentException>(() => response.StatusCode((HttpStatusCode)status));

			Assert.Equal(enumException.ParamName, exception.ParamName);
			Assert.Contains("Location(", exception.Message, StringComparison.Ordinal);
			Assert.Contains("Redirect(", exception.Message, StringComparison.Ordinal);
			AssertUnchanged(before, context, response);
		}
	}

	[Theory]
	[InlineData("HX-Location", "Location(")]
	[InlineData("HX-Push-Url", "PushUrl(")]
	[InlineData("HX-Replace-Url", "ReplaceUrl(")]
	[InlineData("HX-Redirect", "Redirect(")]
	[InlineData("HX-Refresh", "Refresh(")]
	[InlineData("HX-Reswap", "Reswap(")]
	[InlineData("HX-Retarget", "Retarget(")]
	[InlineData("HX-Reselect", "Reselect(")]
	[InlineData("hx-trigger", "Trigger(")]
	[InlineData("HXOR-Private", "reserved")]
	[InlineData("hxor-private", "reserved")]
	[InlineData("X-Application", "HttpContext.Response.Headers")]
	public void Extension_header_name_errors_identify_the_cause_and_alternative(string name, string guidance)
	{
		var context = CreateContext();
		Seed(context, "empty");
		var response = context.GetHtmxContext().Response;
		var before = Capture(context, response);

		var exception = Assert.Throws<ArgumentException>(() => response.SetExtensionHeader(name, "value"));

		Assert.Equal("name", exception.ParamName);
		Assert.Contains(guidance, exception.Message, StringComparison.OrdinalIgnoreCase);
		AssertUnchanged(before, context, response);
	}

	[Theory]
	[InlineData("Location", typeof(string), "url")]
	[InlineData("Location", typeof(Uri), "url")]
	[InlineData("Reswap", typeof(string), "value")]
	public void Authoring_parameter_names_describe_the_complete_value(string operation, Type parameterType, string expected)
	{
		var method = typeof(HtmxResponse).GetMethod(operation, [parameterType]);

		Assert.NotNull(method);
		Assert.Equal(expected, Assert.Single(method.GetParameters()).Name);
	}

	private static T RequireOperation<T>(string name, Type parameterType) where T : Delegate
	{
		var method = typeof(HtmxResponse).GetMethod(name, [parameterType]);
		Assert.NotNull(method);
		return method.CreateDelegate<T>();
	}

	private static DefaultHttpContext CreateContext(string? marker = "true")
	{
		var context = new DefaultHttpContext();
		context.Request.Scheme = "https";
		context.Request.Host = new HostString("app.example");
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = marker;
		return context;
	}

	private static void Seed(HttpContext context, string seed)
	{
		var response = context.GetHtmxContext().Response;
		response.StatusCode(HttpStatusCode.Accepted).SelectFragment("retained-fragment");
		response.Trigger("retained-event");
		context.Response.Headers.Append("X-Application", new StringValues(["first", "second"]));
		context.Items["application"] = "retained";
		switch (seed)
		{
			case "empty": response.EmptyBody(); break;
			case "navigation": response.Location("/existing-location"); break;
			case "status": response.StatusCode(HttpStatusCode.NoContent); break;
		}
	}

	private static State Capture(HttpContext context, HtmxResponse response) => new(
		context.Response.StatusCode, context.Response.Headers.ToArray(), context.Items.ToArray(),
		context.Response.Body, context.Response.HasStarted, response.EmptyResponseBodyRequested,
		response.SelectedFragmentNames.ToArray());

	private static void AssertUnchanged(State before, HttpContext context, HtmxResponse response)
	{
		var after = Capture(context, response);
		Assert.Equal(before.Status, after.Status);
		Assert.Equal(before.Headers, after.Headers);
		Assert.Equal(before.Items, after.Items);
		Assert.Same(before.Body, after.Body);
		Assert.Equal(before.Started, after.Started);
		Assert.Equal(before.Suppressed, after.Suppressed);
		Assert.Equal(before.Fragments, after.Fragments);
	}

	private static void AssertEquivalent(HttpContext expected, HtmxResponse expectedResponse, HttpContext actual, HtmxResponse actualResponse)
	{
		Assert.Equal(expected.Response.StatusCode, actual.Response.StatusCode);
		Assert.Equal(expected.Response.Headers.ToArray(), actual.Response.Headers.ToArray());
		Assert.Equal(expectedResponse.EmptyResponseBodyRequested, actualResponse.EmptyResponseBodyRequested);
		Assert.Equal(expectedResponse.SelectedFragmentNames, actualResponse.SelectedFragmentNames);
	}

	private sealed record State(int Status, KeyValuePair<string, StringValues>[] Headers,
		KeyValuePair<object, object?>[] Items, Stream Body, bool Started, bool Suppressed, string[] Fragments);
}
