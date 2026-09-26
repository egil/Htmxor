using Htmxor.TestAssets.Blazewright;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

/// <summary>
/// Real-browser probes of <see cref="BrowserAttemptRetryRunner"/> itself
/// (https://github.com/egil/Htmxor/issues/252), not of the E2E facts that run through it. Each
/// abort probe aborts one request with a real Chromium network error, using
/// <c>route.AbortAsync</c>. The three retried probes abort only on their first attempt and assert
/// the runner's retry and note rather than that their own abort engaged; see
/// <c>AssertRetryNoteWasWritten</c>.
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
	public Task A_first_navigation_aborted_with_connectionclosed_is_retried_and_the_retry_lands() =>
		RunProbeWithOuterRetryAsync(async innerRetryBegins =>
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
					innerRetryBegins();
					outputHelper.WriteLine(note);
					notes.Add(note);
				});

			Assert.Equal(2, attemptNumber);
			AssertRetryNoteWasWritten(notes);
		});

	// The mid-test htmx variant: the first GotoAsync succeeds, but the htmx GET for a click is
	// aborted, so htmx swaps nothing and the test fails on the assertion timeout below -- an
	// exception whose message never names the network error. Only the recorded RequestFailed error
	// qualifies this attempt for a retry; LogRequestFailures below writes that observation to this
	// test's own output, so the red evidence names net::ERR_CONNECTION_CLOSED even though the thrown
	// exception does not.
	[Fact]
	public Task A_mid_test_htmx_request_aborted_with_connectionclosed_is_retried_and_the_retry_lands() =>
		RunProbeWithOuterRetryAsync(async innerRetryBegins =>
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
					innerRetryBegins();
					outputHelper.WriteLine(note);
					notes.Add(note);
				});

			Assert.Equal(2, attemptNumber);
			AssertRetryNoteWasWritten(notes);
		});

	// P004: the runner must observe request failures on any page the body opens through the shared
	// context, not only the page it hands to the body. This body deliberately ignores the handed
	// page and opens its own, as a real body reasonably might for a second tab. Uses the mid-test
	// shape (abort only the htmx GET, fail on the Expect) deliberately: aborting the own page's
	// first navigation would qualify through the thrown exception alone, telling nothing about
	// whether the runner observed the request failure or not.
	[Fact]
	public Task A_request_aborted_on_a_page_the_body_opens_itself_is_still_observed_and_retried() =>
		RunProbeWithOuterRetryAsync(async innerRetryBegins =>
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
					innerRetryBegins();
					outputHelper.WriteLine(note);
					notes.Add(note);
				});

			Assert.Equal(2, attemptNumber);
			AssertRetryNoteWasWritten(notes);
		});

	// Cheap pin for the "fresh page per attempt" design constraint (issue #252 Work item 3): a
	// runner that reused one page across attempts would hand the retry a page a prior aborted
	// navigation already left in a failed state.
	[Fact]
	public Task Each_attempt_of_a_qualifying_failure_gets_a_fresh_page() =>
		RunProbeWithOuterRetryAsync(async innerRetryBegins =>
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
				_ => innerRetryBegins());

			Assert.Equal(2, pages.Count);
			Assert.NotSame(pages[0], pages[1]);
			Assert.True(pages[0].IsClosed);
		});

	// Negative: net::ERR_CONNECTION_REFUSED is a different error and must not be retried. Aborts on
	// every attempt (not only the first), because a wrongly retried second attempt must fail too.
	// Not wrapped in the outer retry: its route aborts the attempt's only request -- the navigation
	// itself -- before any network I/O, so no other request exists for a coincident real failure to
	// land on, and this probe stays exactly deterministic.
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
		Assert.Equal(1, attemptNumber);
		Assert.Empty(notes);
	}

	// The outer retry for the four probes above that deliberately fail their own first inner
	// attempt, which always spends the inner runner's one retry on that deliberate failure
	// (https://github.com/egil/Htmxor/issues/252). Reuses BrowserAttemptRetryRunner.RunAsync itself
	// for the retry-once-and-note loop, so this differs from the inner seam only in what one
	// "attempt" means: running the whole probe once, which may itself retry internally.
	private Task RunProbeWithOuterRetryAsync(Func<Action, Task> probe) =>
		BrowserAttemptRetryRunner.RunAsync(() => RunProbeAttemptAsync(probe), outputHelper.WriteLine);

	// Observes RequestFailed on this test's Context, like BrowserAttemptRetryRunner.RunAttemptAsync
	// does per inner attempt, but only from the moment probe's own inner retry begins onward: before
	// that, every one of these probes' first inner attempt is a deliberate, injected failure, and
	// counting it here would make the outer decision qualify for any failure at all, including one
	// unrelated to the network. innerRetryBegins is the probe's own inner writeNote callback, so
	// recording starts exactly when the inner runner starts the attempt this outer retry exists for.
	private async Task<BrowserAttemptOutcome> RunProbeAttemptAsync(Func<Action, Task> probe)
	{
		var failures = new List<string>();
		var recording = false;
		void OnRequestFailed(object? _, IRequest request)
		{
			if (recording)
			{
				failures.Add(request.Failure ?? string.Empty);
			}
		}

		Context.RequestFailed += OnRequestFailed;
		try
		{
			await probe(() => recording = true);
			return new BrowserAttemptOutcome(null, failures);
		}
		catch (Exception ex)
		{
			return new BrowserAttemptOutcome(ex, failures);
		}
		finally
		{
			Context.RequestFailed -= OnRequestFailed;
		}
	}

	// Writes each observed RequestFailed straight to this test's own output, independently of the
	// runner's own subscription, so a probe's red evidence names the network error even when the
	// thrown exception (the mid-test variant's assertion timeout) does not.
	private void LogRequestFailures(IPage page, int attemptNumber) =>
		page.RequestFailed += (_, request) =>
			outputHelper.WriteLine($"Attempt {attemptNumber}: RequestFailed {request.Failure} for {request.Url}");

	// Accepts either qualifying error: absent an environmental race, the injected connectionclosed
	// abort is the only thing that can produce this note, but an unrelated real net::ERR_NETWORK_CHANGED
	// can pre-empt it and legitimately cause the same retry-and-pass outcome instead.
	private static void AssertRetryNoteWasWritten(List<string> notes) =>
		Assert.Contains(notes, note =>
			note.Contains(BrowserAttemptRetryRunner.IssueUrl, StringComparison.Ordinal) &&
			(note.Contains(BrowserAttemptRetryScope.NetworkChangedError, StringComparison.Ordinal) ||
				note.Contains(BrowserAttemptRetryScope.ConnectionClosedError, StringComparison.Ordinal)));
}
