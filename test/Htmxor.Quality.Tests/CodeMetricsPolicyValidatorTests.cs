using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class CodeMetricsPolicyValidatorTests
{
	[Theory]
	[InlineData("src/Htmxor/Htmxor.csproj", "legacy-production-baseline")]
	[InlineData("test/Htmxor.TestApp/Htmxor.TestApp.csproj", "legacy-test-app-baseline")]
	[InlineData("test/Htmxor.Tests/Htmxor.Tests.csproj", "legacy-tests-baseline")]
	[InlineData("samples/BlazingPizza/BlazingPizza.csproj", "legacy-samples-baseline")]
	[InlineData("samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj", "legacy-samples-baseline")]
	[InlineData("samples/HtmxorExamples/HtmxorExamples.csproj", "legacy-samples-baseline")]
	public void Validate_accepts_each_path_ratchet_at_its_audited_owner_path(
		string path,
		string profile)
	{
		using var repository = RepositoryPolicyFixture.Create((path, profile, true));

		ValidateWithAssignments(repository, (path, profile));
	}

	[Theory]
	[InlineData("src/NewProduct/NewProduct.csproj", "production")]
	[InlineData("test/NewProduct.Tests/NewProduct.Tests.csproj", "tests")]
	public void Validate_accepts_generic_profiles_for_new_projects(string path, string profile)
	{
		using var repository = RepositoryPolicyFixture.Create((path, profile, true));

		ValidateWithAssignments(repository);
	}

	[Theory]
	[InlineData("src/Htmxor/Htmxor.csproj", "production")]
	[InlineData("test/Htmxor.Tests/Htmxor.Tests.csproj", "tests")]
	public void Validate_accepts_a_deliberately_locked_stricter_assignment(
		string path,
		string profile)
	{
		using var repository = RepositoryPolicyFixture.Create((path, profile, true));
		var assignments = CreateAssignments((path, profile));

		CodeMetricsPolicyValidator.Validate(repository.Path, assignments);
	}

	[Theory]
	[InlineData("src/Htmxor/Htmxor.csproj", "legacy-production-baseline", "production")]
	[InlineData("test/Htmxor.Tests/Htmxor.Tests.csproj", "legacy-tests-baseline", "tests")]
	public void Validate_rejects_a_project_file_rollback_after_a_stricter_assignment_is_locked(
		string path,
		string rolledBackProfile,
		string lockedProfile)
	{
		using var repository = RepositoryPolicyFixture.Create((path, rolledBackProfile, true));
		var assignments = CreateAssignments((path, lockedProfile));

		var exception = Assert.Throws<InvalidOperationException>(
			() => CodeMetricsPolicyValidator.Validate(repository.Path, assignments));

		Assert.Contains($"centrally assigned profile '{lockedProfile}'", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_a_legacy_profile_on_a_new_project()
	{
		using var repository = RepositoryPolicyFixture.Create(
			("src/NewProduct/NewProduct.csproj", "legacy-production-baseline", true));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("must use profile 'production'", exception.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("test/NewProduct.Tests/NewProduct.Tests.csproj", "production", "tests")]
	[InlineData("src/NewProduct/NewProduct.csproj", "tests", "production")]
	public void Validate_rejects_a_generic_profile_that_does_not_match_the_project_role(
		string path,
		string profile,
		string requiredProfile)
	{
		using var repository = RepositoryPolicyFixture.Create((path, profile, true));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains($"must use profile '{requiredProfile}'", exception.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("test/Htmxor.TestApp/Htmxor.TestApp.csproj", "legacy-test-app-baseline", "tests")]
	[InlineData("samples/BlazingPizza/BlazingPizza.csproj", "legacy-samples-baseline", "production")]
	public void Validate_rejects_an_assignment_policy_that_weakens_an_audited_ratchet(
		string path,
		string projectProfile,
		string weakAssignment)
	{
		using var repository = RepositoryPolicyFixture.Create((path, projectProfile, true));
		var assignments = CreateAssignments((path, weakAssignment));

		var exception = Assert.Throws<InvalidOperationException>(
			() => CodeMetricsPolicyValidator.Validate(repository.Path, assignments));

		Assert.Contains("weakens", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_a_case_variant_path_impersonating_a_legacy_owner()
	{
		using var repository = RepositoryPolicyFixture.Create(
			("src/htmxor/Htmxor.csproj", "legacy-production-baseline", true));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("must use profile 'production'", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_a_project_without_an_explicit_profile()
	{
		using var repository = RepositoryPolicyFixture.Create(
			("src/NewProduct/NewProduct.csproj", null, true));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("exactly one CodeMetricsProfile", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_a_project_omitted_from_the_solution()
	{
		using var repository = RepositoryPolicyFixture.Create(
			("src/NewProduct/NewProduct.csproj", "production", false));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("Missing from solution", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_a_solution_project_missing_from_the_repository()
	{
		const string path = "src/NewProduct/NewProduct.csproj";
		using var repository = RepositoryPolicyFixture.Create((path, "production", true));
		File.Delete(repository.ProjectPath(path));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("Missing from repository", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_duplicate_solution_project_entries()
	{
		const string path = "src/NewProduct/NewProduct.csproj";
		using var repository = RepositoryPolicyFixture.Create(
			(path, "production", true),
			(path, "production", true));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("unique across platforms", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_a_profile_outside_a_project_property_group()
	{
		const string path = "src/NewProduct/NewProduct.csproj";
		using var repository = RepositoryPolicyFixture.Create((path, "production", true));
		repository.WriteProjectXml(
			path,
			"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><CodeMetricsProfile>production</CodeMetricsProfile></ItemGroup></Project>");

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("exactly one CodeMetricsProfile", exception.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("legacy-production-baseline", 23)]
	[InlineData("production", 11)]
	public void Validate_rejects_a_raised_complexity_ceiling(string profile, int raisedCeiling)
	{
		using var repository = RepositoryPolicyFixture.Create(
			("src/Htmxor/Htmxor.csproj", "legacy-production-baseline", true));
		File.WriteAllText(
			repository.CodeMetricsProfilePath(profile),
			$"CA1502(Method): {raisedCeiling}{Environment.NewLine}" +
			$"CA1505(Method): 20{Environment.NewLine}" +
			$"CA1505(Type): 20{Environment.NewLine}");

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository, ("src/Htmxor/Htmxor.csproj", "legacy-production-baseline")));

		Assert.Contains("must keep 'CA1502(Method)'", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_project_deletion_with_a_live_assignment()
	{
		using var repository = RepositoryPolicyFixture.Create();
		var assignments = CreateAssignments(
			("src/Htmxor/Htmxor.csproj", "legacy-production-baseline"));

		var exception = Assert.Throws<InvalidOperationException>(
			() => CodeMetricsPolicyValidator.Validate(repository.Path, assignments));

		Assert.Contains("must be explicitly retired", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_rejects_retirement_while_the_audited_project_is_still_live()
	{
		using var repository = RepositoryPolicyFixture.Create(
			("test/Htmxor.TestApp/Htmxor.TestApp.csproj", "tests", true));

		var exception = Assert.Throws<InvalidOperationException>(
			() => ValidateWithAssignments(repository));

		Assert.Contains("must be absent before retirement", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Validate_accepts_explicit_retirement_for_an_absent_owner_path()
	{
		using var repository = RepositoryPolicyFixture.Create();

		ValidateWithAssignments(repository);
	}

	[Fact]
	public void Validate_rejects_a_legacy_profile_when_a_retired_path_is_reintroduced()
	{
		const string path = "test/Htmxor.TestApp/Htmxor.TestApp.csproj";
		using var repository = RepositoryPolicyFixture.Create(
			(path, "legacy-test-app-baseline", true));
		var assignments = CreateAssignments();
		assignments[path] = AuditedOwnerAssignment.Reintroduced("legacy-test-app-baseline");

		var exception = Assert.Throws<InvalidOperationException>(
			() => CodeMetricsPolicyValidator.Validate(repository.Path, assignments));

		Assert.Contains("historical legacy allowance cannot be restored", exception.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("test/Htmxor.TestApp/Htmxor.TestApp.csproj", "tests")]
	[InlineData("samples/BlazingPizza/BlazingPizza.csproj", "production")]
	public void Validate_accepts_a_generic_profile_when_a_retired_path_is_reintroduced(
		string path,
		string profile)
	{
		using var repository = RepositoryPolicyFixture.Create((path, profile, true));
		var assignments = CreateAssignments();
		assignments[path] = AuditedOwnerAssignment.Reintroduced(profile);

		CodeMetricsPolicyValidator.Validate(repository.Path, assignments);
	}

	private static void ValidateWithAssignments(
		RepositoryPolicyFixture repository,
		params (string Project, string? Profile)[] assignments) =>
		CodeMetricsPolicyValidator.Validate(repository.Path, CreateAssignments(assignments));

	private static Dictionary<string, AuditedOwnerAssignment> CreateAssignments(
		params (string Project, string? Profile)[] changes)
	{
		var assignments = new Dictionary<string, AuditedOwnerAssignment>(StringComparer.Ordinal)
		{
			["src/Htmxor/Htmxor.csproj"] = AuditedOwnerAssignment.Retired(),
			["test/Htmxor.TestApp/Htmxor.TestApp.csproj"] = AuditedOwnerAssignment.Retired(),
			["test/Htmxor.Tests/Htmxor.Tests.csproj"] = AuditedOwnerAssignment.Retired(),
			["samples/BlazingPizza/BlazingPizza.csproj"] = AuditedOwnerAssignment.Retired(),
			["samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj"] = AuditedOwnerAssignment.Retired(),
			["samples/HtmxorExamples/HtmxorExamples.csproj"] = AuditedOwnerAssignment.Retired(),
		};
		foreach (var (project, profile) in changes)
		{
			assignments[project] = profile is null
				? AuditedOwnerAssignment.Retired()
				: AuditedOwnerAssignment.Live(profile);
		}

		return assignments;
	}
}

internal sealed class RepositoryPolicyFixture : IDisposable
{
	private RepositoryPolicyFixture(TemporaryDirectory directory)
	{
		Directory = directory;
	}

	private TemporaryDirectory Directory { get; }

	public string Path => Directory.Path;

	public string ProjectPath(string relativePath) =>
		System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

	public string CodeMetricsProfilePath(string profile) =>
		System.IO.Path.Combine(
			Path,
			"eng",
			"quality",
			"code-metrics",
			profile,
			"CodeMetricsConfig.txt");

	public static RepositoryPolicyFixture Create(
		params (string Path, string? Profile, bool IncludeInSolution)[] projects)
	{
		var fixture = new RepositoryPolicyFixture(new TemporaryDirectory());
		fixture.WriteCodeMetricsProfiles();
		foreach (var project in projects)
		{
			fixture.WriteProject(project.Path, project.Profile);
		}

		fixture.WriteSolution(projects.Where(project => project.IncludeInSolution));
		return fixture;
	}

	public static RepositoryPolicyFixture CreateCurrent(
		params (string Path, string? Profile, bool IncludeInSolution)[] additionalProjects)
	{
		var currentProjects = new[]
		{
			("src/Htmxor/Htmxor.csproj", (string?)"legacy-production-baseline", true),
			("test/Htmxor.TestApp/Htmxor.TestApp.csproj", (string?)"legacy-test-app-baseline", true),
			("test/Htmxor.Tests/Htmxor.Tests.csproj", (string?)"legacy-tests-baseline", true),
			("samples/BlazingPizza/BlazingPizza.csproj", (string?)"legacy-samples-baseline", true),
			("samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj", (string?)"legacy-samples-baseline", true),
			("samples/HtmxorExamples/HtmxorExamples.csproj", (string?)"legacy-samples-baseline", true),
		};
		return Create(currentProjects.Concat(additionalProjects).ToArray());
	}

	public void Dispose() => Directory.Dispose();

	public void WriteProjectXml(string relativePath, string xml)
	{
		var path = ProjectPath(relativePath);
		System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
		File.WriteAllText(path, xml);
	}

	private void WriteCodeMetricsProfiles()
	{
		var complexityByProfile = new Dictionary<string, int>(StringComparer.Ordinal)
		{
			["legacy-production-baseline"] = 22,
			["legacy-test-app-baseline"] = 3,
			["legacy-tests-baseline"] = 10,
			["legacy-samples-baseline"] = 7,
			["production"] = 10,
			["tests"] = 5,
		};
		foreach (var (profile, complexity) in complexityByProfile)
		{
			var path = CodeMetricsProfilePath(profile);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
			File.WriteAllText(
				path,
				$"CA1502(Method): {complexity}{Environment.NewLine}" +
				$"CA1505(Method): 20{Environment.NewLine}" +
				$"CA1505(Type): 20{Environment.NewLine}");
		}
	}

	private void WriteProject(string relativePath, string? profile)
	{
		var declaration = profile is null
			? string.Empty
			: $"<CodeMetricsProfile>{profile}</CodeMetricsProfile>";
		WriteProjectXml(
			relativePath,
			$"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{declaration}</PropertyGroup></Project>");
	}

	private void WriteSolution(
		IEnumerable<(string Path, string? Profile, bool IncludeInSolution)> projects)
	{
		var lines = projects.Select(project =>
			$"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"Fixture\", \"{project.Path}\", \"{{00000000-0000-0000-0000-000000000001}}\"{Environment.NewLine}EndProject");
		File.WriteAllText(
			System.IO.Path.Combine(Path, "Htmxor.sln"),
			string.Join(Environment.NewLine, lines));
	}
}
