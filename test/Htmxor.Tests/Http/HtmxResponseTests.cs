using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bunit;
using Htmxor.TestAssets.FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

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

	private static readonly (string HeaderName, Func<HtmxResponse, string, HtmxResponse> Apply)[]
		SwapAndSelectionOperations =
	[
		(HtmxResponseHeaderNames.Reswap, static (response, value) => response.Reswap(value)),
		(HtmxResponseHeaderNames.Retarget, static (response, value) => response.Retarget(value)),
		(HtmxResponseHeaderNames.Reselect, static (response, value) => response.Reselect(value)),
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
		yield return response => response.Retarget("#forbidden-target");
		yield return response => response.Reselect("#forbidden-selection");
		yield return response => response.Trigger("forbidden:event");
		yield return response => response.Trigger("forbidden:event", new { Detail = "forbidden" });
	}

	[Fact]
	public void Swap_and_selection_surface_uses_only_open_string_values()
	{
		var assembly = typeof(HtmxResponse).Assembly;

		Assert.Null(assembly.GetType("Htmxor.SwapStyle"));
		Assert.Null(assembly.GetType("Htmxor.SwapStyleExtensions"));
		var reswap = Assert.Single(
			typeof(HtmxResponse).GetMethods(),
			static method => method.Name == nameof(HtmxResponse.Reswap));
		Assert.Equal(typeof(string), Assert.Single(reswap.GetParameters()).ParameterType);
	}

	[Fact]
	public void Htmx4_response_surface_removes_legacy_status_286_contract_but_keeps_general_status()
	{
		var assembly = typeof(HtmxResponse).Assembly;

		Assert.Null(assembly.GetType("Htmxor.Http.HtmxStatusCodes"));
		Assert.DoesNotContain(
			typeof(HtmxResponse).GetMethods(),
			static method => method.Name == "StopPolling");

		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		Assert.Same(response, response.StatusCode((System.Net.HttpStatusCode)286));
		Assert.Equal(286, context.Response.StatusCode);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(" value")]
	[InlineData("value ")]
	[InlineData("value\ninside")]
	[InlineData("value\u007Finside")]
	[InlineData("café")]
	public void Invalid_swap_and_selection_values_are_rejected_before_the_marker_guard_without_mutation(
		string? value)
	{
		foreach (var (_, apply) in SwapAndSelectionOperations)
		{
			var context = CreateHttpContext();
			context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "false";
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

			Assert.ThrowsAny<ArgumentException>(() => apply(response, value!));

			Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
			Assert.False(response.EmptyResponseBodyRequested);
			Assert.Equal(expectedHeaders.Count, context.Response.Headers.Count);
			foreach (var expectedHeader in expectedHeaders)
			{
				Assert.Equal(expectedHeader.Value, context.Response.Headers[expectedHeader.Key]);
			}
		}
	}

	[Fact]
	public void Invalid_swap_and_selection_values_preserve_existing_body_and_header_decisions()
	{
		foreach (var (_, apply) in SwapAndSelectionOperations)
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;
			response.StatusCode(System.Net.HttpStatusCode.Accepted)
				.EmptyBody()
				.Location("/existing-location");
			context.Response.Headers["X-Application"] = "retained";
			var expectedHeaders = context.Response.Headers.ToDictionary(
				static header => header.Key,
				static header => header.Value,
				StringComparer.OrdinalIgnoreCase);

			Assert.Throws<ArgumentException>(() => apply(response, " value"));

			Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
			Assert.True(response.EmptyResponseBodyRequested);
			Assert.Equal(expectedHeaders.Count, context.Response.Headers.Count);
			foreach (var expectedHeader in expectedHeaders)
			{
				Assert.Equal(expectedHeader.Value, context.Response.Headers[expectedHeader.Key]);
			}
		}
	}

	[Fact]
	public void Swap_and_selection_operations_preserve_open_values_and_overwrite_only_the_matching_header()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		response.StatusCode(System.Net.HttpStatusCode.Accepted)
			.Location("/retained-location");
		context.Response.Headers["X-Application"] = "retained";
		foreach (var (headerName, _) in SwapAndSelectionOperations)
		{
			context.Response.Headers.Append(headerName, "discarded-first");
			context.Response.Headers.Append(headerName, "discarded-second");
		}

		var result = response
			.Reswap("innerHTML")
			.Reswap("acmeMorph settle:25ms")
			.Retarget("#discarded-target")
			.Retarget("closest [data-acme-target]")
			.Reselect("#discarded-selection")
			.Reselect(":scope > [data-acme-selection]");

		Assert.Same(response, result);
		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		Assert.True(response.EmptyResponseBodyRequested);
		Assert.Equal("/retained-location", context.Response.Headers[HtmxResponseHeaderNames.Location]);
		Assert.Equal("retained", context.Response.Headers["X-Application"]);
		Assert.Equal(
			"acmeMorph settle:25ms",
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Reswap]));
		Assert.Equal(
			"closest [data-acme-target]",
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Retarget]));
		Assert.Equal(
			":scope > [data-acme-selection]",
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Reselect]));
	}

	[Fact]
	public void Swap_and_selection_operations_retain_component_output_when_it_was_not_suppressed()
	{
		var context = CreateHttpContext();
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		var response = context.GetHtmxContext().Response;

		var result = response
			.Reswap("outerHTML")
			.Retarget("#target")
			.Reselect("[data-selection]");

		Assert.Same(response, result);
		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		Assert.False(response.EmptyResponseBodyRequested);
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
	[InlineData("https://app.example/caf\u00e9")]
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
	[InlineData("true", "TRUE")]
	[InlineData("false", "False")]
	public void History_destinations_reject_exact_reserved_literals_and_accept_case_variants(
		string reservedLiteral,
		string relativeReference)
	{
		foreach (var operation in new Func<HtmxResponse, HtmxResponse>[]
		{
			response => response.PushUrl(reservedLiteral),
			response => response.ReplaceUrl(reservedLiteral),
		})
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;

			Assert.Throws<ArgumentException>(() => operation(response));
			AssertNavigationStateUnchanged(context, response);
		}

		foreach (var operation in new Func<HtmxResponse, HtmxResponse>[]
		{
			response => response.PushUrl(relativeReference),
			response => response.ReplaceUrl(relativeReference),
		})
		{
			var context = CreateHttpContext();
			var response = context.GetHtmxContext().Response;

			Assert.Same(response, operation(response));
			Assert.Equal(relativeReference, Assert.Single(GetNavigationHeaders(context)).Value.ToString());
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
	public void Trigger_without_details_adds_one_json_object_header()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		response.Trigger("event1");

		Assert.Equal("{\"event1\":{}}", GetTriggerHeader(context));
	}

	[Fact]
	public void Trigger_safely_encodes_an_exact_event_name_as_a_json_property()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		const string eventName = "order,\"quoted\"\\path";

		response.Trigger(eventName);

		var header = GetTriggerHeader(context);
		using var document = JsonDocument.Parse(header);
		var property = Assert.Single(document.RootElement.EnumerateObject().ToArray());
		Assert.Equal(eventName, property.Name);
		Assert.Equal(JsonValueKind.Object, property.Value.ValueKind);
		Assert.Empty(property.Value.EnumerateObject().ToArray());
		Assert.DoesNotContain('\n', header);
		Assert.DoesNotContain('\r', header);
	}

	[Fact]
	public void Trigger_merges_case_sensitive_event_names_in_call_order()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		response.Trigger("event");
		response.Trigger("Event");
		response.Trigger("another:event");

		Assert.Equal(
			"{\"event\":{},\"Event\":{},\"another:event\":{}}",
			GetTriggerHeader(context));
	}

	[Fact]
	public void Trigger_replaces_duplicate_details_in_place_in_both_directions()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;

		response.Trigger("detail-added");
		response.Trigger("middle", new { value = 0 });
		response.Trigger("detail-removed", new { value = 1 });
		response.Trigger("detail-added", new { value = 2 });
		response.Trigger("detail-removed");

		Assert.Equal(
			"{\"detail-added\":{\"value\":2},\"middle\":{\"value\":0},\"detail-removed\":{}}",
			GetTriggerHeader(context));
	}

	[Fact]
	public void Trigger_uses_application_json_options_by_default_and_explicit_options_per_call()
	{
		var context = CreateHttpContext(options =>
		{
			options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
			options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
			options.SerializerOptions.WriteIndented = true;
		});
		var response = context.GetHtmxContext().Response;
		var explicitOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
			DictionaryKeyPolicy = JsonNamingPolicy.KebabCaseLower,
			WriteIndented = true,
		};

		response.Trigger("ApplicationEvent", new { MessageLevel = "info" });
		response.Trigger("OverrideEvent", new { MessageLevel = "warning" }, explicitOptions);

		var header = GetTriggerHeader(context);
		Assert.Equal(
			"{\"ApplicationEvent\":{\"message_level\":\"info\"}," +
			"\"OverrideEvent\":{\"message-level\":\"warning\"}}",
			header);
		Assert.DoesNotContain('\n', header);
		Assert.DoesNotContain('\r', header);
	}

	[Fact]
	public void Trigger_normalizes_every_serialized_json_null_to_an_empty_detail_object()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		using var nullDocument = JsonDocument.Parse("null");
		var converterOptions = new JsonSerializerOptions();
		converterOptions.Converters.Add(new NullTriggerDetailConverter());

		response.Trigger("element:null", nullDocument.RootElement);
		response.Trigger("converter:null", new NullTriggerDetail(), converterOptions);

		Assert.Equal(
			"{\"element:null\":{},\"converter:null\":{}}",
			GetTriggerHeader(context));
	}

	[Fact]
	public void Trigger_uses_header_safe_encoding_and_selected_max_depth_for_details()
	{
		var context = CreateHttpContext();
		var response = context.GetHtmxContext().Response;
		var options = new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			MaxDepth = 1101,
		};

		response.Trigger("encoded", new { Value = "<café>&" }, options);

		var encodedHeader = GetTriggerHeader(context);
		Assert.DoesNotContain('<', encodedHeader);
		Assert.DoesNotContain('>', encodedHeader);
		Assert.DoesNotContain('&', encodedHeader);
		Assert.DoesNotContain('é', encodedHeader);
		using (var encodedDocument = JsonDocument.Parse(encodedHeader))
		{
			Assert.Equal(
				"<café>&",
				encodedDocument.RootElement.GetProperty("encoded").GetProperty("Value").GetString());
		}

		var deepDetail = new DeepTriggerDetail { Value = "end" };
		for (var depth = 0; depth < 1001; depth++)
		{
			deepDetail = new DeepTriggerDetail { Child = deepDetail };
		}

		response.Trigger("deep", deepDetail, options);

		using var deepDocument = JsonDocument.Parse(
			GetTriggerHeader(context),
			new JsonDocumentOptions { MaxDepth = 1105 });
		var current = deepDocument.RootElement.GetProperty("deep");
		for (var depth = 0; depth < 1001; depth++)
		{
			current = current.GetProperty("Child");
		}

		Assert.Equal("end", current.GetProperty("Value").GetString());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(" event")]
	[InlineData("event ")]
	[InlineData("event\ninside")]
	[InlineData("event\u007Finside")]
	public void Invalid_trigger_names_are_rejected_before_the_marker_without_mutation(string? eventName)
	{
		AssertInvalidTriggerNameDoesNotMutate(eventName, withDetail: false);
		AssertInvalidTriggerNameDoesNotMutate(eventName, withDetail: true);
	}

	[Fact]
	public void Isolated_high_surrogate_trigger_name_is_rejected_before_the_marker_without_mutation()
	{
		AssertInvalidTriggerNameDoesNotMutate("event\uD800inside", withDetail: false);
		AssertInvalidTriggerNameDoesNotMutate("event\uD800inside", withDetail: true);
	}

	[Fact]
	public void Isolated_low_surrogate_trigger_name_is_rejected_before_the_marker_without_mutation()
	{
		AssertInvalidTriggerNameDoesNotMutate("event\uDC00inside", withDetail: false);
		AssertInvalidTriggerNameDoesNotMutate("event\uDC00inside", withDetail: true);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Trigger_serializes_before_the_marker_and_serialization_failure_does_not_mutate(
		bool validMarker)
	{
		var context = CreateHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = validMarker ? "true" : "false";
		SetExistingResponseState(context);
		var response = context.GetHtmxContext().Response;
		if (validMarker)
		{
			response.EmptyBody();
		}
		var expected = CaptureResponseState(context, response);

		Assert.Throws<NotSupportedException>(
			() => response.Trigger("unsupported:detail", typeof(string)));

		AssertResponseStateUnchanged(context, response, expected);
	}

	[Fact]
	public void Trigger_marker_failure_preserves_existing_and_internal_response_state()
	{
		var context = CreateHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "false";
		SetExistingResponseState(context);
		var response = context.GetHtmxContext().Response;
		var expected = CaptureResponseState(context, response);

		Assert.Throws<InvalidOperationException>(() => response.Trigger("rejected:without-detail"));
		AssertResponseStateUnchanged(context, response, expected);
		Assert.Throws<InvalidOperationException>(
			() => response.Trigger("rejected:with-detail", new { value = "rejected" }));
		AssertResponseStateUnchanged(context, response, expected);

		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		var validResponse = new HtmxResponse(context);
		validResponse.Trigger("accepted:event");
		Assert.Equal("{\"accepted:event\":{}}", GetTriggerHeader(context));
	}

	[Fact]
	public void First_successful_trigger_replaces_manual_header_then_merges_only_owned_events()
	{
		var context = CreateHttpContext();
		SetExistingResponseState(context);
		var response = context.GetHtmxContext().Response;
		response.EmptyBody();

		var result = response.Trigger("owned:first");

		Assert.Same(response, result);
		Assert.Equal("{\"owned:first\":{}}", GetTriggerHeader(context));
		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
		Assert.Equal(47, context.Response.ContentLength);
		Assert.Equal("retained", context.Response.Headers["X-Application"]);
		Assert.True(response.EmptyResponseBodyRequested);

		context.Response.Headers[HtmxResponseHeaderNames.Trigger] = "{\"manual:rewrite\":true}";
		response.Trigger("owned:second", new { value = 2 });

		Assert.Equal(
			"{\"owned:first\":{},\"owned:second\":{\"value\":2}}",
			GetTriggerHeader(context));
	}

	[Fact]
	public void Trigger_returns_same_response_and_preserves_status_and_body_decisions()
	{
		var retainedContext = CreateHttpContext();
		retainedContext.Response.StatusCode = StatusCodes.Status202Accepted;
		var retainedResponse = retainedContext.GetHtmxContext().Response;
		var retainedResult = retainedResponse.Trigger("retained:body");

		Assert.Same(retainedResponse, retainedResult);
		Assert.Equal(StatusCodes.Status202Accepted, retainedContext.Response.StatusCode);
		Assert.False(retainedResponse.EmptyResponseBodyRequested);

		var emptyContext = CreateHttpContext();
		var emptyResponse = emptyContext.GetHtmxContext().Response;
		emptyResponse.StatusCode(System.Net.HttpStatusCode.Accepted).EmptyBody();
		var emptyResult = emptyResponse.Trigger("empty:body");

		Assert.Same(emptyResponse, emptyResult);
		Assert.Equal(StatusCodes.Status202Accepted, emptyContext.Response.StatusCode);
		Assert.True(emptyResponse.EmptyResponseBodyRequested);

		var navigationContext = CreateHttpContext();
		var navigationResponse = navigationContext.GetHtmxContext().Response;
		navigationResponse.StatusCode(System.Net.HttpStatusCode.Accepted).Location("/preserved-location");
		var navigationResult = navigationResponse.Trigger("navigation:body");

		Assert.Same(navigationResponse, navigationResult);
		Assert.Equal(StatusCodes.Status202Accepted, navigationContext.Response.StatusCode);
		Assert.True(navigationResponse.EmptyResponseBodyRequested);
		Assert.Equal(
			"/preserved-location",
			navigationContext.Response.Headers[HtmxResponseHeaderNames.Location]);
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

	private static string GetTriggerHeader(HttpContext context)
		=> Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Trigger])!;

	private static void AssertInvalidTriggerNameDoesNotMutate(string? eventName, bool withDetail)
	{
		var context = CreateHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "false";
		SetExistingResponseState(context);
		var response = context.GetHtmxContext().Response;
		var expected = CaptureResponseState(context, response);

		var exception = withDetail
			? Assert.ThrowsAny<ArgumentException>(
				() => response.Trigger(eventName!, new { value = "detail" }))
			: Assert.ThrowsAny<ArgumentException>(() => response.Trigger(eventName!));

		Assert.Equal("eventName", exception.ParamName);
		AssertResponseStateUnchanged(context, response, expected);
	}

	private static void SetExistingResponseState(HttpContext context)
	{
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.ContentLength = 47;
		context.Response.Headers["X-Application"] = "retained";
		context.Response.Headers.Append(HtmxResponseHeaderNames.Trigger, "manual:first");
		context.Response.Headers.Append(HtmxResponseHeaderNames.Trigger, "manual:second");
	}

	private static ResponseState CaptureResponseState(HttpContext context, HtmxResponse response)
		=> new(
			context.Response.StatusCode,
			context.Response.ContentLength,
			response.EmptyResponseBodyRequested,
			context.Response.Headers.ToDictionary(
				static header => header.Key,
				static header => header.Value,
				StringComparer.OrdinalIgnoreCase));

	private static void AssertResponseStateUnchanged(
		HttpContext context,
		HtmxResponse response,
		ResponseState expected)
	{
		Assert.Equal(expected.StatusCode, context.Response.StatusCode);
		Assert.Equal(expected.ContentLength, context.Response.ContentLength);
		Assert.Equal(expected.EmptyResponseBodyRequested, response.EmptyResponseBodyRequested);
		Assert.Equal(expected.Headers.Count, context.Response.Headers.Count);
		foreach (var header in expected.Headers)
		{
			Assert.Equal(header.Value, context.Response.Headers[header.Key]);
		}
	}

	private readonly record struct ResponseState(
		int StatusCode,
		long? ContentLength,
		bool EmptyResponseBodyRequested,
		IReadOnlyDictionary<string, StringValues> Headers);

	private sealed class NullTriggerDetail
	{
	}

	private sealed class DeepTriggerDetail
	{
		public DeepTriggerDetail? Child { get; init; }

		public string? Value { get; init; }
	}

	private sealed class NullTriggerDetailConverter : JsonConverter<NullTriggerDetail>
	{
		public override NullTriggerDetail Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
			=> throw new NotSupportedException();

		public override void Write(
			Utf8JsonWriter writer,
			NullTriggerDetail value,
			JsonSerializerOptions options)
			=> writer.WriteNullValue();
	}
}
