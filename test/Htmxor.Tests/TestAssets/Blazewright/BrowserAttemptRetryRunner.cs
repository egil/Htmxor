using System.Runtime.ExceptionServices;
using Microsoft.Playwright;

namespace Htmxor.TestAssets.Blazewright;

/// <summary>
/// Runs a browser E2E attempt, retrying it exactly once when
/// <see cref="BrowserAttemptRetryScope.ShouldRetry"/> says so
/// (https://github.com/egil/Htmxor/issues/252), writing a note through the <c>writeNote</c> callback
/// before the retry runs. A second failed attempt is never retried again; it fails with that
/// attempt's own exception, rethrown through <c>ExceptionDispatchInfo.Throw</c> so the original
/// failing line survives instead of being replaced by this rethrow site.
/// </summary>
public static class BrowserAttemptRetryRunner
{
	public const string IssueUrl = "https://github.com/egil/Htmxor/issues/252";

	public static async Task<BrowserAttemptOutcome> RunAsync(
		Func<Task<BrowserAttemptOutcome>> runAttempt,
		Action<string> writeNote)
	{
		var first = await runAttempt();
		if (first.Exception is null)
		{
			return first;
		}

		var reason = BrowserAttemptRetryScope.QualifyingError(first);
		if (reason is null)
		{
			ExceptionDispatchInfo.Throw(first.Exception);
		}

		writeNote($"Retried once after {reason} ({IssueUrl}).");

		var second = await runAttempt();
		if (second.Exception is not null)
		{
			ExceptionDispatchInfo.Throw(second.Exception);
		}

		return second;
	}

	/// <summary>
	/// The real seam: gives <paramref name="body"/> a fresh page from <paramref name="context"/> for
	/// each attempt, so a retry cannot reuse a page that is missing scripts a failed subresource load
	/// left out. Closes every page it opens. Observes <c>RequestFailed</c> on the whole
	/// <paramref name="context"/>, not only the handed page, because a body may reasonably open a
	/// second page of its own through the same context; the subscription is scoped to one attempt, so
	/// a later attempt never sees an earlier attempt's event.
	/// </summary>
	public static async Task RunOnPageAsync(IBrowserContext context, Func<IPage, Task> body, Action<string> writeNote)
	{
		await RunAsync(() => RunAttemptAsync(context, body), writeNote);
	}

	private static async Task<BrowserAttemptOutcome> RunAttemptAsync(IBrowserContext context, Func<IPage, Task> body)
	{
		var failures = new List<string>();
		void OnRequestFailed(object? _, IRequest request) => failures.Add(request.Failure ?? string.Empty);

		context.RequestFailed += OnRequestFailed;
		var page = await context.NewPageAsync();

		try
		{
			await body(page);
			return new BrowserAttemptOutcome(null, failures);
		}
		catch (Exception ex)
		{
			return new BrowserAttemptOutcome(ex, failures);
		}
		finally
		{
			context.RequestFailed -= OnRequestFailed;
			await page.CloseAsync();
		}
	}
}
