namespace Htmxor.Quality.Tests;

public sealed class WorkflowBoundaryTests
{
	[Theory]
	[InlineData("job-permissions", "  monitor:\n", "  monitor:\n    permissions: write-all\n")]
	[InlineData("job-extra-permission", "  monitor:\n", "  monitor:\n    permissions: { contents: read, issues: write, packages: write }\n")]
	[InlineData("wrong-token", "GH_TOKEN: ${{ github.token }}", "GH_TOKEN: ''")]
	[InlineData("wrong-executable", "run: dotnet run", "run: echo run")]
	[InlineData("wrong-project", "eng/Htmxor.Quality/Htmxor.Quality.csproj", "eng/Other/Other.csproj")]
	[InlineData("missing-separator", " -- check", " check")]
	[InlineData("different-workspace", "      - name: Upload reports", "  other:\n    steps:\n      - name: Upload reports")]
	public void Workflow_oracle_rejects_effective_job_and_command_regressions(string scenario, string before, string after)
	{
		var invalid = UpstreamMonitorPolicyTests.ValidWorkflow().Replace(before, after, StringComparison.Ordinal);

		Assert.True(UpstreamMonitorPolicyTests.ExpectedWorkflow() != WorkflowPolicyProjection.Parse(invalid), scenario);
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
