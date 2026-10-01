namespace Htmxor.AspNetCore10;

// Lets the test control exactly when Issue261PostStartNotFoundPage raises NotFound() after the response has
// started, exactly when the endpoint's own request-handling pipeline is allowed to finish and release its
// request scope, and exactly when Issue261StrayUpdateChild's detached, quiescence-untracked continuation asks
// the renderer for another render, instead of guessing with fixed sleeps. Registered per host (see
// Issue260Host's configureServices hook): this test sends exactly one request per host, so one instance per
// host is exactly one instance per request too.
//
// A bound on every signal, so a request that faults or returns before a page reaches its waypoint fails the
// one test on that waypoint instead of hanging the whole test boundary (see Issue260Gate's own comment).
internal sealed class Issue261NotFoundGate
{
	private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(20);

	private readonly TaskCompletionSource notFoundReleased = CreateSource();
	private readonly TaskCompletionSource endpointFinished = CreateSource();
	private readonly TaskCompletionSource responseReleased = CreateSource();
	private readonly TaskCompletionSource strayReleased = CreateSource();
	private readonly TaskCompletionSource<Exception?> strayOutcome = CreateOutcomeSource();
	private string lastObservedStrayRenderState = "";

	public Task NotFoundReleased => notFoundReleased.Task.WaitAsync(SignalTimeout);

	// Resolves once the endpoint's own request-handling task has completed, including any stop the renderer
	// set along the way, while the request's service scope -- and therefore the renderer and its response
	// writer -- is still alive, because the middleware that signals this has not yet returned itself (see
	// Issue261Host.CreateAsync's configurePipeline).
	public Task EndpointFinished => endpointFinished.Task.WaitAsync(SignalTimeout);

	// Lets the endpoint's own request scope -- and the middleware awaiting this -- finish and dispose.
	public Task ResponseReleased => responseReleased.Task.WaitAsync(SignalTimeout);

	public Task StrayReleased => strayReleased.Task.WaitAsync(SignalTimeout);

	// Resolves once Issue261StrayUpdateChild's deferred continuation has run its own InvokeAsync(StateHasChanged)
	// to completion: null when that attempt produced no exception, or the exception it threw.
	public Task<Exception?> StrayOutcome => strayOutcome.Task.WaitAsync(SignalTimeout);

	// The state Issue261StrayUpdateChild's markup last actually rendered. Recorded by the component itself
	// (see its RenderedState property), so this reflects whether the renderer built a new render tree for the
	// stray's post-update state at all, independently of whether sending that render tree later succeeded or
	// threw.
	public string LastObservedStrayRenderState => lastObservedStrayRenderState;

	public void ReleaseNotFound() => notFoundReleased.TrySetResult();

	public void SignalEndpointFinished() => endpointFinished.TrySetResult();

	public void ReleaseResponse() => responseReleased.TrySetResult();

	public void ReleaseStray() => strayReleased.TrySetResult();

	public void RecordStrayOutcome(Exception? exception) => strayOutcome.TrySetResult(exception);

	public void RecordStrayRenderObserved(string state) => lastObservedStrayRenderState = state;

	private static TaskCompletionSource CreateSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private static TaskCompletionSource<Exception?> CreateOutcomeSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
