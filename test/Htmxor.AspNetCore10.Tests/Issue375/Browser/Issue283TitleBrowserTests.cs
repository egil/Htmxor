#if NET10_0
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Htmxor.AspNetCore10.Issue375.Browser;

[Trait("Category", "Browser")]
public sealed class Issue283TitleBrowserTests(ITestOutputHelper output)
{
	[Fact]
	public async Task Application_owned_htmx4_defaults_apply_title_from_direct_partial_response()
	{
		await using var app = Issue211Host.Create(new Issue211Probe(true));
		await app.StartAsync();
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
		var page = await browser.NewPageAsync();
		await page.GotoAsync(Assert.Single(app.Urls) + "/issue-283/title");
		await page.WaitForFunctionAsync("() => window.htmx?.version === '4.0.0'");
		await page.EvaluateAsync("() => document.title = 'Issue 283 initial title'");
		var response = await page.RunAndWaitForResponseAsync(
			() => page.GetByRole(AriaRole.Button, new() { Name = "Load title", Exact = true }).ClickAsync(),
			response => response.Url.EndsWith("/issue-283/title-partial", StringComparison.Ordinal));
		Assert.Equal(200, response.Status);
		Assert.Contains("<title>Issue 283 partial title</title>", await response.TextAsync(), StringComparison.Ordinal);
		Assert.Equal("partial", (await response.Request.AllHeadersAsync())["hx-request-type"]);
		await Assertions.Expect(page.Locator("#issue283-loaded")).ToHaveTextAsync("loaded");
		await Assertions.Expect(page).ToHaveTitleAsync("Issue 283 partial title");
		output.WriteLine($"htmx 4.0.0 defaults applied partial <title>; Chromium {browser.Version}; {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
	}
}

#endif
