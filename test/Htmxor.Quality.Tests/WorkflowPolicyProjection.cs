using YamlDotNet.RepresentationModel;

namespace Htmxor.Quality.Tests;

internal static class WorkflowPolicyProjection
{
	public static UpstreamMonitorPolicyTests.WorkflowPolicy Parse(string yaml)
	{
		var workflow = ParseRoot(yaml);
		var triggers = Mapping(Child(workflow, "on"));
		var permissions = Mapping(Child(workflow, "permissions"));
		var steps = Steps(workflow).ToArray();
		var monitor = SingleStep(steps, "run", "check --profile upstream");
		var upload = SingleStep(steps, "uses", "actions/upload-artifact@");

		return new UpstreamMonitorPolicyTests.WorkflowPolicy(
			string.Join(',', Keys(triggers).Order(StringComparer.Ordinal)),
			Cron(triggers),
			string.Join(',', Values(Child(Mapping(Child(triggers, "repository_dispatch")), "types")).Order(StringComparer.Ordinal)),
			string.Join(',', Entries(permissions).Order(StringComparer.Ordinal)),
			MonitorCommand(monitor),
			string.Join(',', Keys(Mapping(Child(monitor, "env"))).Order(StringComparer.Ordinal)),
			Scalar(Child(upload, "if")),
			string.Join(',', BlockLines(Child(Mapping(Child(upload, "with")), "path"))),
			PositiveInt(Scalar(Child(Mapping(Child(upload, "with")), "retention-days"))));
	}

	private static YamlMappingNode ParseRoot(string yaml)
	{
		var stream = new YamlStream();
		stream.Load(new StringReader(yaml));
		return Mapping(stream.Documents.Single().RootNode);
	}

	private static YamlNode? Child(YamlMappingNode parent, string key) =>
		parent.Children.FirstOrDefault(pair => Scalar(pair.Key) == key).Value;

	private static YamlMappingNode Mapping(YamlNode? node) => node as YamlMappingNode ?? new YamlMappingNode();

	private static string Scalar(YamlNode? node) => (node as YamlScalarNode)?.Value ?? string.Empty;

	private static IEnumerable<string> Keys(YamlMappingNode mapping) =>
		mapping.Children.Keys.Select(Scalar);

	private static IEnumerable<string> Entries(YamlMappingNode mapping) =>
		mapping.Children.Select(pair => $"{Scalar(pair.Key)}={Scalar(pair.Value)}");

	private static IEnumerable<string> Values(YamlNode? node) => node switch
	{
		YamlSequenceNode sequence => sequence.Children.Select(Scalar),
		YamlScalarNode scalar when !string.IsNullOrEmpty(scalar.Value) => [scalar.Value],
		_ => [],
	};

	private static string Cron(YamlMappingNode triggers) =>
		(Child(triggers, "schedule") as YamlSequenceNode)?.Children
			.OfType<YamlMappingNode>()
			.Select(item => Scalar(Child(item, "cron")))
			.SingleOrDefault() ?? string.Empty;

	private static IEnumerable<YamlMappingNode> Steps(YamlMappingNode workflow) =>
		Mapping(Child(workflow, "jobs")).Children.Values
			.OfType<YamlMappingNode>()
			.Select(job => Child(job, "steps"))
			.OfType<YamlSequenceNode>()
			.SelectMany(sequence => sequence.Children.OfType<YamlMappingNode>());

	private static YamlMappingNode SingleStep(
		IEnumerable<YamlMappingNode> steps,
		string key,
		string value) =>
		steps.Where(step => Scalar(Child(step, key)).Contains(value, StringComparison.Ordinal))
			.SingleOrDefault() ?? new YamlMappingNode();

	private static string? MonitorCommand(YamlMappingNode step)
	{
		var command = Scalar(Child(step, "run"));
		return string.IsNullOrEmpty(command) ? null : command.Split(" -- ", 2).Last();
	}

	private static IEnumerable<string> BlockLines(YamlNode? node) =>
		Scalar(node).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static int? PositiveInt(string value) =>
		int.TryParse(value, out var number) && number > 0 ? number : null;
}
