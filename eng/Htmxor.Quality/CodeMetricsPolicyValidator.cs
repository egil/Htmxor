using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Htmxor.Quality;

internal sealed record AuditedOwnerAssignment(string? Profile, bool LegacyAllowanceRetired)
{
	public static AuditedOwnerAssignment Live(string profile) => new(profile, LegacyAllowanceRetired: false);

	public static AuditedOwnerAssignment Retired() => new(Profile: null, LegacyAllowanceRetired: true);

	public static AuditedOwnerAssignment Reintroduced(string profile) => new(profile, LegacyAllowanceRetired: true);
}

internal static partial class CodeMetricsPolicyValidator
{
	private sealed record ExpectedMetrics(int Complexity, int MethodMaintainability, int TypeMaintainability);

	private static readonly IReadOnlyDictionary<string, string> AuditedBaselineProfileByOwner =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["src/Htmxor/Htmxor.csproj"] = "legacy-production-baseline",
			["test/Htmxor.TestApp/Htmxor.TestApp.csproj"] = "legacy-test-app-baseline",
			["test/Htmxor.Tests/Htmxor.Tests.csproj"] = "legacy-tests-baseline",
			["samples/BlazingPizza/BlazingPizza.csproj"] = "legacy-samples-baseline",
			["samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj"] = "legacy-samples-baseline",
			["samples/HtmxorExamples/HtmxorExamples.csproj"] = "legacy-samples-baseline",
		};
	private static readonly IReadOnlyDictionary<string, AuditedOwnerAssignment> CurrentAssignmentByAuditedOwner =
		new Dictionary<string, AuditedOwnerAssignment>(StringComparer.Ordinal)
		{
			["src/Htmxor/Htmxor.csproj"] = AuditedOwnerAssignment.Live("legacy-production-baseline"),
			["test/Htmxor.TestApp/Htmxor.TestApp.csproj"] = AuditedOwnerAssignment.Live("legacy-test-app-baseline"),
			["test/Htmxor.Tests/Htmxor.Tests.csproj"] = AuditedOwnerAssignment.Live("legacy-tests-baseline"),
			["samples/BlazingPizza/BlazingPizza.csproj"] = AuditedOwnerAssignment.Live("legacy-samples-baseline"),
			["samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj"] = AuditedOwnerAssignment.Live("legacy-samples-baseline"),
			["samples/HtmxorExamples/HtmxorExamples.csproj"] = AuditedOwnerAssignment.Live("legacy-samples-baseline"),
		};
	private static readonly IReadOnlyDictionary<string, ExpectedMetrics> ExpectedMetricsByProfile =
		new Dictionary<string, ExpectedMetrics>(StringComparer.Ordinal)
		{
			["legacy-production-baseline"] = new(22, 20, 20),
			["legacy-test-app-baseline"] = new(3, 20, 20),
			["legacy-tests-baseline"] = new(10, 20, 20),
			["legacy-samples-baseline"] = new(7, 20, 20),
			["production"] = new(10, 20, 20),
			["tests"] = new(5, 20, 20),
		};

	public static void Validate(string repositoryRoot) =>
		Validate(repositoryRoot, CurrentAssignmentByAuditedOwner);

	internal static void Validate(
		string repositoryRoot,
		IReadOnlyDictionary<string, AuditedOwnerAssignment> currentAssignmentByAuditedOwner)
	{
		ValidateProfileFiles(repositoryRoot);
		var projects = DiscoverProjects(repositoryRoot);
		var solutionProjects = ReadSolutionProjects(repositoryRoot);
		RequireUnambiguousProjects(projects, "repository");
		RequireUnambiguousProjects(solutionProjects, "Htmxor.sln");
		RequireSameProjects(projects, solutionProjects);
		ValidateCurrentAssignments(projects, currentAssignmentByAuditedOwner);
		foreach (var project in projects)
		{
			ValidateProfile(repositoryRoot, project, currentAssignmentByAuditedOwner);
		}
	}

	private static void ValidateCurrentAssignments(
		IReadOnlyCollection<string> projects,
		IReadOnlyDictionary<string, AuditedOwnerAssignment> currentAssignmentByAuditedOwner)
	{
		var missing = AuditedBaselineProfileByOwner.Keys
			.Except(currentAssignmentByAuditedOwner.Keys, StringComparer.Ordinal)
			.ToArray();
		var unexpected = currentAssignmentByAuditedOwner.Keys
			.Except(AuditedBaselineProfileByOwner.Keys, StringComparer.Ordinal)
			.ToArray();
		if (missing.Length > 0 || unexpected.Length > 0)
		{
			throw new InvalidOperationException(
				$"The current audited project assignment map must be exact. " +
				$"Missing: {List(missing)}. Unexpected: {List(unexpected)}.");
		}

		foreach (var (project, baselineProfile) in AuditedBaselineProfileByOwner)
		{
			ValidateCurrentAssignment(
				project,
				baselineProfile,
				projects.Contains(project, StringComparer.Ordinal),
				currentAssignmentByAuditedOwner[project]);
		}
	}

	private static void ValidateCurrentAssignment(
		string project,
		string baselineProfile,
		bool projectExists,
		AuditedOwnerAssignment assignment)
	{
		if (!projectExists)
		{
			if (assignment.Profile is not null)
			{
				throw new InvalidOperationException(
					$"Absent audited project '{project}' must be explicitly retired instead of retaining live " +
					$"profile assignment '{assignment.Profile}'.");
			}
			if (!assignment.LegacyAllowanceRetired)
			{
				throw new InvalidOperationException(
					$"Absent audited project '{project}' must permanently retire its legacy allowance.");
			}

			return;
		}

		if (assignment.Profile is null)
		{
			throw new InvalidOperationException(
				$"Audited project '{project}' must be absent before retirement can remove its live profile assignment.");
		}

		if (assignment.LegacyAllowanceRetired)
		{
			var requiredProfile = RequiredRoleProfile(project);
			if (!assignment.Profile.Equals(requiredProfile, StringComparison.Ordinal))
			{
				throw new InvalidOperationException(
					$"Reintroduced project '{project}' must use profile '{requiredProfile}' instead of " +
					$"'{assignment.Profile}'; its historical legacy allowance cannot be restored.");
			}

			return;
		}

		var currentProfile = assignment.Profile;
		var roleProfile = RequiredRoleProfile(project);
		if (!currentProfile.Equals(baselineProfile, StringComparison.Ordinal) &&
			!currentProfile.Equals(roleProfile, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				$"Audited project '{project}' may be assigned only '{baselineProfile}' or its role profile " +
				$"'{roleProfile}', not '{currentProfile}'.");
		}
		if (!IsAtLeastAsStrict(currentProfile, baselineProfile))
		{
			throw new InvalidOperationException(
				$"Current profile assignment '{currentProfile}' for audited project '{project}' weakens " +
				$"its original '{baselineProfile}' ratchet.");
		}
	}

	private static void ValidateProfileFiles(string repositoryRoot)
	{
		foreach (var (profile, expected) in ExpectedMetricsByProfile)
		{
			var path = Path.Combine(
				repositoryRoot,
				"eng",
				"quality",
				"code-metrics",
				profile,
				"CodeMetricsConfig.txt");
			var values = ReadProfile(path, profile);
			RequireMetric(profile, values, "CA1502(Method)", expected.Complexity);
			RequireMetric(profile, values, "CA1505(Method)", expected.MethodMaintainability);
			RequireMetric(profile, values, "CA1505(Type)", expected.TypeMaintainability);
		}
	}

	private static IReadOnlyDictionary<string, int> ReadProfile(string path, string profile)
	{
		if (!File.Exists(path))
		{
			throw new InvalidOperationException(
				$"Code-metrics profile '{profile}' is missing '{path}'.");
		}

		var values = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var rawLine in File.ReadLines(path))
		{
			var line = rawLine.Trim();
			if (line.Length == 0)
			{
				continue;
			}

			var separator = line.IndexOf(':', StringComparison.Ordinal);
			if (separator <= 0 ||
				!int.TryParse(
					line[(separator + 1)..].Trim(),
					NumberStyles.None,
					CultureInfo.InvariantCulture,
					out var value) ||
					!values.TryAdd(line[..separator].Trim(), value))
			{
				throw new InvalidOperationException(
					$"Code-metrics profile '{profile}' contains an invalid or duplicate entry '{line}'.");
			}
		}

		if (values.Count != 3)
		{
			throw new InvalidOperationException(
				$"Code-metrics profile '{profile}' must contain exactly the three repository ratchet entries.");
		}

		return values;
	}

	private static void RequireMetric(
		string profile,
		IReadOnlyDictionary<string, int> values,
		string metric,
		int expected)
	{
		if (!values.TryGetValue(metric, out var actual) || actual != expected)
		{
			var actualValue = values.TryGetValue(metric, out actual)
				? actual.ToString(CultureInfo.InvariantCulture)
				: "missing";
			throw new InvalidOperationException(
				$"Code-metrics profile '{profile}' must keep '{metric}' at {expected}; found " +
				$"{actualValue}.");
		}
	}

	private static string[] DiscoverProjects(string repositoryRoot) =>
		Directory.EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories)
			.Select(path => Normalize(Path.GetRelativePath(repositoryRoot, path)))
			.Where(path => !IsGeneratedPath(path))
			.Order(StringComparer.Ordinal)
			.ToArray();

	private static string[] ReadSolutionProjects(string repositoryRoot)
	{
		var solution = File.ReadAllText(Path.Combine(repositoryRoot, "Htmxor.sln"));
		return ProjectLine().Matches(solution)
			.Select(match => Normalize(match.Groups[1].Value))
			.Order(StringComparer.Ordinal)
			.ToArray();
	}

	private static void RequireSameProjects(
		IReadOnlyCollection<string> projects,
		IReadOnlyCollection<string> solutionProjects)
	{
		var missing = projects.Except(solutionProjects, StringComparer.Ordinal).ToArray();
		var absent = solutionProjects.Except(projects, StringComparer.Ordinal).ToArray();
		if (missing.Length > 0 || absent.Length > 0)
		{
			throw new InvalidOperationException(
				$"Htmxor.sln project boundary differs from the repository. " +
				$"Missing from solution: {List(missing)}. Missing from repository: {List(absent)}.");
		}
	}

	private static void ValidateProfile(
		string repositoryRoot,
		string project,
		IReadOnlyDictionary<string, AuditedOwnerAssignment> currentAssignmentByAuditedOwner)
	{
		var document = XDocument.Load(Path.Combine(repositoryRoot, project));
		var profiles = document.Root?.Elements()
			.Where(element => element.Name.LocalName == "PropertyGroup")
			.SelectMany(group => group.Elements())
			.Where(element => element.Name.LocalName == "CodeMetricsProfile")
			.Select(element => element.Value)
			.ToArray() ?? [];
		if (profiles.Length != 1)
		{
			throw new InvalidOperationException(
				$"Project '{project}' must declare exactly one CodeMetricsProfile.");
		}

		RequireAllowedProfile(project, profiles[0], currentAssignmentByAuditedOwner);
	}

	private static void RequireUnambiguousProjects(
		IReadOnlyCollection<string> projects,
		string source)
	{
		var collisions = projects
			.GroupBy(project => project, StringComparer.OrdinalIgnoreCase)
			.Where(group => group.Count() > 1)
			.SelectMany(group => group)
			.Order(StringComparer.Ordinal)
			.ToArray();
		if (collisions.Length > 0)
		{
			throw new InvalidOperationException(
				$"Project paths in {source} must be unique across platforms: {List(collisions)}.");
		}
	}

	private static void RequireAllowedProfile(
		string project,
		string profile,
		IReadOnlyDictionary<string, AuditedOwnerAssignment> currentAssignmentByAuditedOwner)
	{
		if (AuditedBaselineProfileByOwner.ContainsKey(project))
		{
			var assignedProfile = currentAssignmentByAuditedOwner[project].Profile;
			if (assignedProfile is not null && profile.Equals(assignedProfile, StringComparison.Ordinal))
			{
				return;
			}

			throw new InvalidOperationException(
				$"Audited project '{project}' must use its centrally assigned profile '{assignedProfile ?? "retired"}' " +
				$"instead of '{profile}'.");
		}

		RequireRoleProfile(project, profile);
	}

	private static void RequireRoleProfile(string project, string profile)
	{
		var requiredProfile = RequiredRoleProfile(project);
		if (profile.Equals(requiredProfile, StringComparison.Ordinal))
		{
			return;
		}

		throw new InvalidOperationException(
			$"Project '{project}' must use profile '{requiredProfile}' instead of '{profile}'. " +
			"Legacy profiles are reserved for live audited project assignments.");
	}

	private static string RequiredRoleProfile(string project) =>
		project.StartsWith("test/", StringComparison.Ordinal) ? "tests" : "production";

	private static bool IsAtLeastAsStrict(string candidateProfile, string baselineProfile)
	{
		var candidate = ExpectedMetricsByProfile[candidateProfile];
		var baseline = ExpectedMetricsByProfile[baselineProfile];
		return candidate.Complexity <= baseline.Complexity &&
			candidate.MethodMaintainability >= baseline.MethodMaintainability &&
			candidate.TypeMaintainability >= baseline.TypeMaintainability;
	}

	private static bool IsGeneratedPath(string path) =>
		path.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase) ||
		path.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
		path.Contains("/obj/", StringComparison.OrdinalIgnoreCase);

	private static string Normalize(string path) => path.Replace('\\', '/');

	private static string List(IReadOnlyCollection<string> values) =>
		values.Count == 0 ? "none" : string.Join(", ", values);

	[GeneratedRegex(
		"Project\\(\"[^\"]+\"\\) = \"[^\"]+\", \"([^\"]+\\.csproj)\",",
		RegexOptions.IgnoreCase)]
	private static partial Regex ProjectLine();
}
