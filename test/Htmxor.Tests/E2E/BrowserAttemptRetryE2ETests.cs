using Htmxor.TestAssets.Blazewright;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

/// <summary>
/// Real-browser probes of <see cref="BrowserAttemptRetryRunner"/> itself
/// (https://github.com/egil/Htmxor/issues/252), not of the four existing E2E facts it will later
/// run. Each probe aborts one request with a real Chromium network error, using
/// <c>route.AbortAsync</c>, on the first attempt only, and asserts the abort actually engaged so a
/// drifted probe cannot pass vacuously.
/// </summary>
public class BrowserAttemptRetryE2ETests : PageTest
{
	private readonly ITestOutputHelper outputHelper;

	public BrowserAttemptRetryE2ETests(ITestOutputHelper outputHelper, PlaywrightFixture fixture) : base(fixture)
	{
		this.outputHelper = outputHelper;
	}

	// The first-navigation variant: Chromium fails the first GotoAsync itself, so the thrown
	// PlaywrightException names net::ERR_CONNECTION_CLOSED directly.
	[Fact]
	public async Task A_first_navigation_aborted_with_connectionclosed_is_retried_and_the_retry_lands()
	{
		var attemptNumber = 0;
		var notes = new List<string>();
		var abortEngaged = false;

		await BrowserAttemptRetryRunner.RunOnPageAsync(
			Context,
			async page =>
			{
				attemptNumber++;
				var thisAttemptIsFirst = attemptNumber == 1;

				await page.RouteAsync("**/EventHandlers", async route =>
				{
					if (thisAttemptIsFirst)
					{
						abortEngaged = true;
						await route.AbortAsync("connectionclosed");
						return;
					}

					await route.ContinueAsync();
				});

				await page.GotoAsync("/EventHandlers");
				await Expect(page.Locator("#event-handlers")).ToBeVisibleAsync();
			},
			note =>
			{
				outputHelper.WriteLine(note);
				notes.Add(note);
			});

		Assert.True(abortEngaged, "The connectionclosed abort never engaged.");
		Assert.Equal(2, attemptNumber);
		AssertRetryNoteWasWritten(notes, BrowserAttemptRetryScope.ConnectionClosedError);
	}

	// The mid-test htmx variant: the first GotoAsync succeeds, but the htmx GET for a click is
	// aborted, so htmx swaps nothing and the test fails on the assertion timeout below -- an
	// exception whose message never names the network error. Only the recorded RequestFailed error
	// qualifies this attempt for a retry.
	[Fact]
	public async Task A_mid_test_htmx_request_aborted_with_connectionclosed_is_retried_and_the_retry_lands()
	{
		var attemptNumber = 0;
		var notes = new List<string>();
		var abortEngaged = false;

		await BrowserAttemptRetryRunner.RunOnPageAsync(
			Context,
			async page =>
			{
				attemptNumber++;
				var thisAttemptIsFirst = attemptNumber == 1;

				await page.RouteAsync("**/EventHandlers", async route =>
				{
					var isHtmxGet = route.Request.Method == "GET" && route.Request.Headers.ContainsKey("hx-request");
					if (thisAttemptIsFirst && isHtmxGet)
					{
						abortEngaged = true;
						await route.AbortAsync("connectionclosed");
						return;
					}

					await route.ContinueAsync();
				});

				await page.GotoAsync("/EventHandlers");
				await page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).First.ClickAsync();
				await Expect(page.Locator("#handler")).ToContainTextAsync("OnGet");
			},
			note =>
			{
				outputHelper.WriteLine(note);
				notes.Add(note);
			});

		Assert.True(abortEngaged, "The connectionclosed abort never engaged.");
		Assert.Equal(2, attemptNumber);
		AssertRetryNoteWasWritten(notes, BrowserAttemptRetryScope.ConnectionClosedError);
	}

	// Negative: net::ERR_CONNECTION_REFUSED is a different error and must not be retried, even
	// though it is a real Chromium abort on the first attempt, exactly like the two facts above.
	[Fact]
	public async Task A_first_navigation_aborted_with_connectionrefused_is_not_retried()
	{
		var attemptNumber = 0;
		var notes = new List<string>();
		var abortEngaged = false;

		var thrown = await Assert.ThrowsAnyAsync<PlaywrightException>(() =>
			BrowserAttemptRetryRunner.RunOnPageAsync(
				Context,
				async page =>
				{
					attemptNumber++;

					await page.RouteAsync("**/EventHandlers", async route =>
					{
						abortEngaged = true;
						await route.AbortAsync("connectionrefused");
					});

					await page.GotoAsync("/EventHandlers");
				},
				note =>
				{
					outputHelper.WriteLine(note);
					notes.Add(note);
				}));

		Assert.True(abortEngaged, "The connectionrefused abort never engaged.");
		Assert.Contains("net::ERR_CONNECTION_REFUSED", thrown.Message, StringComparison.Ordinal);
		Assert.Equal(1, attemptNumber);
		Assert.Empty(notes);
	}

	private static void AssertRetryNoteWasWritten(List<string> notes, string reason) =>
		Assert.Contains(notes, note =>
			note.Contains(reason, StringComparison.Ordinal) &&
			note.Contains(BrowserAttemptRetryRunner.IssueUrl, StringComparison.Ordinal));
}
