using System.Text.Json;
using System.Text.RegularExpressions;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedExplicitActionConsumerTests
{
	[Fact]
	public async Task Packaged_explicit_actions_support_every_authoring_form_and_signature()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue180ExplicitActions();
		var result = await workspace.RunAsync();
		RetainEvidence(workspace, result, "http");
		var tests = TrxTestRun.Read(workspace.TrxPath);

		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError + tests);
		Assert.Equal(new TrxTestRun(47, 47, 47, 0, 0, 0, 0), tests);
	}

	[Theory]
	[InlineData("invalid-method", "GET", "method")]
	[InlineData("normal-only", "PUT", "DisableHtmxDirectRouting")]
	[InlineData("base-owner", "PUT", "route")]
	[InlineData("stock-form", "POST", "form")]
	[InlineData("htmx-form", "POST", "form")]
	[InlineData("cross-source", "PUT", "handler")]
	[InlineData("allowlist", "PUT", "Methods")]
	[InlineData("dynamic", "PUT", "HtmxAction")]
	[InlineData("method-call", "PUT", "HtmxAction")]
	[InlineData("computed", "PUT", "HtmxAction")]
	[InlineData("implicit-get", "GET", "HtmxAction")]
	[InlineData("no-route", "PUT", "HtmxAction")]
	public async Task Packaged_final_compilation_rejects_invalid_explicit_declarations(string scenario, string method, string cause)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue180RejectedDeclaration(scenario, method);
		var result = await workspace.BuildForDiagnosticAsync();
		var evidenceDirectory = RetainEvidence(workspace, result, scenario);
		var output = result.StandardOutput + result.StandardError;
		var locations = CaptureDeclarationLocations(workspace, scenario, output, evidenceDirectory);

		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
		Assert.True(result.ExitCode != 0, "Invalid explicit action built successfully. " + output);
		Assert.Contains("HTMXOR002", output, StringComparison.Ordinal);
		Assert.NotEmpty(locations.Actual);
		Assert.All(locations.Actual, actual => Assert.Contains(locations.Expected, expected =>
			expected.File == actual.File && expected.Line == actual.Line &&
			expected.Column <= actual.Column && actual.Column <= expected.EndColumn));
		Assert.Contains(cause, output, StringComparison.OrdinalIgnoreCase);
	}

	private static (AuthoredLocation[] Expected, AuthoredLocation[] Actual) CaptureDeclarationLocations(
		PackageConsumerWorkspace workspace, string scenario, string output, string evidenceDirectory)
	{
		var sourceFile = scenario == "base-owner" ? "Issue180RejectedBase.cs" : "Issue180Rejected.razor";
		var anchors = new Dictionary<string, string[]>(StringComparer.Ordinal)
		{
			["dynamic"] = ["@onput"],
			["method-call"] = ["@onput"],
			["computed"] = ["@onput"],
			["implicit-get"] = ["@onget"],
			["no-route"] = ["@onput"],
			["cross-source"] = ["[Htmxor.HtmxAction", "@onput"],
			["stock-form"] = ["[Htmxor.HtmxAction", "<form", "@formname"],
			["htmx-form"] = ["[Htmxor.HtmxAction", "<form", "@formname"],
		}.GetValueOrDefault(scenario, ["[Htmxor.HtmxAction"]);
		var expected = File.ReadAllLines(Path.Combine(workspace.ConsumerDirectory, sourceFile))
			.SelectMany((line, index) => DeclarationLocations(sourceFile, index + 1, line, anchors)).ToArray();
		var actual = Regex.Matches(output, @"(?<file>[^\r\n]+)\((?<line>\d+),(?<column>\d+)\): error HTMXOR002:")
			.Select(static match => new AuthoredLocation(
				Path.GetFileName(match.Groups["file"].Value), int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture),
				int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture),
				int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture))).Distinct().ToArray();
		File.WriteAllText(Path.Combine(evidenceDirectory, "diagnostic-mapping.json"), JsonSerializer.Serialize(new { Expected = expected, Actual = actual }));
		return (expected, actual);
	}

	private static IEnumerable<AuthoredLocation> DeclarationLocations(string file, int lineNumber, string line, string[] anchors)
	{
		foreach (var anchor in anchors)
		{
			var start = line.IndexOf(anchor, StringComparison.Ordinal);
			if (start < 0) continue;
			var end = anchor.StartsWith("[", StringComparison.Ordinal) ? line.IndexOf(']', start) + 1 : start + anchor.Length;
			yield return new AuthoredLocation(file, lineNumber, start + 1, end);
		}
	}

	private readonly record struct AuthoredLocation(string File, int Line, int Column, int EndColumn);

	private static string RetainEvidence(PackageConsumerWorkspace workspace, ProcessResult result, string scenario)
	{
		var directory = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-180-explicit", scenario, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, "command.log"), result.StandardOutput + result.StandardError);
		File.Copy(workspace.PackagePath, Path.Combine(directory, Path.GetFileName(workspace.PackagePath)));
		foreach (var source in Directory.EnumerateFiles(workspace.ConsumerDirectory, "*", SearchOption.AllDirectories)
			.Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
		{
			var destination = Path.Combine(directory, "consumer", Path.GetRelativePath(workspace.ConsumerDirectory, source));
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(source, destination);
		}
		var deps = Path.ChangeExtension(workspace.ConsumerAssemblyPath, ".deps.json");
		File.WriteAllText(Path.Combine(directory, "boundary-presence.txt"), $"assets={File.Exists(Path.Combine(workspace.ConsumerDirectory, "obj", "project.assets.json"))} deps={File.Exists(deps)} trx={File.Exists(workspace.TrxPath)} captured={DateTimeOffset.UtcNow:O}");
		if (File.Exists(deps)) File.Copy(deps, Path.Combine(directory, Path.GetFileName(deps)));
		if (File.Exists(workspace.TrxPath)) File.Copy(workspace.TrxPath, Path.Combine(directory, "package-consumer.trx"));
		return directory;
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue180ExplicitActions()
	{
		UseSelectionScenario("Issue180");
		UseFramework("net10.0", "10.0.400", "10.0.11");
		var assets = Path.Combine(repositoryRoot, "test", "Htmxor.AspNetCore10.Tests");
		var destination = Path.Combine(consumerDirectory, "Areas", "Unusual-Folder");
		Directory.CreateDirectory(destination);
		foreach (var source in Directory.EnumerateFiles(assets, "Issue180*"))
			File.Copy(source, Path.Combine(destination, Path.GetFileName(source)));
		File.WriteAllText(projectPath, File.ReadAllText(projectPath).Replace("<Nullable>enable</Nullable>", "<Nullable>enable</Nullable><EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles><CompilerGeneratedFilesOutputPath>obj/generated</CompilerGeneratedFilesOutputPath>", StringComparison.Ordinal));
	}

	public void UseIssue180RejectedDeclaration(string scenario, string method)
	{
		UseIssue180ExplicitActions();
		Directory.Delete(Path.Combine(consumerDirectory, "Areas"), recursive: true);
		var route = new[] { "htmx-form", "allowlist" }.Contains(scenario, StringComparer.Ordinal)
			? "@attribute [Htmxor.HtmxRoute(\"/rejected\", Methods = new[] { \"GET\", \"POST\" })]"
			: "@page \"/rejected\"";
		var marker = scenario == "normal-only" ? "@attribute [Htmxor.DisableHtmxDirectRouting]" : "";
		var markup = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["stock-form"] = "<form method=\"post\" @formname=\"save\"></form>",
			["htmx-form"] = "<form method=\"post\" @formname=\"save\"></form>",
			["cross-source"] = "<button @onput=\"Other\">Save</button>",
			["dynamic"] = "<button @onput=\"@((Htmxor.HtmxEventArgs args) => Save())\">Save</button>",
			["method-call"] = "<button @onput=\"@(MakeHandler())\">Save</button>",
			["computed"] = "<button @onput=\"@(flag ? Save : Other)\">Save</button>",
			["implicit-get"] = "<button @onget=\"Save\">Load</button>",
			["no-route"] = "<button @onput=\"Save\">Save</button>",
		}.GetValueOrDefault(scenario, "");
		var attribute = $"[Htmxor.HtmxAction(\"{method}\")]";
		var members = scenario == "base-owner" ? "" : attribute + " private void Save() { } private void Other() { }";
		var inherit = scenario == "base-owner" ? "@inherits Htmxor.Explicit.Anywhere.Issue180RejectedBase" : "";
		File.WriteAllText(Path.Combine(consumerDirectory, "Issue180Rejected.razor"), $$"""
			@namespace Htmxor.Explicit.Anywhere
			{{route}}
			{{marker}}
			{{inherit}}
			{{markup}}
			@code { {{members}} }
			""");
		WriteIssue180Base(scenario, attribute);
		UseIssue180InferredRemediation(scenario);
	}

	private void UseIssue180InferredRemediation(string scenario)
	{
		if (!new[] { "method-call", "computed", "implicit-get", "no-route" }.Contains(scenario, StringComparer.Ordinal)) return;
		var path = Path.Combine(consumerDirectory, "Issue180Rejected.razor");
		var source = File.ReadAllText(path);
		var members = "private Task Save(Htmxor.HtmxEventArgs args) => Task.CompletedTask; " +
			"private Task Other(Htmxor.HtmxEventArgs args) => Task.CompletedTask; " +
			"private Func<Htmxor.HtmxEventArgs, Task> MakeHandler() => Save; private bool flag => true;";
		source = source[..source.IndexOf("@code", StringComparison.Ordinal)] + "@code { " + members + " }";
		if (scenario == "no-route") source = source.Replace("@page \"/rejected\"", "", StringComparison.Ordinal);
		File.WriteAllText(path, source);
	}

	private void WriteIssue180Base(string scenario, string attribute)
	{
		if (scenario == "base-owner") File.WriteAllText(Path.Combine(consumerDirectory, "Issue180RejectedBase.cs"), $$"""
			namespace Htmxor.Explicit.Anywhere;
			public class Issue180RejectedBase : Microsoft.AspNetCore.Components.ComponentBase
			{
				{{attribute}} protected void Save() { }
			}
			""");
	}
}
