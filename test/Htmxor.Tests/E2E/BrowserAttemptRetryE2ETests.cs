using Htmxor.TestAssets.Blazewright;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

/// <summary>
/// Real-browser probes of <see cref="BrowserAttemptRetryRunner"/> itself
/// (https://github.com/egil/Htmxor/issues/252), not of the E2E facts that run through it. Each
/// abort probe aborts one request with a real Chromium network error, using
/// <c>route.AbortAsync</c>, and asserts that the runner actually retried (a note was written) so a
/// drifted probe cannot pass vacuously. The three retried abort probes abort only on their first
/// attempt. None of the retried probes assert that its own injected abort engaged: an unrelated
/// real qualifying failure (about 1% of runs, per the issue's own evidence) can pre-empt it and
/// still produce the same observable retry-and-pass outcome, and asserting the injected abort
/// itself engaged would turn that correct behavior into a false failure.
/// </summary>
public class BrowserAttemptRetryE2ETests : PageTest
{
	private const string ConnectionRefusedError = "net::ERR_CONNECTION_REFUSED";

	private readonly ITestOutputHelper outputHelper;

	public BrowserAttemptRetryE2ETests(ITestOutputHelper outputHelper, PlaywrightFixture fixture) : base(fixture)
	{
		this.outputHelper = outputHelper;
	}

