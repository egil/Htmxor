namespace Htmxor.Quality.Tests;

/// <summary>
/// Runs <see cref="Htmx4PackageBrowserTests"/>' nested `dotnet test` invocation, and reruns it
/// exactly once when <see cref="NetworkChangedRetryScope.ShouldRetry"/> says the nested run's TRX
/// qualifies (https://github.com/egil/Htmxor/issues/248), writing a note naming the reason and
/// this issue at the point of retry. <typeparamref name="TRun"/> and <paramref name="trxPathOf"/>
/// let the real wiring pass its own `ProcessResult` and assert on it directly: both the first run
/// and its retry write the same fixed TRX path, so the run value itself, not the path, is what the
/// caller distinguishes.
/// </summary>
internal static class NetworkChangedRetryRunner
{
	public static async Task<TRun> RunAsync<TRun>(
		Func<Task<TRun>> runNested,
		Func<TRun, string> trxPathOf,
		Action<string> writeNote)
	{
		var first = await runNested();
		if (!NetworkChangedRetryScope.ShouldRetry(trxPathOf(first)))
		{
			return first;
		}

		writeNote(
			$"Retried the nested run once: every failed test carried " +
			$"{NetworkChangedRetryScope.NetworkChangedError} (https://github.com/egil/Htmxor/issues/248).");
		return await runNested();
	}
}
