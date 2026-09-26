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

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).First, IsOwnPlainGetResponse);
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).Nth(1), IsOwnPlainGetResponse);
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "GET INLINE", Exact = true }), IsOwnInlineGetResponse);
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGetInline");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "POST" }), r => IsOwnMethodResponse(r, "POST"));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPost");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "PUT" }), r => IsOwnMethodResponse(r, "PUT"));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPut");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "PATCH" }), r => IsOwnMethodResponse(r, "PATCH"));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPatch");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "DELETE" }), r => IsOwnMethodResponse(r, "DELETE"));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnDelete");

		await ClickAndWaitForOwnResponseAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "SUBMIT" }), r => IsOwnMethodResponse(r, "POST"));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnSubmit");
	}

	// Arms the wait for this click's own response before dispatching the click, so the very next
	// matching response is unambiguously the one this click caused - never a response an earlier
	// step already consumed, and never dependent on what #handler currently shows. Two steps here
	// share a method and URL with another step (both GET clicks; the POST button and the SUBMIT
	// form), so request content alone cannot always tell "this click's response" apart from
	// another step's. Arming the wait immediately before the click, and never issuing a click
	// before the previous one's own wait resolved, is what makes the very next matching response
	// this click's own rather than a stale or future one.
	private static async Task ClickAndWaitForOwnResponseAsync(IPage page, ILocator button, Func<IResponse, bool> isOwnResponse)
	{
		var ownResponse = page.WaitForResponseAsync(isOwnResponse);
		await button.ClickAsync();
		await ownResponse;
	}

	private static bool IsOwnPlainGetResponse(IResponse response) =>
		IsOwnMethodResponse(response, "GET") && !response.Url.Contains("inline");

	private static bool IsOwnInlineGetResponse(IResponse response) =>
		IsOwnMethodResponse(response, "GET") && response.Url.Contains("inline");

	private static bool IsOwnMethodResponse(IResponse response, string method) =>
		response.Request.Method == method && response.Request.Headers.ContainsKey("hx-request");
}
