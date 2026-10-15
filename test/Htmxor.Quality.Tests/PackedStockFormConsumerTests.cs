using System.Security.Cryptography;
using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedStockFormConsumerTests
{
	[Fact]
	public async Task Package_only_application_infers_stock_forms_and_preserves_framework_submission()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue179StockFormsScenario();

		var result = await workspace.RunAsync();
		var tests = TrxTestRun.Read(workspace.TrxPath);
		RetainEvidence(workspace, result, "runtime");

		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError + Environment.NewLine + tests);
		Assert.Equal(new TrxTestRun(9, 9, 9, 0, 0, 0, 0), tests);
	}

	[Fact]
	public async Task Packaged_analyzer_rejects_an_own_named_form_outside_the_explicit_allowlist()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue179RejectedFormScenario();

		var result = await workspace.BuildForDiagnosticAsync();
		RetainEvidence(workspace, result, "diagnostic");
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;

		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
		Assert.True(result.ExitCode != 0, "An own named form outside explicit Methods built successfully. " + output);
		Assert.True(output.Contains("HTMXOR033", StringComparison.Ordinal), output);
		Assert.Contains("Issue179Rejected.razor(3,", output, StringComparison.Ordinal);
		Assert.Contains("Methods", output, StringComparison.Ordinal);
		Assert.Contains("POST", output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
	}

	private static void RetainEvidence(PackageConsumerWorkspace workspace, ProcessResult result, string scenario)
	{
		var directory = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-179-stock-forms", scenario);
		ResetEvidenceDirectory(directory);
		File.WriteAllText(Path.Combine(directory, "command.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		File.WriteAllText(Path.Combine(directory, "consumer-outcome.txt"), $"exit={result.ExitCode} assembly={File.Exists(workspace.ConsumerAssemblyPath)} package={workspace.PackageVersion}");
		if (File.Exists(workspace.TrxPath)) File.Copy(workspace.TrxPath, Path.Combine(directory, "package-consumer.trx"), overwrite: true);
		foreach (var file in Directory.EnumerateFiles(workspace.ConsumerDirectory, "*.razor")) File.Copy(file, Path.Combine(directory, Path.GetFileName(file)), overwrite: true);
		File.Copy(Path.Combine(workspace.ConsumerDirectory, "Htmxor.PackageConsumer.csproj"), Path.Combine(directory, "consumer.csproj"), overwrite: true);
		RetainBoundaryFiles(workspace, directory);
		var generated = Path.Combine(workspace.ConsumerDirectory, "obj", "generated");
		if (!Directory.Exists(generated)) return;
		foreach (var file in Directory.EnumerateFiles(generated, "*.cs", SearchOption.AllDirectories)) File.Copy(file, Path.Combine(directory, Path.GetFileName(file)), overwrite: true);
	}

	private static void ResetEvidenceDirectory(string directory)
	{
		if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
		Directory.CreateDirectory(directory);
	}

	private static void RetainBoundaryFiles(PackageConsumerWorkspace workspace, string directory)
	{
		var paths = new[] { Path.Combine(workspace.ConsumerDirectory, "obj", "project.assets.json"), Path.ChangeExtension(workspace.ConsumerAssemblyPath, ".deps.json") };
		var provenance = new List<object>();
		var capturedAt = DateTimeOffset.UtcNow;
		foreach (var source in paths)
		{
			var exists = File.Exists(source);
			var hash = exists ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))) : null;
			if (exists) File.Copy(source, Path.Combine(directory, Path.GetFileName(source)), overwrite: true);
			provenance.Add(new { SourcePath = source, Exists = exists, Sha256 = hash, CapturedAt = capturedAt });
		}
		File.WriteAllText(Path.Combine(directory, "boundary-provenance.json"), JsonSerializer.Serialize(provenance, new JsonSerializerOptions { WriteIndented = true }));
	}

}

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
		foreach (var file in Directory.EnumerateFiles(consumerDirectory).Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))) File.Delete(file);
		File.WriteAllText(Path.Combine(consumerDirectory, "Issue179Rejected.razor"), """
			@attribute [Htmxor.HtmxRoute("/issue-179/rejected", Methods = new[] { "GET" })]
			@namespace Htmxor.PackageConsumer
			<form method="post" @formname="save"></form>
			""");
	}
}
