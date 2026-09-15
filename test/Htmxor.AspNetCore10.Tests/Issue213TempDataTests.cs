#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10;

public sealed class Issue213TempDataTests
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Completed_parameter_write_issues_a_cookie_and_reaches_the_next_request(bool htmxor, bool fragment)
	{
		await using var host = await Issue213TempDataHost.StartAsync(htmxor, new EphemeralDataProtectionProvider());
		var cookies = new CookieContainer();

		var written = await host.SendAsync(cookies, fragment, "/issue-213/tempdata?Change=completed");
		AssertMessage(written, "completed");
		Assert.Equal(!fragment, written.Body.Contains("data-outside", StringComparison.Ordinal));
		Assert.Contains(written.Cookies, cookie => cookie.StartsWith("issue213-tempdata=", StringComparison.Ordinal));
		AssertMessage(await host.SendAsync(cookies, fragment, "/issue-213/read"), "completed");
		AssertMessage(await host.SendAsync(cookies, fragment, "/issue-213/read"), "missing");
	}

	[Theory]
	[InlineData(false, false, "read")]
	[InlineData(true, false, "read")]
	[InlineData(true, true, "read")]
	[InlineData(false, false, "keep")]
	[InlineData(true, false, "keep")]
	[InlineData(true, true, "keep")]
	[InlineData(false, false, "peek")]
	[InlineData(true, false, "peek")]
	[InlineData(true, true, "peek")]
	public async Task Stock_cookie_is_consumed_or_retained_by_the_requested_control(bool htmxor, bool fragment, string mode)
	{
		var protection = new EphemeralDataProtectionProvider();
		await using var stock = await Issue213TempDataHost.StartAsync(false, protection);
		await using var host = await Issue213TempDataHost.StartAsync(htmxor, protection);
		var cookies = new CookieContainer();
		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/tempdata?Change=retained"), "retained");

		AssertMessage(await host.SendAsync(cookies, fragment, "/issue-213/read?Mode=" + mode), "retained");
		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/read"), mode == "read" ? "missing" : "retained");
		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/read"), "missing");
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Supplied_parameter_reads_a_stock_cookie_without_crossing_users(bool htmxor, bool fragment)
	{
		var protection = new EphemeralDataProtectionProvider();
		await using var stock = await Issue213TempDataHost.StartAsync(false, protection);
		await using var host = await Issue213TempDataHost.StartAsync(htmxor, protection);
		var alice = new CookieContainer();
		var bob = new CookieContainer();
		AssertMessage(await stock.SendAsync(alice, false, "/issue-213/tempdata?Change=alice"), "alice");
		AssertMessage(await stock.SendAsync(bob, false, "/issue-213/tempdata?Change=bob"), "bob");

		AssertMessage(await host.SendAsync(alice, fragment, "/issue-213/tempdata"), "alice");
		AssertMessage(await host.SendAsync(bob, fragment, "/issue-213/tempdata"), "bob");
		AssertMessage(await host.SendAsync(new CookieContainer(), fragment, "/issue-213/tempdata"), "missing");
	}

	[Theory]
	[InlineData(false, false, "POST", false)]
	[InlineData(true, false, "POST", false)]
	[InlineData(true, true, "PUT", false)]
	[InlineData(false, false, "POST", true)]
	[InlineData(true, false, "POST", true)]
	[InlineData(true, true, "POST", true)]
	public async Task Completed_action_preserves_message_through_response_or_redirect(bool htmxor, bool fragment, string method, bool redirect)
	{
		var protection = new EphemeralDataProtectionProvider();
		await using var host = await Issue213TempDataHost.StartAsync(htmxor, protection);
		await using var stock = await Issue213TempDataHost.StartAsync(false, protection);
		var cookies = new CookieContainer();

		var path = method == "PUT" ? "/issue-213/action" : "/issue-213/tempdata";
		var response = await host.SendAsync(cookies, fragment,
			$"{path}?ActionValue=saved&Redirect={redirect}", method);
		if (redirect)
		{
			Assert.Equal(fragment ? HttpStatusCode.OK : HttpStatusCode.Found, response.Status);
			Assert.Equal("/issue-213/read", fragment ? response.HxRedirect : new Uri(response.Location!, UriKind.RelativeOrAbsolute).PathAndQuery);
		}
		else
		{
			AssertMessage(response, "saved");
		}

		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/read"), "saved");
		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/read"), "missing");
	}

	[Theory]
	[InlineData(false, false, "POST")]
	[InlineData(true, false, "POST")]
	[InlineData(true, true, "PUT")]
	public async Task Rejected_request_does_not_write_or_consume_the_existing_message(bool htmxor, bool fragment, string method)
	{
		var protection = new EphemeralDataProtectionProvider();
		await using var stock = await Issue213TempDataHost.StartAsync(false, protection);
		await using var host = await Issue213TempDataHost.StartAsync(htmxor, protection);
		var cookies = new CookieContainer();
		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/tempdata?Change=original"), "original");

		var path = method == "PUT" ? "/issue-213/action" : "/issue-213/tempdata";
		var rejected = await host.SendAsync(cookies, fragment,
			path + "?Change=lifecycle&ActionValue=callback", method, rejected: true);

		Assert.Equal(HttpStatusCode.BadRequest, rejected.Status);
		Assert.Empty(rejected.Cookies);
		AssertMessage(await stock.SendAsync(cookies, false, "/issue-213/read"), "original");
	}

	private static void AssertMessage(Issue213Response response, string message)
	{
		Assert.Equal(HttpStatusCode.OK, response.Status);
		Assert.Contains($"data-message=\"{message}\"", response.Body, StringComparison.Ordinal);
	}
}
#endif
