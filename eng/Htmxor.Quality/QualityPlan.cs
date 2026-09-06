namespace Htmxor.Quality;

internal sealed record TestCommand(
	ProcessCommand Command,
	string Project,
	string TrxPath,
	bool RequiresCoverage);

internal sealed record QualityPlan(
	IReadOnlyList<ProcessCommand> Preparation,
	IReadOnlyList<TestCommand> Tests,
	ProcessCommand? Mutation,
	ProcessCommand? UpstreamMonitor = null);

internal static class QualityPlanFactory
{
	private const string BrowserFilter = "Category!=Browser";
	private const string LegacyBrowserFilter = "FullyQualifiedName!~Htmxor.E2E";

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
			QualityProfile.Fast => CreateTests(
				repositoryRoot,
				resultsDirectory,
				BrowserFilter,
				LegacyBrowserFilter,
				collectCoverage: false),
			QualityProfile.Full => CreateTests(
				repositoryRoot,
				resultsDirectory,
				qualityFilter: null,
				htmxorFilter: null,
				collectCoverage: true),
			QualityProfile.Mutation => CreateMutation(repositoryRoot, resultsDirectory),
			QualityProfile.Upstream => CreateUpstream(repositoryRoot, resultsDirectory),
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
		string? qualityFilter,
		string? htmxorFilter,
		bool collectCoverage)
	{
		var tests = new[]
		{
			Test(repositoryRoot, resultsDirectory, "test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj", "upstream-fixtures", null, collectCoverage: false),
			Test(repositoryRoot, resultsDirectory, "test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj", "quality", qualityFilter, collectCoverage: false),
			Test(repositoryRoot, resultsDirectory, "test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj", "aspnetcore10", null, collectCoverage: false),
			Test(repositoryRoot, resultsDirectory, "test/Htmxor.Tests/Htmxor.Tests.csproj", "htmxor", htmxorFilter, collectCoverage),
		};
		return new(CommonPreparation(repositoryRoot), tests, null);
	}

	private static QualityPlan CreateUpstream(string repositoryRoot, string resultsDirectory)
	{
		var reports = Path.Combine(repositoryRoot, "artifacts", "upstream-monitor");
		var monitor = DotNet(repositoryRoot, "run", "--project",
			Path.Combine(repositoryRoot, "eng/Htmxor.UpstreamMonitor/Htmxor.UpstreamMonitor.csproj"), "--",
			"--json", Path.Combine(reports, "upstream-monitor.json"), "--markdown", Path.Combine(reports, "upstream-monitor.md")) with
		{
			EnsureSuccess = false,
			NetworkAccess = NetworkAccess.Enabled,
		};
		return new(CommonPreparation(repositoryRoot),
			[
				Test(repositoryRoot, resultsDirectory, "test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj", "upstream-policy", "FullyQualifiedName~UpstreamMonitorPolicyTests", collectCoverage: false),
				Test(repositoryRoot, resultsDirectory, "test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj", "upstream-fixtures", null, collectCoverage: false),
			], null, monitor);
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
			new ProcessCommand("dotnet", repositoryRoot, arguments, EnsureSuccess: false, NetworkAccess: NetworkAccess.Disabled),
			project,
			trxPath,
			collectCoverage);
	}

	private static ProcessCommand DotNet(string repositoryRoot, params string[] arguments) =>
		new("dotnet", repositoryRoot, arguments, NetworkAccess: NetworkAccess.Disabled);

	private static string Solution(string repositoryRoot) =>
		Path.Combine(repositoryRoot, "Htmxor.sln");
}
