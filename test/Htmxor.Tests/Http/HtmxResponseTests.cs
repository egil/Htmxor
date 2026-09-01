using Bunit;
using Htmxor.TestAssets.FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Http;

public class HtmxResponseTests : BunitContext
{
	private static HttpContext CreateHttpContext(Action<JsonOptions>? configureJsonOptions = null)
	{
		var services = new ServiceCollection();
		if (configureJsonOptions is not null)
		{
			services.Configure(configureJsonOptions);
		}

		var result = new DefaultHttpContext()
		{
			RequestServices = services.BuildServiceProvider()
		};
		result.Request.Scheme = "https";
		result.Request.Host = new HostString("app.example");
		result.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		return result;
	}

	private static readonly string[] NavigationHeaderNames =
	[
		HtmxResponseHeaderNames.Location,
		HtmxResponseHeaderNames.PushUrl,
		HtmxResponseHeaderNames.Redirect,
		HtmxResponseHeaderNames.Refresh,
		HtmxResponseHeaderNames.ReplaceUrl,
	];

	[Fact]
	public void Invalid_request_markers_reject_response_operations_without_mutation()
	{
		string[]?[] invalidMarkers =
		[
			null,
			[""],
			[" "],
			["false"],
			["TRUE"],
			["invalid"],
			["true,false"],
			["true", "true"],
			["\rtrue\r"],
		];

		foreach (var markerValues in invalidMarkers)
		{
			foreach (var operation in GetGuardedResponseOperations())
			{
				var context = CreateHttpContext();
				if (markerValues is null)
				{
					context.Request.Headers.Remove(HtmxRequestHeaderNames.HtmxRequest);
				}
				else
				{
					context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] =
						new Microsoft.Extensions.Primitives.StringValues(markerValues);
				}

				var response = context.GetHtmxContext().Response;

				Assert.Throws<InvalidOperationException>(() => operation(response));
				Assert.Empty(context.Response.Headers);
				Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
				Assert.False(response.EmptyResponseBodyRequested);
			}
		}
	}

	private static IEnumerable<Action<HtmxResponse>> GetGuardedResponseOperations()
	{
		yield return response => response.StatusCode(System.Net.HttpStatusCode.Created);
		yield return response => response.EmptyBody();
		yield return response => response.Location("/forbidden-location");
		yield return response => InvokeLocation(response, new Uri("/forbidden-location", UriKind.Relative));
		yield return response => response.PushUrl("/forbidden-push");
		yield return response => response.PushUrl(new Uri("/forbidden-push", UriKind.Relative));
		yield return response => response.PreventBrowserHistoryUpdate();
		yield return response => response.PreventBrowserCurrentUrlUpdate();
		yield return response => response.Redirect("/forbidden-redirect");
		yield return response => response.Redirect(new Uri("/forbidden-redirect", UriKind.Relative));
		yield return response => response.Refresh();
		yield return response => response.ReplaceUrl("/forbidden-replace");
		yield return response => response.ReplaceUrl(new Uri("/forbidden-replace", UriKind.Relative));
		yield return response => response.Reswap("acmeMorph settle:25ms");
		yield return response => response.Reswap(SwapStyle.outerHTML, "settle:25ms");
		yield return response => response.Retarget("#forbidden-target");
		yield return response => response.Reselect("#forbidden-selection");
		yield return response => response.Trigger("forbidden:event");
		yield return response => response.Trigger("forbidden:event", new { Detail = "forbidden" });
	}

	[Fact]
	public void Navigation_surface_uses_string_and_Uri_destinations_without_prototype_types()
	{
		var assembly = typeof(HtmxResponse).Assembly;

		Assert.Null(assembly.GetType("Htmxor.Http.AjaxContext"));
		Assert.Null(assembly.GetType("Htmxor.Http.LocationTarget"));
		AssertNavigationOverloads(nameof(HtmxResponse.Location));
		AssertNavigationOverloads(nameof(HtmxResponse.PushUrl));
		AssertNavigationOverloads(nameof(HtmxResponse.Redirect));
		AssertNavigationOverloads(nameof(HtmxResponse.ReplaceUrl));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(" /orders/42")]
	[InlineData("/orders/42 ")]
	[InlineData("/orders/\n42")]
	[InlineData("http://[::1")]
	public void Invalid_navigation_destinations_are_rejected_before_the_marker_guard_without_mutation(
		string? destination)
	{
		foreach (var operation in GetStringNavigationOperations(destination!))
		{
			var context = CreateHttpContext();
			context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "false";
			var response = context.GetHtmxContext().Response;

			Assert.ThrowsAny<ArgumentException>(() => operation(response));
			AssertNavigationStateUnchanged(context, response);
		}
	}

	[Fact]
	public void Navigation_policy_is_validated_before_the_marker_guard_without_mutation()
	{
		var invalidOperations = new Func<HtmxResponse, HtmxResponse>[]
		{
			response => response.PushUrl("true"),
			response => response.ReplaceUrl("false"),
			response => response.Location("http://app.example/orders/42"),
			response => response.PushUrl("https://app.example:444/orders/42"),
			response => response.ReplaceUrl("//other.example/orders/42"),
			response => response.Redirect("mailto:user@example.com"),
			response => InvokeLocation(response, null!),
			response => response.PushUrl((Uri)null!),
			response => response.Redirect((Uri)null!),
			response => response.ReplaceUrl((Uri)null!),
			response => InvokeLocation(response, new Uri("https://other.example/orders/42")),
			response => response.PushUrl(new Uri("ftp://app.example/orders/42")),
			response => response.Redirect(new Uri("mailto:user@example.com")),
			response => response.ReplaceUrl(new Uri("false", UriKind.Relative)),
		};

		foreach (var operation in invalidOperations)
		{
			var context = CreateHttpContext();
			context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "false";
			var response = context.GetHtmxContext().Response;

			Assert.ThrowsAny<ArgumentException>(() => operation(response));
			AssertNavigationStateUnchanged(context, response);
		}
	}

	[Fact]
	public void Invalid_navigation_call_preserves_existing_response_state()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		response.StatusCode(System.Net.HttpStatusCode.Accepted)
			.EmptyBody()
			.PushUrl("/existing");
		context.Response.Headers["X-Application"] = "retained";
		var headers = context.Response.Headers.ToDictionary(
			static header => header.Key,
			static header => header.Value,
			StringComparer.OrdinalIgnoreCase);

		Assert.Throws<ArgumentException>(() => response.Location(" /invalid"));

		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		Assert.True(response.EmptyResponseBodyRequested);
		Assert.Equal(headers.Count, context.Response.Headers.Count);
		foreach (var header in headers)
		{
			Assert.Equal(header.Value, context.Response.Headers[header.Key]);
		}
	}

	[Theory]
	[InlineData("true")]
	[InlineData("false")]
	public void History_destinations_reject_reserved_literals_without_mutation(string destination)
	{
		foreach (var operation in new Func<HtmxResponse, HtmxResponse>[]
		{
			response => response.PushUrl(destination),
			response => response.ReplaceUrl(destination),
		})
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;

			Assert.Throws<ArgumentException>(() => operation(response));
			AssertNavigationStateUnchanged(context, response);
		}
	}

	[Fact]
	public void Relative_and_same_origin_destinations_are_allowed_but_local_operations_reject_other_origins()
	{
		foreach (var operation in GetLocalNavigationOperations())
		{
			var relativeContext = CreateHttpContext();
			var relativeResponse = relativeContext.GetHtmxContext().Response;
			Assert.Same(relativeResponse, operation(relativeResponse, "?page=2"));

			var sameOriginContext = CreateHttpContext();
			var sameOriginResponse = sameOriginContext.GetHtmxContext().Response;
			Assert.Same(
				sameOriginResponse,
				operation(sameOriginResponse, "https://APP.example:443/orders/42"));

			foreach (var rejected in new[]
			{
				"https://other.example/orders/42",
				"http://app.example/orders/42",
				"https://app.example:444/orders/42",
				"//other.example/orders/42",
				"ftp://app.example/orders/42",
			})
			{
				var rejectedContext = CreateHttpContext();
				var rejectedResponse = rejectedContext.GetHtmxContext().Response;

				Assert.Throws<ArgumentException>(() => operation(rejectedResponse, rejected));
				AssertNavigationStateUnchanged(rejectedContext, rejectedResponse);
			}
		}
	}

	[Fact]
	public void Http_same_origin_destinations_and_cross_origin_http_redirect_are_allowed()
	{
		foreach (var operation in GetLocalNavigationOperations())
		{
			var context = CreateHttpContext();
			context.Request.Scheme = "http";
			context.Request.Host = new HostString("app.example");
			var response = context.GetHtmxContext().Response;

			Assert.Same(response, operation(response, "http://APP.example/orders/42"));
		}

		var redirectContext = CreateHttpContext();
		redirectContext.Request.Scheme = "http";
		var redirectResponse = redirectContext.GetHtmxContext().Response;
		Assert.Same(
			redirectResponse,
			redirectResponse.Redirect("http://idp.example/login"));
	}

	[Fact]
	public void Redirect_allows_relative_and_cross_origin_http_destinations_but_rejects_other_schemes()
	{
		foreach (var allowed in new[] { "/orders/42", "https://idp.example/login" })
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;

			Assert.Same(response, response.Redirect(allowed));
			Assert.Equal(allowed, context.Response.Headers[HtmxResponseHeaderNames.Redirect]);
		}

		var rejectedContext = CreateHttpContext();
		var rejectedResponse = rejectedContext.GetHtmxContext().Response;
		Assert.Throws<ArgumentException>(() => rejectedResponse.Redirect("mailto:user@example.com"));
		AssertNavigationStateUnchanged(rejectedContext, rejectedResponse);
	}

	[Fact]
	public void Uri_overloads_emit_OriginalString_without_normalizing_the_destination()
	{
		var destination = new Uri("https://APP.example:443/a/../orders%20archive", UriKind.Absolute);
		Assert.NotEqual(destination.OriginalString, destination.ToString());

		foreach (var operation in new Func<HtmxResponse, Uri, HtmxResponse>[]
		{
			InvokeLocation,
			(response, uri) => response.PushUrl(uri),
			(response, uri) => response.Redirect(uri),
			(response, uri) => response.ReplaceUrl(uri),
		})
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;

			Assert.Same(response, operation(response, destination));
			Assert.Equal(destination.OriginalString, Assert.Single(GetNavigationHeaders(context)).Value.ToString());
		}
	}

	[Fact]
	public void String_overloads_emit_relative_reference_text_without_normalizing_it()
	{
		const string destination = "../orders/%7E42?next=/a/../b";
		foreach (var operation in new Func<HtmxResponse, string, HtmxResponse>[]
		{
			(response, value) => response.Location(value),
			(response, value) => response.PushUrl(value),
			(response, value) => response.Redirect(value),
			(response, value) => response.ReplaceUrl(value),
		})
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;

			Assert.Same(response, operation(response, destination));
			Assert.Equal(destination, Assert.Single(GetNavigationHeaders(context)).Value.ToString());
		}
	}

	[Fact]
	public void Last_navigation_call_wins_and_replaces_automatic_body_behavior()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		var result = response
			.Location("/location")
			.Redirect("/redirect")
			.Refresh()
			.PushUrl("/push")
			.ReplaceUrl("/replace")
			.PreventBrowserHistoryUpdate()
			.PreventBrowserCurrentUrlUpdate();

		Assert.Same(response, result);
		var header = Assert.Single(GetNavigationHeaders(context));
		Assert.Equal(HtmxResponseHeaderNames.ReplaceUrl, header.Key);
		Assert.Equal("false", header.Value);
		Assert.False(response.EmptyResponseBodyRequested);
		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
	}

	[Fact]
	public void Explicit_empty_body_remains_suppressing_after_a_body_retaining_navigation_call()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		var result = response.EmptyBody().Redirect("/redirect").PushUrl("/push");

		Assert.Same(response, result);
		var header = Assert.Single(GetNavigationHeaders(context));
		Assert.Equal(HtmxResponseHeaderNames.PushUrl, header.Key);
		Assert.Equal("/push", header.Value);
		Assert.True(response.EmptyResponseBodyRequested);
	}

	[Fact]
	public void Navigation_operations_apply_their_documented_automatic_body_effect()
	{
		var operations = new (Func<HtmxResponse, HtmxResponse> Operation, bool SuppressesBody)[]
		{
			(response => response.Location("/location"), true),
			(response => response.Redirect("/redirect"), true),
			(response => response.Refresh(), true),
			(response => response.PushUrl("/push"), false),
			(response => response.ReplaceUrl("/replace"), false),
			(response => response.PreventBrowserHistoryUpdate(), false),
			(response => response.PreventBrowserCurrentUrlUpdate(), false),
		};

		foreach (var (operation, suppressesBody) in operations)
		{
			var context = CreateHttpContext();
			context.Response.StatusCode = StatusCodes.Status202Accepted;
			var response = context.GetHtmxContext().Response;

			Assert.Same(response, operation(response));
			Assert.Equal(suppressesBody, response.EmptyResponseBodyRequested);
			Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		}
	}

	[Fact]
	public void Every_navigation_operation_replaces_multiple_values_and_other_navigation_headers()
	{
		var operations = new (Func<HtmxResponse, HtmxResponse> Operation, string Header, string Value)[]
		{
			(response => response.Location("/location"), HtmxResponseHeaderNames.Location, "/location"),
			(response => response.PushUrl("/push"), HtmxResponseHeaderNames.PushUrl, "/push"),
			(response => response.PreventBrowserHistoryUpdate(), HtmxResponseHeaderNames.PushUrl, "false"),
			(response => response.Redirect("/redirect"), HtmxResponseHeaderNames.Redirect, "/redirect"),
			(response => response.Refresh(), HtmxResponseHeaderNames.Refresh, "true"),
			(response => response.ReplaceUrl("/replace"), HtmxResponseHeaderNames.ReplaceUrl, "/replace"),
			(response => response.PreventBrowserCurrentUrlUpdate(), HtmxResponseHeaderNames.ReplaceUrl, "false"),
		};

		foreach (var (operation, expectedHeader, expectedValue) in operations)
		{
			var context = CreateHttpContext();
			foreach (var headerName in NavigationHeaderNames)
			{
				context.Response.Headers.Append(headerName, "first");
				context.Response.Headers.Append(headerName, "second");
			}

			var response = context.GetHtmxContext().Response;
			Assert.Same(response, operation(response));

			var header = Assert.Single(GetNavigationHeaders(context));
			Assert.Equal(expectedHeader, header.Key);
			Assert.Equal(expectedValue, Assert.Single(header.Value));
		}
	}

	private static IEnumerable<Func<HtmxResponse, HtmxResponse>> GetStringNavigationOperations(string destination)
	{
		yield return response => response.Location(destination);
		yield return response => response.PushUrl(destination);
		yield return response => response.Redirect(destination);
		yield return response => response.ReplaceUrl(destination);
	}

	private static IEnumerable<Func<HtmxResponse, string, HtmxResponse>> GetLocalNavigationOperations()
	{
		yield return (response, destination) => response.Location(destination);
		yield return (response, destination) => response.PushUrl(destination);
		yield return (response, destination) => response.ReplaceUrl(destination);
	}

	private static IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetNavigationHeaders(
		HttpContext context)
		=> context.Response.Headers.Where(header => NavigationHeaderNames.Contains(header.Key, StringComparer.OrdinalIgnoreCase));

	private static void AssertNavigationStateUnchanged(HttpContext context, HtmxResponse response)
	{
		Assert.Empty(GetNavigationHeaders(context));
		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.False(response.EmptyResponseBodyRequested);
	}

	private static void AssertNavigationOverloads(string methodName)
	{
		var parameterTypes = typeof(HtmxResponse).GetMethods()
			.Where(method => method.Name == methodName)
			.Select(method => Assert.Single(method.GetParameters()).ParameterType)
			.ToArray();

		Assert.Equal([typeof(string), typeof(Uri)], parameterTypes.OrderBy(type => type.Name));
	}

	private static HtmxResponse InvokeLocation(HtmxResponse response, Uri destination)
	{
		var method = Assert.Single(
			typeof(HtmxResponse).GetMethods(),
			method => method.Name == nameof(HtmxResponse.Location) &&
				method.GetParameters() is [{ ParameterType: var parameterType }] &&
				parameterType == typeof(Uri));
		var operation = method.CreateDelegate<Func<HtmxResponse, Uri, HtmxResponse>>();
		return operation(response, destination);
	}

	[Fact]
	public void Location_AddsLocationHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Location("/new-location");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Location]
			.Should()
			.Equal(["/new-location"]);
	}

	[Fact]
	public void PushUrl_AddsPushUrlHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.PushUrl("/new-url");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.PushUrl].Should().Equal(["/new-url"]);
	}

	[Fact]
	public void Redirect_AddsRedirectHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Redirect("/new-redirect");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Redirect].Should().Equal(["/new-redirect"]);
	}

	[Fact]
	public void Refresh_AddsRefreshHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Refresh();

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Refresh].Should().Equal(["true"]);
	}

	[Fact]
	public void ReplaceUrl_AddsReplaceUrlHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.ReplaceUrl("/new-replace-url");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl].Should().Equal(["/new-replace-url"]);
	}

	[Fact]
	public void PushUrl_AddsPushUrlBrowserHistoryHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.PreventBrowserHistoryUpdate();

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.PushUrl].Should().Equal(["false"]);
	}

	[Fact]
	public void ReplaceUrl_AddsReplaceUrlBrowserCUrrentUrlHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.PreventBrowserCurrentUrlUpdate();

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.ReplaceUrl].Should().Equal(["false"]);
	}

	[Fact]
	public void Reswap_AddsReswapHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Reswap(SwapStyle.innerHTML);

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Reswap].Should().Equal(["innerHTML"]);
	}

	[Fact]
	public void Reswap_accepts_an_unknown_extension_value()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		response.Reswap("acmeMorph settle:25ms");

		context.Response.Headers[HtmxResponseHeaderNames.Reswap]
			.Should()
			.Equal(["acmeMorph settle:25ms"]);
	}

	[Fact]
	public void Retarget_AddsRetargetHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Retarget(".new-target");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Retarget].Should().Equal([".new-target"]);
	}

	[Fact]
	public void Reselect_AddsReselectHeader()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Reselect(".new-selection");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Reselect].Should().Equal([".new-selection"]);
	}

	[Fact]
	public void Trigger_without_details_adds_trigger_header()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("event1");
	}

	[Fact]
	public void Multiple_trigger_events_without_details_share_trigger_header()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");
		response.Trigger("event2");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("event1,event2");
	}

	[Fact]
	public void Duplicate_trigger_event_is_emitted_once()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");
		response.Trigger("event1");

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("event1");
	}

	[Fact]
	public void Trigger_with_detail_adds_json_trigger_header()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		var triggerObject = new { level = "info", message = "Here Is A Message" };

		// Act
		response.Trigger("showMessage", triggerObject);

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "showMessage": { "level": "info", "message": "Here Is A Message" } }
                """);
	}

	[Fact]
	public void Trigger_combines_events_with_and_without_details()
	{
		// Arrange
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		// Act
		response.Trigger("event1");
		response.Trigger("event2", new { magic = "something" });
		response.Trigger("event3", new { moremagic = false });

		// Assert
		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "event1": null, "event2": { "magic": "something" }, "event3": { "moremagic": false } }
                """);
	}

	[Fact]
	public void Trigger_uses_application_json_options_for_event_details()
	{
		var context = CreateHttpContext(options =>
			options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);
		var response = context.GetHtmxContext().Response;

		response.Trigger("showMessage", new { MessageLevel = "info" });

		context.Response.Headers[HtmxResponseHeaderNames.Trigger]
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeJsonSemanticallyEqualTo("""
                { "showMessage": { "message_level": "info" } }
                """);
	}

	[Fact]
	public void Trigger_overloads_reject_null_event_names()
	{
		var withoutDetail = CreateHttpContext().GetHtmxContext().Response;
		var withDetail = CreateHttpContext().GetHtmxContext().Response;

		var withoutDetailException = Assert.Throws<ArgumentNullException>(
			() => withoutDetail.Trigger(null!));
		var withDetailException = Assert.Throws<ArgumentNullException>(
			() => withDetail.Trigger(null!, new { Message = "detail" }));

		Assert.Equal("eventName", withoutDetailException.ParamName);
		Assert.Equal("eventName", withDetailException.ParamName);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void Trigger_overloads_reject_whitespace_event_names(string eventName)
	{
		var withoutDetail = CreateHttpContext().GetHtmxContext().Response;
		var withDetail = CreateHttpContext().GetHtmxContext().Response;

		var withoutDetailException = Assert.Throws<ArgumentException>(
			() => withoutDetail.Trigger(eventName));
		var withDetailException = Assert.Throws<ArgumentException>(
			() => withDetail.Trigger(eventName, new { Message = "detail" }));

		Assert.Equal("eventName", withoutDetailException.ParamName);
		Assert.Equal("eventName", withDetailException.ParamName);
	}

	[Fact]
	public void Htmx4_response_trigger_surface_does_not_expose_removed_timing_api()
	{
		var assembly = typeof(HtmxResponse).Assembly;

		assembly.GetType("Htmxor.TriggerTiming").Should().BeNull();
		typeof(HtmxResponseHeaderNames).GetField("TriggerAfterSwap").Should().BeNull();
		typeof(HtmxResponseHeaderNames).GetField("TriggerAfterSettle").Should().BeNull();
		Assert.All(
			typeof(HtmxResponse).GetMethods().Where(method => method.Name == nameof(HtmxResponse.Trigger)),
			method => Assert.DoesNotContain(
				method.GetParameters(),
				parameter => parameter.ParameterType.FullName == "Htmxor.TriggerTiming"));
	}
}
