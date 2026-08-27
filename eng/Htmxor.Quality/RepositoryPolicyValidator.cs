using System.Text.Json;

namespace Htmxor.Quality;

internal static class RepositoryPolicyValidator
{
	private static readonly string[] RequiredReporters = ["progress", "json", "html", "markdown"];
	private static readonly string[] AllowedMutationProperties =
		[
			"additional-timeout",
			"concurrency",
			"configuration",
			"coverage-analysis",
			"project",
			"report-file-name",
			"reporters",
			"test-runner",
		];
	private static readonly string[] DisallowedMutationProperties =
		["baseline", "break", "dashboard", "mutate", "mutation-level", "since", "thresholds"];

	public static void Validate(string repositoryRoot)
	{
		CodeMetricsPolicyValidator.Validate(repositoryRoot);
		ValidateToolManifest(repositoryRoot);
		ValidateMutationConfig(repositoryRoot);
	}

	private static void ValidateToolManifest(string repositoryRoot)
	{
		using var document = ReadJson(Path.Combine(repositoryRoot, ".config", "dotnet-tools.json"));
		var root = document.RootElement;
		RequireNumber(root, "version", 1);
		RequireBoolean(root, "isRoot", true);
		var tools = RequireObject(root, "tools");
		var stryker = RequireObject(tools, "dotnet-stryker");
		RequireString(stryker, "version", "4.16.0");
		RequireBoolean(stryker, "rollForward", false);
		var commands = RequireStringArray(stryker, "commands");
		if (commands.Length != 1 || commands[0] != "dotnet-stryker")
		{
			throw new InvalidOperationException(
				"The local dotnet-stryker manifest entry must expose only the 'dotnet-stryker' command.");
		}
	}

	private static void ValidateMutationConfig(string repositoryRoot)
	{
		using var document = ReadJson(Path.Combine(repositoryRoot, "stryker-config.json"));
		var config = RequireObject(document.RootElement, "stryker-config");
		RequireOnlyAllowedProperties(config);
		RequireNoDisallowedProperties(config);
		RequireString(config, "project", "src/Htmxor/Htmxor.csproj");
		RequireString(config, "configuration", "Release");
		RequireString(config, "report-file-name", "mutation-report");
		RequireString(config, "test-runner", "vstest");
		RequireString(config, "coverage-analysis", "perTest");
		RequireNumber(config, "additional-timeout", 30000);
		RequireNumber(config, "concurrency", 1);
		var reporters = RequireStringArray(config, "reporters");
		if (reporters.Length != RequiredReporters.Length ||
			RequiredReporters.Any(reporter => !reporters.Contains(reporter, StringComparer.Ordinal)))
		{
			throw new InvalidOperationException(
				"Stryker reporters must be exactly progress, json, html, and markdown.");
		}
	}

	private static void RequireOnlyAllowedProperties(JsonElement config)
	{
		var properties = config.EnumerateObject()
			.Select(property => property.Name)
			.ToArray();
		var unexpected = properties
			.Except(AllowedMutationProperties, StringComparer.Ordinal)
			.Order(StringComparer.Ordinal)
			.ToArray();
		if (unexpected.Length > 0)
		{
			throw new InvalidOperationException(
				$"Stryker configuration contains unexpected properties: {string.Join(", ", unexpected)}.");
		}

		if (properties.Length != AllowedMutationProperties.Length)
		{
			throw new InvalidOperationException(
				"Stryker configuration must declare each allowed property exactly once.");
		}
	}

	private static JsonDocument ReadJson(string path)
	{
		if (!File.Exists(path))
		{
			throw new InvalidOperationException($"Required repository policy file '{path}' does not exist.");
		}

		return JsonDocument.Parse(File.ReadAllText(path));
	}

	private static JsonElement RequireObject(JsonElement parent, string name)
	{
		if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
		{
			throw new InvalidOperationException($"Repository policy property '{name}' must be an object.");
		}

		return value;
	}

	private static string[] RequireStringArray(JsonElement parent, string name)
	{
		if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException($"Repository policy property '{name}' must be an array.");
		}

		var values = value.EnumerateArray().ToArray();
		if (values.Any(item => item.ValueKind != JsonValueKind.String))
		{
			throw new InvalidOperationException($"Repository policy property '{name}' must contain only strings.");
		}

		return values.Select(item => item.GetString()!).ToArray();
	}

	private static void RequireString(JsonElement parent, string name, string expected)
	{
		if (!parent.TryGetProperty(name, out var value) ||
			value.ValueKind != JsonValueKind.String ||
			value.GetString() != expected)
		{
			throw new InvalidOperationException(
				$"Repository policy property '{name}' must be '{expected}'.");
		}
	}

	private static void RequireNumber(JsonElement parent, string name, int expected)
	{
		if (!parent.TryGetProperty(name, out var value) ||
			!value.TryGetInt32(out var actual) ||
			actual != expected)
		{
			throw new InvalidOperationException(
				$"Repository policy property '{name}' must be {expected}.");
		}
	}

	private static void RequireBoolean(JsonElement parent, string name, bool expected)
	{
		if (!parent.TryGetProperty(name, out var value) ||
			value.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
			value.GetBoolean() != expected)
		{
			throw new InvalidOperationException(
				$"Repository policy property '{name}' must be {expected.ToString().ToLowerInvariant()}.");
		}
	}

	private static void RequireNoDisallowedProperties(JsonElement element)
	{
		if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in element.EnumerateArray())
			{
				RequireNoDisallowedProperties(item);
			}

			return;
		}

		if (element.ValueKind != JsonValueKind.Object)
		{
			return;
		}

		foreach (var property in element.EnumerateObject())
		{
			if (IsDisallowed(property.Name))
			{
				throw new InvalidOperationException(
					$"Stryker property '{property.Name}' is not allowed by the full-scope mutation policy.");
			}

			RequireNoDisallowedProperties(property.Value);
		}
	}

	private static bool IsDisallowed(string name) =>
		DisallowedMutationProperties.Contains(name, StringComparer.OrdinalIgnoreCase) ||
		name.Contains("dashboard", StringComparison.OrdinalIgnoreCase) ||
		name.Contains("api-key", StringComparison.OrdinalIgnoreCase);
}
