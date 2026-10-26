using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue180ExplicitActions()
	{
		UseSelectionScenario("Issue180");
		UseFramework("net10.0", "10.0.402", "10.0.13");
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
		if (!new[] { "method-call", "computed", "implicit-get", "no-route" }.Contains(scenario, StringComparer.Ordinal))
			return;
		var path = Path.Combine(consumerDirectory, "Issue180Rejected.razor");
		var source = File.ReadAllText(path);
		var members = "private Task Save(Htmxor.HtmxEventArgs args) => Task.CompletedTask; " +
			"private Task Other(Htmxor.HtmxEventArgs args) => Task.CompletedTask; " +
			"private Func<Htmxor.HtmxEventArgs, Task> MakeHandler() => Save; private bool flag => true;";
		source = source[..source.IndexOf("@code", StringComparison.Ordinal)] + "@code { " + members + " }";
		if (scenario == "no-route")
			source = source.Replace("@page \"/rejected\"", "", StringComparison.Ordinal);
		File.WriteAllText(path, source);
	}

	private void WriteIssue180Base(string scenario, string attribute)
	{
		if (scenario == "base-owner")
			File.WriteAllText(Path.Combine(consumerDirectory, "Issue180RejectedBase.cs"), $$"""
			namespace Htmxor.Explicit.Anywhere;
			public class Issue180RejectedBase : Microsoft.AspNetCore.Components.ComponentBase
			{
				{{attribute}} protected void Save() { }
			}
			""");
	}
}
