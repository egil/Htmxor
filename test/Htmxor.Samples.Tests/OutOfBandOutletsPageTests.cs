using System.Net;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace Htmxor.Samples.Tests;

/// <summary>
/// Protects <c>samples/HtmxorExamples/Components/Pages/Examples/OutOfBandOutlets/Index.razor</c>:
/// a partial PUT from count zero returns the selected counter, toast and odd header message update.
/// </summary>
public sealed class OutOfBandOutletsPageTests(ITestOutputHelper output)
{
	[Fact]
	public async Task Put_out_of_band_outlets_returns_counter_toast_and_odd_header_message()
	{
		await using var host = await SampleKestrelHost.StartAsync("samples/HtmxorExamples/HtmxorExamples.csproj");
		using var page = await host.Client.GetAsync("/examples/out-of-band-outlets");
		var markup = await page.Content.ReadAsStringAsync();
		var token = Regex.Match(markup, "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"", RegexOptions.CultureInvariant).Groups["token"].Value;
		var cookie = Assert.Single(page.Headers.GetValues("Set-Cookie").Where(value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))).Split(';')[0];
		Assert.False(string.IsNullOrWhiteSpace(token));

		using var request = new HttpRequestMessage(HttpMethod.Put, "/examples/out-of-band-outlets")
		{
			Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("CurrentCount", "0")]),
		};
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("HX-Target", "div#counter");
		request.Headers.Add("Cookie", cookie);
		request.Headers.Add("RequestVerificationToken", token);
		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		output.WriteLine("Partial PUT response: {0}", body);
		PartialResponseAssertions.AreCounterToastAndMessageFragments(body, "The count is odd!");
	}
}
