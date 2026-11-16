using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class StrykerInitialTestRunTests
{
	private const string Started =
		"[14:11:42 INF] Number of tests found: 1343 for project /repo/src/Htmxor/Htmxor.csproj. Initial test run started.";

	[Fact]
	public void Green_baseline_has_no_failure()
	{
		var console = Lines(
			Started,
			"[14:12:48 DBG] Initial test run output: .",
			"[14:12:49 INF] Capture mutant coverage using 'CoverageBasedTest' mode.");

		Assert.Null(StrykerInitialTestRun.GetValidityFailure(console));
	}

	[Fact]
	public void Failing_baseline_names_the_tests_from_Strykers_debug_entry()
	{
		// Shape taken from Stryker 4.16 debug output for the #383 baseline failure.
		var console = Lines(
			Started,
			"[14:12:48 DBG] Initial test run output: Htmxor.E2E.BlazingPizzaRemovalBrowserTests.Clicking_remove_preserves_the_unselected_pizza",
			"",
			"System.InvalidOperationException : samples/BlazingPizza/BlazingPizza.csproj closed its output before Kestrel started: [2026-11-16T14:12:44.7463051+00:00 +0.486s] Could not execute",
			"[2026-11-16T14:12:44.7468108+00:00 +0.487s] Possible reasons for this include:",
			"Actual body text was:",
			"",
			"Htmxor.RouteTests.Route<Int32>(id: \"a b\", value: 1)",
			"",
			"Assert.Equal() Failure.",
			"[14:12:48 WRN] 2 tests are failing. Stryker will continue but outcome will be impacted.");

		var failure = StrykerInitialTestRun.GetValidityFailure(console);

		Assert.Equal(
			"Stryker's initial unmutated test run had 2 failing tests, so mutant outcomes are not trustworthy: " +
			"Htmxor.E2E.BlazingPizzaRemovalBrowserTests.Clicking_remove_preserves_the_unselected_pizza; " +
			"Htmxor.RouteTests.Route<Int32>(id: \"a b\", value: 1).",
			failure);
	}

	[Fact]
	public void Failing_baseline_without_debug_names_still_fails()
	{
		var console = Lines(Started, "[14:12:48 WRN] 1 tests are failing. Stryker will continue but outcome will be impacted.");

		Assert.Equal(
			"Stryker's initial unmutated test run had 1 failing test, so mutant outcomes are not trustworthy: " +
			"no failing test names were found in Stryker's debug output.",
			StrykerInitialTestRun.GetValidityFailure(console));
	}

	private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines) + Environment.NewLine;
}
