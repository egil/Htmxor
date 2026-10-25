namespace Htmxor.Quality.Tests;

public sealed class WorkflowBoundaryTests
{
	[Fact]
	public void CI_runs_full_once_after_browser_provisioning_without_repeating_fast()
	{
		var workflow = File.ReadAllText(Path.Combine(RepositoryLocator.Find(), ".github", "workflows", "ci.yml"));
		var commands = WorkflowPolicyProjection.RunCommandsForJob(workflow, "run-test").ToArray();
		const string full = "dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full";
		var checks = commands.Where(command => command.Contains("check --profile", StringComparison.Ordinal)).ToArray();

		Assert.Collection(checks, command => Assert.Equal(full, command));
		var build = Array.IndexOf(commands, "dotnet build test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release");
		var browsers = Array.IndexOf(commands, "pwsh test/Htmxor.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium");
		Assert.True(build >= 0);
		Assert.True(browsers > build);
		Assert.True(Array.IndexOf(commands, full) > browsers);
	}

	[Theory]
	[InlineData("job-permissions", "  monitor:\n", "  monitor:\n    permissions: write-all\n")]
	[InlineData("job-extra-permission", "  monitor:\n", "  monitor:\n    permissions: { contents: read, issues: write, packages: write }\n")]
	[InlineData("wrong-token", "GH_TOKEN: ${{ github.token }}", "GH_TOKEN: ''")]
	[InlineData("wrong-executable", "run: dotnet run", "run: echo run")]
	[InlineData("wrong-project", "eng/Htmxor.Quality/Htmxor.Quality.csproj", "eng/Other/Other.csproj")]
	[InlineData("missing-separator", " -- check", " check")]
	[InlineData("different-workspace", "      - name: Upload reports", "  other:\n    steps:\n      - name: Upload reports")]
	[InlineData("job-continues-on-error", "  monitor:\n", "  monitor:\n    continue-on-error: true\n")]
	[InlineData("monitor-continues-on-error", "        run: dotnet run", "        continue-on-error: true\n        run: dotnet run")]
	public void Workflow_oracle_rejects_effective_job_and_command_regressions(string scenario, string before, string after)
	{
		var invalid = UpstreamMonitorPolicyTests.ValidWorkflow().Replace(before, after, StringComparison.Ordinal);

		Assert.True(UpstreamMonitorPolicyTests.ExpectedWorkflow() != WorkflowPolicyProjection.Parse(invalid), scenario);
	}

	[Theory]
	[InlineData("  monitor:\n", "  monitor:\n    continue-on-error: false\n")]
	[InlineData("        run: dotnet run", "        continue-on-error: false\n        run: dotnet run")]
	public void Explicit_false_continue_on_error_retains_failure_propagation(string before, string after)
	{
		var workflow = UpstreamMonitorPolicyTests.ValidWorkflow().Replace(before, after, StringComparison.Ordinal);

		Assert.Equal(UpstreamMonitorPolicyTests.ExpectedWorkflow(), WorkflowPolicyProjection.Parse(workflow));
	}

	[Fact]
	public void Upload_before_monitor_does_not_retain_generated_reports()
	{
		var workflow = UpstreamMonitorPolicyTests.ValidWorkflow();
		var start = workflow.IndexOf("      - name: Run upstream profile", StringComparison.Ordinal);
		var upload = workflow.IndexOf("      - name: Upload reports", StringComparison.Ordinal);
		var reversed = workflow[..start] + workflow[upload..] + "\n" + workflow[start..upload];

		Assert.NotEqual(UpstreamMonitorPolicyTests.ExpectedWorkflow(), WorkflowPolicyProjection.Parse(reversed));
	}

	[Fact]
	public void Exact_job_override_remains_least_privilege()
	{
		var workflow = UpstreamMonitorPolicyTests.ValidWorkflow().Replace("  monitor:\n",
			"  monitor:\n    permissions: { contents: read, issues: write }\n", StringComparison.Ordinal);

		Assert.Equal(UpstreamMonitorPolicyTests.ExpectedWorkflow(), WorkflowPolicyProjection.Parse(workflow));
	}
}
