namespace Htmxor.Quality.Tests;

/// <summary>
/// The minimal `.config/dotnet-tools.json` and `stryker-config.json` policy files
/// `RepositoryPolicyValidator` requires before `QualityCommand.ExecuteAsync` dispatches any
/// process. Shared by every fixture that drives whole profiles through fakes, so a policy change
/// is made once rather than in every fixture file that needs a passing repository.
/// </summary>
internal static class QualityPolicyFiles
{
	public const string ValidManifest =
		"""
		{"version":1,"isRoot":true,"tools":{"dotnet-stryker":{"version":"4.16.0","commands":["dotnet-stryker"],"rollForward":false}}}
		""";

	public const string ValidMutationConfig =
		"""
		{"stryker-config":{"project":"src/Htmxor/Htmxor.csproj","configuration":"Release","reporters":["progress","json","html","markdown"],"report-file-name":"mutation-report","test-runner":"vstest","coverage-analysis":"perTest","additional-timeout":30000,"concurrency":1}}
		""";

	public static void WriteValid(string repositoryRoot)
	{
		var directory = Path.Combine(repositoryRoot, ".config");
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, "dotnet-tools.json"), ValidManifest);
		File.WriteAllText(Path.Combine(repositoryRoot, "stryker-config.json"), ValidMutationConfig);
	}
}

/// <summary>
/// Shared `ProcessCommand.Arguments` lookup for fakes that read a flag's value, so every fake
/// parses `--output`, `--results-directory`, and `--logger` the same way.
/// </summary>
internal static class ProcessCommandArguments
{
	public static string After(IReadOnlyList<string> arguments, string flag)
	{
		var values = arguments.ToArray();
		return values[Array.IndexOf(values, flag) + 1];
	}
}
