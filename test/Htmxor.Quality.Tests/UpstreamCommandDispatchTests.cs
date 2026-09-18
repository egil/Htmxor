using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class UpstreamCommandDispatchTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public async Task Canonical_upstream_command_runs_static_gates_then_monitor_and_preserves_outcome(int monitorExit)
	{
		using var repository = CreateRepository();
		var runner = new UpstreamDispatchRunner(monitorExit);
		var command = new QualityCommand(repository.Path, runner, CreateDispatchPlan);

		var error = await Record.ExceptionAsync(() => command.ExecuteAsync(new(QualityAction.Check, QualityProfile.Upstream)));

		Assert.Equal(new[] { "restore", "tool", "analyzers", "style", "build", "policy", "fixtures", "monitor" }, runner.Phases);
		var monitor = runner.Commands.Last();
		Assert.Equal(NetworkAccess.Enabled, monitor.NetworkAccess);
		Assert.Equal("dotnet", monitor.FileName);
		Assert.Equal(repository.Path, monitor.WorkingDirectory);
		Assert.Equal(new[] { "run", "--project", Path.Combine(repository.Path, "eng/Htmxor.UpstreamMonitor/Htmxor.UpstreamMonitor.csproj") }, monitor.Arguments);
		if (monitorExit == 0)
		{
			Assert.Null(error);
			return;
		}

		Assert.IsType<InvalidOperationException>(error);
		Assert.Contains($"{monitorExit}", error.Message, StringComparison.Ordinal);
		// QualityCommand.RunUpstreamAsync classifies the monitor's exit code, its MonitorStatus, as
		// its own finding category rather than collapsing every non-drift code to "infrastructure":
		// exit 3 (UnresolvedWatch) already has its own filed review issue, so reporting it as an
		// infrastructure failure would misdirect a human toward "the tool didn't work" instead of
		// "the manifest needs a path corrected". Only a genuinely unclassified code (here, 2) falls
		// back to "infrastructure".
		var expectedCategory = monitorExit switch
		{
			1 => "drift",
			3 => "unresolved watch path",
			_ => "infrastructure",
		};
		Assert.Contains(expectedCategory, error.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("build")]
	[InlineData("policy")]
	[InlineData("fixtures")]
	public async Task Failed_static_gate_prevents_network_monitor_dispatch(string failurePhase)
	{
		using var repository = CreateRepository();
		var runner = new UpstreamDispatchRunner(0, failurePhase);

		var error = await Record.ExceptionAsync(() => new QualityCommand(repository.Path, runner, CreateDispatchPlan)
			.ExecuteAsync(new(QualityAction.Check, QualityProfile.Upstream)));

		Assert.Contains(failurePhase, runner.Phases);
		Assert.DoesNotContain("monitor", runner.Phases);
		Assert.IsType<InvalidOperationException>(error);
	}

	private static QualityPlan CreateDispatchPlan(string root, string output, QualityOptions options)
	{
		Assert.Equal(QualityProfile.Upstream, options.Profile);
		var preparation = QualityPlanFactory.Create(root, output, new(QualityAction.Check, QualityProfile.Fast)).Preparation;
		return new(preparation,
			[TestBoundary(root, output, "Htmxor.Quality.Tests", "policy"), TestBoundary(root, output, "Htmxor.UpstreamMonitor.Tests", "fixtures")],
			null,
			new ProcessCommand("dotnet", root,
				["run", "--project", Path.Combine(root, "eng/Htmxor.UpstreamMonitor/Htmxor.UpstreamMonitor.csproj")],
				EnsureSuccess: false, NetworkAccess: NetworkAccess.Enabled));
	}

	private static TestCommand TestBoundary(string root, string output, string project, string name)
	{
		var directory = Path.Combine(output, name);
		var path = $"test/{project}/{project}.csproj";
		return new(new ProcessCommand("dotnet", root,
			["test", path, "--logger", $"trx;LogFileName={name}.trx", "--results-directory", directory],
			EnsureSuccess: false, NetworkAccess: NetworkAccess.Disabled), path, Path.Combine(directory, $"{name}.trx"), false);
	}

	private static RepositoryPolicyFixture CreateRepository()
	{
		var repository = RepositoryPolicyFixture.CreateCurrent();
		var root = RepositoryLocator.Find();
		foreach (var path in new[] { ".config/dotnet-tools.json", "stryker-config.json" })
		{
			var destination = Path.Combine(repository.Path, path);
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(Path.Combine(root, path), destination, overwrite: true);
		}

		return repository;
	}
}
