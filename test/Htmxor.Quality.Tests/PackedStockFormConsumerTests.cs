using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed partial class PackageConsumerWorkspace
{
	public void UseIssue179StockFormsScenario()
	{
		UseSelectionScenario("Issue179");
		UseFramework("net10.0", "10.0.402", "10.0.13");
		File.WriteAllText(Path.Combine(consumerDirectory, "_Imports.razor"), "@using Microsoft.AspNetCore.Components\n@using Microsoft.AspNetCore.Components.Routing\n@using Microsoft.AspNetCore.Components.Web\n");
		File.WriteAllText(projectPath, File.ReadAllText(projectPath).Replace("<Nullable>enable</Nullable>", "<Nullable>enable</Nullable><EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles><CompilerGeneratedFilesOutputPath>obj/generated</CompilerGeneratedFilesOutputPath>", StringComparison.Ordinal));
	}

	public void UseIssue179RejectedFormScenario()
	{
		UseIssue179StockFormsScenario();
		foreach (var file in Directory.EnumerateFiles(consumerDirectory).Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal)))
			File.Delete(file);
		File.WriteAllText(Path.Combine(consumerDirectory, "Issue179Rejected.razor"), """
			@attribute [Htmxor.HtmxRoute("/issue-179/rejected", Methods = new[] { "GET" })]
			@namespace Htmxor.PackageConsumer
			<form method="post" @formname="save"></form>
			""");
	}
}
