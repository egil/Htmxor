using Microsoft.Playwright;

namespace Htmxor.TestAssets.Blazewright;

/// <summary>
/// Runs a browser E2E attempt, retrying it exactly once when
/// <see cref="BrowserAttemptRetryScope.ShouldRetry"/> says so
/// (https://github.com/egil/Htmxor/issues/252), writing a note through <paramref name="writeNote"/>
/// before the retry runs. A second failed attempt is never retried again; it fails with that
/// attempt's own exception. <typeparamref name="TAttempt"/> and <paramref name="outcomeOf"/> let the
/// real seam (<see cref="RunOnPageAsync"/>) pass a page-carrying result, and let non-browser tests
/// drive this with a constructed <see cref="BrowserAttemptOutcome"/> directly.
/// </summary>
public static class BrowserAttemptRetryRunner
{
	public const string IssueUrl = "https://github.com/egil/Htmxor/issues/252";

	public static async Task<TAttempt> RunAsync<TAttempt>(
		Func<Task<TAttempt>> runAttempt,
		Func<TAttempt, BrowserAttemptOutcome> outcomeOf,
		Action<string> writeNote)
	{
		var first = await runAttempt();
		var firstOutcome = outcomeOf(first);
		if (firstOutcome.Exception is null)
		{
			return first;
		}

		if (!BrowserAttemptRetryScope.ShouldRetry(firstOutcome))
		{
			throw firstOutcome.Exception;
		}

		writeNote(DescribeRetry(firstOutcome));

		var second = await runAttempt();
		var secondOutcome = outcomeOf(second);
		if (secondOutcome.Exception is not null)
		{
			throw secondOutcome.Exception;
		}

		return second;
	}

	// A request failure is checked before the thrown exception because the mid-test htmx variant's
	// thrown exception is an assertion timeout that never names the network error; only the
	// recorded request failure identifies the reason in that case.
	private static string DescribeRetry(BrowserAttemptOutcome outcome)
	{
		var reason = outcome.RequestFailureErrors.FirstOrDefault(IsQualifyingError)
			?? ExtractQualifyingError(outcome.Exception!.Message)
			?? "an unrecognized qualifying condition";
		return $"Retried once after {reason} ({IssueUrl}).";
	}

	private static bool IsQualifyingError(string text) =>
		text.Contains(BrowserAttemptRetryScope.NetworkChangedError, StringComparison.Ordinal) ||
		text.Contains(BrowserAttemptRetryScope.ConnectionClosedError, StringComparison.Ordinal);

	private static string? ExtractQualifyingError(string message)
	{
		if (message.Contains(BrowserAttemptRetryScope.NetworkChangedError, StringComparison.Ordinal))
		{
			return BrowserAttemptRetryScope.NetworkChangedError;
		}

		if (message.Contains(BrowserAttemptRetryScope.ConnectionClosedError, StringComparison.Ordinal))
		{
			return BrowserAttemptRetryScope.ConnectionClosedError;
		}

		return null;
	}

	/// <summary>
	/// The real seam: gives <paramref name="body"/> a fresh page from <paramref name="context"/> for
	/// each attempt, so a retry cannot reuse a page that is missing scripts a failed subresource load
	/// left out. Closes every page it opens. The decision reads only this attempt's own
	/// <c>RequestFailed</c> errors and thrown exception, never a prior attempt's.
	/// </summary>
	public static async Task RunOnPageAsync(IBrowserContext context, Func<IPage, Task> body, Action<string> writeNote)
	{
		await RunAsync(() => RunAttemptAsync(context, body), attempt => attempt.Outcome, writeNote);
	}

	private static async Task<PageAttempt> RunAttemptAsync(IBrowserContext context, Func<IPage, Task> body)
	{
		var failures = new List<string>();
		var page = await context.NewPageAsync();
		page.RequestFailed += (_, request) => failures.Add(request.Failure ?? string.Empty);

		try
		{
			await body(page);
			return new PageAttempt(new BrowserAttemptOutcome(null, failures));
		}
		catch (Exception ex)
		{
			return new PageAttempt(new BrowserAttemptOutcome(ex, failures));
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	private sealed record PageAttempt(BrowserAttemptOutcome Outcome);
}
