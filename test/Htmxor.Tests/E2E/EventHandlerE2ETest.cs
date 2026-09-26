using Htmxor.TestAssets.Blazewright;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

public class EventHandlerE2ETest : PageTest
{
	private const int SecondGetReleaseBoundMilliseconds = 3_000;
	private const int InlineHoldFallbackMilliseconds = 6_000;

	private readonly ITestOutputHelper outputHelper;

	public EventHandlerE2ETest(ITestOutputHelper outputHelper, PlaywrightFixture fixture) : base(fixture)
	{
		this.outputHelper = outputHelper;
	}

	[Fact]
	public async Task Invoke_event_handler_methods()
	{
		var page = await Context.NewPageAsync();
		await ClickEachHandlerAndAssert(page);
	}

	// Holds the second GET's response for SecondGetReleaseBoundMilliseconds, and GET INLINE's
	// response until the second GET's has arrived. If the shared sequence clicks GET INLINE before
	// the second GET's swap lands, that swap replaces #event-handlers under GET INLINE's in-flight
	// request and #handler never shows OnGetInline. The bound rejects a fixed sleep shorter than
	// it; it cannot tell a wait on the click's own request from a wait on some other signal.
	//
	// Only this step is held: it is the only one whose expected text ("OnGet") the previous step
	// already rendered. Every other step expects text only its own response renders, so delaying
	// that response cannot make the sequence move on early.
	[Fact]
	public async Task Sequence_shows_get_inlines_own_result_even_when_the_second_gets_swap_is_delayed()
	{
		var page = await Context.NewPageAsync();

		var htmxGetCount = 0;
		var secondGetLanded = new TaskCompletionSource();

		await page.RouteAsync("**/EventHandlers**", async route =>
		{
			var request = route.Request;
			var isInline = request.Url.Contains("inline");
			var isSecondPlainGet = !isInline && request.Headers.ContainsKey("hx-request") && Interlocked.Increment(ref htmxGetCount) == 2;

			if (isSecondPlainGet)
			{
				await Task.Delay(SecondGetReleaseBoundMilliseconds);

				var ownResponse = page.WaitForResponseAsync(r => r.Request == request);
				await route.ContinueAsync();
				await ownResponse;
				secondGetLanded.TrySetResult();
				return;
			}

			if (isInline)
			{
				// Defensive fallback only; not expected to fire in either the red or the green
				// case, since the second GET's own response always resolves well before it.
				await Task.WhenAny(secondGetLanded.Task, Task.Delay(InlineHoldFallbackMilliseconds));
			}

			await route.ContinueAsync();
		});

		await ClickEachHandlerAndAssert(page);
	}

	// Also run by Sequence_shows_get_inlines_own_result_even_when_the_second_gets_swap_is_delayed
	// under delayed responses; each step must wait for its own click's result.
	private async Task ClickEachHandlerAndAssert(IPage page)
	{
		await page.GotoAsync("/EventHandlers");

		await page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).First.ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");

		await page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).Nth(1).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");

		await page.GetByRole(AriaRole.Button, new() { Name = "GET INLINE", Exact = true }).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGetInline");

		await page.GetByRole(AriaRole.Button, new() { Name = "POST" }).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPost");

		await page.GetByRole(AriaRole.Button, new() { Name = "PUT" }).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPut");

		await page.GetByRole(AriaRole.Button, new() { Name = "PATCH" }).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPatch");

		await page.GetByRole(AriaRole.Button, new() { Name = "DELETE" }).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnDelete");

		await page.GetByRole(AriaRole.Button, new() { Name = "SUBMIT" }).ClickAsync();
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnSubmit");
	}
}
