using System.Text.Json;
using System.Text.Json.Serialization;
using Htmxor.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests(Issue359ResponseHost host) : IClassFixture<Issue359ResponseHost>
{

	private const string RoutePrefix = "/issue359-response";
	private const string ReportHost = "reports.internal.example";
	private const string MarkerPageRequestPath = RoutePrefix + "/issue-154-marker";
	private const string NavigationPageRequestPath = RoutePrefix + "/issue-154-navigation";
	private const string SwapSelectionPageRequestPath = RoutePrefix + "/issue-154-swap-selection";
	private const string TriggerPageRequestPath = RoutePrefix + "/issue-154-trigger";
	private const string ReportRequestPath = RoutePrefix + "/issue359-reports/42";
	private WebApplication app => host.App;
	private HttpClient client => host.Client;
	public static TheoryData<string, string[]?> InvalidHtmxRequestMarkers => new()
	{
		{ "missing", null },
		{ "blank", [""] },
		{ "whitespace", [" "] },
		{ "false", ["false"] },
		{ "malformed", ["TRUE"] },
		{ "comma-joined", ["true,false"] },
		{ "repeated", ["true", "true"] },
		{ "contradictory", ["true", "false"] },
	};

	public sealed record WireProbeSnapshot(
		string RequestType,
		string RoutingMode,
		string IsBoosted,
		string IsHistoryRestore,
		string CurrentUrl,
		string Source,
		string Target);

	public sealed record InvalidCoreRequestHeaderWireCase(
		string CaseName,
		string HeaderName,
		string[]? Values,
		WireProbeSnapshot Expected);

	private static readonly WireProbeSnapshot ValidWireProbe = new(
		"partial",
		"Direct",
		"true",
		"true",
		"https://example.test/orders",
		"button#submit",
		"section#result");

	private static readonly WireProbeSnapshot InvalidRequestTypeWireProbe = ValidWireProbe with
	{
		RequestType = "null",
		RoutingMode = "Standard",
	};

	private static readonly WireProbeSnapshot InvalidBoostedWireProbe = ValidWireProbe with
	{
		IsBoosted = "false",
	};

	private static readonly WireProbeSnapshot InvalidHistoryRestoreWireProbe = ValidWireProbe with
	{
		IsHistoryRestore = "false",
	};

	private static readonly WireProbeSnapshot InvalidCurrentUrlWireProbe = ValidWireProbe with
	{
		CurrentUrl = "null",
	};

	private static readonly WireProbeSnapshot InvalidSourceWireProbe = ValidWireProbe with
	{
		Source = "null",
	};

	private static readonly WireProbeSnapshot InvalidTargetWireProbe = ValidWireProbe with
	{
		Target = "null",
	};

	public static IEnumerable<object[]> InvalidCoreRequestHeaderWireCases()
	{
		yield return WireCase("request-type-missing", HtmxRequestHeaderNames.RequestType, null, InvalidRequestTypeWireProbe);
		yield return WireCase("request-type-false", HtmxRequestHeaderNames.RequestType, ["false"], InvalidRequestTypeWireProbe);
		yield return WireCase("request-type-malformed", HtmxRequestHeaderNames.RequestType, ["whole"], InvalidRequestTypeWireProbe);
		yield return WireCase("request-type-repeated", HtmxRequestHeaderNames.RequestType, ["partial", "partial"], InvalidRequestTypeWireProbe);
		yield return WireCase("request-type-contradictory", HtmxRequestHeaderNames.RequestType, ["partial", "full"], InvalidRequestTypeWireProbe);
		yield return WireCase("boosted-missing", HtmxRequestHeaderNames.Boosted, null, InvalidBoostedWireProbe);
		yield return WireCase("boosted-false", HtmxRequestHeaderNames.Boosted, ["false"], InvalidBoostedWireProbe);
		yield return WireCase("boosted-malformed", HtmxRequestHeaderNames.Boosted, ["TRUE"], InvalidBoostedWireProbe);
		yield return WireCase("boosted-repeated", HtmxRequestHeaderNames.Boosted, ["true", "true"], InvalidBoostedWireProbe);
		yield return WireCase("boosted-contradictory", HtmxRequestHeaderNames.Boosted, ["true", "false"], InvalidBoostedWireProbe);
		yield return WireCase("history-missing", HtmxRequestHeaderNames.HistoryRestoreRequest, null, InvalidHistoryRestoreWireProbe);
		yield return WireCase("history-false", HtmxRequestHeaderNames.HistoryRestoreRequest, ["false"], InvalidHistoryRestoreWireProbe);
		yield return WireCase("history-malformed", HtmxRequestHeaderNames.HistoryRestoreRequest, ["TRUE"], InvalidHistoryRestoreWireProbe);
		yield return WireCase("history-repeated", HtmxRequestHeaderNames.HistoryRestoreRequest, ["true", "true"], InvalidHistoryRestoreWireProbe);
		yield return WireCase("history-contradictory", HtmxRequestHeaderNames.HistoryRestoreRequest, ["true", "false"], InvalidHistoryRestoreWireProbe);
		yield return WireCase("current-url-missing", HtmxRequestHeaderNames.CurrentUrl, null, InvalidCurrentUrlWireProbe);
		yield return WireCase("current-url-malformed", HtmxRequestHeaderNames.CurrentUrl, ["ftp://example.test/orders"], InvalidCurrentUrlWireProbe);
		yield return WireCase("current-url-repeated", HtmxRequestHeaderNames.CurrentUrl, ["https://example.test/orders", "https://example.test/orders"], InvalidCurrentUrlWireProbe);
		yield return WireCase("current-url-contradictory", HtmxRequestHeaderNames.CurrentUrl, ["https://example.test/orders", "https://example.test/other"], InvalidCurrentUrlWireProbe);
		yield return WireCase("source-missing", HtmxRequestHeaderNames.Source, null, InvalidSourceWireProbe);
		yield return WireCase("source-malformed", HtmxRequestHeaderNames.Source, [" "], InvalidSourceWireProbe);
		yield return WireCase("source-repeated", HtmxRequestHeaderNames.Source, ["button#submit", "button#submit"], InvalidSourceWireProbe);
		yield return WireCase("source-contradictory", HtmxRequestHeaderNames.Source, ["button#submit", "button#other"], InvalidSourceWireProbe);
		yield return WireCase("target-missing", HtmxRequestHeaderNames.Target, null, InvalidTargetWireProbe);
		yield return WireCase("target-malformed", HtmxRequestHeaderNames.Target, [" "], InvalidTargetWireProbe);
		yield return WireCase("target-repeated", HtmxRequestHeaderNames.Target, ["section#result", "section#result"], InvalidTargetWireProbe);
		yield return WireCase("target-contradictory", HtmxRequestHeaderNames.Target, ["section#result", "div#other"], InvalidTargetWireProbe);
	}

	private static object[] WireCase(
		string caseName,
		string headerName,
		string[]? values,
		WireProbeSnapshot expected)
		=> [new InvalidCoreRequestHeaderWireCase(caseName, headerName, values, expected)];

	private static HttpRequestMessage CreateMarkerRequest(string path, string[]? markerValues)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Host = ReportHost;
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add(
			Issue359AuthenticationHandler.UserHeaderName,
			Issue359AuthenticationHandler.ReportUserName);
		if (markerValues is not null)
		{
			request.Headers.TryAddWithoutValidation("HX-Request", markerValues);
		}

		return request;
	}

	private static HttpRequestMessage CreateWireProbeRequest(string headerName, string[]? values)
	{
		var request = new HttpRequestMessage(
			HttpMethod.Get,
			MarkerPageRequestPath + "?wire-probe=true");
		request.Headers.Host = ReportHost;
		AddHeader(request, HtmxRequestHeaderNames.HtmxRequest, ["true"]);
		AddHeader(request, HtmxRequestHeaderNames.RequestType, ["partial"]);
		AddHeader(request, HtmxRequestHeaderNames.Boosted, ["true"]);
		AddHeader(request, HtmxRequestHeaderNames.HistoryRestoreRequest, ["true"]);
		AddHeader(request, HtmxRequestHeaderNames.CurrentUrl, ["https://example.test/orders"]);
		AddHeader(request, HtmxRequestHeaderNames.Source, ["button#submit"]);
		AddHeader(request, HtmxRequestHeaderNames.Target, ["section#result"]);
		request.Headers.Add(
			Issue359AuthenticationHandler.UserHeaderName,
			Issue359AuthenticationHandler.ReportUserName);
		request.Headers.Remove(headerName);
		AddHeader(request, headerName, values);

		return request;
	}

	private static void AddHeader(HttpRequestMessage request, string name, string[]? values)
	{
		if (values is null)
		{
			return;
		}

		foreach (var value in values)
		{
			request.Headers.TryAddWithoutValidation(name, value);
		}
	}

	private static HtmxRequest ParseWireRequest(
		string[]? marker = null,
		string[]? requestType = null,
		string[]? boosted = null,
		string[]? historyRestore = null,
		string[]? currentUrl = null,
		string[]? source = null,
		string[]? target = null)
	{
		var context = new DefaultHttpContext();
		SetHeader(context, HtmxRequestHeaderNames.HtmxRequest, marker);
		SetHeader(context, HtmxRequestHeaderNames.RequestType, requestType);
		SetHeader(context, HtmxRequestHeaderNames.Boosted, boosted);
		SetHeader(context, HtmxRequestHeaderNames.HistoryRestoreRequest, historyRestore);
		SetHeader(context, HtmxRequestHeaderNames.CurrentUrl, currentUrl);
		SetHeader(context, HtmxRequestHeaderNames.Source, source);
		SetHeader(context, HtmxRequestHeaderNames.Target, target);
		return context.GetHtmxContext().Request;
	}

	private static void SetHeader(DefaultHttpContext context, string name, string[]? values)
	{
		if (values is not null)
		{
			context.Request.Headers[name] = new Microsoft.Extensions.Primitives.StringValues(values);
		}
	}

	private static readonly string[] NavigationHeaderNames =
	[
		HtmxResponseHeaderNames.Location,
		HtmxResponseHeaderNames.PushUrl,
		HtmxResponseHeaderNames.Redirect,
		HtmxResponseHeaderNames.Refresh,
		HtmxResponseHeaderNames.ReplaceUrl,
	];

	private DefaultHttpContext CreateTriggerContext(bool validMarker)
	{
		var context = new DefaultHttpContext
		{
			RequestServices = app.Services,
		};
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = validMarker ? "true" : "false";
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers["X-Application"] = "retained";
		context.Response.Headers[HtmxResponseHeaderNames.Trigger] = "manual";
		context.Response.Body = new MemoryStream([1, 2, 3]);
		context.Response.ContentLength = 3;
		return context;
	}

	private static void AssertTriggerContextUnchanged(DefaultHttpContext context)
	{
		Assert.Multiple(
			() => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
			() => Assert.Equal("retained", context.Response.Headers["X-Application"]),
			() => Assert.Equal("manual", context.Response.Headers[HtmxResponseHeaderNames.Trigger]),
			() => Assert.Equal(3, context.Response.ContentLength),
			() => Assert.Equal(3, context.Response.Body.Length),
			() => Assert.Equal(0, context.Response.Body.Position)
		);
	}

	private sealed class ThrowingTriggerDetail
	{
	}

	private sealed class NullTriggerDetail
	{
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

	private sealed class ThrowingTriggerDetailConverter : JsonConverter<ThrowingTriggerDetail>
	{
		public override ThrowingTriggerDetail Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options) => throw new NotSupportedException();

		public override void Write(
			Utf8JsonWriter writer,
			ThrowingTriggerDetail value,
			JsonSerializerOptions options) => throw new JsonException("Deliberate trigger detail serialization failure.");
	}

	private static DefaultHttpContext CreateNavigationContext(bool validMarker)
	{
		var context = new DefaultHttpContext
		{
			RequestServices = new ServiceCollection().BuildServiceProvider(),
		};
		context.Request.Scheme = "https";
		context.Request.Host = new HostString(ReportHost);
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = validMarker ? "true" : "false";
		return context;
	}

	private static IEnumerable<Func<HtmxResponse, HtmxResponse>> GetNavigationOperations(string destination)
	{
		yield return response => response.Location(destination);
		yield return response => response.PushUrl(destination);
		yield return response => response.Redirect(destination);
		yield return response => response.ReplaceUrl(destination);
	}

	private static IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetNavigationHeaders(
		HttpContext context)
		=> context.Response.Headers.Where(header =>
			NavigationHeaderNames.Contains(header.Key, StringComparer.OrdinalIgnoreCase));

	private static void AssertRejectedNavigation(Func<HtmxResponse, HtmxResponse> operation)
	{
		var context = CreateNavigationContext(validMarker: true);
		var response = context.GetHtmxContext().Response;

		Assert.Throws<ArgumentException>(() => operation(response));
		AssertNavigationStateUnchanged(context);
	}

	private static void AssertNavigationStateUnchanged(HttpContext context)
	{
		Assert.Multiple(
			() => Assert.Empty(GetNavigationHeaders(context)),
			() => Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode)
		);
	}

}
