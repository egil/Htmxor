using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed record ConsumerDiagnostic(string File, int Line, int Column, string Id, string Message)
{
	public static ImmutableArray<ConsumerDiagnostic> Parse(string output) => Regex.Matches(output,
		@"^(?<file>[^\r\n]+)\((?<line>\d+),(?<column>\d+)\): (?:error|warning) (?<id>\w+): (?<message>[^\r\n]+)", RegexOptions.Multiline)
		.Select(static match => new ConsumerDiagnostic(match.Groups["file"].Value.Trim(),
			int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture),
			int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture),
			match.Groups["id"].Value, match.Groups["message"].Value)).Distinct().ToImmutableArray();
}

internal sealed record PackedDiagnosticResult(int ExitCode, bool AssemblyExists, string Registration,
	ImmutableArray<ConsumerDiagnostic> Diagnostics, ImmutableDictionary<string, string> Sources)
{
	public ImmutableArray<ConsumerDiagnostic> ForCase(string name) => Diagnostics.Where(diagnostic =>
		diagnostic.File.Replace('\\', '/').Contains("/" + name + "/", StringComparison.Ordinal)).ToImmutableArray();
}

internal static class PackedDiagnosticConsumers
{
	private static readonly string[] GeneratorCases = ["lambda", "explicit-dynamic", "explicit-method-call", "explicit-computed", "explicit-implicit-get"];

	// Generator errors prevent the final compiler/analyzer pass; see PackageConsumer/DiagnosticPreservation.md.
	public static string Phase(string name) => GeneratorCases.Contains(name, StringComparer.Ordinal) ? "generator" : "compilation";

	public static async Task<PackedDiagnosticResult> BuildAsync(PackedPackageFixture package, string framework, string phase)
	{
		using var combined = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);
		combined.PrepareDiagnosticConsumer(framework);
		foreach (var item in PackedDiagnosticCases.All.Where(item => Phase(item.Name) == phase))
		{
			using var input = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);
			item.Stage(input);
			StageCase(combined.ConsumerDirectory, input.ConsumerDirectory, item);
		}
		var result = await combined.BuildForDiagnosticAsync();
		PackageConsumerEvidence.AssertPackage(combined.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(combined.ConsumerDirectory, combined.PackageVersion, framework);
		var snapshot = new PackedDiagnosticResult(result.ExitCode, File.Exists(combined.ConsumerAssemblyPath),
			combined.ReadGeneratedRouteRegistration(), ConsumerDiagnostic.Parse(result.StandardOutput + Environment.NewLine + result.StandardError),
			PackedDiagnosticCases.All.Where(item => Phase(item.Name) == phase).ToImmutableDictionary(item => item.Name,
				item => File.ReadAllText(Path.Combine(combined.ConsumerDirectory, item.Name, item.Source))));
		Retain(combined, framework, phase, result, snapshot);
		return snapshot;
	}

	private static void StageCase(string destination, string source, PackedDiagnosticCase item)
	{
		foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).Where(IsSource))
		{
			var relative = Path.GetRelativePath(source, path);
			var target = Path.Combine(destination, item.Name, relative);
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			var text = File.ReadAllText(path)
				.Replace("Htmxor.PackageConsumer", item.Namespace, StringComparison.Ordinal)
				.Replace("Htmxor.Explicit.Anywhere", item.Namespace, StringComparison.Ordinal);
			File.WriteAllText(target, text);
		}
	}

	private static bool IsSource(string path) =>
		path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal);

	private static IEnumerable<object> Mapping(PackedDiagnosticResult snapshot) => PackedDiagnosticCases.All
		.Where(item => snapshot.Sources.ContainsKey(item.Name)).Select(item => (object)new
		{
			item.Name, item.Source, item.Ids, item.Text,
			ForbiddenIds = item.ForbiddenIds.IsDefault ? [] : item.ForbiddenIds,
			ForbiddenText = item.ForbiddenText.IsDefault ? [] : item.ForbiddenText,
			ExpectedLocations = item.Locations(snapshot.Sources[item.Name]),
			Actual = snapshot.ForCase(item.Name),
		});

	private static void Retain(PackageConsumerWorkspace workspace, string framework, string phase, ProcessResult result, PackedDiagnosticResult snapshot)
	{
		var directory = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-369", workspace.PackageVersion, framework, phase);
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, "build.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		File.WriteAllText(Path.Combine(directory, "diagnostics.json"), JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
		File.WriteAllText(Path.Combine(directory, "diagnostic-mapping.json"), JsonSerializer.Serialize(Mapping(snapshot), new JsonSerializerOptions { WriteIndented = true }));
		foreach (var path in Directory.EnumerateFiles(workspace.ConsumerDirectory, "*", SearchOption.AllDirectories))
		{
			var target = Path.Combine(directory, "consumer", Path.GetRelativePath(workspace.ConsumerDirectory, path));
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			File.Copy(path, target);
		}
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	public void PrepareDiagnosticConsumer(string framework)
	{
		foreach (var path in Directory.EnumerateFiles(consumerDirectory).Where(static path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal)))
			File.Delete(path);
		File.WriteAllText(Path.Combine(consumerDirectory, "_Imports.razor"), "@using Microsoft.AspNetCore.Components\n@using Microsoft.AspNetCore.Components.Routing\n@using Microsoft.AspNetCore.Components.Web\n@using Microsoft.AspNetCore.Http\n");
		var (sdk, runtime) = framework == "net10.0" ? ("10.0.402", "10.0.13") : ("11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105");
		UseFramework(framework, sdk, runtime);
	}
}