	// The first-navigation variant: Chromium aborts the first GotoAsync, and the thrown
	// PlaywrightException names net::ERR_CONNECTION_CLOSED directly. In a real browser Chromium also
	// reports the same aborted navigation as a qualifying RequestFailed, so this probe does not
	// isolate the thrown-exception-only branch. That branch (a qualifying thrown message with no
	// accompanying qualifying request failure) has constructed evidence only, in
	// BrowserAttemptRetryScopeTests' A_thrown_exception_naming_*_retries facts.
	[Fact]
	public async Task A_first_navigation_aborted_with_connectionclosed_is_retried_and_the_retry_lands()
	{
		var attemptNumber = 0;
		var notes = new List<string>();

		await BrowserAttemptRetryRunner.RunOnPageAsync(
			Context,
			async page =>
			{
				attemptNumber++;
				var thisAttemptIsFirst = attemptNumber == 1;
				LogRequestFailures(page, attemptNumber);

				await page.RouteAsync("**/EventHandlers", async route =>
				{
					if (thisAttemptIsFirst)
					{
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

		Assert.Equal(2, attemptNumber);
		AssertRetryNoteWasWritten(notes);
	}

	// The mid-test htmx variant: the first GotoAsync succeeds, but the htmx GET for a click is
	// aborted, so htmx swaps nothing and the test fails on the assertion timeout below -- an
	// exception whose message never names the network error. Only the recorded RequestFailed error
	// qualifies this attempt for a retry; LogRequestFailures below writes that observation to this
	// test's own output, so the red evidence names net::ERR_CONNECTION_CLOSED even though the thrown
	// exception does not.
	[Fact]
	public async Task A_mid_test_htmx_request_aborted_with_connectionclosed_is_retried_and_the_retry_lands()
	{
		var attemptNumber = 0;
		var notes = new List<string>();

		await BrowserAttemptRetryRunner.RunOnPageAsync(
			Context,
			async page =>
			{
				attemptNumber++;
				var thisAttemptIsFirst = attemptNumber == 1;
				LogRequestFailures(page, attemptNumber);

				await page.RouteAsync("**/EventHandlers", async route =>
				{
					var isHtmxGet = route.Request.Method == "GET" && route.Request.Headers.ContainsKey("hx-request");
					if (thisAttemptIsFirst && isHtmxGet)
					{
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

		Assert.Equal(2, attemptNumber);
		AssertRetryNoteWasWritten(notes);
	}

	// P004: the runner must observe request failures on any page the body opens through the shared
	// context, not only the page it hands to the body. This body deliberately ignores the handed
	// page and opens its own, as a real body reasonably might for a second tab. Uses the mid-test
	// shape (abort only the htmx GET, fail on the Expect) deliberately: aborting the own page's
	// first navigation would qualify through the thrown exception alone, telling nothing about
	// whether the runner observed the request failure or not.
	[Fact]
	public async Task A_request_aborted_on_a_page_the_body_opens_itself_is_still_observed_and_retried()
	{
		var attemptNumber = 0;
		var notes = new List<string>();

		await BrowserAttemptRetryRunner.RunOnPageAsync(
			Context,
			async _ =>
			{
				attemptNumber++;
				var thisAttemptIsFirst = attemptNumber == 1;

				var ownPage = await Context.NewPageAsync();
				try
				{
					LogRequestFailures(ownPage, attemptNumber);

					await ownPage.RouteAsync("**/EventHandlers", async route =>
					{
						var isHtmxGet = route.Request.Method == "GET" && route.Request.Headers.ContainsKey("hx-request");
						if (thisAttemptIsFirst && isHtmxGet)
						{
							await route.AbortAsync("connectionclosed");
							return;
						}

						await route.ContinueAsync();
					});

					await ownPage.GotoAsync("/EventHandlers");
					await ownPage.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).First.ClickAsync();
					await Expect(ownPage.Locator("#handler")).ToContainTextAsync("OnGet");
				}
				finally
				{
					await ownPage.CloseAsync();
				}
			},
			note =>
			{
				outputHelper.WriteLine(note);
				notes.Add(note);
			});

		Assert.Equal(2, attemptNumber);
		AssertRetryNoteWasWritten(notes);
	}

	// Cheap pin for the "fresh page per attempt" design constraint (issue #252 Work item 3): a
	// runner that reused one page across attempts would hand the retry a page a prior aborted
	// navigation already left in a failed state. The qualifying failure here is a constructed throw,
	// not an injected network abort, so it is unaffected by the environmental race the other probes
	// guard against: whether the real navigation underneath also happens to fail with the same
	// error changes nothing observable, because either one alone already qualifies the attempt.
	[Fact]
	public async Task Each_attempt_of_a_qualifying_failure_gets_a_fresh_page()
	{
		var pages = new List<IPage>();
		var attemptNumber = 0;

		await BrowserAttemptRetryRunner.RunOnPageAsync(
			Context,
			async page =>
			{
				attemptNumber++;
				pages.Add(page);
				await page.GotoAsync("/EventHandlers");

				if (attemptNumber == 1)
				{
					throw new PlaywrightException("net::ERR_NETWORK_CHANGED at https://127.0.0.1/EventHandlers");
				}
			},
			_ => { });

		Assert.Equal(2, pages.Count);
		Assert.NotSame(pages[0], pages[1]);
		Assert.True(pages[0].IsClosed);
	}

	// Negative: net::ERR_CONNECTION_REFUSED is a different error and must never itself be the reason
	// for a retry. Aborts on every attempt (not only the first), because a wrongly retried second
	// attempt must fail too. An unrelated real qualifying failure can still legitimately cause one
	// retry here (the runner cannot tell this attempt's own connectionrefused apart from an
	// unrelated real failure recorded on the same attempt); the invariant that must hold either way
	// is that connectionrefused itself never appears as a retry's reason, and the final exception --
	// whether after one attempt or two -- always names connectionrefused, because the abort fires
	// unconditionally every time.
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
					LogRequestFailures(page, attemptNumber);

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
		Assert.Contains(ConnectionRefusedError, thrown.Message, StringComparison.Ordinal);
		Assert.DoesNotContain(notes, note => note.Contains(ConnectionRefusedError, StringComparison.Ordinal));
	}

	// Writes each observed RequestFailed straight to this test's own output, independently of the
	// runner's own subscription, so a probe's red evidence names the network error even when the
	// thrown exception (the mid-test variant's assertion timeout) does not.
	private void LogRequestFailures(IPage page, int attemptNumber) =>
		page.RequestFailed += (_, request) =>
			outputHelper.WriteLine($"Attempt {attemptNumber}: RequestFailed {request.Failure} for {request.Url}");

	// Accepts either qualifying error: absent an environmental race, the injected connectionclosed
	// abort is the only thing that can produce this note, but an unrelated real net::ERR_NETWORK_CHANGED
	// (see this class's summary) can legitimately cause the same retry-and-pass outcome instead.
	private static void AssertRetryNoteWasWritten(List<string> notes) =>
		Assert.Contains(notes, note =>
			note.Contains(BrowserAttemptRetryRunner.IssueUrl, StringComparison.Ordinal) &&
			(note.Contains(BrowserAttemptRetryScope.NetworkChangedError, StringComparison.Ordinal) ||
				note.Contains(BrowserAttemptRetryScope.ConnectionClosedError, StringComparison.Ordinal)));
}
