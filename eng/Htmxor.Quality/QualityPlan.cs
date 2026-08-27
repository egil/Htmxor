namespace Htmxor.Quality;

internal sealed record TestCommand(
	ProcessCommand Command,
	string Project,
	string TrxPath,
	bool RequiresCoverage);

internal sealed record QualityPlan(
	IReadOnlyList<ProcessCommand> Preparation,
	IReadOnlyList<TestCommand> Tests,
	ProcessCommand? Mutation);

internal static class QualityPlanFactory
{
	private const string BrowserFilter = "FullyQualifiedName!~Htmxor.E2E";

	public static QualityPlan Create(
		string repositoryRoot,
		string resultsDirectory,
		QualityOptions options)
	{
		if (options.Action == QualityAction.Fix)
		{
			return CreateFix(repositoryRoot);
		}

		return options.Profile switch
		{
			QualityProfile.Fast => CreateTests(repositoryRoot, resultsDirectory, BrowserFilter, collectCoverage: false),
			QualityProfile.Full => CreateTests(repositoryRoot, resultsDirectory, null, collectCoverage: true),
			QualityProfile.Mutation => CreateMutation(repositoryRoot, resultsDirectory),
			_ => throw new ArgumentOutOfRangeException(nameof(options)),
		};
	}

	private static QualityPlan CreateFix(string repositoryRoot) =>
		new(
			[
				Restore(repositoryRoot),
				ToolRestore(repositoryRoot),
				DotNet(repositoryRoot, "format", "analyzers", Solution(repositoryRoot), "--no-restore", "--severity", "error"),
				DotNet(repositoryRoot, "format", "style", Solution(repositoryRoot), "--no-restore", "--severity", "error"),
			],
			[],
			null);

	private static QualityPlan CreateTests(
		string repositoryRoot,
		string resultsDirectory,
		string? filter,
		bool collectCoverage)
	{
		var tests = new[]
		{
			Test(repositoryRoot, resultsDirectory, "test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj", "quality", null, collectCoverage: false),
			Test(repositoryRoot, resultsDirectory, "test/Htmxor.Tests/Htmxor.Tests.csproj", "htmxor", filter, collectCoverage),
		};
		return new(CommonPreparation(repositoryRoot), tests, null);
	}

	private static QualityPlan CreateMutation(string repositoryRoot, string resultsDirectory)
	{
		var command = DotNet(
			Path.Combine(repositoryRoot, "test", "Htmxor.Tests"),
			"tool",
			"run",
			"dotnet-stryker",
			"--",
			"--config-file",
			Path.Combine(repositoryRoot, "stryker-config.json"),
			"--output",
			resultsDirectory,
			"--skip-version-check") with
		{
			EnsureSuccess = false,
		};
		return new([Restore(repositoryRoot), ToolRestore(repositoryRoot)], [], command);
	}

	private static IReadOnlyList<ProcessCommand> CommonPreparation(string repositoryRoot) =>
		[
			Restore(repositoryRoot),
			ToolRestore(repositoryRoot),
			DotNet(repositoryRoot, "format", "analyzers", Solution(repositoryRoot), "--verify-no-changes", "--no-restore", "--severity", "error", "--verbosity", "minimal"),
			DotNet(repositoryRoot, "format", "style", Solution(repositoryRoot), "--verify-no-changes", "--no-restore", "--severity", "error", "--verbosity", "minimal"),
			DotNet(repositoryRoot, "build", Solution(repositoryRoot), "--configuration", "Release", "--no-restore"),
		];

	private static ProcessCommand Restore(string repositoryRoot) =>
		DotNet(repositoryRoot, "restore", Solution(repositoryRoot));

	private static ProcessCommand ToolRestore(string repositoryRoot) =>
		DotNet(
			repositoryRoot,
			"tool",
			"restore",
			"--tool-manifest",
			Path.Combine(repositoryRoot, ".config", "dotnet-tools.json"));

	private static TestCommand Test(
		string repositoryRoot,
		string resultsDirectory,
		string project,
		string artifactName,
		string? filter,
		bool collectCoverage)
	{
		var projectResults = Path.Combine(resultsDirectory, artifactName);
		var trxPath = Path.Combine(projectResults, $"{artifactName}.trx");
		var arguments = new List<string>
		{
			"test",
			Path.Combine(repositoryRoot, project.Replace('/', Path.DirectorySeparatorChar)),
			"--configuration",
			"Release",
			"--no-build",
			"--no-restore",
			"--blame-hang",
			"--blame-hang-timeout",
			"5min",
			"--logger",
			$"trx;LogFileName={artifactName}.trx",
			"--results-directory",
			projectResults,
		};
		if (collectCoverage)
		{
			arguments.Add("--collect");
			arguments.Add("XPlat Code Coverage");
		}
		if (filter is not null)
		{
			arguments.Add("--filter");
			arguments.Add(filter);
		}

		return new(
			new ProcessCommand("dotnet", repositoryRoot, arguments, EnsureSuccess: false),
			project,
			trxPath,
			collectCoverage);
	}

	private static ProcessCommand DotNet(string repositoryRoot, params string[] arguments) =>
		new("dotnet", repositoryRoot, arguments);

	private static string Solution(string repositoryRoot) =>
		Path.Combine(repositoryRoot, "Htmxor.sln");
}
