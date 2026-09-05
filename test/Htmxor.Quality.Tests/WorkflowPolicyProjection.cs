namespace Htmxor.Quality.Tests;

internal static class WorkflowPolicyProjection
{
	public static UpstreamMonitorPolicyTests.WorkflowPolicy Parse(string yaml)
	{
		var lines = yaml.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
		var triggers = DirectKeys(lines, "on", 2);
		var permissions = DirectEntries(lines, "permissions", 2);
		var cron = DescendantValue(lines, "schedule", 2, "cron");
		var dispatchTypes = BracketValues(DescendantValue(lines, "repository_dispatch", 2, "types"));
		var monitorStep = FindStep(lines, line => Value(line).Contains("check --profile upstream", StringComparison.Ordinal));
		var uploadStep = FindStep(lines, line => Value(line).StartsWith("actions/upload-artifact@", StringComparison.Ordinal));

		return new UpstreamMonitorPolicyTests.WorkflowPolicy(
			string.Join(',', triggers.Order(StringComparer.Ordinal)),
			Unquote(cron),
			string.Join(',', dispatchTypes.Order(StringComparer.Ordinal)),
			string.Join(',', permissions.Order(StringComparer.Ordinal)),
			MonitorCommand(monitorStep),
			MonitorEnvironment(monitorStep),
			StepValue(uploadStep, "if"),
			UploadPaths(uploadStep),
			ParsePositiveInt(StepValue(uploadStep, "retention-days")));
	}

	private static IReadOnlyList<string> DirectKeys(string[] lines, string parent, int childIndent) =>
		Descendants(lines, parent, childIndent - 2)
			.Where(line => Indent(line) == childIndent && line.TrimEnd().EndsWith(':'))
			.Select(Key)
			.ToArray();

	private static IReadOnlyList<string> DirectEntries(string[] lines, string parent, int childIndent) =>
		Descendants(lines, parent, childIndent - 2)
			.Where(line => Indent(line) == childIndent && line.Contains(':', StringComparison.Ordinal))
			.Select(line => $"{Key(line)}={Value(line)}")
			.ToArray();

	private static string? DescendantValue(string[] lines, string parent, int parentIndent, string child) =>
		Descendants(lines, parent, parentIndent)
			.Where(line => Indent(line) > parentIndent && Key(line) == child)
			.Select(Value)
			.FirstOrDefault();

	private static string[] Descendants(string[] lines, string parent, int parentIndent)
	{
		var start = Array.FindIndex(lines, line => Indent(line) == parentIndent && Key(line) == parent);
		return start < 0
			? []
			: lines.Skip(start + 1)
				.TakeWhile(line => string.IsNullOrWhiteSpace(line) || Indent(line) > parentIndent)
				.ToArray();
	}

	private static (string[] Lines, int Indent)? FindStep(string[] lines, Func<string, bool> predicate)
	{
		return Steps(lines).FirstOrDefault(step => step.Lines.Any(predicate));
	}

	private static IEnumerable<(string[] Lines, int Indent)> Steps(string[] lines) =>
		lines.Select((line, index) => (line, index))
			.Where(item => item.line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
			.Select(item =>
			{
				var indent = Indent(item.line);
				return (
					lines.Skip(item.index).TakeWhile((line, offset) =>
						offset == 0 || string.IsNullOrWhiteSpace(line) || Indent(line) > indent).ToArray(),
					indent);
			});

	private static string[] BlockValues(string[] lines, string key)
	{
		var index = Array.FindIndex(lines, line => Key(line) == key);
		if (index < 0 || Value(lines[index]) != "|")
		{
			return [];
		}

		var indent = Indent(lines[index]);
		return lines.Skip(index + 1)
			.TakeWhile(line => string.IsNullOrWhiteSpace(line) || Indent(line) > indent)
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line => line.Trim())
			.ToArray();
	}

	private static IReadOnlyList<string> BracketValues(string? value) =>
		value is null || !value.StartsWith("[", StringComparison.Ordinal) || !value.EndsWith("]", StringComparison.Ordinal)
			? []
			: value[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static int? ParsePositiveInt(string? value) =>
		int.TryParse(value, out var number) && number > 0 ? number : null;

	private static string? ValueOrNull(string[] lines, string key) =>
		lines.Where(line => Key(line) == key).Select(Value).FirstOrDefault();

	private static string? MonitorCommand((string[] Lines, int Indent)? step) =>
		step is null ? null : Value(step.Value.Lines.Single(line => Key(line) == "run")).Split(" -- ", 2).Last();

	private static string MonitorEnvironment((string[] Lines, int Indent)? step) =>
		step is null
			? string.Empty
			: string.Join(',', DirectEntryKeys(step.Value.Lines, "env", step.Value.Indent + 4).Order(StringComparer.Ordinal));

	private static IReadOnlyList<string> DirectEntryKeys(string[] lines, string parent, int childIndent) =>
		Descendants(lines, parent, childIndent - 2)
			.Where(line => Indent(line) == childIndent && line.Contains(':', StringComparison.Ordinal))
			.Select(Key)
			.ToArray();

	private static string? StepValue((string[] Lines, int Indent)? step, string key) =>
		step is null ? null : ValueOrNull(step.Value.Lines, key);

	private static string UploadPaths((string[] Lines, int Indent)? step) =>
		step is null ? string.Empty : string.Join(',', BlockValues(step.Value.Lines, "path"));

	private static string Unquote(string? value) => value?.Trim('\'', '"') ?? string.Empty;

	private static int Indent(string line) => line.Length - line.TrimStart().Length;

	private static string Key(string line)
	{
		var trimmed = line.TrimStart().TrimStart('-', ' ');
		var separator = trimmed.IndexOf(':');
		return separator < 0 ? string.Empty : trimmed[..separator].Trim();
	}

	private static string Value(string line)
	{
		var trimmed = line.TrimStart().TrimStart('-', ' ');
		var separator = trimmed.IndexOf(':');
		return separator < 0 ? string.Empty : trimmed[(separator + 1)..].Trim();
	}
}
