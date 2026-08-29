using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class QualityPlanTests
{
	private readonly string repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-plan"));
	private readonly string resultsDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-results"));

	[Fact]
	public void Fast_constructs_verified_preparation_and_non_browser_test_boundaries()
	{
		var plan = Create(QualityAction.Check, QualityProfile.Fast);

		AssertCommonPreparation(plan.Preparation);
		Assert.Equal(3, plan.Tests.Count);
		AssertAspNetCore10Boundary(plan);
		var quality = Assert.Single(plan.Tests, test => test.Project == "test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj");
		Assert.Equal(
			["--filter", "Category!=Browser"],
			quality.Command.Arguments.TakeLast(2));
		var htmxor = Assert.Single(plan.Tests, test => test.Project == "test/Htmxor.Tests/Htmxor.Tests.csproj");
		Assert.Equal(
			[
				"test",
				Path.Combine(repositoryRoot, "test", "Htmxor.Tests", "Htmxor.Tests.csproj"),
				"--configuration",
				"Release",
				"--no-build",
				"--no-restore",
				"--blame-hang",
				"--blame-hang-timeout",
				"5min",
				"--logger",
				"trx;LogFileName=htmxor.trx",
				"--results-directory",
				Path.Combine(resultsDirectory, "htmxor"),
				"--filter",
				"FullyQualifiedName!~Htmxor.E2E",
			],
			htmxor.Command.Arguments);
		Assert.DoesNotContain("--collect", htmxor.Command.Arguments);
		Assert.False(htmxor.RequiresCoverage);
	}

	[Fact]
	public void Full_constructs_unfiltered_test_boundaries()
	{
		var plan = Create(QualityAction.Check, QualityProfile.Full);

		AssertCommonPreparation(plan.Preparation);
		Assert.Equal(3, plan.Tests.Count);
		AssertAspNetCore10Boundary(plan);
		var htmxor = Assert.Single(plan.Tests, test => test.Project == "test/Htmxor.Tests/Htmxor.Tests.csproj");
		Assert.Equal(
			[
				"test",
				Path.Combine(repositoryRoot, "test", "Htmxor.Tests", "Htmxor.Tests.csproj"),
				"--configuration",
				"Release",
				"--no-build",
				"--no-restore",
				"--blame-hang",
				"--blame-hang-timeout",
				"5min",
				"--logger",
				"trx;LogFileName=htmxor.trx",
				"--results-directory",
				Path.Combine(resultsDirectory, "htmxor"),
				"--collect",
				"XPlat Code Coverage",
			],
			htmxor.Command.Arguments);
		Assert.True(htmxor.RequiresCoverage);
		var quality = Assert.Single(plan.Tests, test => test.Project == "test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj");
		Assert.DoesNotContain("--collect", quality.Command.Arguments);
		Assert.DoesNotContain("--filter", quality.Command.Arguments);
		Assert.False(quality.RequiresCoverage);
	}

	[Fact]
	public void Mutation_constructs_pinned_local_tool_boundary_from_test_project()
	{
		var plan = Create(QualityAction.Check, QualityProfile.Mutation);
		var command = Assert.IsType<ProcessCommand>(plan.Mutation);

		Assert.Equal(Path.Combine(repositoryRoot, "test", "Htmxor.Tests"), command.WorkingDirectory);
		Assert.Equal("dotnet", command.FileName);
		Assert.False(command.EnsureSuccess);
		Assert.Equal(
			[
				"tool",
				"run",
				"dotnet-stryker",
				"--",
				"--config-file",
				Path.Combine(repositoryRoot, "stryker-config.json"),
				"--output",
				resultsDirectory,
				"--skip-version-check",
			],
			command.Arguments);
	}

	[Fact]
	public void Fix_constructs_analyzer_then_style_commands_without_whitespace_formatting()
	{
		var plan = Create(QualityAction.Fix, QualityProfile.Fast);
		var solution = Path.Combine(repositoryRoot, "Htmxor.sln");

		Assert.Equal(
			["format", "analyzers", solution, "--no-restore", "--severity", "error"],
			plan.Preparation[2].Arguments);
		Assert.Equal(
			["format", "style", solution, "--no-restore", "--severity", "error"],
			plan.Preparation[3].Arguments);
		Assert.All(plan.Preparation, command => Assert.DoesNotContain("whitespace", command.Arguments));
	}

	private QualityPlan Create(QualityAction action, QualityProfile profile) =>
		QualityPlanFactory.Create(repositoryRoot, resultsDirectory, new(action, profile));

	private void AssertAspNetCore10Boundary(QualityPlan plan)
	{
		var test = Assert.Single(plan.Tests, test => test.Project == "test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj");
		Assert.DoesNotContain("--collect", test.Command.Arguments);
		Assert.DoesNotContain("--filter", test.Command.Arguments);
		Assert.False(test.RequiresCoverage);
	}

	private void AssertCommonPreparation(IReadOnlyList<ProcessCommand> commands)
	{
		var solution = Path.Combine(repositoryRoot, "Htmxor.sln");
		Assert.Equal(5, commands.Count);
		Assert.Equal(["restore", solution], commands[0].Arguments);
		Assert.Equal(
			["tool", "restore", "--tool-manifest", Path.Combine(repositoryRoot, ".config", "dotnet-tools.json")],
			commands[1].Arguments);
		Assert.Equal(
			["format", "analyzers", solution, "--verify-no-changes", "--no-restore", "--severity", "error", "--verbosity", "minimal"],
			commands[2].Arguments);
		Assert.Equal(
			["format", "style", solution, "--verify-no-changes", "--no-restore", "--severity", "error", "--verbosity", "minimal"],
			commands[3].Arguments);
		Assert.Equal(
			["build", solution, "--configuration", "Release", "--no-restore"],
			commands[4].Arguments);
	}
}
