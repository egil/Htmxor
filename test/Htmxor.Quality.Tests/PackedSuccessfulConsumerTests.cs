using Htmxor.Quality;
using System.Xml.Linq;

namespace Htmxor.Quality.Tests;

[Collection(PackedConsumerCollection.Name)]
public sealed class PackedSuccessfulConsumerTests(PackedPackageFixture package)
{
	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Package_preserves_all_successful_compiler_and_runtime_claims(string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);
		workspace.UseSuccessfulScenarios(package);
		workspace.UseFramework(framework, sdk, runtime);

		var result = await workspace.RunAsync();
		NamedSelectionEvidence.Retain(workspace.ConsumerDirectory, workspace.TrxPath, result, "issue-363-success", framework);
		var tests = TrxTestRun.Read(workspace.TrxPath);

		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.DoesNotContain("HTMXOR", workspace.BuildResult.StandardOutput + workspace.BuildResult.StandardError, StringComparison.Ordinal);
		Assert.Equal(new TrxTestRun(21, 21, 21, 0, 0, 0, 0), tests);
		AssertNestedClaims(workspace.TrxPath);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumer(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		NamedSelectionEvidence.AssertPackageTarget(workspace.ConsumerDirectory, workspace.PackageVersion, framework);
		await workspace.AssertGeneratorIsRequiredAsync();
	}
	private static void AssertNestedClaims(string trxPath)
	{
		XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
		var classes = XDocument.Load(trxPath).Descendants(ns + "TestMethod")
			.Select(method => method.Attribute("className")!.Value.Split(',')[0])
			.GroupBy(name => name)
			.Select(group => group.Key + ":" + group.Count())
			.OrderBy(name => name, StringComparer.Ordinal);

		Assert.Equal(new[]
		{
			"Htmxor.PackageConsumer.Issue168SelectionTests:1",
			"Htmxor.PackageConsumer.Issue169SelectionTests:1",
			"Htmxor.PackageConsumer.Issue209GetTests:2",
			"Htmxor.PackageConsumer.Issue287StatusCodeTests:1",
			"Htmxor.PackageConsumer.Issue291StatusCodeTests:1",
			"Htmxor.PackageConsumer.Issue293StatusCodeTests:1",
			"Htmxor.PackageConsumer.Issue299ContextTests:5",
			"Htmxor.PackageConsumer.PackageConsumerTests:5",
			"Htmxor.PackageConsumer.PackageDiscoveryTests:4",
		}, classes);
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	private string successFilter = string.Empty;

	public void UseSuccessfulScenarios(PackedPackageFixture package)
	{
		File.WriteAllText(projectPath, File.ReadAllText(projectPath).Replace("<Nullable>enable</Nullable>",
			"<Nullable>enable</Nullable><EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles><CompilerGeneratedFilesOutputPath>obj/generated</CompilerGeneratedFilesOutputPath>", StringComparison.Ordinal));
		File.AppendAllText(Path.Combine(consumerDirectory, "_Imports.razor"), "@using Microsoft.AspNetCore.Components.Web\n");
		var discovery = Path.Combine(repositoryRoot, "test", "Htmxor.Quality.Tests", "PackageConsumer", "PackageDiscoveryTests.cs.scenario");
		File.Copy(discovery, Path.Combine(consumerDirectory, "PackageDiscoveryTests.cs"));
		UseLaterPageDirectiveLikeComment();
		StageCompilerScenarios(package);
		StageRuntimeScenarios(package);
		StageComponentBindingScenarios(package);
		successFilter = string.Join('|', new[]
		{
			"PackageConsumerTests.", "PackageDiscoveryTests.", "Issue168SelectionTests.", "Issue169SelectionTests.",
			"Issue287StatusCodeTests.", "Issue291StatusCodeTests.", "Issue293StatusCodeTests.",
			"Issue299ContextTests.", "Issue209GetTests.",
		}.Select(name => "FullyQualifiedName~Htmxor.PackageConsumer." + name));
	}

	private void StageCompilerScenarios(PackedPackageFixture package)
	{
		Action<PackageConsumerWorkspace>[] scenarios =
		[
			workspace => workspace.UseIssue175NormalOnlyScenario(),
			workspace => workspace.UseIssue179StockFormsScenario(),
			workspace => workspace.UseIssue180ExplicitActions(),
			workspace => workspace.UseLayoutScenario("net10.0", "10.0.402", "10.0.13"),
			workspace => workspace.UseIssue284AuthorizationParityScenario(),
			workspace => workspace.UseIssue285AnyFolderScenario(),
			workspace => workspace.UseIssue286StockRouteTemplateScenario(),
			workspace => workspace.UseIssue306BindingAnywhereScenario(),
			workspace => workspace.UseIssue309MultiplicityScenario(),
			workspace => workspace.UseIssue316ConventionsScenario(),
			workspace => workspace.UseRequestApiScenario("net10.0", "10.0.402", "10.0.13"),
		];
		foreach (var scenario in scenarios)
		{
			StageSuccessInputs(package, scenario);
		}
	}

	private void StageRuntimeScenarios(PackedPackageFixture package)
	{
		StageSuccessInputs(package, workspace => workspace.UseIssue168SelectionScenario());
		StageSuccessInputs(package, workspace => workspace.UseIssue169SelectionScenario());
		StageSuccessInputs(package, workspace => workspace.UseIssue287StatusCodeScenario("net10.0", "10.0.402", "10.0.13"));
		StageSuccessInputs(package, workspace => workspace.UseIssue293ResponseAuthoringScenario("net10.0", "10.0.402", "10.0.13"));
		StageSuccessInputs(package, workspace => workspace.UseIssue299ContextConstructionScenario("net10.0", "10.0.402", "10.0.13"));
		StageSuccessInputs(package, workspace => workspace.UseGetScenario("net10.0", "10.0.402", "10.0.13"),
			source => source.Replace("/reports/", "/issue-209/reports/", StringComparison.Ordinal));
		var source = Path.Combine(repositoryRoot, "test", "Htmxor.Quality.Tests", "PackageConsumer", "Issue291StatusCodeTests.cs.scenario");
		File.Copy(source, Path.Combine(consumerDirectory, "Issue291StatusCodeTests.cs"));
	}

	private void StageComponentBindingScenarios(PackedPackageFixture package)
	{
		StageSuccessInputs(package, workspace => workspace.UseIssue310SupportedComponentBindingScenario(false),
			source => RenameBindingScenario(source, "Plain"));
		StageSuccessInputs(package, workspace => workspace.UseIssue310SupportedComponentBindingScenario(true),
			source => RenameBindingScenario(source, "Generic"));
		StageSuccessInputs(package, workspace => workspace.UseIssue310HandwrittenTemplateScenario(),
			source => RenameBindingScenario(source, "Handwritten"));
	}

	private static string RenameBindingScenario(string source, string shape) => source
		.Replace("Issue310", "Issue310" + shape, StringComparison.Ordinal)
		.Replace("GateChild", shape + "GateChild", StringComparison.Ordinal)
		.Replace("/issue-310/", "/issue-310-" + shape.ToLowerInvariant() + "/", StringComparison.Ordinal);

	private void StageSuccessInputs(PackedPackageFixture package, Action<PackageConsumerWorkspace> stage, Func<string, string>? rewrite = null)
	{
		// Stage each original topology in isolation so its imports, code-behind and sample inputs
		// stay intact. Only the mutually exclusive binding types and colliding GET path are renamed.
		using var source = new PackageConsumerWorkspace(repositoryRoot, package);
		stage(source);
		foreach (var path in Directory.EnumerateFiles(source.ConsumerDirectory, "*", SearchOption.AllDirectories).Where(IsConsumerSource))
		{
			CopySuccessInput(source.ConsumerDirectory, path, rewrite);
		}
	}

	private static bool IsConsumerSource(string path) => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal);

	private void CopySuccessInput(string sourceDirectory, string path, Func<string, string>? rewrite)
	{
		rewrite ??= static value => value;
		var relative = Path.GetRelativePath(sourceDirectory, path);
		if (relative == "_Imports.razor")
		{
			return;
		}

		var destination = Path.Combine(consumerDirectory, rewrite(relative));
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, rewrite(File.ReadAllText(path)));
	}
}
