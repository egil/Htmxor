using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	[Fact]
	public async Task Native_htmx4_markup_passes_through_the_component_endpoint_with_server_response_operations()
	{
		await using var scope = app.Services.CreateAsyncScope();
		await using var renderer = new HtmlRenderer(
			scope.ServiceProvider,
			scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		var markup = await renderer.Dispatcher.InvokeAsync(async () =>
		{
			var output = await renderer.RenderComponentAsync<Issue359ClientMarkup>();
			return output.ToHtmlString();
		});

		Assert.Multiple(
			() => Assert.Contains(
			"hx-trigger=\"intersect rootMargin:100px passive prevent\"",
			markup,
			StringComparison.Ordinal),
			() => Assert.Contains(
			"hx-swap=\"outerMorph target:#result strip:true swapEmpty:true\"",
			markup,
			StringComparison.Ordinal),
			() => Assert.Contains(
			"<hx-partial hx-target=\"#result\" hx-swap=\"acmeMorph\">",
			markup,
			StringComparison.Ordinal)
		);

		using var request = CreateMarkerRequest(RoutePrefix + "/issue359-native-markup", ["true"]);
		using var rendered = await client.SendAsync(request);
		var body = await rendered.Content.ReadAsStringAsync();
		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, rendered.StatusCode),
			() => Assert.Contains("hx-trigger=\"intersect rootMargin:100px passive prevent\"", body, StringComparison.Ordinal),
			() => Assert.Contains("hx-swap=\"outerMorph target:#result strip:true swapEmpty:true\"", body, StringComparison.Ordinal),
			() => Assert.Contains("<hx-partial hx-target=\"#result\" hx-swap=\"acmeMorph\">", body, StringComparison.Ordinal)
		);

		var context = new DefaultHttpContext
		{
			RequestServices = new ServiceCollection().BuildServiceProvider(),
		};
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		var response = context.GetHtmxContext().Response;

		var result = response.Reswap("acmeMorph settle:25ms");

		Assert.Multiple(
			() => Assert.Same(response, result),
			() => Assert.Equal(
			"acmeMorph settle:25ms",
			context.Response.Headers[HtmxResponseHeaderNames.Reswap].ToString())
		);
		response.Retarget("closest [data-acme-target]");
		response.Reselect(":scope > [data-acme-selection]");
		Assert.Multiple(
			() => Assert.Equal(
			"closest [data-acme-target]",
			context.Response.Headers[HtmxResponseHeaderNames.Retarget].ToString()),
			() => Assert.Equal(
			":scope > [data-acme-selection]",
			context.Response.Headers[HtmxResponseHeaderNames.Reselect].ToString())
		);
		response.Trigger("client:event");
		Assert.Equal(
			"{\"client:event\":{}}",
			context.Response.Headers[HtmxResponseHeaderNames.Trigger].ToString());
	}

}
