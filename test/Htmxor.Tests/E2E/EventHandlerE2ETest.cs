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

	// htmx reads a response's body (`await r.text()`) and applies the swap only after that,
	// strictly later than the response's headers, which is what a wait keyed on the response
	// event alone cannot see. Playwright cannot stall a body after real headers are already
	// sent, so this delays only the second GET's body read, through htmx's own per-request
	// `ctx.fetch` hook; the response itself is untouched. GET INLINE's response is also held, so
	// the second GET's swap (once its delayed body is finally read) reliably lands while GET
	// INLINE's own request is still in flight, deterministically - not only in a narrow window.
	private const int SecondGetBodyDelayMilliseconds = 300;
	private const int InlineHoldForBodyDelayMilliseconds = 600;

	private const string SecondGetBodyDelayScript = @"
(() => {
  let plainGets = 0;
  document.addEventListener('htmx:config:request', ev => {
    const ctx = ev.detail.ctx;
    const method = String(ctx.request.method || 'GET').toUpperCase();
    const url = String(ctx.request.action);
    if (method === 'GET' && !url.includes('inline') && ++plainGets === 2) {
      window.secondGetBodyDelayed = true;
      const f = window.fetch.bind(window);
      ctx.fetch = async (a, o) => {
        const r = await f(a, o);
        const text = r.text.bind(r);
        r.text = () => new Promise(res => setTimeout(() => res(text()), DELAY));
        return r;
      };
    }
  });
})();";

	[Fact]
	public async Task Sequence_shows_get_inlines_own_result_even_when_the_second_gets_body_lags_its_headers()
	{
		var page = await Context.NewPageAsync();
		await page.AddInitScriptAsync(SecondGetBodyDelayScript.Replace("DELAY", SecondGetBodyDelayMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));

		await page.RouteAsync("**/EventHandlers**", async route =>
		{
			var request = route.Request;
			if (request.Method == "GET" && request.Headers.ContainsKey("hx-request") && request.Url.Contains("inline"))
			{
				await Task.Delay(InlineHoldForBodyDelayMilliseconds);
			}

			await route.ContinueAsync();
		});

		await ClickEachHandlerAndAssert(page);

		Assert.True(await page.EvaluateAsync<bool>("() => window.secondGetBodyDelayed === true"), "The second GET's body-read delay never engaged.");
	}

	// Also run by Sequence_shows_get_inlines_own_result_even_when_the_second_gets_swap_is_delayed
	// and Sequence_shows_get_inlines_own_result_even_when_the_second_gets_body_lags_its_headers
	// under delayed responses; each step must wait for its own click's result.
	private async Task ClickEachHandlerAndAssert(IPage page)
	{
		await page.GotoAsync("/EventHandlers");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).First);
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).Nth(1));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "GET INLINE", Exact = true }));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnGetInline");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "POST" }));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPost");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "PUT" }));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPut");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "PATCH" }));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnPatch");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "DELETE" }));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnDelete");

		await ClickAndWaitForOwnSwapAsync(page.GetByRole(AriaRole.Button, new() { Name = "SUBMIT" }));
		await Expect(page.Locator("#handler")).ToContainTextAsync("OnSubmit");
	}

	// The next click must not be sent while this click's swap of #event-handlers is still pending.
	private async Task ClickAndWaitForOwnSwapAsync(ILocator button)
	{
		var region = button.Page.Locator("#event-handlers");
		await region.EvaluateAsync("el => el.dataset.replaced = 'pending'");
		await button.ClickAsync();
		await Expect(button.Page.Locator("#event-handlers[data-replaced]")).ToHaveCountAsync(0);
	}
}
