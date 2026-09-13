using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue210FormActionTests
{
	[Theory]
	[InlineData(true)]
#if NET11_0_OR_GREATER
	[InlineData(false)]
#endif
	public async Task Accepted_named_form_POST_supplies_submitted_value_to_generated_callback(bool tokens)
	{
		await using var host = await Issue210SecurityHost.StartAsync(tokens: tokens);
		using var request = Issue210SecurityHost.Request("POST");
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "save",
			["Value"] = "submitted",
		});
		if (tokens)
		{
			await host.AddCredentialsAsync(request);
			request.Headers.Add("Sec-Fetch-Site", "cross-site");
			request.Headers.Add("Origin", "https://untrusted.example");
		}
		else
		{
			request.Headers.Add("Sec-Fetch-Site", "same-origin");
		}

		using var response = await host.Client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();

		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		Assert.Equal(1, host.Probe.Callbacks);
		Assert.Contains("data-callback-value=\"submitted\"", html, StringComparison.Ordinal);
	}
}
