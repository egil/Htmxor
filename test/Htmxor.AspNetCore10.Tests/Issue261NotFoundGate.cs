namespace Htmxor.AspNetCore10;

// Lets the test control exactly when Issue261PostStartNotFoundPage raises NotFound() after the response has
// started, and exactly when Issue261StrayUpdateChild's detached, quiescence-untracked continuation asks the
// renderer for another render, instead of guessing with fixed sleeps. Registered per request (see
// Issue260Host's configureServices hook), so there is exactly one instance per host and no reset between
// requests.
//
// A bound on every signal, so a request that faults or returns before a page reaches its waypoint fails the
// one test on that waypoint instead of hanging the whole test boundary (see Issue260Gate's own comment).
internal sealed class Issue261NotFoundGate
{
	private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(20);

	private readonly TaskCompletionSource notFoundReleased = CreateSource();
	private readonly TaskCompletionSource strayReleased = CreateSource();
	private readonly TaskCompletionSource<Exception?> strayOutcome = CreateOutcomeSource();

	public Task NotFoundReleased => notFoundReleased.Task.WaitAsync(SignalTimeout);

	public Task StrayReleased => strayReleased.Task.WaitAsync(SignalTimeout);

	// Resolves once Issue261StrayUpdateChild's deferred continuation has run its own InvokeAsync(StateHasChanged)
	// to completion: null when that attempt produced no exception, or the exception it threw. The test
	// releases StrayReleased only while Issue261ResponseHold is holding the response open, then asserts this
	// outcome against stock's own: see Issue261PostStartNotFoundParityTests for why that ordering, not a
	// sleep, is what makes the attempt's timing deterministic.
	public Task<Exception?> StrayOutcome => strayOutcome.Task.WaitAsync(SignalTimeout);

	public void ReleaseNotFound() => notFoundReleased.TrySetResult();

	public void ReleaseStray() => strayReleased.TrySetResult();

	public void RecordStrayOutcome(Exception? exception) => strayOutcome.TrySetResult(exception);

	private static TaskCompletionSource CreateSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private static TaskCompletionSource<Exception?> CreateOutcomeSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
