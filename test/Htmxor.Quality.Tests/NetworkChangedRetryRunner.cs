namespace Htmxor.Quality.Tests;

/// <summary>
/// Test-owned orchestration for the retry <see cref="Htmx4PackageBrowserTests"/> is expected to
/// perform: await the nested run, and when <see cref="NetworkChangedRetryScope.ShouldRetry"/>
/// says so for that run's TRX path, await it again exactly once more and write a note naming the
/// reason and https://github.com/egil/Htmxor/issues/248. <typeparamref name="TRun"/> and
/// <paramref name="trxPathOf"/> let the real wiring pass its own `ProcessResult`, and assert on
/// it, without reshaping this seam: both runs write the same fixed TRX path, so what actually
/// distinguishes them is the run value itself, not the path. This shell always awaits the nested
/// delegate once and returns that result: it characterizes today's behavior (no retry exists yet)
/// and is not wired into <see cref="Htmx4PackageBrowserTests"/>; that wiring is separate, later
/// work.
/// </summary>
internal static class NetworkChangedRetryRunner
{
	public static async Task<TRun> RunAsync<TRun>(
		Func<Task<TRun>> runNested,
		Func<TRun, string> trxPathOf,
		Action<string> writeNote)
	{
		return await runNested();
	}
}
