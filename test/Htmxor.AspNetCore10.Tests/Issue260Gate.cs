namespace Htmxor.AspNetCore10;

// A process-local, per-host, named gate: every #260 host registers exactly one instance (see Issue260Host), and
// every page below injects it to signal when it has reached a specific await and to await a specific release, by
// name. One class serves every #260 page instead of a fresh single-purpose probe type per page (the shape
// Issue186's and Issue264's probes use), because #260's cases each need a different number of named waypoints
// along one non-streaming pending-work chain rather than a fixed "initial render reached / released / completed"
// shape. "Reached" and "Release" are two independent named signal spaces (not one shared name per waypoint): a
// page calls Reached(name) and then awaits WaitForReleaseAsync(name) with the *same* name, and those must stay
// two different underlying signals, or the Reached call would immediately satisfy the page's own release wait.
internal sealed class Issue260Gate
{
	private readonly Dictionary<string, TaskCompletionSource> reached = new(StringComparer.Ordinal);
	private readonly Dictionary<string, TaskCompletionSource> released = new(StringComparer.Ordinal);
	private readonly object sync = new();

	// Called by a page the instant it reaches the named await, before it actually awaits: the test can then wait
	// on this to know the page has run exactly as far as intended, instead of racing the dispatcher with a fixed
	// delay.
	public void Reached(string name) => Get(reached, name).TrySetResult();

	public Task WaitForReachedAsync(string name) => Get(reached, name).Task;

	public void Release(string name) => Get(released, name).TrySetResult();

	public Task WaitForReleaseAsync(string name) => Get(released, name).Task;

	private TaskCompletionSource Get(Dictionary<string, TaskCompletionSource> signals, string name)
	{
		lock (sync)
		{
			if (!signals.TryGetValue(name, out var source))
			{
				source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				signals[name] = source;
			}

			return source;
		}
	}
}
