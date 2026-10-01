namespace Htmxor.AspNetCore10;

// Lets the test control exactly when Issue261PostStartNotFoundPage raises NotFound() after the response has
// started, and exactly when Issue261StrayUpdateChild's detached, quiescence-untracked continuation asks the
// renderer for another render, instead of guessing with fixed sleeps. One process-wide instance, reset per
// request, is sufficient because xUnit runs a class's [Fact] methods sequentially (see Issue264StreamingGate's
// own comment, the same shape, in the SwitchOnHost test support project).
//
// A bound on every signal, so a request that faults or returns before a page reaches its waypoint fails the
// one test on that waypoint instead of hanging the whole test boundary (see Issue260Gate's own comment).
internal sealed class Issue261NotFoundGate
{
	private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(20);

	private TaskCompletionSource notFoundReleased = CreateSource();
	private TaskCompletionSource strayReleased = CreateSource();
	private TaskCompletionSource<Exception?> strayOutcome = CreateOutcomeSource();

	public Task NotFoundReleased => notFoundReleased.Task.WaitAsync(SignalTimeout);

	public Task StrayReleased => strayReleased.Task.WaitAsync(SignalTimeout);

	// Resolves once Issue261StrayUpdateChild's deferred continuation has run its own InvokeAsync(StateHasChanged)
	// to completion, successfully or not. The test awaits this before reading the rest of the response body,
	// so that render attempt -- whether the renderer lets it through as a streamed batch or (observed in an
	// earlier draft of this case) it instead throws trying to touch an already-completed response -- has a
	// chance to happen before the body read reaches a genuine end-of-stream, on both stock and the candidate.
	public Task<Exception?> StrayOutcome => strayOutcome.Task.WaitAsync(SignalTimeout);

	public void Reset()
	{
		notFoundReleased = CreateSource();
		strayReleased = CreateSource();
		strayOutcome = CreateOutcomeSource();
	}

	public void ReleaseNotFound() => notFoundReleased.TrySetResult();

	public void ReleaseStray() => strayReleased.TrySetResult();

	public void RecordStrayOutcome(Exception? exception) => strayOutcome.TrySetResult(exception);

	private static TaskCompletionSource CreateSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private static TaskCompletionSource<Exception?> CreateOutcomeSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
