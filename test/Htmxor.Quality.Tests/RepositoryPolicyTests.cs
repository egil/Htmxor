using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class RepositoryPolicyTests
{
	[Fact]
	public void Code_metrics_policy_validates_the_current_solution_boundary()
	{
		RepositoryPolicyValidator.Validate(RepositoryLocator.Find());
	}

	[Fact]
	public void Tool_manifest_pins_Stryker_4_16_without_roll_forward()
	{
		var root = RepositoryLocator.Find();
		using var manifest = JsonDocument.Parse(
			File.ReadAllText(Path.Combine(root, ".config", "dotnet-tools.json")));
		var tool = manifest.RootElement.GetProperty("tools").GetProperty("dotnet-stryker");

		Assert.Equal("4.16.0", tool.GetProperty("version").GetString());
		Assert.False(tool.GetProperty("rollForward").GetBoolean());
	}

	[Fact]
	public void Mutation_config_has_no_remote_or_legacy_escape_hatches()
	{
		var root = RepositoryLocator.Find();
		var config = File.ReadAllText(Path.Combine(root, "stryker-config.json"));

		Assert.DoesNotContain("dashboard", config, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("baseline", config, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("break", config, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("mutation-level", config, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Workflow_separates_mutation_cadence_from_deployment_events()
	{
		var root = RepositoryLocator.Find();
		var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

		Assert.Contains("cron: '17 3 * * 1'", workflow, StringComparison.Ordinal);
		Assert.Contains("github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'", workflow, StringComparison.Ordinal);
		Assert.Contains("github.event_name == 'release' && github.event.action == 'published'", workflow, StringComparison.Ordinal);
		Assert.DoesNotContain("github.event_name == 'release' || (github.event_name == 'push' && github.ref == 'refs/heads/main')", workflow, StringComparison.Ordinal);
		Assert.Contains("check --profile fast", workflow, StringComparison.Ordinal);
		Assert.Contains("check --profile full", workflow, StringComparison.Ordinal);
	}
}
