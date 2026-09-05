using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class UpstreamMonitorPolicyTests
{
	private const string MonitorTestProject = "test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj";

	[Fact]
	public void Upstream_is_a_canonical_profile()
	{
		var upstream = QualityOptions.Parse(["check", "--profile", "upstream"]);

		Assert.Equal("Upstream", upstream.Profile.ToString());
	}

	[Fact]
	public void Upstream_profile_runs_static_fixture_and_network_monitor_plan()
	{
		var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-upstream-plan"));
		var results = Path.Combine(root, "artifacts", "results", "upstream");

		var plan = QualityPlanFactory.Create(
			root,
			results,
			QualityOptions.Parse(["check", "--profile", "upstream"]));

		Assert.Equal(ExpectedUpstreamPlan(), Project(plan, root));
	}

	[Theory]
	[InlineData("fast")]
	[InlineData("full")]
	public void Ordinary_profiles_run_fixture_tests_but_no_network_capable_command(string profileName)
	{
		var profile = profileName == "fast" ? QualityProfile.Fast : QualityProfile.Full;
		var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-upstream-policy"));
		var results = Path.Combine(root, "results");
		var plan = QualityPlanFactory.Create(root, results, new QualityOptions(QualityAction.Check, profile));
		var commandBoundaries = AllCommands(plan).ToArray();

		Assert.All(commandBoundaries, command => Assert.Equal("Disabled", NetworkAccess(command)));
		Assert.Contains(plan.Tests, test => test.Project == MonitorTestProject);
	}

	[Fact]
	public void Committed_manifest_tracks_exact_reviewed_relationships_and_dependencies()
	{
		var root = RepositoryLocator.Find();
		var path = Path.Combine(root, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");

		Assert.True(File.Exists(path), "The upstream monitor manifest must be committed.");
		using var document = JsonDocument.Parse(File.ReadAllText(path));
		var manifest = document.RootElement;
		Assert.Equal("dotnet/aspnetcore", manifest.GetProperty("repository").GetString());
		Assert.Equal("v10.0.11", manifest.GetProperty("reviewed").GetProperty("tag").GetString());
		Assert.Equal(
			"a5383385245bdacc20ec19f30e46090a8154d8da",
			manifest.GetProperty("reviewed").GetProperty("commit").GetString());

		Assert.Equal(
			[
				"src/Components/Components/src/Rendering/ComponentState.cs|file|subclass|subclasses|src/Htmxor/Rendering/HtmxorComponentState.cs",
				"src/Components/Components/src/RenderTree/Renderer.cs|file|subclass|subclasses|src/Htmxor/Rendering/HtmxorRenderer.cs",
				"src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs|file|interface|implements|src/Htmxor/Endpoints/HtmxorComponentEndpointInvoker.cs,src/Htmxor/IHtmxorComponentEndpointInvoker.cs",
				"src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs|file|none|reimplements|src/Htmxor/Endpoints/HtmxorComponentEndpointInvoker.cs",
				"src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer|prefix|subclass|reimplements|src/Htmxor/Rendering/HtmxorRenderer.EventDispatch.cs,src/Htmxor/Rendering/HtmxorRenderer.HtmxorEventDispatch.cs,src/Htmxor/Rendering/HtmxorRenderer.Rendering.cs,src/Htmxor/Rendering/HtmxorRenderer.cs",
				"src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs|file|subclass|mirrors|src/Htmxor/Rendering/HtmxorRenderer.HtmlWriting.cs",
				"src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs|file|subclass|mirrors|src/Htmxor/Rendering/HtmxorRenderer.cs",
			],
			ProjectWatches(manifest, root));
	}

	[Fact]
	public void Upstream_workflow_has_exact_isolated_trigger_permission_and_upload_policy()
	{
		var root = RepositoryLocator.Find();
		var path = Path.Combine(root, ".github", "workflows", "upstream-monitor.yml");

		Assert.True(File.Exists(path), "The upstream monitor workflow must be separate from ordinary CI.");
		Assert.Equal(ExpectedWorkflow(), WorkflowPolicyProjection.Parse(File.ReadAllText(path)));
	}

	[Theory]
	[InlineData("workflow trigger")]
	[InlineData("inline workflow trigger")]
	[InlineData("permission")]
	[InlineData("dispatch type")]
	[InlineData("upload condition")]
	[InlineData("retention")]
	[InlineData("report paths")]
	public void Workflow_projection_rejects_policy_regressions(string scenario)
	{
		var workflow = ValidWorkflow();
		var invalid = scenario switch
		{
			"workflow trigger" => workflow.Replace("  workflow_dispatch:\n", "  pull_request:\n  workflow_dispatch:\n", StringComparison.Ordinal),
			"inline workflow trigger" => workflow.Replace("  workflow_dispatch:\n", "  pull_request: {}\n  workflow_dispatch:\n", StringComparison.Ordinal),
			"permission" => workflow.Replace("  issues: write\n", "  issues: write\n  packages: write\n", StringComparison.Ordinal),
			"dispatch type" => workflow.Replace("types: [aspnetcore-release-published]\n", "types: [something-else]\n", StringComparison.Ordinal),
			"upload condition" => workflow.Replace("if: always()\n", "if: success()\n", StringComparison.Ordinal),
			"retention" => workflow.Replace("retention-days: 14", "retention-days: 0", StringComparison.Ordinal),
			"report paths" => workflow.Replace("path: |\n            artifacts/upstream-monitor/*.json\n            artifacts/upstream-monitor/*.md\n", "path: reports/*.json\n", StringComparison.Ordinal),
			_ => throw new InvalidOperationException(scenario),
		};

		Assert.NotEqual(ExpectedWorkflow(), WorkflowPolicyProjection.Parse(invalid));
	}

	[Fact]
	public void Workflow_projection_accepts_only_the_exact_policy_shape()
	{
		Assert.Equal(ExpectedWorkflow(), WorkflowPolicyProjection.Parse(ValidWorkflow()));
	}

	private static IEnumerable<ProcessCommand> AllCommands(QualityPlan plan)
	{
		foreach (var command in plan.Preparation)
		{
			yield return command;
		}

		foreach (var test in plan.Tests)
		{
			yield return test.Command;
		}

		if (plan.Mutation is not null)
		{
			yield return plan.Mutation;
		}

		if (plan.UpstreamMonitor is not null)
		{
			yield return plan.UpstreamMonitor;
		}
	}

	private static string NetworkAccess(ProcessCommand command) => command.NetworkAccess.ToString();

	private static UpstreamPlanObservation Project(QualityPlan plan, string root) => new(
		string.Join('\n', plan.Preparation.Select(command => Command(command, root))),
		string.Join('\n', plan.Tests.Select(test => $"{test.Project}|{Command(test.Command, root)}")),
		plan.UpstreamMonitor is null ? null : Command(plan.UpstreamMonitor, root));

	private static UpstreamPlanObservation ExpectedUpstreamPlan() => new(
		string.Join('\n',
			"Disabled|dotnet restore <root>/Htmxor.sln",
			"Disabled|dotnet tool restore --tool-manifest <root>/.config/dotnet-tools.json",
			"Disabled|dotnet format analyzers <root>/Htmxor.sln --verify-no-changes --no-restore --severity error --verbosity minimal",
			"Disabled|dotnet format style <root>/Htmxor.sln --verify-no-changes --no-restore --severity error --verbosity minimal",
			"Disabled|dotnet build <root>/Htmxor.sln --configuration Release --no-restore"),
		string.Join('\n',
			"test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj|Disabled|dotnet test <root>/test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-build --no-restore --blame-hang --blame-hang-timeout 5min --logger trx;LogFileName=upstream-policy.trx --results-directory <root>/artifacts/results/upstream/upstream-policy --filter FullyQualifiedName~UpstreamMonitorPolicyTests",
			"test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj|Disabled|dotnet test <root>/test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj --configuration Release --no-build --no-restore --blame-hang --blame-hang-timeout 5min --logger trx;LogFileName=upstream-fixtures.trx --results-directory <root>/artifacts/results/upstream/upstream-fixtures"),
		"Enabled|dotnet run --project <root>/eng/Htmxor.UpstreamMonitor/Htmxor.UpstreamMonitor.csproj -- --json <root>/artifacts/upstream-monitor/upstream-monitor.json --markdown <root>/artifacts/upstream-monitor/upstream-monitor.md");

	private static string Command(ProcessCommand command, string root) =>
		$"{command.NetworkAccess}|{command.Display.Replace(root, "<root>", StringComparison.Ordinal).Replace('\\', '/')}";

	private static IReadOnlyList<string> ProjectWatches(JsonElement manifest, string repositoryRoot)
	{
		var projection = new List<string>();
		foreach (var watch in manifest.GetProperty("watches").EnumerateArray())
		{
			var dependencies = watch.GetProperty("dependencies")
				.EnumerateArray()
				.Select(item => item.GetString()!)
				.Order(StringComparer.Ordinal)
				.ToArray();
			Assert.All(dependencies, dependency => Assert.True(
				File.Exists(Path.Combine(repositoryRoot, dependency)),
				$"Manifest dependency '{dependency}' must exist."));
			projection.Add(string.Join('|',
				watch.GetProperty("path").GetString(),
				watch.GetProperty("match").GetString(),
				watch.GetProperty("api").GetString(),
				watch.GetProperty("relationship").GetString(),
				string.Join(',', dependencies)));
		}

		return projection.Order(StringComparer.Ordinal).ToArray();
	}

	private static WorkflowPolicy ExpectedWorkflow() => new(
		"repository_dispatch,schedule,workflow_dispatch",
		"17 * * * *",
		"aspnetcore-release-published",
		"contents=read,issues=write",
		"check --profile upstream",
		"GH_TOKEN",
		"always()",
		"artifacts/upstream-monitor/*.json,artifacts/upstream-monitor/*.md",
		14);

	private static string ValidWorkflow() =>
		"""
		name: Upstream monitor
		on:
		  schedule:
		    - cron: '17 * * * *'
		  workflow_dispatch:
		  repository_dispatch:
		    types: [aspnetcore-release-published]
		permissions:
		  contents: read
		  issues: write
		jobs:
		  monitor:
		    steps:
		      - name: Run upstream profile
		        run: dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile upstream
		        env:
		          GH_TOKEN: ${{ github.token }}
		      - name: Upload reports
		        if: always()
		        uses: actions/upload-artifact@v4
		        with:
		          path: |
		            artifacts/upstream-monitor/*.json
		            artifacts/upstream-monitor/*.md
		          retention-days: 14
		""";

	internal sealed record WorkflowPolicy(
		string Triggers,
		string? Cron,
		string DispatchTypes,
		string Permissions,
		string? MonitorCommand,
		string MonitorEnvironment,
		string? UploadCondition,
		string UploadPaths,
		int? RetentionDays);

	private sealed record UpstreamPlanObservation(
		string Preparation,
		string Tests,
		string? Monitor);
}
