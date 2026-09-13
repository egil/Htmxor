#if NET11_0_OR_GREATER
using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue212SessionTests
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Completed_async_change_is_available_to_the_next_request(bool htmxor, bool fragment)
	{
		await using var host = await Issue212SessionHost.StartAsync(htmxor);
		var cookie = await host.CreateSessionAsync();

		var changed = await host.SendAsync(cookie, fragment, "?Change=completed");
		AssertValue(changed, "completed");
		Assert.Equal(!fragment, changed.Body.Contains("data-outside", StringComparison.Ordinal));

		AssertValue(await host.SendAsync(cookie, fragment), "completed");
		AssertValue(await host.SendAsync(await host.CreateSessionAsync(), fragment), "missing");
	}

	[Theory]
	[InlineData(false, false, "POST")]
	[InlineData(true, false, "POST")]
	[InlineData(true, true, "PUT")]
	public async Task Protected_action_persists_its_completed_change(bool htmxor, bool fragment, string method)
	{
		await using var host = await Issue212SessionHost.StartAsync(htmxor);
		var cookie = await host.CreateSessionAsync();

		AssertValue(await host.SendAsync(cookie, fragment, "?ActionValue=saved", method), "saved");
		AssertValue(await host.SendAsync(cookie, fragment), "saved");
	}

	[Theory]
	[InlineData(false, false, "POST")]
	[InlineData(true, false, "POST")]
	[InlineData(true, true, "PUT")]
	public async Task Rejected_unsafe_request_leaves_the_session_unchanged(bool htmxor, bool fragment, string method)
	{
		await using var host = await Issue212SessionHost.StartAsync(htmxor);
		var cookie = await host.CreateSessionAsync();
		AssertValue(await host.SendAsync(cookie, fragment, "?Change=retained"), "retained");
		AssertValue(await host.SendAsync(cookie, fragment), "retained");

		var rejected = await host.SendAsync(cookie, fragment, "?Change=lifecycle&ActionValue=callback", method, rejected: true);

		Assert.Equal(HttpStatusCode.BadRequest, rejected.Status);
		AssertValue(await host.SendAsync(cookie, fragment), "retained");
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Overlapping_distinct_cookie_sessions_keep_their_completed_values(bool htmxor, bool fragment)
	{
		await using var host = await Issue212SessionHost.StartAsync(htmxor);
		var alice = await host.CreateSessionAsync();
		var bob = await host.CreateSessionAsync();
		Assert.NotEqual(alice, bob);
		var first = host.SendAsync(alice, fragment, "?Change=alice&Hold=true");
		var second = host.SendAsync(bob, fragment, "?Change=bob&Hold=true");
		try
		{
			await host.Gate.BothArrived.Task.WaitAsync(TimeSpan.FromSeconds(20));
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
		}
		finally
		{
			host.Gate.Release.TrySetResult();
			await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(20));
		}

		AssertValue(await first, "alice");
		AssertValue(await second, "bob");
		AssertValue(await host.SendAsync(alice, fragment), "alice");
		AssertValue(await host.SendAsync(bob, fragment), "bob");
	}

	private static void AssertValue((HttpStatusCode Status, string Body) response, string value)
	{
		Assert.Equal(HttpStatusCode.OK, response.Status);
		Assert.Contains($"data-session=\"{value}\"", response.Body, StringComparison.Ordinal);
	}
}
#endif
