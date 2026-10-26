using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	[Fact]
	public void Trigger_merges_one_deterministic_object_with_selected_serializer_options()
	{
		const string unsafeEventName = "quote\"slash\\event";
		var context = CreateTriggerContext(validMarker: true);
		var originalBody = context.Response.Body;
		var response = context.GetHtmxContext().Response;
		var explicitOptions = new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
			WriteIndented = true,
		};
		var nullOptions = new JsonSerializerOptions();
		nullOptions.Converters.Add(new NullTriggerDetailConverter());

		var result = response
			.Trigger("plain,event")
			.Trigger(unsafeEventName, new { MessageLevel = "application" })
			.Trigger("serializedNull", new NullTriggerDetail(), nullOptions)
			.Trigger("CaseEvent")
			.Trigger("caseEvent", new { MessageLevel = "lower-case-distinct" })
			.Trigger("replaceEvent", new { MessageLevel = "discarded" })
			.Trigger("tailEvent")
			.Trigger("replaceEvent", new { MessageLevel = "final" })
			.Trigger(
				"overrideEvent",
				new { MessageLevel = "per-call", UnsafeValue = "<café>&" },
				explicitOptions);

		var expected =
			"{\"plain,event\":{}," +
			JsonSerializer.Serialize(unsafeEventName) +
			":{\"message_level\":\"application\"}," +
			"\"serializedNull\":{}," +
			"\"CaseEvent\":{}," +
			"\"caseEvent\":{\"message_level\":\"lower-case-distinct\"}," +
			"\"replaceEvent\":{\"message_level\":\"final\"}," +
			"\"tailEvent\":{}," +
			"\"overrideEvent\":{\"message-level\":\"per-call\"," +
			"\"unsafe-value\":\"\\u003Ccaf\\u00E9\\u003E\\u0026\"}}";
		Assert.Multiple(
			() => Assert.Same(response, result),
			() => Assert.Equal(
			expected,
			Assert.Single(context.Response.Headers[HtmxResponseHeaderNames.Trigger])),
			() => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
			() => Assert.Equal("retained", context.Response.Headers["X-Application"]),
			() => Assert.Equal(3, context.Response.ContentLength),
			() => Assert.Same(originalBody, context.Response.Body),
			() => Assert.Equal(3, context.Response.Body.Length),
			() => Assert.Equal(0, context.Response.Body.Position)
		);
	}

	[Fact]
	public void Trigger_failures_leave_manual_response_state_unchanged()
	{
		string?[] invalidEventNames =
		[
			null,
			string.Empty,
			" ",
			" event",
			"event ",
			"event\ninside",
			"event\u007Finside",
			"event\uD800inside",
			"event\uDC00inside",
		];
		foreach (var eventName in invalidEventNames)
		{
			var context = CreateTriggerContext(validMarker: false);
			var response = context.GetHtmxContext().Response;

			Assert.ThrowsAny<ArgumentException>(() => response.Trigger(eventName!));
			AssertTriggerContextUnchanged(context);
		}

		var serializerOptions = new JsonSerializerOptions();
		serializerOptions.Converters.Add(new ThrowingTriggerDetailConverter());
		foreach (var validMarker in new[] { true, false })
		{
			var context = CreateTriggerContext(validMarker);
			var response = context.GetHtmxContext().Response;

			Assert.Throws<JsonException>(() => response.Trigger(
				"serializationFailure",
				new ThrowingTriggerDetail(),
				serializerOptions));
			AssertTriggerContextUnchanged(context);
		}

		var markerContext = CreateTriggerContext(validMarker: false);
		var markerResponse = markerContext.GetHtmxContext().Response;

		Assert.Throws<InvalidOperationException>(() => markerResponse.Trigger("validEvent"));
		AssertTriggerContextUnchanged(markerContext);
	}

	[Fact]
	public async Task Trigger_preserves_status_and_body_decisions_through_TestServer()
	{
		var cases = new (string Operation, bool ExpectsBody, bool ExpectsNavigation)[]
		{
			("retain", true, false),
			("empty", false, false),
			("navigation", false, true),
		};

		foreach (var testCase in cases)
		{
			using var request = CreateMarkerRequest(
				$"{TriggerPageRequestPath}?operation={testCase.Operation}",
				["true"]);
			using var response = await client.SendAsync(request);
			var body = await response.Content.ReadAsStringAsync();

			Assert.Multiple(
				() => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode),
				() => Assert.Equal(
				"{\"componentEvent\":{}}",
				Assert.Single(response.Headers.GetValues(HtmxResponseHeaderNames.Trigger))),
				() => Assert.Equal("retained", Assert.Single(response.Headers.GetValues("X-Application"))),
				() => Assert.Equal("same", Assert.Single(response.Headers.GetValues("X-Fluent"))),
				() => Assert.Equal(
				testCase.ExpectsNavigation,
				response.Headers.Contains(HtmxResponseHeaderNames.Location)),
				() => Assert.Equal(
				testCase.ExpectsBody,
				body.Contains("data-issue-154-trigger", StringComparison.Ordinal))
			);
			if (!testCase.ExpectsBody)
			{
				Assert.Equal(string.Empty, body);
			}
		}
	}

}
