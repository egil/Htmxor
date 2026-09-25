namespace Htmxor.Quality.Tests;

public sealed class NetworkChangedRetryScopeTests
{
	// Verbatim message text from https://github.com/egil/Htmxor/issues/233#issuecomment-5776924258
	// (instance 12), the only recorded sighting of this error.
	private const string NetworkChangedMessage =
		"Microsoft.Playwright.PlaywrightException : net::ERR_NETWORK_CHANGED at http://127.0.0.1:46349/issue-154/navigation\n" +
		"Call log:\n" +
		"  - navigating to \"http://127.0.0.1:46349/issue-154/navigation\", waiting until \"load\"";

	// Verbatim message text of instance 9's nested failure, recorded from its CI job log in
	// https://github.com/egil/Htmxor/issues/233#issuecomment-5816514415: the recorded unrelated
	// failure of the same outer suite.
	private const string UnrelatedMessage =
		"Microsoft.Playwright.PlaywrightException : Execution context was destroyed, most likely because of a navigation.";

	// Constructed, in the same shape as instance 12, for the owner-named exclusion in the
	// 2026-09-24 decision comment: "net::ERR_CONNECTION_CLOSED remains #247's ... This decision
	// does not extend the retry to it." No sighting of this exact error is recorded against this
	// suite; it stands in for "a different network error".
	private const string ConnectionClosedMessage =
		"Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_CLOSED at http://127.0.0.1:46349/issue-154/navigation\n" +
		"Call log:\n" +
		"  - navigating to \"http://127.0.0.1:46349/issue-154/navigation\", waiting until \"load\"";

	[Fact]
	public void Every_failed_test_carrying_network_changed_retries()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(directory.Path, total: 42, passed: 40, failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage]);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.True(shouldRetry);
	}

	[Fact]
	public void A_failure_without_network_changed_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(directory.Path, total: 42, passed: 41, failedErrorMessages: [UnrelatedMessage]);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void A_mix_of_network_changed_and_another_failure_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(directory.Path, total: 42, passed: 39, failedErrorMessages: [NetworkChangedMessage, UnrelatedMessage, NetworkChangedMessage]);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void A_failing_exit_with_no_failed_tests_in_the_trx_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(directory.Path, total: 42, passed: 42, failedErrorMessages: []);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void A_different_network_error_such_as_connection_closed_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(directory.Path, total: 42, passed: 41, failedErrorMessages: [ConnectionClosedMessage]);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void A_mix_of_network_changed_and_connection_closed_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(directory.Path, total: 42, passed: 39, failedErrorMessages: [NetworkChangedMessage, ConnectionClosedMessage, NetworkChangedMessage]);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	// Modeled on a real `--blame-hang` abort TRX, captured with the nested project's own flags
	// (`dotnet test --blame-hang --blame-hang-timeout <n> --logger "trx;..."` against a scratch
	// project with a hung test and two net::ERR_NETWORK_CHANGED failures). A failing run always
	// carries a RunInfo per failed test, and this abort shape also carries the Blame collector's
	// hang-dump text (see TrxFixtures); only the abort text itself is common to every abort and
	// absent from every non-abort run (see the crash fact below, which has no Blame entry at all).
	// The contract keys the decision on the abort text.
	private const string RunAbortedMessage =
		"The active test run was aborted. Reason: Test host process crashed : Process path: /home/egil/.dotnet/dotnet";

	[Fact]
	public void A_real_blame_hang_abort_does_not_retry_even_when_completed_failures_are_all_network_changed()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(
			directory.Path,
			total: 4,
			passed: 2,
			failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage],
			runAbortedMessage: RunAbortedMessage,
			blameMessage: TrxFixtures.BlameHangDumpMessage);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	// Modeled on a real test-host crash abort TRX (`Environment.FailFast`), captured with the same
	// nested flags on SDK 10.0.400 and 11.0.100-rc.1: normal Counters, the same per-failure
	// RunInfos, and the abort text, but no Blame RunInfo at all. Unlike the hang abort above, this
	// shape carries no Blame collector entry of any kind, which is exactly why the decision must
	// key on the abort text rather than on any Blame text or on Blame's mere presence.
	private const string CrashAbortedMessage =
		"The active test run was aborted. Reason: Test host process crashed : Process terminated.\n" +
		"simulated test host crash";

	[Fact]
	public void A_real_test_host_crash_abort_does_not_retry_even_when_completed_failures_are_all_network_changed()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(
			directory.Path,
			total: 4,
			passed: 2,
			failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage],
			runAbortedMessage: CrashAbortedMessage,
			blameMessage: null);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	// Defensive shapes: no real capture recorded for #248 has shown a nonzero error or timeout
	// count, and the abort text already covers every observed abort. Executed short of total is
	// what a skipped nested test produces on this VSTest stack, which leaves notExecuted at zero;
	// the nested suite has no skipped test today. These three facts guard Counters shapes a TRX can
	// represent, so a run otherwise scoped to retry does not retry when the run itself was not
	// clean, without weakening the abort-text signal.

	[Fact]
	public void A_network_changed_failure_with_a_nonzero_error_counter_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(
			directory.Path,
			total: 42,
			passed: 39,
			failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage],
			error: 1);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void A_network_changed_failure_with_a_nonzero_timeout_counter_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(
			directory.Path,
			total: 42,
			passed: 39,
			failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage],
			timeout: 1);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}

	[Fact]
	public void A_network_changed_failure_with_executed_less_than_total_does_not_retry()
	{
		using var directory = new TemporaryDirectory();
		var trxPath = TrxFixtures.Write(
			directory.Path,
			total: 42,
			passed: 39,
			failedErrorMessages: [NetworkChangedMessage, NetworkChangedMessage],
			executed: 41);

		var shouldRetry = NetworkChangedRetryScope.ShouldRetry(trxPath);

		Assert.False(shouldRetry);
	}
}
