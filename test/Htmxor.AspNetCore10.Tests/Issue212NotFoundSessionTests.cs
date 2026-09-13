#if NET11_0_OR_GREATER
using System.Net;
using System.Text.RegularExpressions;

namespace Htmxor.AspNetCore10;

public sealed class Issue212NotFoundSessionTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Completed_not_found_preserves_the_surviving_roots_async_session_change(bool htmxor)
	{
		await using var host = await Issue212SessionHost.StartAsync(htmxor, rootSession: true);
		var cookie = await host.CreateSessionAsync();
		AssertRootValue(await host.SendAsync(cookie, false, "?RootChange=before"), "before");
		AssertRootValue(await host.SendAsync(cookie, false), "before");
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-212/not-found?RootChange=after-404");
		request.Headers.Add("Cookie", cookie);

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		AssertRootValue(await host.SendAsync(cookie, false), "after-404");
	}

	private static void AssertRootValue((HttpStatusCode Status, string Body) response, string value)
	{
		Assert.Equal(HttpStatusCode.OK, response.Status);
		Assert.Equal(value, Regex.Match(response.Body, "data-root-session=\"([^\"]*)\"").Groups[1].Value);
	}
}
#endif
