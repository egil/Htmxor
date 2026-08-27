using System.Diagnostics;

namespace Htmxor.Quality.Tests;

public sealed class CodeMetricsAnalyzerGateTests
{
	[Fact]
	public async Task Tests_profile_rejects_complexity_above_five()
	{
		var result = await CodeMetricsBuildFixture.BuildProfileAsync(
			"tests",
			CreateBranches(6));

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("error CA1502", result.Output, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Production_profile_rejects_complexity_above_ten()
	{
		var result = await CodeMetricsBuildFixture.BuildProfileAsync(
			"production",
			CreateBranches(11));

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("error CA1502", result.Output, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Strict_profiles_accept_focused_code()
	{
		const string source = "internal static class Fixture { public static int Double(int value) => value * 2; }";

		var test = await CodeMetricsBuildFixture.BuildProfileAsync("tests", source);
		var tooling = await CodeMetricsBuildFixture.BuildProfileAsync("production", source);

		Assert.True(test.ExitCode == 0, test.Output);
		Assert.True(tooling.ExitCode == 0, tooling.Output);
	}

	[Fact]
	public async Task Invalid_code_metrics_configuration_reports_CA1509()
	{
		var result = await CodeMetricsBuildFixture.BuildInvalidConfigurationAsync("CA1502: invalid");

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("error CA1509", result.Output, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Missing_profile_is_rejected_by_the_real_MSBuild_target()
	{
		var result = await CodeMetricsBuildFixture.BuildProfilesAsync([], "internal sealed class Fixture;");

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("must declare exactly one CodeMetricsProfile", result.Output, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Duplicate_profiles_are_rejected_by_the_real_MSBuild_target()
	{
		var result = await CodeMetricsBuildFixture.BuildProfilesAsync(
			["production", "tests"],
			"internal sealed class Fixture;");

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("must declare exactly one CodeMetricsProfile", result.Output, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Declared_profile_cannot_be_overridden()
	{
		var result = await CodeMetricsBuildFixture.BuildProfileWithOverrideAsync(
			"tests",
			"production",
			"internal sealed class Fixture;");

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("must use its declared CodeMetricsProfile", result.Output, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Legacy_profile_cannot_be_reused_by_a_new_project()
	{
		var result = await CodeMetricsBuildFixture.BuildProfileAsync(
			"legacy-production-baseline",
			"internal sealed class Fixture;");

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("is reserved for src/Htmxor/Htmxor.csproj", result.Output, StringComparison.Ordinal);
	}

	private static string CreateBranches(int count)
	{
		var branches = string.Join(
			Environment.NewLine,
			Enumerable.Range(1, count).Select(value => $"if (value > {value}) result++;"));
		return $$"""
			internal static class Fixture
			{
				public static int Evaluate(int value)
				{
					var result = 0;
					{{branches}}
					return result;
				}
			}
			""";
	}
}

internal static class CodeMetricsBuildFixture
{
	public static Task<BuildResult> BuildProfileAsync(string profile, string source) =>
		BuildProfilesAsync([profile], source);

	public static Task<BuildResult> BuildProfilesAsync(string[] profiles, string source) =>
		BuildAsync(
			string.Join(string.Empty, profiles.Select(profile => $"<CodeMetricsProfile>{profile}</CodeMetricsProfile>")),
			source,
			null,
			null);

	public static Task<BuildResult> BuildProfileWithOverrideAsync(
		string declaredProfile,
		string overriddenProfile,
		string source) =>
		BuildAsync(
			$"<CodeMetricsProfile>{declaredProfile}</CodeMetricsProfile>",
			source,
			null,
			overriddenProfile);

	public static Task<BuildResult> BuildInvalidConfigurationAsync(string configuration) =>
		BuildAsync(
			"<ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>",
			"internal sealed class Fixture;",
			configuration,
			null);

	private static async Task<BuildResult> BuildAsync(
		string projectProperties,
		string source,
		string? configuration,
		string? overriddenProfile)
	{
		var repositoryRoot = RepositoryLocator.Find();
		var fixtureDirectory = Path.Combine(
			repositoryRoot,
			"artifacts",
			"code-metrics-fixtures",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(fixtureDirectory);
		try
		{
			await WriteFixtureAsync(fixtureDirectory, projectProperties, source, configuration);
			return await RunBuildAsync(fixtureDirectory, overriddenProfile);
		}
		finally
		{
			Directory.Delete(fixtureDirectory, recursive: true);
		}
	}

	private static async Task WriteFixtureAsync(
		string directory,
		string projectProperties,
		string source,
		string? configuration)
	{
		var additionalFile = configuration is null
			? string.Empty
			: "<ItemGroup><AdditionalFiles Include=\"CodeMetricsConfig.txt\" /></ItemGroup>";
		var project = $$"""
			<Project Sdk="Microsoft.NET.Sdk">
				<PropertyGroup>
					<TargetFramework>net8.0</TargetFramework>
					{{projectProperties}}
				</PropertyGroup>
				{{additionalFile}}
			</Project>
			""";
		await File.WriteAllTextAsync(Path.Combine(directory, "Fixture.csproj"), project);
		await File.WriteAllTextAsync(Path.Combine(directory, "Fixture.cs"), source);
		if (configuration is not null)
		{
			await File.WriteAllTextAsync(
				Path.Combine(directory, "CodeMetricsConfig.txt"),
				configuration);
		}
	}

	private static async Task<BuildResult> RunBuildAsync(
		string fixtureDirectory,
		string? overriddenProfile)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = fixtureDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		AddBuildArguments(startInfo, overriddenProfile);
		using var process = Process.Start(startInfo)!;
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();
		return new(process.ExitCode, $"{await output}{await error}");
	}

	private static void AddBuildArguments(ProcessStartInfo startInfo, string? overriddenProfile)
	{
		startInfo.ArgumentList.Add("build");
		startInfo.ArgumentList.Add("Fixture.csproj");
		startInfo.ArgumentList.Add("--configuration");
		startInfo.ArgumentList.Add("Release");
		startInfo.ArgumentList.Add("-property:UseSharedCompilation=false");
		if (overriddenProfile is not null)
		{
			startInfo.ArgumentList.Add($"-property:CodeMetricsProfile={overriddenProfile}");
		}
	}
}

internal sealed record BuildResult(int ExitCode, string Output);
